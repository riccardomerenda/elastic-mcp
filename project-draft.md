# ElasticMCP — MCP Server for Elasticsearch in C#

> Project Draft — v0.1

---

## Elevator Pitch

An open-source MCP Server in C# that lets any AI agent (Claude, Copilot, Cursor, ChatGPT…) query, explore, and analyze data on Elasticsearch and OpenSearch using natural language — including semantic search on vector fields.

---

## The Problem

Today, a developer or data analyst who wants to explore data on Elasticsearch has to:

- Know Elasticsearch's Query DSL (not trivial)
- Write complex JSON for bool queries, aggregations, kNN search
- Use Kibana or Dev Tools, copy/paste results, interpret them manually
- For semantic search: know which field contains the embeddings, which model was used, what kNN parameters to set

With an MCP Server, the user simply asks the AI:
*"Find documents most similar to 'connection refused by server' in the production logs index from the last week"*
and the agent translates the request into correct Elasticsearch queries, executes them, and returns interpreted results.

---

## Target Users

- **.NET developers** working with Elasticsearch who want to integrate AI agents into their workflows
- **DevOps / SRE teams** analyzing logs indexed on Elasticsearch
- **Data analysts** exploring data without mastering the Query DSL
- **Teams building RAG pipelines** who want to expose their vector store to an LLM via a standard protocol

---

## How It Works

**The MCP Server does NOT call any LLM. It doesn't need to.**

The intelligence lives entirely in the client (Claude, Copilot, etc.). The flow is:

1. **User** writes to Claude: *"Show me the top 5 error types in production logs from the last hour"*
2. **Claude** reads the tool descriptions exposed by ElasticMCP at connection time, reasons about the request, and decides to call the `aggregate` tool with the right parameters
3. **Claude** sends a structured JSON-RPC call to the MCP Server:
   ```json
   {
     "method": "tools/call",
     "params": {
       "name": "aggregate",
       "arguments": {
         "index": "logs-prod",
         "aggregation_type": "terms",
         "field": "error.type",
         "time_range": "last:1h",
         "size": 5
       }
     }
   }
   ```
4. **ElasticMCP** receives the structured call, translates it into Elasticsearch Query DSL (pure C# code, no LLM involved), executes it against the cluster, and returns the results
5. **Claude** interprets the results and responds to the user in natural language

The server is a **translator between the MCP protocol and the Elasticsearch API**. All the natural language understanding happens in the client — which is an LLM that someone else already hosts and manages.

This means: **zero AI/LLM costs.** You only write C# code that receives structured parameters and talks to Elasticsearch.

---

## Features — Phase 1 (MVP)

### Tools (actions the LLM can invoke)

| Tool | Description | Key Parameters |
|------|-------------|----------------|
| `search` | Full-text search on an index | `index`, `query`, `size`, `from` |
| `semantic_search` | Similarity search via kNN | `index`, `query_text`, `vector_field`, `k` |
| `aggregate` | Run aggregations (terms, date_histogram, avg, etc.) | `index`, `aggregation_type`, `field`, `filters` |
| `get_document` | Retrieve a document by ID | `index`, `document_id` |
| `count` | Count documents with optional filter | `index`, `query` |
| `explain_query` | Show the generated Query DSL without executing it | `index`, `description` |

### Resources (context readable by the LLM)

| Resource | Description |
|----------|-------------|
| `elasticsearch://cluster/health` | Cluster status (green/yellow/red, nodes, shards) |
| `elasticsearch://cluster/indices` | Index list with doc count, size, status |
| `elasticsearch://index/{name}/mapping` | Index schema/mapping (fields, types, analyzers) |
| `elasticsearch://index/{name}/settings` | Index settings (replicas, shards, custom analyzers) |
| `elasticsearch://index/{name}/sample` | Sample of N documents to understand data structure |

### Prompts (reusable templates)

| Prompt | Description |
|--------|-------------|
| `explore_index` | Guided workflow: analyze mapping → sample data → suggest useful queries |
| `log_analysis` | Template for log analysis: frequent errors, time trends, anomalies |
| `semantic_qa` | Template for semantic Q&A: search documents and answer with citations |

---

## Features — Phase 2 (Post-MVP)

- **Hybrid search**: full-text + kNN combination with score fusion (RRF)
- **Index management**: create indices with mappings, reindex, aliases
- **Ingest pipeline inspection**: show configured ingest pipelines
- **Watcher/alerting**: query active alerting rules
- **Multi-cluster**: support for multiple Elasticsearch clusters with named connections
- **Query history**: resource exposing the last N executed queries with performance metrics
- **On-the-fly embedding**: optional integration with a local embedding model to generate vectors from query text

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                    MCP Host                          │
│          (Claude, VS Code, Cursor, ...)              │
└──────────────────────┬──────────────────────────────┘
                       │ JSON-RPC
                       │ (stdio or Streamable HTTP)
┌──────────────────────▼──────────────────────────────┐
│                  ElasticMCP Server                    │
│                                                      │
│  ┌─────────────┐  ┌──────────────┐  ┌────────────┐  │
│  │   Tools      │  │  Resources   │  │  Prompts   │  │
│  │             │  │              │  │            │  │
│  │ search      │  │ health       │  │ explore    │  │
│  │ semantic    │  │ indices      │  │ log_analysis│ │
│  │ aggregate   │  │ mapping      │  │ semantic_qa│  │
│  │ get_doc     │  │ settings     │  │            │  │
│  │ count       │  │ sample       │  │            │  │
│  │ explain     │  │              │  │            │  │
│  └──────┬──────┘  └──────┬───────┘  └────────────┘  │
│         │                │                           │
│  ┌──────▼────────────────▼───────┐                   │
│  │     Elasticsearch Client      │                   │
│  │    (Elastic.Clients.          │                   │
│  │     Elasticsearch)            │                   │
│  └──────────────┬────────────────┘                   │
│                 │                                    │
│  ┌──────────────▼────────────────┐                   │
│  │     Configuration             │                   │
│  │  · Connection string          │                   │
│  │  · Auth (API key / Basic)     │                   │
│  │  · Default index patterns     │                   │
│  │  · Read-only mode             │                   │
│  │  · Max result size            │                   │
│  └───────────────────────────────┘                   │
└──────────────────────┬──────────────────────────────┘
                       │ HTTPS
┌──────────────────────▼──────────────────────────────┐
│              Elasticsearch / OpenSearch               │
│                   Cluster                            │
└─────────────────────────────────────────────────────┘
```

---

## Tech Stack

| Component | Technology |
|-----------|------------|
| **Runtime** | .NET 10 |
| **MCP SDK** | `ModelContextProtocol` v1.0 (official NuGet) |
| **ES Client** | `Elastic.Clients.Elasticsearch` (official .NET client) |
| **Hosting** | `Microsoft.Extensions.Hosting` |
| **Transport** | stdio (local) + ASP.NET Core (remote HTTP) |
| **Config** | `appsettings.json` + environment variables |
| **Testing** | xUnit + Testcontainers (ES in Docker for integration tests) |
| **CI/CD** | GitHub Actions |
| **Packaging** | NuGet (dotnet tool) + Docker image |

---

## Security & Guardrails

A critical aspect — the LLM must not be able to cause damage:

- **Read-only by default**: no write/delete operations without an explicit configuration flag
- **Query timeout**: configurable timeout (default 30s) to prevent expensive queries
- **Max result size**: upper limit on returned documents (default 100)
- **Index allowlist/denylist**: patterns to restrict accessible indices (e.g. `logs-*`, exclude `.security-*`)
- **Field redaction**: option to hide sensitive fields from mappings and results
- **Audit log**: structured log of every query executed by the LLM
- **No cluster admin**: no tools for destructive operations (delete index, update cluster settings, etc.) in the MVP

---

## Configuration

```json
{
  "ElasticMcp": {
    "Nodes": ["https://localhost:9200"],
    "Authentication": {
      "Type": "ApiKey",
      "ApiKey": "${ES_API_KEY}"
    },
    "DefaultIndex": "logs-*",
    "ReadOnly": true,
    "MaxResultSize": 100,
    "QueryTimeout": "30s",
    "AllowedIndices": ["logs-*", "documents-*", "products-*"],
    "DeniedIndices": [".security-*", ".kibana*"],
    "RedactedFields": ["password", "ssn", "credit_card"],
    "SemanticSearch": {
      "DefaultVectorField": "embedding",
      "DefaultK": 10
    }
  }
}
```

---

## Usage

### As a local process (stdio)

```bash
# Install as a dotnet tool
dotnet tool install -g ElasticMcp

# Configure in Claude Desktop (claude_desktop_config.json)
{
  "mcpServers": {
    "elasticsearch": {
      "command": "elastic-mcp",
      "args": ["--config", "./elastic-mcp.json"]
    }
  }
}

# Configure in VS Code (.vscode/mcp.json)
{
  "servers": {
    "elasticsearch": {
      "type": "stdio",
      "command": "elastic-mcp",
      "args": ["--config", "./elastic-mcp.json"]
    }
  }
}
```

### As a remote server (HTTP)

```bash
# Via Docker
docker run -p 8080:8080 \
  -e ES_NODES=https://my-cluster:9200 \
  -e ES_API_KEY=my-key \
  riccardomerenda/elastic-mcp

# The MCP endpoint will be available at http://localhost:8080/mcp
```

---

## Testing During Development

No LLM API costs required. Three testing tiers:

1. **MCP Inspector** (free) — Visual tool to connect to your server, see exposed tools/resources, and invoke them manually. Perfect for the development phase, no LLM needed at all.

2. **Claude Desktop App** — With your existing Claude subscription, add the server config and test the full natural language experience locally. Write "show me the cluster health" and Claude will call your `cluster/health` resource.

3. **VS Code + Copilot** — If you use Copilot, configure the server in `.vscode/mcp.json` and the tools become available in Copilot's Agent mode.

---

## Repository Structure

```
ElasticMcp/
├── src/
│   ├── ElasticMcp/                    # Main project (console + stdio)
│   │   ├── Program.cs
│   │   ├── Configuration/
│   │   │   └── ElasticMcpOptions.cs
│   │   ├── Tools/
│   │   │   ├── SearchTool.cs
│   │   │   ├── SemanticSearchTool.cs
│   │   │   ├── AggregateTool.cs
│   │   │   ├── GetDocumentTool.cs
│   │   │   ├── CountTool.cs
│   │   │   └── ExplainQueryTool.cs
│   │   ├── Resources/
│   │   │   ├── ClusterHealthResource.cs
│   │   │   ├── IndicesResource.cs
│   │   │   ├── MappingResource.cs
│   │   │   ├── SettingsResource.cs
│   │   │   └── SampleResource.cs
│   │   ├── Prompts/
│   │   │   ├── ExploreIndexPrompt.cs
│   │   │   ├── LogAnalysisPrompt.cs
│   │   │   └── SemanticQaPrompt.cs
│   │   └── Services/
│   │       ├── ElasticClientFactory.cs
│   │       ├── QueryBuilder.cs         # Translates parameters into Query DSL
│   │       ├── ResultFormatter.cs      # Formats ES results for the LLM
│   │       └── SecurityGuard.cs        # Enforces guardrails
│   │
│   └── ElasticMcp.Http/               # HTTP server (ASP.NET Core)
│       └── Program.cs
│
├── tests/
│   ├── ElasticMcp.Tests/              # Unit tests
│   └── ElasticMcp.IntegrationTests/   # Integration tests with Testcontainers
│
├── samples/
│   ├── docker-compose.yml             # ES + ElasticMcp for quick demo
│   ├── sample-data/                   # Example datasets to index
│   └── README.md                      # Quick start guide
│
├── .github/
│   └── workflows/
│       ├── ci.yml
│       └── release.yml
│
├── Dockerfile
├── README.md
├── CHANGELOG.md
├── CONTRIBUTING.md
├── LICENSE (MIT)
└── ElasticMcp.sln
```

---

## Development Roadmap

### v0.1 — Foundation (Week 1–2)
- Project setup, CI/CD, Testcontainers
- Base Elasticsearch connection
- `search` and `count` tools
- `cluster/health` and `cluster/indices` resources
- Working stdio transport
- Testing with MCP Inspector

### v0.2 — Core Tools (Week 3–4)
- `aggregate`, `get_document`, `explain_query` tools
- `mapping`, `settings`, `sample` resources
- Full configuration (allowlist, denylist, field redaction)
- SecurityGuard with all guardrails
- Complete unit test coverage

### v0.3 — Semantic Search (Week 5–6)
- `semantic_search` tool with kNN
- Prompt templates (`explore_index`, `log_analysis`, `semantic_qa`)
- Comprehensive README with demo GIF
- NuGet publish as dotnet tool
- Docker image

### v0.4 — Polish & Launch (Week 7–8)
- HTTP server with ASP.NET Core
- Sample data + docker-compose for onboarding
- OpenSearch compatibility
- Blog post / demo video
- First GitHub release tag

---

## Why This Project Stands Out

1. **Underserved niche**: an MCP server for Elasticsearch in C# — virtually nonexistent today
2. **Bridges all your skills**: .NET, Elasticsearch, semantic search, RAG, AI agents
3. **Immediately useful**: anyone working with ES can use it right away
4. **Demonstrates architectural thinking**: security, configurability, testing, CI/CD, packaging
5. **Connects to the AI ecosystem**: shows you're not "just" a .NET dev but understand the emerging protocol powering AI agents
6. **Scales gracefully**: the project can grow with new features without rewrites
7. **Complements your portfolio**: logq (log analysis CLI) + FamilyFinance (.NET full-stack) + ElasticMCP (AI + search) cover three distinct areas

---

## Possible Future Extensions

- **logq plugin**: connect logq as a data source via MCP
- **Natural language Kibana**: build dashboard-like visualizations from text descriptions
- **Full RAG pipeline**: document indexing + chunking + embedding + retrieval all via MCP
- **Anomaly detection**: expose Elasticsearch's ML features to the LLM
- **Cross-cluster search**: federated queries across multiple clusters

---

## Cost Summary

| Item | Cost |
|------|------|
| .NET 10 SDK | Free |
| MCP C# SDK (NuGet) | Free |
| Elastic .NET Client (NuGet) | Free |
| Elasticsearch (Docker, local dev) | Free |
| Testcontainers (integration tests) | Free |
| GitHub repo + Actions (CI/CD) | Free |
| NuGet.org (package publishing) | Free |
| Docker Hub (public image) | Free |
| MCP Inspector (testing) | Free |
| Claude Desktop (testing with LLM) | Included in existing subscription |
| **Total** | **$0** |