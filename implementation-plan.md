# ElasticMCP v0.1 — Implementation Plan

> Foundation milestone: project scaffolding, CI/CD, Elasticsearch connection, `search` and `count` tools, `cluster/health` and `cluster/indices` resources, working stdio transport, and integration testing with Testcontainers.

---

## Package Versions

| Package | Version | Purpose |
|---------|---------|---------|
| `ModelContextProtocol` | 1.1.0 | MCP SDK with hosting/DI extensions |
| `Elastic.Clients.Elasticsearch` | 9.3.3 | Elasticsearch .NET client |
| `Microsoft.Extensions.Hosting` | 10.0.x | Generic host for stdio server |
| `Testcontainers.Elasticsearch` | 4.10.0 | ES Docker container for integration tests |
| `xunit` | v3 | Test framework |
| `NSubstitute` | 5.x | Mocking (unit tests) |

Target framework: `net10.0` (.NET 10 SDK 10.0.100+)

---

## Phase A: Scaffolding (Steps 1–5)

### Step 1 — Git init + solution structure

```bash
git init
dotnet new sln -n ElasticMcp
dotnet new console -n ElasticMcp -o src/ElasticMcp --framework net10.0
dotnet new xunit -n ElasticMcp.Tests -o tests/ElasticMcp.Tests --framework net10.0
dotnet new xunit -n ElasticMcp.IntegrationTests -o tests/ElasticMcp.IntegrationTests --framework net10.0
dotnet sln ElasticMcp.sln add src/ElasticMcp/ElasticMcp.csproj
dotnet sln ElasticMcp.sln add tests/ElasticMcp.Tests/ElasticMcp.Tests.csproj
dotnet sln ElasticMcp.sln add tests/ElasticMcp.IntegrationTests/ElasticMcp.IntegrationTests.csproj
```

Create subdirectories inside `src/ElasticMcp/`:

```
src/ElasticMcp/
├── Configuration/
├── Tools/
├── Resources/
└── Services/
```

Also create: `.gitignore` (via `dotnet new gitignore`), `LICENSE` (MIT).

**Validation**: `dotnet build ElasticMcp.sln` succeeds with zero errors.

---

### Step 2 — NuGet package references

**`src/ElasticMcp/ElasticMcp.csproj`**:

```xml
<PackageReference Include="ModelContextProtocol" Version="1.1.0" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.0" />
<PackageReference Include="Elastic.Clients.Elasticsearch" Version="9.3.3" />
```

**`tests/ElasticMcp.Tests/ElasticMcp.Tests.csproj`**:

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
<PackageReference Include="xunit.v3" Version="*" />
<PackageReference Include="xunit.runner.visualstudio" Version="*" />
<PackageReference Include="NSubstitute" Version="5.*" />
<ProjectReference Include="..\..\src\ElasticMcp\ElasticMcp.csproj" />
```

**`tests/ElasticMcp.IntegrationTests/ElasticMcp.IntegrationTests.csproj`**:

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
<PackageReference Include="xunit.v3" Version="*" />
<PackageReference Include="xunit.runner.visualstudio" Version="*" />
<PackageReference Include="Testcontainers.Elasticsearch" Version="4.10.0" />
<PackageReference Include="Elastic.Clients.Elasticsearch" Version="9.3.3" />
<ProjectReference Include="..\..\src\ElasticMcp\ElasticMcp.csproj" />
```

**Validation**: `dotnet restore && dotnet build` succeeds.

---

### Step 3 — Configuration (`ElasticMcpOptions`)

**File**: `src/ElasticMcp/Configuration/ElasticMcpOptions.cs`

```csharp
namespace ElasticMcp.Configuration;

public class ElasticMcpOptions
{
    public const string SectionName = "ElasticMcp";

    public List<string> Nodes { get; set; } = ["http://localhost:9200"];
    public AuthenticationOptions? Authentication { get; set; }
    public string? DefaultIndex { get; set; }
    public bool ReadOnly { get; set; } = true;
    public int MaxResultSize { get; set; } = 100;
    public string QueryTimeout { get; set; } = "30s";
    public List<string> AllowedIndices { get; set; } = [];
    public List<string> DeniedIndices { get; set; } = [];
}

public class AuthenticationOptions
{
    public string Type { get; set; } = "None";  // "None", "ApiKey", "Basic"
    public string? ApiKey { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}
```

**File**: `src/ElasticMcp/appsettings.json` (copied to output via `<Content CopyToOutputDirectory="PreserveNewest" />`)

```json
{
  "ElasticMcp": {
    "Nodes": ["http://localhost:9200"],
    "Authentication": { "Type": "None" },
    "ReadOnly": true,
    "MaxResultSize": 100,
    "QueryTimeout": "30s"
  }
}
```

---

### Step 4 — Elasticsearch client registration

**File**: `src/ElasticMcp/Services/ElasticClientRegistration.cs`

Singleton `ElasticsearchClient` wired via DI. Reads `ElasticMcpOptions` to configure connection and authentication (None / ApiKey / Basic).

Key points:
- `ElasticsearchClient` is thread-safe, designed for reuse as a singleton.
- `RequestTimeout` is parsed from config.
- For multi-node, use `NodePool` with all configured URIs.

---

### Step 5 — `Program.cs` (first runnable server)

**File**: `src/ElasticMcp/Program.cs`

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<ElasticMcpOptions>(
    builder.Configuration.GetSection(ElasticMcpOptions.SectionName));

builder.Services.AddElasticsearchClient();

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "ElasticMcp", Version = "0.1.0" };
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly();

// MCP stdio: logs must go to stderr, stdout is reserved for JSON-RPC
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

await builder.Build().RunAsync();
```

SDK patterns:
- `WithToolsFromAssembly()` discovers classes with `[McpServerToolType]` and methods with `[McpServerTool]`.
- `WithResourcesFromAssembly()` discovers `[McpServerResourceType]` / `[McpServerResource]`.
- DI services (`ElasticsearchClient`, `IOptions<ElasticMcpOptions>`) are injectable as method parameters in tool/resource methods.

**Checkpoint**: `dotnet run --project src/ElasticMcp` starts and responds to MCP `initialize` handshake. No tools/resources yet.

---

## Phase B: Tools & Resources (Steps 6–9)

### Step 6 — `search` tool

**File**: `src/ElasticMcp/Tools/SearchTool.cs`

```csharp
[McpServerToolType]
public class SearchTool
{
    [McpServerTool(Name = "search")]
    [Description("Perform a full-text search on an Elasticsearch index.")]
    public static async Task<string> Search(
        ElasticsearchClient client,
        IOptions<ElasticMcpOptions> options,
        [Description("Index name or pattern (e.g. 'logs-*')")] string index,
        [Description("Search query string")] string query,
        [Description("Number of results (default: 10)")] int size = 10,
        [Description("Offset for pagination (default: 0)")] int from = 0,
        CancellationToken cancellationToken = default)
```

Key decisions:
- Uses `SearchAsync<JsonDocument>` for schema-agnostic results.
- `size` is clamped to `MaxResultSize` from config.
- Uses `QueryString` query (Lucene syntax) for maximum LLM flexibility.
- Returns structured JSON: `{ total, returned, hits: [{ id, index, score, source }] }`.

---

### Step 7 — `count` tool

**File**: `src/ElasticMcp/Tools/CountTool.cs`

```csharp
[McpServerToolType]
public class CountTool
{
    [McpServerTool(Name = "count")]
    [Description("Count documents in an Elasticsearch index, optionally filtered by a query.")]
    public static async Task<string> Count(
        ElasticsearchClient client,
        [Description("Index name or pattern")] string index,
        [Description("Optional query string filter (default: match all)")] string? query = null,
        CancellationToken cancellationToken = default)
```

Returns: `{ index, count, query }`.

---

### Step 8 — `cluster/health` resource

**File**: `src/ElasticMcp/Resources/ClusterHealthResource.cs`

```csharp
[McpServerResourceType]
public class ClusterHealthResource
{
    [McpServerResource(
        UriTemplate = "elasticsearch://cluster/health",
        Name = "Cluster Health",
        MimeType = "application/json")]
    [Description("Cluster health: status color, node count, shard information")]
    public static async Task<string> GetClusterHealth(
        ElasticsearchClient client,
        CancellationToken cancellationToken = default)
```

Returns: `{ status, clusterName, numberOfNodes, numberOfDataNodes, activeShards, ... }`.

---

### Step 9 — `cluster/indices` resource

**File**: `src/ElasticMcp/Resources/IndicesResource.cs`

```csharp
[McpServerResourceType]
public class IndicesResource
{
    [McpServerResource(
        UriTemplate = "elasticsearch://cluster/indices",
        Name = "Cluster Indices",
        MimeType = "application/json")]
    [Description("List all indices with doc count, size, status, and health")]
    public static async Task<string> GetIndices(
        ElasticsearchClient client,
        CancellationToken cancellationToken = default)
```

Uses `client.Cat.IndicesAsync()`. Returns: `{ indices: [{ index, health, status, docsCount, storeSize, ... }] }`.

**Note**: Cat Indices API property names may differ between ES client v8 and v9 — verify against IntelliSense during implementation.

---

## Phase C: Testing (Steps 10–12)

### Step 10 — Testcontainers fixture

**File**: `tests/ElasticMcp.IntegrationTests/Fixtures/ElasticsearchFixture.cs`

Shared xUnit collection fixture — single ES container for the entire test suite.

```csharp
public class ElasticsearchFixture : IAsyncLifetime
{
    private readonly ElasticsearchContainer _container = new ElasticsearchBuilder()
        .WithImage("docker.elastic.co/elasticsearch/elasticsearch:8.17.0")
        .Build();

    public ElasticsearchClient Client { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        // Configure client with AllowAll cert validation (self-signed cert)
        // Seed 5 test documents into "test-logs" index
    }
}

[CollectionDefinition("Elasticsearch")]
public class ElasticsearchCollection : ICollectionFixture<ElasticsearchFixture>;
```

Test classes use `[Collection("Elasticsearch")]` to share the container.

---

### Step 11 — Integration tests

**Tests to write**:

| File | Tests |
|------|-------|
| `Tools/SearchToolTests.cs` | match-all returns docs, respects max size, handles invalid index |
| `Tools/CountToolTests.cs` | counts all docs, counts with query filter |
| `Resources/ClusterHealthResourceTests.cs` | returns valid status (green/yellow) |
| `Resources/IndicesResourceTests.cs` | lists test-logs index |

All tests call the static tool/resource methods directly, passing the fixture's `ElasticsearchClient`.

**Validation**: `dotnet test tests/ElasticMcp.IntegrationTests/` — all green (requires Docker running).

---

### Step 12 — Unit tests

**File**: `tests/ElasticMcp.Tests/Configuration/ElasticMcpOptionsTests.cs`

- Default values are correct
- `MaxResultSize` clamping logic (if extracted to a helper)
- Configuration binding from JSON

Lower priority in v0.1 — integration tests carry most of the weight. Add unit tests for any pure-logic methods extracted into services.

---

## Phase D: CI & Finishing (Steps 13–15)

### Step 13 — GitHub Actions CI

**File**: `.github/workflows/ci.yml`

```yaml
name: CI
on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore ElasticMcp.sln
      - run: dotnet build ElasticMcp.sln --no-restore --configuration Release
      - run: dotnet test tests/ElasticMcp.Tests/ --no-build --configuration Release
      - run: dotnet test tests/ElasticMcp.IntegrationTests/ --no-build --configuration Release
```

Docker is pre-installed on GitHub-hosted runners — Testcontainers works out of the box.

---

### Step 14 — Supporting files

- `.gitignore` — standard .NET template (`dotnet new gitignore`)
- `LICENSE` — MIT
- `README.md` — minimal: project description, prerequisites (.NET 10, Docker), build/test commands, MCP Inspector usage, config example

---

### Step 15 — MCP Inspector manual validation

1. Start a local ES: `docker run -d -p 9200:9200 -e "discovery.type=single-node" -e "xpack.security.enabled=false" docker.elastic.co/elasticsearch/elasticsearch:8.17.0`
2. Index some test data
3. Start server: `dotnet run --project src/ElasticMcp`
4. Connect MCP Inspector to the stdio server
5. Verify: 2 tools (`search`, `count`) and 2 resources (`cluster/health`, `cluster/indices`) are listed
6. Invoke each and confirm responses

---

## Dependency Graph

```
Step 1 (scaffolding)
  └─> Step 2 (packages)
        └─> Step 3 (configuration)
              └─> Step 4 (ES client factory)
                    ├─> Step 5 (Program.cs) ← FIRST RUNNABLE SERVER
                    │     ├─> Step 6 (search tool)
                    │     ├─> Step 7 (count tool)
                    │     ├─> Step 8 (health resource)
                    │     └─> Step 9 (indices resource)
                    └─> Step 10 (test fixture)
                          └─> Step 11 (integration tests)

Step 12 (unit tests)     — parallel with Steps 6–9
Step 13 (CI/CD)          — can start after Step 2, expand as tests arrive
Step 14 (supporting files) — any time
Step 15 (manual testing) — after all tools/resources exist
```

---

## Risks

| Risk | Mitigation |
|------|------------|
| ES client v9 API differences from v8 docs | Use IntelliSense + official v9 docs; verify `Cat.IndicesAsync()` property names |
| `SearchAsync<JsonDocument>` deserialization issues | Fallback to `Dictionary<string, object>` or `JsonElement` |
| `WithResourcesFromAssembly()` may not exist in SDK 1.1.0 | Fallback to explicit `.WithResources<ClusterHealthResource>().WithResources<IndicesResource>()` |
| Testcontainers ES uses HTTPS + self-signed cert | Already handled with `CertificateValidations.AllowAll` in fixture |
| `net10.0` TFM NuGet tooling quirk (`net10.0` → `net100`) | Use latest .NET 10 SDK; fallback to `net9.0` if needed |
