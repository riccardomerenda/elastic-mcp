using ElasticMcp.Configuration;
using ElasticMcp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ElasticMcpOptions>(
    builder.Configuration.GetSection(ElasticMcpOptions.SectionName));

builder.Services.AddElasticsearchClient();
builder.Services.AddSingleton<SecurityGuard>();

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "ElasticMcp",
            Version = "0.3.0"
        };
    })
    .WithHttpTransport()
    .WithToolsFromAssembly(typeof(ElasticMcpOptions).Assembly)
    .WithResourcesFromAssembly(typeof(ElasticMcpOptions).Assembly)
    .WithPromptsFromAssembly(typeof(ElasticMcpOptions).Assembly);

var app = builder.Build();

app.MapMcp("/mcp");

app.Run();
