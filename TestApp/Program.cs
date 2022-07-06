namespace TestApp
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var detector = new PersonDetector();
            var predictions = detector.Detect(@"E:\Desktop\test\1.JPG");
        }
    }
}