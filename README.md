<div align="center">

# ElasticMCP

### An MCP Server for Elasticsearch — built with .NET 10

[![CI](https://github.com/riccardomerenda/elastic-mcp/actions/workflows/ci.yml/badge.svg)](https://github.com/riccardomerenda/elastic-mcp/actions/workflows/ci.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512bd4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Elasticsearch 9](https://img.shields.io/badge/Elasticsearch-9.x-00bfb3?logo=elasticsearch&logoColor=white)](https://www.elastic.co/elasticsearch)
[![MCP Protocol](https://img.shields.io/badge/MCP-1.1-blue?logo=data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0IiBmaWxsPSJ3aGl0ZSI+PHBhdGggZD0iTTEyIDJMMiA3djEwbDEwIDUgMTAtNVY3TDEyIDJ6Ii8+PC9zdmc+)](https://modelcontextprotocol.io/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**Let any AI agent query, explore, and analyze your Elasticsearch data using natural language.**

[Getting Started](#getting-started) · [Tools & Resources](#tools--resources) · [Configuration](#configuration) · [Architecture](#how-it-works)

</div>

---

## Why ElasticMCP?

Elasticsearch's Query DSL is powerful — but complex. Writing bool queries, aggregations, and kNN searches requires deep knowledge of the syntax. **ElasticMCP bridges that gap.**

It's an [MCP](https://modelcontextprotocol.io/) server that gives AI agents — Claude, GitHub Copilot, Cursor, ChatGPT — the ability to search, aggregate, and explore your Elasticsearch clusters through structured tool calls. No copy-pasting JSON. No memorizing DSL.

> **Zero AI/LLM costs.** ElasticMCP does not call any LLM. The intelligence lives in the client. The server is a pure translator between the MCP protocol and the Elasticsearch API.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- An Elasticsearch 9.x cluster (local or remote)

### Option 1 — Install as .NET tool

```bash
dotnet tool install -g ElasticMcp
elastic-mcp
```

### Option 2 — Run from source

```bash
git clone https://github.com/riccardomerenda/elastic-mcp.git
cd elastic-mcp
dotnet run --project src/ElasticMcp/ElasticMcp.csproj
```

### Option 3 — Docker (HTTP transport)

```bash
docker build -t elastic-mcp .
docker run -p 8080:8080 -e ElasticMcp__Nodes__0=http://host.docker.internal:9200 elastic-mcp
```

### Connect to Claude Desktop

Add to your `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "elasticsearch": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/elastic-mcp/src/ElasticMcp/ElasticMcp.csproj"]
    }
  }
}
```

### Connect via HTTP (remote / Streamable HTTP)

Run the HTTP server:

```bash
dotnet run --project src/ElasticMcp.Http/ElasticMcp.Http.csproj
```

Then connect any MCP client to `http://localhost:5000/mcp` using Streamable HTTP transport.

### Connect to VS Code / Copilot

Add to `.vscode/mcp.json`:

```json
{
  "servers": {
    "elasticsearch": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/elastic-mcp/src/ElasticMcp/ElasticMcp.csproj"]
    }
  }
}
```

---

## Tools & Resources

### Tools — actions the AI can invoke

| Tool | Description |
|------|-------------|
| `search` | Full-text search with Lucene query syntax, pagination, and result size limits |
| `semantic_search` | kNN vector search on `dense_vector` fields — find similar documents by embedding |
| `count` | Count documents matching an optional query filter |
| `aggregate` | Run aggregations — terms, date_histogram, avg, sum, min, max, cardinality |
| `get_document` | Retrieve a single document by ID |
| `explain_query` | Show the generated Query DSL without executing it |

### Prompts — guided workflow templates

| Prompt | Description |
|--------|-------------|
| `explore_index` | Step-by-step exploration: mapping, samples, counts, aggregations |
| `log_analysis` | Identify error patterns, top services, time-based trends |
| `semantic_qa` | Answer questions over a vector-indexed knowledge base via kNN |

### Resources — read-only context for the AI

| Resource | URI | Description |
|----------|-----|-------------|
| Cluster Health | `elasticsearch://cluster/health` | Cluster status, node count, shard info |
| Cluster Indices | `elasticsearch://cluster/indices` | All indices with doc counts and store sizes |
| Index Mapping | `elasticsearch://index/{name}/mapping` | Field names, types, and capabilities |
| Index Settings | `elasticsearch://index/{name}/settings` | Shards, replicas, analyzers, refresh interval |
| Index Sample | `elasticsearch://index/{name}/sample` | Sample documents to understand data structure |

---

## Configuration

ElasticMCP is configured via `appsettings.json` in the project root:

```json
{
  "ElasticMcp": {
    "Nodes": ["https://localhost:9200"],
    "Authentication": {
      "Type": "ApiKey",
      "ApiKey": "your-api-key-here"
    },
    "ReadOnly": true,
    "MaxResultSize": 100,
    "QueryTimeout": "30s",
    "AllowedIndices": ["logs-*", "documents-*"],
    "DeniedIndices": [".security-*", ".kibana*"],
    "RedactedFields": ["password", "ssn", "credit_card"],
    "SemanticSearch": {
      "DefaultVectorField": "embedding",
      "DefaultK": 10,
      "DefaultNumCandidates": 100
    }
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `Nodes` | `["http://localhost:9200"]` | Elasticsearch node URLs |
| `Authentication.Type` | `None` | `None`, `ApiKey`, or `Basic` |
| `ReadOnly` | `true` | Prevent write/delete operations |
| `MaxResultSize` | `100` | Upper limit on returned documents |
| `QueryTimeout` | `30s` | Per-query timeout |
| `AllowedIndices` | `[]` | Index patterns the AI can access |
| `DeniedIndices` | `[]` | Index patterns blocked from the AI |
| `RedactedFields` | `[]` | Field names to strip from results and mappings |
| `SemanticSearch.DefaultVectorField` | `embedding` | Default dense_vector field for kNN search |
| `SemanticSearch.DefaultK` | `10` | Default number of nearest neighbors |
| `SemanticSearch.DefaultNumCandidates` | `100` | Default candidate pool size for kNN |

Environment variables override any setting (e.g., `ElasticMcp__Authentication__ApiKey`).

---

## How It Works

```
You: "Show me the top error types in production logs from the last hour"
 │
 ▼
┌─────────────────────────────────────────────────┐
│  AI Client (Claude, Copilot, Cursor, ChatGPT)   │
│  Reasons about the request, picks the right     │
│  tool, builds structured parameters             │
└────────────────────┬────────────────────────────┘
                     │  MCP (JSON-RPC over stdio
                     │  or Streamable HTTP)
                     ▼
┌─────────────────────────────────────────────────┐
│  ElasticMCP Server                              │
│                                                 │
│  Translates structured params → Query DSL       │
│  Executes against Elasticsearch                 │
│  Returns formatted results                      │
│                                                 │
│  No LLM calls. Pure C# translation layer.       │
└────────────────────┬────────────────────────────┘
                     │  HTTPS
                     ▼
┌─────────────────────────────────────────────────┐
│  Elasticsearch 9.x Cluster                      │
└─────────────────────────────────────────────────┘
```

The key insight: **ElasticMCP never calls an LLM.** It receives structured tool calls from the AI client, translates them into Elasticsearch Query DSL using pure C# logic, and returns the results. The AI client handles all the natural language understanding.

---

## Development

### Build & Test

```powershell
# Build
dotnet build ElasticMcp.slnx

# Run unit tests only (no Docker needed)
dotnet test tests/ElasticMcp.Tests/

# Run integration tests (requires Docker)
dotnet test tests/ElasticMcp.IntegrationTests/

# Run all tests
dotnet test ElasticMcp.slnx

# Run a single test class
dotnet test --filter "FullyQualifiedName~SearchToolTests"

# Full local pipeline (build + unit + integration)
.\test.ps1

# Unit tests only
.\test.ps1 -UnitOnly

# With code coverage
.\test.ps1 -Coverage
```

Integration tests use [Testcontainers](https://dotnet.testcontainers.org/) to spin up a real Elasticsearch 9.x instance in Docker — no mocks, no fakes.

### Try it with demo data

```powershell
# Start Elasticsearch
docker compose -f samples/docker-compose.yml up -d

# Seed sample data (logs, products, users, vector embeddings)
powershell -File samples/seed-data.ps1

# Start the HTTP server
dotnet run --project src/ElasticMcp.Http/ElasticMcp.Http.csproj

# Open MCP Inspector
npx @modelcontextprotocol/inspector
# Connect to http://localhost:5000/mcp using Streamable HTTP
```

### Project Structure

```
src/
├── ElasticMcp/                    # Main project (stdio transport)
│   ├── Program.cs
│   ├── Configuration/             # ElasticMcpOptions + SemanticSearchOptions
│   ├── Tools/                     # search, semantic_search, count, aggregate, get_document, explain_query
│   ├── Resources/                 # cluster health, indices, mapping, settings, sample
│   ├── Prompts/                   # explore_index, log_analysis, semantic_qa
│   └── Services/                  # ES client, SecurityGuard
├── ElasticMcp.Http/               # HTTP server (Streamable HTTP transport)

tests/
├── ElasticMcp.Tests/              # Unit tests (no Docker needed)
└── ElasticMcp.IntegrationTests/   # Integration tests (Testcontainers + real ES)

samples/
├── docker-compose.yml             # Single-node ES 9.x for local dev
└── seed-data.ps1                  # Seed demo data (logs, products, users, vectors)
```

---

## Roadmap

### v0.1 — Foundation ✅

Project setup, CI/CD, `search` + `count` tools, cluster health + indices resources, stdio transport, Testcontainers integration tests.

### v0.2 — Core Tools ✅

`aggregate`, `get_document`, `explain_query` tools. Index mapping, settings, and sample resources. SecurityGuard with index allowlist/denylist, field redaction, result size clamping, and audit logging.

### v0.3 — Semantic Search ✅

`semantic_search` tool with kNN vector search on `dense_vector` fields. Three prompt templates (`explore_index`, `log_analysis`, `semantic_qa`). HTTP server with Streamable HTTP transport. NuGet dotnet tool packaging. Dockerfile for containerized deployment. Demo environment with Docker Compose and sample data including vector embeddings.

### v0.4 — Polish & Launch

- OpenSearch compatibility
- Additional prompt templates
- Performance optimizations

---

## Tech Stack

| Component | Technology |
|-----------|------------|
| Runtime | .NET 10 |
| MCP SDK | [ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol) 1.1.0 |
| ES Client | [Elastic.Clients.Elasticsearch](https://www.nuget.org/packages/Elastic.Clients.Elasticsearch) 9.3.3 |
| Hosting | Microsoft.Extensions.Hosting |
| HTTP Transport | ASP.NET Core + ModelContextProtocol.AspNetCore |
| Testing | xUnit + Testcontainers |
| CI/CD | GitHub Actions |

---

## License

[MIT](LICENSE)
