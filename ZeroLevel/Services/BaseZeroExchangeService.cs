using ZeroLevel.Network;

namespace ZeroLevel.Services.Applications
{
    public abstract class BaseZeroExchangeService
        : BaseZeroService, IExchangeService
    {
        public string Key { get; private set; }
        public string Version { get; private set; }
        public string Group { get; private set; }
        public string Type { get; private set; }

        protected readonly IConfigurationSet _configSet;
        protected IConfiguration _config => _configSet?.Default;

        protected Exchange Exchange { get; }

        private BaseZeroExchangeService()
        {

        }

        protected BaseZeroExchangeService(IConfigurationSet configuration = null)
            : base()
        {
            _configSet = configuration ?? Configuration.DefaultSet;
            base.Name = ReadName();
            this.Key = ReadKey();
            this.Version = ReadVersion();
            this.Group = ReadServiceGroup();
            this.Type = ReadServiceType();

            var discovery = _config.First("discovery");

            Exchange = new Exchange(new DiscoveryClient(discovery));
        }

        private IExService _self_service = null;
        private readonly object _self_create_lock = new object();
        protected IExService Self
        {
            get
            {
                if (_self_service == null)
                {
                    lock (_self_create_lock)
                    {
                        if (_self_service == null)
                        {
                            _self_service = Exchange.RegisterService(this);
                        }
                    }
                }
                return _self_service;
            }
        }

        #region Config

        private string ReadName()
        {
            return FindInConfig<string>(new[] { "ServiceName", "AppName" }, string.Empty, "service")
                ?? this.GetType().Name;
        }

        private string ReadKey()
        {
            return FindInConfig<string>(new[] { "ServiceKey", "AppKey" }, string.Empty, "service");
        }

        private string ReadVersion()
        {
            return FindInConfig<string>(new[] { "Version", "AppVersion" }, string.Empty, "service")
                ?? "1.0";
        }

        private string ReadServiceGroup()
        {
            return FindInConfig<string>(new[] { "DiscoveryGroup", "ServiceGroup" }, string.Empty, "service")
                ?? ExServiceInfo.DEFAULT_GROUP_NAME;
        }

        private string ReadServiceType()
        {
            return FindInConfig<string>(new[] { "DiscoveryType", "ServiceType" }, string.Empty, "service")
                ?? ExServiceInfo.DEFAULT_TYPE_NAME;
        }

        protected T FindInConfig<T>(string[] keys, params string[] sections)
        {
            foreach (var section in sections)
            {
                if (string.IsNullOrWhiteSpace(section))
                {
                    foreach (var key in keys)
                    {
                        if (_configSet.Default.Contains(key))
                        {
                            return _configSet.Default.First<T>(key);
                        }
                    }
                }
                else if (_configSet.ContainsSection(section))
                {
                    foreach (var key in keys)
                    {
                        if (_configSet[section].Contains(key))
                        {
                            return _configSet[section].First<T>(key);
                        }
                    }
                }
            }
            return default(T);
        }
        #endregion Config

        public string Endpoint { get; private set; }

        protected override void StopAction()
        {
            this.Exchange.Dispose();
        }
    }
}