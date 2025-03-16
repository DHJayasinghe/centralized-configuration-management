namespace ConsulConfigurationManagement;

public class ConsulConfigurationSource(ConsulConfiguration configuration) : IConfigurationSource
{
    private readonly ConsulConfiguration _configuration = configuration;
    public IConfigurationProvider Build(IConfigurationBuilder builder) => new ConsulConfigurationProvider(_configuration);
}
