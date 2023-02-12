using System;

namespace ZeroLevel.Services.Config
{
    public class ConfigRecordParseAttribute
        : Attribute
    {
        public IConfigRecordParser Parser { get; set; }

        public ConfigRecordParseAttribute(Type parserType)
        {
            if (parserType == null) throw new ArgumentNullException(nameof(parserType));
            Parser = (IConfigRecordParser)Activator.CreateInstance(parserType);
        }
    }
}
