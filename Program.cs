using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using TeamsIntegration;
using TouchPortalSDK.Configuration;

Assembly assembly = Assembly.GetExecutingAssembly();
String baseDirectory = Path.GetDirectoryName(assembly.Location);

IConfigurationRoot configurationRoot = new ConfigurationBuilder()
                                      .SetBasePath(baseDirectory)
                                      .AddJsonFile("appsettings.json")
                                      .Build();

// Standard method for build a ServiceProvider in .Net:
ServiceCollection serviceCollection = new ServiceCollection();

// Set console logging
serviceCollection.AddLogging(configure =>
{
    configure.AddSimpleConsole(options => options.TimestampFormat = "[yyyy.MM.dd HH:mm:ss] ");
    configure.AddConfiguration(configurationRoot.GetSection("Logging"));
});

// Registering the Plugin to the IoC container:
serviceCollection.AddTouchPortalSdk(configurationRoot);
serviceCollection.AddSingleton<TeamsWsIntegration>();
serviceCollection.AddSingleton<TeamsTpPlugin>();

// Get the IoC service provider
ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider(true);

// Use your IoC framework to resolve the plugin with it's dependencies,
TeamsTpPlugin tpPlugin = serviceProvider.GetRequiredService<TeamsTpPlugin>();
tpPlugin.Run();
