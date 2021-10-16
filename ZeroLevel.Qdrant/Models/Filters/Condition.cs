namespace ZeroLevel.Qdrant.Models.Filters
{
    /// <summary>
    /// Condition for qdrant filters
    /// </summary>
    public class Condition
        : Operand
    {
        /*
        integer - 64-bit integer in the range -9223372036854775808 to 9223372036854775807.      array of long
        float - 64-bit floating point number.                                                   array of double
        keyword - string value.                                                                 array of strings
        geo - Geographical coordinates. Example: { "lon": 52.5200, "lat": 13.4050 }             array of lon&lat of double
         */
        public string Json { get; set; }

        public static Condition Ids(long[] values)
        {
            return new Condition
            {
                Json = $"{{ \"has_id\": [{string.Join(",", values)}] }}"
            };
        }

        public static Condition IntegerMatch(string name, long value)
        {
            return new Condition
            {
                Json = $"{{ \"key\": \"{name.ToLowerInvariant()}\", \"match\": {{ \"integer\": {value} }} }}"
            };
        }

        public static Condition IntegerRange(string name, long left, long rigth, bool include_left, bool include_right)
        {
            var left_cond = include_left ? $"\"lt\": null,\"lte\": {rigth}" : $"\"lt\": {rigth},\"lte\": null";
            var right_cond = include_right ? $"\"gt\": null,\"gte\": {left}" : $"\"gt\": {left},\"gte\": null";
            return new Condition
            {
                Json = $"{{ \"key\": \"{name.ToLowerInvariant()}\", \"range\": {{ {right_cond}, {left_cond} }} }}"
            };
        }

        public static Condition FloatRange(string name, double left, double rigth, bool include_left, bool include_right)
        {
            var left_cond = include_left ? $"\"lt\": null,\"lte\": {rigth.ConvertToString()}" : $"\"lt\": {rigth.ConvertToString()},\"lte\": null";
            var right_cond = include_right ? $"\"gt\": null,\"gte\": {left.ConvertToString()}" : $"\"gt\": {left.ConvertToString()},\"gte\": null";
            return new Condition
            {
                Json = $"{{ \"key\": \"{name.ToLowerInvariant()}\", \"range\": {{ {left_cond}, {right_cond} }} }}"
            };
        }

        public static Condition KeywordMatch(string name, string value)
        {
            return new Condition
            {
                Json = $"{{ \"key\": \"{name.ToLowerInvariant()}\", \"match\": {{ \"keyword\": \"{value}\" }} }}"
            };
        }

        public static Condition GeoBox(string name, Location top_left, Location bottom_right)
        {
            return new Condition
            {
                Json = $"{{ \"key\": \"{name.ToLowerInvariant()}\", \"geo_bounding_box\": {{ \"bottom_right\": {{ \"lat\": {bottom_right.lat.ConvertToString()}, \"lon\": {bottom_right.lon.ConvertToString()} }}, \"top_left\": {{ \"lat\": {top_left.lat.ConvertToString()}, \"lon\": {top_left.lon.ConvertToString()} }} }} }}"
            };
        }

        public static Condition GeoRadius(string name, Location location, double radius)
        {
            return new Condition
            {
                Json = $"{{\"key\": \"{name.ToLowerInvariant()}\", \"geo_radius\": {{\"center\": {{ \"lat\": {location.lat.ConvertToString()}, \"lon\": {location.lon.ConvertToString()} }}, \"radius\": {radius.ConvertToString()} }} }}"
            };
        }
        public override string ToJSON()
        {
            return Json;
        }
    }
}
