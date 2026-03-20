using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using ElasticMcp.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ElasticMcp.Services;

public static class ElasticClientRegistration
{
    public static IServiceCollection AddElasticsearchClient(this IServiceCollection services)
    {
        services.AddSingleton<ElasticsearchClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ElasticMcpOptions>>().Value;

            var settings = new ElasticsearchClientSettings(new Uri(options.Nodes.First()));

            if (options.Authentication is { } auth)
            {
                settings = auth.Type switch
                {
                    "ApiKey" => settings.Authentication(new ApiKey(auth.ApiKey!)),
                    "Basic" => settings.Authentication(new BasicAuthentication(auth.Username!, auth.Password!)),
                    _ => settings
                };
            }

            return new ElasticsearchClient(settings);
        });

        return services;
    }
}
