extern alias CoreDrawing;

using ZeroLevel.ML.DNN.Detectors;
using ZeroLevel.ML.DNN.Models;
using System.Collections.Concurrent;
using System.Threading;
using System;
using System.IO;
using System.Linq;

namespace ZeroLevel.ML.DNN
{
    public sealed class ObjectDetectionOptions
    {
        public string DetectionModelPath { get; set; } = null!;
        public int ChunksCount { get; set; } = 4;
        public int PredictionThreadsCount { get; set; } = 3;
        public int PreprocessThreadsCount { get; set; } = 3;
        public int PostprocessThreadsCount { get; set; } = 3;
        public int CropSize { get; set; } = 960;
        public float ImageScaleValue { get; set; } = 0.8f;
        public int TensorBufferSize { get; set; } = 24;
        public float WithoutClassifyDetectionTreshold { get; set; } = 0.5f;
        public string AfterProcessingTargetFolder { get; set; } = null!;
        public int SizeMultiplicity { get; set; } = 1;
    }

    public static class ObjectDetectionFolderAnalizer
    {
        private sealed class LocalThreadsSet
            : IDisposable
        {
            private readonly CountdownEvent _threadsCountdown;
            private readonly Thread[] _threads;
            private class WrapperContext
            {
                public Action<object> ThreadBody = null!;
                public object Obj = null!;
            }
            public LocalThreadsSet(int threadsCount)
            {
                _threadsCountdown = new CountdownEvent(threadsCount);
                _threads = new Thread[threadsCount];
                for (int i = 0; i < threadsCount; i++)
                {
                    _threads[i] = new Thread(ThreadBodyWrapper!);
                }
            }

            public void Run(Action<object> threadBody, object state)
            {
                var context = new WrapperContext { ThreadBody = threadBody, Obj = state };
                for (int i = 0; i < _threads.Length; i++)
                {
                    _threads[i].Start(context);
                }
            }

            private void ThreadBodyWrapper(object ctx)
            {
                try
                {
                    var context = (WrapperContext)ctx;
                    context.ThreadBody.Invoke(context.Obj);
                }
                catch (Exception ex)
                {

                }
                _threadsCountdown.Signal();
            }

            public void Wait()
            {
                _threadsCountdown.Wait();
            }

            public void Dispose()
            {
                _threadsCountdown.Dispose();
            }
        }

        private sealed class ObjectDetectionContext
            : IDisposable
        {
            private readonly ConcurrentQueue<string> _files;
            private readonly BlockingCollection<FastTensorPool> _preprocessedItems = new BlockingCollection<FastTensorPool>();
            private readonly BlockingCollection<ImagePrediction> _proceedItems = new BlockingCollection<ImagePrediction>();

            private readonly IDetector _detector;
            private readonly ObjectDetectionOptions _options;
            private readonly Action<ImagePrediction> _postprocessAction;

            private int _handled = 0;
            private readonly Action<int, int, int, int> _progressHandler;
            public ObjectDetectionContext(IDetector detector, string folder, ObjectDetectionOptions options, Action<ImagePrediction> imagePredictionHandler, Action<int, int, int, int> progressHandler)
            {
                _detector = detector;
                _options = options;
                _progressHandler = progressHandler;
                _postprocessAction = imagePredictionHandler;
                var files = Directory.GetFiles(folder)?.Where(f => KnownImageFormats.IsKnownFormat(f)) ?? Enumerable.Empty<string>();
                _files = new ConcurrentQueue<string>(files);
            }

            private readonly object _lockProgressUpdate = new object();
            private void UpdateProgress()
            {
                lock (_lockProgressUpdate)
                {
                    _progressHandler.Invoke(_files.Count, _preprocessedItems.Count, _proceedItems.Count, _handled);
                }
            }

            public string TakeFile()
            {
                if (_files.Count > 0)
                {
                    if (_files.TryDequeue(out var r))
                    {
                        return r;
                    }
                }
                return null!;
            }

            public void CreateInput(string file)
            {
                var input = _detector.CreateInput(file);
                while (_preprocessedItems.Count > _options.TensorBufferSize)
                {
                    Thread.Sleep(200);
                }
                _preprocessedItems.Add(input);
                UpdateProgress();
            }

            public void NoMoreInputs()
            {
                _preprocessedItems.CompleteAdding();
            }

            public FastTensorPool GetInput()
            {
                while (_preprocessedItems.IsCompleted == false)
                {
                    if (_preprocessedItems.TryTake(out var tensor, 200))
                    {
                        return tensor;
                    }
                }
                return null!;
            }

            public void Predict(FastTensorPool tensor)
            {
                var predictions = _detector.Detect(tensor)
                    ?.Select(p => new YoloPredictionWithGeo(p));

                _proceedItems.Add(new ImagePrediction
                {
                    Width = tensor.SourceWidth,
                    Height = tensor.SourceHeight,
                    Name = tensor.Name,
                    Path = tensor.Path,
                    Predictions = predictions?.ToArray()!
                });

                tensor.Dispose();

                UpdateProgress();

                GC.Collect(2);
                GC.WaitForPendingFinalizers();
            }

            public void NoMorePredictions()
            {
                _proceedItems.CompleteAdding();
            }

            public ImagePrediction GetPrediction()
            {
                while (_proceedItems.IsCompleted == false)
                {
                    if (_proceedItems.TryTake(out var prediction, 200))
                    {
                        return prediction;
                    }
                }
                return null!;
            }

            public void HandlePrediction(ImagePrediction prediction)
            {
                _postprocessAction.Invoke(prediction);
                Interlocked.Increment(ref _handled);
                UpdateProgress();
            }

            public void Dispose()
            {
                _preprocessedItems.Dispose();
                _proceedItems.Dispose();
            }
        }

        public static void Analize(IDetector detector, string folder, ObjectDetectionOptions options, Action<ImagePrediction> imagePredictionHandler, Action<int, int, int, int> progressHandler)
        {
            using (var context = new ObjectDetectionContext(detector, folder, options, imagePredictionHandler, progressHandler))
            {
                var preprocessThreadsSet = new LocalThreadsSet(options.PreprocessThreadsCount);
                var processThreadSet = new LocalThreadsSet(options.PredictionThreadsCount);
                var postprocessThreadSet = new LocalThreadsSet(options.PostprocessThreadsCount);

                preprocessThreadsSet.Run(PreprocessThreadBody, context);
                processThreadSet.Run(ProcessThreadBody, context);
                postprocessThreadSet.Run(PostprocessThreadBody, context);

                preprocessThreadsSet.Wait();
                preprocessThreadsSet.Dispose();

                context.NoMoreInputs();

                processThreadSet.Wait();
                processThreadSet.Dispose();

                context.NoMorePredictions();

                postprocessThreadSet.Wait();
                postprocessThreadSet.Dispose();
            }
        }

        private static void PreprocessThreadBody(object ctx)
        {
            string file;
            var context = (ObjectDetectionContext)ctx;
            while ((file = context.TakeFile()) != null)
            {
                context.CreateInput(file);
            }
        }

        private static void ProcessThreadBody(object ctx)
        {
            var context = (ObjectDetectionContext)ctx;
            FastTensorPool input;
            while ((input = context.GetInput()) != null)
            {
                context.Predict(input);
            }
        }

        private static void PostprocessThreadBody(object ctx)
        {
            var context = (ObjectDetectionContext)ctx;
            ImagePrediction prediction;
            while ((prediction = context.GetPrediction()) != null)
            {
                context.HandlePrediction(prediction);
            }
        }
    }
}
