using ZeroLevel.Network;

namespace ZeroLevel.Services.Applications
{
    public abstract class BaseWindowsExService
        : BaseWindowsService, IExchangeService
    {
        public string Key { get; private set; }
        public string Version { get; private set; }
        public string Group { get; private set; }
        public string Type { get; private set; }

        protected readonly IConfiguration _config;

        protected Exchange Exchange { get; }

        protected BaseWindowsExService(IConfiguration configuration = null)
            : base()
        {
            _config = configuration ?? Configuration.Default;
            base.Name = ReadName(_config);
            this.Key = ReadKey(_config);
            this.Version = ReadVersion(_config);
            this.Group = ReadServiceGroup(_config);
            this.Type = ReadServiceType(_config);

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

        private string ReadName(IConfiguration configuration)
        {
            if (_config.Contains("ServiceName"))
                return _config.First("ServiceName");
            if (_config.Contains("AppName"))
                return _config.First("AppName");
            return this.GetType().Name;
        }

        private string ReadKey(IConfiguration configuration)
        {
            if (_config.Contains("ServiceKey"))
                return _config.First("ServiceKey");
            if (_config.Contains("AppKey"))
                return _config.First("AppKey");
            return null;
        }

        private string ReadVersion(IConfiguration configuration)
        {
            if (_config.Contains("Version"))
                return _config.First("Version");
            if (_config.Contains("AppVersion"))
                return _config.First("AppVersion");
            return "1.0";
        }

        private string ReadServiceGroup(IConfiguration configuration)
        {
            if (_config.Contains("DiscoveryGroup"))
                return _config.First("DiscoveryGroup");
            if (_config.Contains("ServiceGroup"))
                return _config.First("ServiceGroup");
            return ExServiceInfo.DEFAULT_GROUP_NAME;
        }

        private string ReadServiceType(IConfiguration configuration)
        {
            if (_config.Contains("DiscoveryType"))
                return _config.First("DiscoveryType");
            if (_config.Contains("ServiceType"))
                return _config.First("ServiceType");
            return ExServiceInfo.DEFAULT_TYPE_NAME;
        }

        #endregion Config

        public string Endpoint { get; private set; }

        public override void DisposeResources()
        {
            this.Exchange.Dispose();
        }
    }
}