namespace ConsulConfigurationManagement;

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
