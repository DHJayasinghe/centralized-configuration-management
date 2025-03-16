using Microsoft.AspNetCore.Mvc;

namespace ConsulConfigurationManagement.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController(IConfiguration configuration) : ControllerBase
    {
        private readonly IConfiguration _configuration = configuration;

        [HttpGet]
        public string Get()
        {
            return _configuration["ConnectionString"];
        }
    }
}
