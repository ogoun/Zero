/*
 
PORTS FROM https://github.com/hollance/BlazeFace-PyTorch/blob/master/Anchors.ipynb
 
 */

namespace Zero.NN.Services
{
    public class Anchor
    {
        public float cx;
        public float cy;
        public float w;
        public float h;
    }

    // Options to generate anchors for SSD object detection models.
    public class AnchorOptions
    {
        // Number of output feature maps to generate the anchors on.
        public int num_layers;

        // Min and max scales for generating anchor boxes on feature maps.
        public float min_scale;
        public float max_scale;

        // Size of input images.
        public int input_size_height;
        public int input_size_width;

        // The offset for the center of anchors. The value is in the scale of stride.
        // E.g. 0.5 meaning 0.5 * |current_stride| in pixels.
        public float anchor_offset_x = 0.5f;
        public float anchor_offset_y = 0.5f;

        // Strides of each output feature maps
        public int[] strides;

        // List of different aspect ratio to generate anchors
        public float[] aspect_ratios;

        // A boolean to indicate whether the fixed 3 boxes per location is used in the lowest layer.
        public bool reduce_boxes_in_lowest_layer = false;

        // An additional anchor is added with this aspect ratio and a scale
        // interpolated between the scale for a layer and the scale for the next layer
        // (1.0 for the last layer). This anchor is not included if this value is 0.
        public float interpolated_scale_aspect_ratio = 1.0f;

        // Whether use fixed width and height (e.g. both 1.0f) for each anchor.
        // This option can be used when the predicted anchor width and height are in
        // pixels.
        public bool fixed_anchor_size = false;

        #region PRESETS
        public static AnchorOptions FaceDetectionBackMobileGpuOptions => new AnchorOptions
        {
            num_layers = 4,
            min_scale = 0.15625f,
            max_scale = 0.75f,
            input_size_height = 256,
            input_size_width = 256,
            anchor_offset_x = 0.5f,
            anchor_offset_y = 0.5f,
            strides = new[] { 16, 32, 32, 32 },
            aspect_ratios = new[] { 1.0f },
            reduce_boxes_in_lowest_layer = false,
            interpolated_scale_aspect_ratio = 1.0f,
            fixed_anchor_size = true
        };

        public static AnchorOptions FaceDetectionMobileGpuOptions => new AnchorOptions
        {
            num_layers = 4,
            min_scale = 0.1484375f,
            max_scale = 0.75f,
            input_size_height = 128,
            input_size_width = 128,
            anchor_offset_x = 0.5f,
            anchor_offset_y = 0.5f,
            strides = new[] { 8, 16, 16, 16 },
            aspect_ratios = new[] { 1.0f },
            reduce_boxes_in_lowest_layer = false,
            interpolated_scale_aspect_ratio = 1.0f,
            fixed_anchor_size = true
        };

        public static AnchorOptions MobileSSDOptions => new AnchorOptions
        {
            num_layers = 6,
            min_scale = 0.2f,
            max_scale = 0.95f,
            input_size_height = 300,
            input_size_width = 300,
            anchor_offset_x = 0.5f,
            anchor_offset_y = 0.5f,
            strides = new[] { 16, 32, 64, 128, 256, 512 },
            aspect_ratios = new[] { 1.0f, 2.0f, 0.5f, 3.0f, 0.3333f },
            reduce_boxes_in_lowest_layer = true,
            interpolated_scale_aspect_ratio = 1.0f,
            fixed_anchor_size = false
        };
        #endregion
    }

    internal class AnchorsGenerator
    {
        private static float calculate_scale(float min_scale, float max_scale, float stride_index, float num_strides)
        {
            return (float)(min_scale + (max_scale - min_scale) * stride_index / (num_strides - 1.0f));
        }

        private readonly AnchorOptions _options;
        private readonly List<Anchor> anchors = new List<Anchor>();

        public IList<Anchor> Anchors => anchors;

        public AnchorsGenerator(AnchorOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (options.strides == null)
            {
                throw new ArgumentNullException(nameof(options.strides));
            }
            _options = options;
            Generate();
        }

        private void Generate()
        {
            var strides_size = _options.strides?.Length ?? 0;
            if (_options.num_layers != strides_size)
            {
                throw new ArgumentException($"Expected {_options.num_layers} strides (as num_layer), got {strides_size} strides");
            }
            var layer_id = 0;
            while (layer_id < strides_size)
            {
                var anchor_height = new List<float>();
                var anchor_width = new List<float>();
                var aspect_ratios = new List<float>();
                var scales = new List<float>();

                // For same strides, we merge the anchors in the same order.
                var last_same_stride_layer = layer_id;
                while ((last_same_stride_layer < strides_size) && (_options.strides[last_same_stride_layer] == _options.strides[layer_id]))
                {
                    var scale = calculate_scale(_options.min_scale, _options.max_scale, last_same_stride_layer, strides_size);

                    if (last_same_stride_layer == 0 && _options.reduce_boxes_in_lowest_layer)
                    {
                        //  For first layer, it can be specified to use predefined anchors.
                        aspect_ratios.Add(1.0f);
                        aspect_ratios.Add(2.0f);
                        aspect_ratios.Add(0.5f);
                        scales.Add(0.1f);
                        scales.Add(scale);
                        scales.Add(scale);
                    }
                    else
                    {
                        foreach (var aspect_ratio in _options.aspect_ratios)
                        {
                            aspect_ratios.Add(aspect_ratio);
                            scales.Add(scale);
                        }
                        if (_options.interpolated_scale_aspect_ratio > 0.0f)
                        {
                            var scale_next = (last_same_stride_layer == (strides_size - 1))
                                ? 1.0
                                : calculate_scale(_options.min_scale, _options.max_scale, last_same_stride_layer + 1, strides_size);
                            scales.Add((float)Math.Sqrt(scale * scale_next));
                            aspect_ratios.Add(_options.interpolated_scale_aspect_ratio);
                        }
                    }
                    last_same_stride_layer += 1;
                }

                for (var i = 0; i < aspect_ratios.Count; i++)
                {
                    var ratio_sqrts = (float)Math.Sqrt(aspect_ratios[i]);
                    anchor_height.Add(scales[i] / ratio_sqrts);
                    anchor_width.Add(scales[i] * ratio_sqrts);
                }

                var stride = _options.strides[layer_id];
                var feature_map_height = (int)(Math.Ceiling((float)_options.input_size_height / stride));
                var feature_map_width = (int)(Math.Ceiling((float)_options.input_size_width / stride));

                for (var y = 0; y < feature_map_height; y++)
                {
                    for (var x = 0; x < feature_map_width; x++)
                    {
                        for (var anchor_id = 0; anchor_id < anchor_height.Count; anchor_id++)
                        {
                            var x_center = (x + _options.anchor_offset_x) / feature_map_width;
                            var y_center = (y + _options.anchor_offset_y) / feature_map_height;

                            var anchor = new Anchor
                            {
                                cx = x_center,
                                cy = y_center,
                                w = 0f,
                                h = 0f
                            };
                            if (_options.fixed_anchor_size)
                            {
                                anchor.w = 1.0f;
                                anchor.h = 1.0f;
                            }
                            else
                            {
                                anchor.w = anchor_width[anchor_id];
                                anchor.h = anchor_height[anchor_id];
                            }
                            anchors.Add(anchor);
                        }
                    }
                }
                layer_id = last_same_stride_layer;
            }
        }
    }
}
