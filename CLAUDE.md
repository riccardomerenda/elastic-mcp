# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ElasticMCP is an open-source MCP (Model Context Protocol) server in C# that lets AI agents (Claude, Copilot, Cursor, ChatGPT) query, explore, and analyze data on Elasticsearch/OpenSearch using natural language — including semantic search on vector fields.

**Key insight**: The server does NOT call any LLM. It's a stateless translator between MCP protocol and the Elasticsearch API. All NLU happens in the client. Zero AI/LLM costs.

## Tech Stack

- **.NET 10** runtime
- **ModelContextProtocol** v1.1.0 (official C# MCP SDK, NuGet)
- **Elastic.Clients.Elasticsearch** (official .NET client)
- **Microsoft.Extensions.Hosting** for DI and lifecycle
- **xUnit + Testcontainers** for testing (ES in Docker for integration tests)
- **Transport**: stdio (local) + ASP.NET Core (remote HTTP via Streamable HTTP)

## Build & Run Commands

```bash
# Build
dotnet build ElasticMcp.slnx

# Run all tests
dotnet test --verbosity minimal

# Run a single test class
dotnet test --filter "FullyQualifiedName~SearchToolTests"

# Run stdio server locally
dotnet run --project src/ElasticMcp/ElasticMcp.csproj

# Run HTTP server
dotnet run --project src/ElasticMcp.Http/ElasticMcp.Http.csproj

# Package as dotnet tool
dotnet pack

# Docker build
docker build -t elastic-mcp .
```

## Architecture

```
MCP Host (Claude, VS Code, Cursor)
    │ JSON-RPC (stdio or Streamable HTTP)
    ▼
ElasticMCP Server
    ├── Tools/        → Actions the LLM invokes (search, semantic_search, aggregate, get_document, count, explain_query)
    ├── Resources/    → Read-only context (cluster health, indices, mapping, settings, sample docs)
    ├── Prompts/      → Reusable workflow templates (explore_index, log_analysis, semantic_qa)
    └── Services/     → Core logic
        ├── ElasticClientFactory   → Connection management
        ├── QueryBuilder           → Translates structured params → ES Query DSL
        ├── ResultFormatter        → Formats ES responses for LLM consumption
        └── SecurityGuard          → Enforces all guardrails
    │ HTTPS
    ▼
Elasticsearch / OpenSearch Cluster
```

**Flow**: User asks Claude → Claude reasons and calls a Tool with structured JSON params → ElasticMCP translates to Query DSL (pure C#) → executes against ES → returns results → Claude interprets for user.

## Repository Structure

```
ElasticMcp/
├── src/
│   ├── ElasticMcp/                    # Main project (console + stdio transport)
│   │   ├── Program.cs
│   │   ├── Configuration/             # ElasticMcpOptions and settings binding
│   │   ├── Tools/                     # One class per tool
│   │   ├── Resources/                 # One class per resource
│   │   ├── Prompts/                   # Prompt templates
│   │   └── Services/                  # Core services (QueryBuilder, SecurityGuard, etc.)
│   └── ElasticMcp.Http/              # HTTP server (ASP.NET Core, Streamable HTTP transport)
├── tests/
│   ├── ElasticMcp.Tests/             # Unit tests (QueryBuilder, SecurityGuard, ResultFormatter)
│   └── ElasticMcp.IntegrationTests/  # Integration tests (Testcontainers with real ES)
├── samples/                           # Docker-compose + sample datasets for demos
└── ElasticMcp.slnx
```

## MCP Concepts

- **Tools**: Actions the LLM can invoke — these execute queries and return results. Each tool receives structured parameters and translates them to ES Query DSL.
- **Resources**: Read-only context the LLM can read — URI-addressable (e.g., `elasticsearch://cluster/health`). No side effects.
- **Prompts**: Reusable templates for guided multi-step workflows.

## Security & Guardrails

This is security-critical — an LLM is accessing a data layer. All guardrails are enforced by `SecurityGuard`:

- **Read-only by default** — no write/delete without explicit config flag
- **Query timeout** — configurable, default 30s
- **Max result size** — upper limit on returned docs, default 100
- **Index allowlist/denylist** — patterns like `logs-*` allowed, `.security-*` denied
- **Field redaction** — hide sensitive fields (password, ssn, credit_card) from mappings and results
- **Audit logging** — structured log of every query the LLM executes
- **No cluster admin ops** in MVP — no delete index, no cluster settings changes

## Configuration

Configuration lives in `appsettings.json` under the `ElasticMcp` section, bound to `ElasticMcpOptions`. Supports environment variable overrides. Key settings: `Nodes`, `Authentication` (ApiKey or Basic), `DefaultIndex`, `ReadOnly`, `MaxResultSize`, `QueryTimeout`, `AllowedIndices`, `DeniedIndices`, `RedactedFields`, `SemanticSearch` (DefaultVectorField, DefaultK).

## Testing

- **Unit tests**: Core logic — QueryBuilder, SecurityGuard, ResultFormatter. No ES needed.
- **Integration tests**: Use **Testcontainers** to spin up a real ES instance in Docker. These validate actual query execution.
- **Manual testing**: Use **MCP Inspector** (free visual tool) to connect to the server, see exposed tools/resources, and invoke them without needing an LLM.

## Development Roadmap

- **v0.1 (Foundation)**: Project setup, CI/CD, ES connection, `search` + `count` tools, `cluster/health` + `cluster/indices` resources, stdio transport
- **v0.2 (Core Tools)**: Remaining tools + resources, full config, SecurityGuard with all guardrails
- **v0.3 (Semantic Search)**: `semantic_search` tool with kNN, prompt templates, NuGet + Docker publish
- **v0.4 (Polish)**: HTTP server, sample data, OpenSearch compatibility, first release
