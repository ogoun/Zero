using System;

namespace ZeroMappingTest.Models
{
    public abstract class BaseClass
    {
        public Guid Id;
        public string Title { get; set; }
        public string Description { get; set; }

        protected long Version { get; set; }
        private DateTime Created { get; set; }
    }
}
