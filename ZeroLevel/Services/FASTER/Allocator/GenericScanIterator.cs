﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Diagnostics;

namespace FASTER.core
{
    /// <summary>
    /// Scan iterator for hybrid log
    /// </summary>
    public class GenericScanIterator<Key, Value> : IFasterScanIterator<Key, Value>
        where Key : new()
        where Value : new()
    {
        private readonly int frameSize;
        private readonly GenericAllocator<Key, Value> hlog;
        private readonly long beginAddress, endAddress;
        private readonly GenericFrame<Key, Value> frame;
        private readonly CountdownEvent[] loaded;
        private readonly int recordSize;

        private bool first = true;
        private long currentAddress, nextAddress;
        private Key currentKey;
        private Value currentValue;

        /// <summary>
        /// Current address
        /// </summary>
        public long CurrentAddress => currentAddress;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="hlog"></param>
        /// <param name="beginAddress"></param>
        /// <param name="endAddress"></param>
        /// <param name="scanBufferingMode"></param>
        public unsafe GenericScanIterator(GenericAllocator<Key, Value> hlog, long beginAddress, long endAddress, ScanBufferingMode scanBufferingMode)
        {
            this.hlog = hlog;

            if (beginAddress == 0)
                beginAddress = hlog.GetFirstValidLogicalAddress(0);

            this.beginAddress = beginAddress;
            this.endAddress = endAddress;

            recordSize = hlog.GetRecordSize(0);
            currentAddress = -1;
            nextAddress = beginAddress;

            if (scanBufferingMode == ScanBufferingMode.SinglePageBuffering)
                frameSize = 1;
            else if (scanBufferingMode == ScanBufferingMode.DoublePageBuffering)
                frameSize = 2;
            else if (scanBufferingMode == ScanBufferingMode.NoBuffering)
            {
                frameSize = 0;
                return;
            }

            frame = new GenericFrame<Key, Value>(frameSize, hlog.PageSize);
            loaded = new CountdownEvent[frameSize];

            // Only load addresses flushed to disk
            if (nextAddress < hlog.HeadAddress)
            {
                var frameNumber = (nextAddress >> hlog.LogPageSizeBits) % frameSize;
                hlog.AsyncReadPagesFromDeviceToFrame
                    (nextAddress >> hlog.LogPageSizeBits,
                    1, endAddress, AsyncReadPagesCallback, Empty.Default,
                    frame, out loaded[frameNumber]);
            }
        }

        /// <summary>
        /// Gets reference to current key
        /// </summary>
        /// <returns></returns>
        public ref Key GetKey()
        {
            return ref currentKey;
        }

        /// <summary>
        /// Gets reference to current value
        /// </summary>
        /// <returns></returns>
        public ref Value GetValue()
        {
            return ref currentValue;
        }

        /// <summary>
        /// Get next record in iterator
        /// </summary>
        /// <param name="recordInfo"></param>
        /// <returns></returns>
        public bool GetNext(out RecordInfo recordInfo)
        {
            recordInfo = default(RecordInfo);
            currentKey = default(Key);
            currentValue = default(Value);

            currentAddress = nextAddress;
            while (true)
            {
                // Check for boundary conditions
                if (currentAddress >= endAddress)
                {
                    return false;
                }

                if (currentAddress < hlog.BeginAddress)
                {
                    throw new Exception("Iterator address is less than log BeginAddress " + hlog.BeginAddress);
                }

                if (frameSize == 0 && currentAddress < hlog.HeadAddress)
                {
                    throw new Exception("Iterator address is less than log HeadAddress in memory-scan mode");
                }

                var currentPage = currentAddress >> hlog.LogPageSizeBits;

                var offset = (currentAddress & hlog.PageSizeMask) / recordSize;

                if (currentAddress < hlog.HeadAddress)
                    BufferAndLoad(currentAddress, currentPage, currentPage % frameSize);

                // Check if record fits on page, if not skip to next page
                if ((currentAddress & hlog.PageSizeMask) + recordSize > hlog.PageSize)
                {
                    currentAddress = (1 + (currentAddress >> hlog.LogPageSizeBits)) << hlog.LogPageSizeBits;
                    continue;
                }

                if (currentAddress >= hlog.HeadAddress)
                {
                    // Read record from cached page memory
                    nextAddress = currentAddress + recordSize;

                    var page = currentPage % hlog.BufferSize;

                    if (hlog.values[page][offset].info.Invalid)
                        continue;

                    recordInfo = hlog.values[page][offset].info;
                    currentKey = hlog.values[page][offset].key;
                    currentValue = hlog.values[page][offset].value;
                    return true;
                }

                nextAddress = currentAddress + recordSize;

                var currentFrame = currentPage % frameSize;

                if (frame.GetInfo(currentFrame, offset).Invalid)
                    continue;

                recordInfo = frame.GetInfo(currentFrame, offset);
                currentKey = frame.GetKey(currentFrame, offset);
                currentValue = frame.GetValue(currentFrame, offset);
                return true;
            }
        }

        /// <summary>
        /// Get next record using iterator
        /// </summary>
        /// <param name="recordInfo"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool GetNext(out RecordInfo recordInfo, out Key key, out Value value)
        {
            key = default(Key);
            value = default(Value);

            if (GetNext(out recordInfo))
            {
                key = currentKey;
                value = currentValue;
                return true;
            }

            return false;
        }

        private unsafe void BufferAndLoad(long currentAddress, long currentPage, long currentFrame)
        {
            if (first || (currentAddress & hlog.PageSizeMask) == 0)
            {
                // Prefetch pages based on buffering mode
                if (frameSize == 1)
                {
                    if (!first)
                    {
                        hlog.AsyncReadPagesFromDeviceToFrame(currentAddress >> hlog.LogPageSizeBits, 1, endAddress, AsyncReadPagesCallback, Empty.Default, frame, out loaded[currentFrame]);
                    }
                }
                else
                {
                    var endPage = endAddress >> hlog.LogPageSizeBits;
                    if ((endPage > currentPage) &&
                        ((endPage > currentPage + 1) || ((endAddress & hlog.PageSizeMask) != 0)))
                    {
                        hlog.AsyncReadPagesFromDeviceToFrame(1 + (currentAddress >> hlog.LogPageSizeBits), 1, endAddress, AsyncReadPagesCallback, Empty.Default, frame, out loaded[(currentPage + 1) % frameSize]);
                    }
                }
                first = false;
            }
            loaded[currentFrame].Wait();
        }

        /// <summary>
        /// Dispose iterator
        /// </summary>
        public void Dispose()
        {
            if (loaded != null)
                for (int i = 0; i < frameSize; i++)
                    loaded[i]?.Wait();

            frame?.Dispose();
        }

        private unsafe void AsyncReadPagesCallback(uint errorCode, uint numBytes, NativeOverlapped* overlap)
        {
            if (errorCode != 0)
            {
                Trace.TraceError("OverlappedStream GetQueuedCompletionStatus error: {0}", errorCode);
            }

            var result = (PageAsyncReadResult<Empty>)Overlapped.Unpack(overlap).AsyncResult;

            if (result.freeBuffer1 != null)
            {
                hlog.PopulatePage(result.freeBuffer1.GetValidPointer(), result.freeBuffer1.required_bytes, ref frame.GetPage(result.page % frame.frameSize));
                result.freeBuffer1.Return();
            }

            if (result.handle != null)
            {
                result.handle.Signal();
            }

            Interlocked.MemoryBarrier();
            Overlapped.Free(overlap);
        }
    }
}
