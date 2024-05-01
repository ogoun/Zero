using System;
using System.Collections.Generic;
using System.Text;

namespace DOM.DSL.Model
{
    public sealed class TEnvironment
    {
        public int Delay { get; set; } = 0;
        public string FileName { get; set; } = null!;
        public Encoding Encoding { get; set; } = null!;
        public string ContractName { get; set; } = null!;
        public string SubscriptionName { get; set; } = null!;
        public Guid SubscriptionId { get; set; } = Guid.Empty;
        public IDictionary<string, object> CustomVariables { get; }

        public TEnvironment()
        {
            CustomVariables = new Dictionary<string, object>();
        }

        public void AddCustomVar(string name, object value)
        {
            CustomVariables.Add(name.ToLowerInvariant().Trim(), value);
        }
    }
}