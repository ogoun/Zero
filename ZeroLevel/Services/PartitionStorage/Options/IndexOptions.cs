namespace ZeroLevel.Services.PartitionStorage
{
    public enum IndexStepType
    {
        AbsoluteCount,
        Step
    }

    public class IndexOptions
    {
        public bool Enabled { get; set; }

        public IndexStepType StepType { get; set; } = IndexStepType.AbsoluteCount;
        public int StepValue { get; set; } = 64;
    }
}
