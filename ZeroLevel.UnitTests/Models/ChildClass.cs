namespace ZeroMappingTest.Models
{
    public class ChildClass: 
        BaseClass
    {
        public int Number;
        public int Balance { get; set; }
        public int ReadOnlyProperty { get { return Number; } }
        public int WriteOnlyProperty { set { Number = value; } }
    }
}
