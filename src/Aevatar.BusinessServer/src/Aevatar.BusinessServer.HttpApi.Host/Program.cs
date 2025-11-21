using System;
using System.Threading.Tasks;
using Aevatar.BusinessServer.HttpApi.Host.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Serilog;
using Serilog.Events;
using Orleans.Serialization;

namespace Aevatar.BusinessServer.HttpApi.Host;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Async(c => c.File("Logs/logs.txt"))
            .WriteTo.Async(c => c.Console())
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting BusinessServer HttpApi.Host.");
            var builder = WebApplication.CreateBuilder(args);
            
            // Read Agent Runtime configuration
            var runtimeOptions = builder.Configuration
                .GetSection(AgentRuntimeOptions.SectionName)
                .Get<AgentRuntimeOptions>() ?? new AgentRuntimeOptions();
            
            // Configure Orleans if using Orleans runtime
            if (runtimeOptions.RuntimeType == AgentRuntimeType.Orleans)
            {
                ConfigureOrleans(builder, runtimeOptions.Orleans);
            }
            
            builder.Host
                .AddAppSettingsSecretsJson()
                .UseAutofac()
                .UseSerilog((context, services, loggerConfiguration) =>
                {
                    loggerConfiguration
                    #if DEBUG
                        .MinimumLevel.Debug()
                    #else
                        .MinimumLevel.Information()
                    #endif
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                        .Enrich.FromLogContext()
                        .WriteTo.Async(c => c.File("Logs/logs.txt"))
                        .WriteTo.Async(c => c.Console())
                        .WriteTo.Async(c => c.AbpStudio(services));
                });
            await builder.AddApplicationAsync<BusinessServerHttpApiHostModule>();
            var app = builder.Build();
            await app.InitializeApplicationAsync();
            await app.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// Configure Orleans when using Orleans runtime
    /// </summary>
    private static void ConfigureOrleans(WebApplicationBuilder builder, OrleansRuntimeOptions orleansOptions)
    {
        builder.Host.UseOrleansClient((context, clientBuilder) =>
        {
            if (orleansOptions.UseLocalhostClustering)
            {
                // Development: localhost clustering
                clientBuilder.UseLocalhostClustering(orleansOptions.GatewayPort);
                Log.Information("🌐 Orleans Client configured for localhost clustering");
            }
            else
            {
                // Production: configure actual clustering
                clientBuilder.Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = orleansOptions.ClusterId;
                    options.ServiceId = orleansOptions.ServiceId;
                });
                Log.Information("🌐 Orleans Client configured for production clustering");
            }
            
            // Add memory stream provider
            clientBuilder.AddMemoryStreams(orleansOptions.StreamProviderName);

            // Add Protobuf serializer
            clientBuilder.ConfigureServices(services =>
            {
                services.AddSerializer(serializerBuilder =>
                {
                    serializerBuilder.AddProtobufSerializer();
                });
            });
            
            Log.Information("   ClusterId: {ClusterId}", orleansOptions.ClusterId);
            Log.Information("   ServiceId: {ServiceId}", orleansOptions.ServiceId);
            Log.Information("   GatewayPort: {GatewayPort}", orleansOptions.GatewayPort);
        });
    }
}
