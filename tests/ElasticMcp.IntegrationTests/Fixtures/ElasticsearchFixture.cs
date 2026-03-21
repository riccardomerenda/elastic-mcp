using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Testcontainers.Elasticsearch;

namespace ElasticMcp.IntegrationTests.Fixtures;

public class ElasticsearchFixture : IAsyncLifetime
{
    private readonly ElasticsearchContainer _container;

    public ElasticsearchClient Client { get; private set; } = null!;

    public ElasticsearchFixture()
    {
        _container = new ElasticsearchBuilder("docker.elastic.co/elasticsearch/elasticsearch:9.0.2")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();
        var uri = new Uri(connectionString);

        var nodeUri = new Uri($"{uri.Scheme}://{uri.Host}:{uri.Port}");

        var settings = new ElasticsearchClientSettings(nodeUri)
            .ServerCertificateValidationCallback(CertificateValidations.AllowAll);

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var parts = uri.UserInfo.Split(':');
            settings = settings.Authentication(new BasicAuthentication(parts[0], parts[1]));
        }

        Client = new ElasticsearchClient(settings);

        // Use a real request instead of Ping to verify connectivity
        var health = await Client.Cluster.HealthAsync();
        if (!health.IsValidResponse)
            throw new InvalidOperationException(
                $"Failed to connect to Elasticsearch container: {health.DebugInformation}");

        await SeedTestDataAsync();
    }

    private async Task SeedTestDataAsync()
    {
        for (var i = 1; i <= 5; i++)
        {
            await Client.IndexAsync(new
            {
                title = $"Test Document {i}",
                message = $"This is test document number {i}",
                level = i % 2 == 0 ? "error" : "info",
                timestamp = DateTime.UtcNow.AddMinutes(-i)
            }, idx => idx.Index("test-logs").Id(i.ToString()));
        }

        await Client.Indices.RefreshAsync("test-logs");

        await SeedVectorDataAsync();
    }

    private async Task SeedVectorDataAsync()
    {
        // Create index with dense_vector mapping
        await Client.Indices.CreateAsync("test-vectors", c => c
            .Mappings(m => m
                .Properties(p => p
                    .Text("title")
                    .Text("content")
                    .DenseVector("embedding", dv => dv.Dims(3).Similarity(Elastic.Clients.Elasticsearch.Mapping.DenseVectorSimilarity.Cosine))
                )
            )
        );

        await Client.IndexAsync(new
        {
            title = "Document about cats",
            content = "Cats are small domesticated animals",
            embedding = new[] { 1.0f, 0.0f, 0.0f }
        }, idx => idx.Index("test-vectors").Id("1"));

        await Client.IndexAsync(new
        {
            title = "Document about dogs",
            content = "Dogs are loyal companions",
            embedding = new[] { 0.0f, 1.0f, 0.0f }
        }, idx => idx.Index("test-vectors").Id("2"));

        await Client.IndexAsync(new
        {
            title = "Another cat document",
            content = "Kittens are young cats",
            embedding = new[] { 0.9f, 0.1f, 0.0f }
        }, idx => idx.Index("test-vectors").Id("3"));

        await Client.Indices.RefreshAsync("test-vectors");
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync().AsTask();
    }
}

[CollectionDefinition("Elasticsearch")]
public class ElasticsearchCollection : ICollectionFixture<ElasticsearchFixture>;
