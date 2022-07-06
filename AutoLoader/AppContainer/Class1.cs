namespace AppContainer
{

    public class Endpoint
    {
        public string VersionEndpoint { get; set; }
        public string AppEndpoint { get; set; }
        public string PingEndpoint { get; set; }
        public string Token { get; set; }
    }
    public class HostSettings
    {
        public IEnumerable<Endpoint> Endpoints { get; set; }
        public string EntryClass { get; set; }
        public string EntryMethod { get; set; }
    }

    public class Host
    {
        private readonly HostSettings _settings;
        public Host(HostSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _settings = settings;
        }

        public async Task<bool> HasUpdate()
        {
            if (_settings.Endpoints != null)
            {
                foreach (var ep in _settings.Endpoints)
                {
                    try
                    {
                        using (var client = new HttpClient())
                        {
                            client.DefaultRequestHeaders.Add("token", ep.Token);
                            var response = await client.GetAsync(ep.PingEndpoint);
                            response.EnsureSuccessStatusCode();
                            
                        }
                    }
                    catch (Exception ex)
                    { 
                    }
                }
            }
        }
    }
}