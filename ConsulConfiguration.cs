using Consul;

namespace ConsulConfigurationManagement
{
    /// <summary>
    /// </summary>
    /// <param name="consulAddress"></param>
    /// <param name="reloadOnChange">Reload when version flag changed</param>
    /// <param name="changeCheckInterval">Default 5min when reloadOnChange is true</param>
    /// <param name="versionCheckFlag">Flag name on Consul which used to poll for changes</param>
    public class ConsulConfiguration(string consulAddress, bool reloadOnChange = false, TimeSpan? changeCheckInterval = null, string versionCheckFlag = "Version")
    {
        private readonly ConsulClient _client = new(cfg => { cfg.Address = new Uri(consulAddress); });

        public bool ReloadOnChange { get; } = reloadOnChange;
        public TimeSpan? ReloadInterval { get; } = changeCheckInterval;
        public string Version { get; } = versionCheckFlag;

        public async Task<string> GetValueAsync(string key)
        {
            var kv = await _client.KV.Get(key);
            if (kv.Response.Value != null)
                return System.Text.Encoding.UTF8.GetString(kv.Response.Value);

            return null;
        }

        public async Task<Dictionary<string, string>> GetValuesAsync()
        {
            Dictionary<string, string> values = [];

            var kv = await _client.KV.List("");
            foreach (var entry in kv.Response)
            {
                values.Add(entry.Key, System.Text.Encoding.UTF8.GetString(entry.Value));
            }

            return values;
        }


    }

    public class ConsulConfigurationProvider(ConsulConfiguration configuration) : ConfigurationProvider, IDisposable
    {
        private string _localConfigVersion = "";
        private readonly ConsulConfiguration _configuration = configuration;
        private bool _backgroundRefreshRunning = false;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly object _lock = new();

        public override void Load()
        {
            var values = _configuration.GetValuesAsync().GetAwaiter().GetResult();
            Data = values ?? new Dictionary<string, string>();

            lock (_lock)
            {
                Data.TryGetValue(_configuration.Version, out _localConfigVersion);
            }

            if (!_backgroundRefreshRunning && _configuration.ReloadOnChange)
            {
                _ = StartBackgroundRefreshAsync();
            }
        }

        private Task StartBackgroundRefreshAsync()
        {
            _backgroundRefreshRunning = true;

            return Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(_configuration.ReloadInterval ?? TimeSpan.FromMinutes(30), _cancellationTokenSource.Token);

                        var remoteConfigVersion = await _configuration.GetValueAsync(_configuration.Version);
                        string localVersionCopy;
                        lock (_lock)
                        {
                            localVersionCopy = _localConfigVersion;
                        }

                        if (remoteConfigVersion != localVersionCopy)
                        {
                            Console.WriteLine("Consul Configuration: Detected a change, reloading...");
                            Load();
                            OnReload(); // Notify IConfiguration that values have changed
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        Console.WriteLine("Consul Configuration: Background refresh task canceled.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: Background refresh failed - {ex.Message}");
                    }
                }
            }, _cancellationTokenSource.Token);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }

    public class ConsulConfigurationSource(ConsulConfiguration configuration) : IConfigurationSource
    {
        private readonly ConsulConfiguration _configuration = configuration;
        public IConfigurationProvider Build(IConfigurationBuilder builder) => new ConsulConfigurationProvider(_configuration);
    }

    public static class ConfigurationManagerExtensions
    {
        public static ConfigurationManager AddConsulConfiguration(
            this ConfigurationManager manager,
            bool reloadOnChange = false, TimeSpan? changeChecKInterval = null, string versionFlag = "Version")
        {
            var consulEndpoint = manager.GetConnectionString("Consul");
            var consulConfiguration = new ConsulConfiguration(consulEndpoint, reloadOnChange, changeChecKInterval, versionFlag);

            IConfigurationBuilder configBuilder = manager;
            configBuilder.Add(new ConsulConfigurationSource(consulConfiguration));

            return manager;
        }
    }
}
