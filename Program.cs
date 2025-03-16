using ConsulConfigurationManagement;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddConsulConfiguration(reloadOnChange: true, changeChecKInterval: TimeSpan.FromSeconds(30));

// Add services to the container.
var services = builder.Services;
services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
