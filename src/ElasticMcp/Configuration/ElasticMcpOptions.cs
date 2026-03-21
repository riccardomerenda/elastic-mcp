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
    public List<string> RedactedFields { get; set; } = [];
    public SemanticSearchOptions SemanticSearch { get; set; } = new();
}

public class SemanticSearchOptions
{
    public string DefaultVectorField { get; set; } = "embedding";
    public int DefaultK { get; set; } = 10;
    public int DefaultNumCandidates { get; set; } = 100;
}

public class AuthenticationOptions
{
    public string Type { get; set; } = "None";
    public string? ApiKey { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}
