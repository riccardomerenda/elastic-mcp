using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace ElasticMcp.Prompts;

[McpServerPromptType]
public class ExploreIndexPrompt
{
    [McpServerPrompt(Name = "explore_index")]
    [Description("A guided workflow to explore and understand an Elasticsearch index. " +
                 "Reads mappings, samples documents, analyzes field distributions, and summarizes findings.")]
    public static IEnumerable<ChatMessage> ExploreIndex(
        [Description("The Elasticsearch index name to explore")] string index)
    {
        return [
            new ChatMessage(ChatRole.User,
                $"I want to understand the Elasticsearch index '{index}'. " +
                "Please explore it step by step and give me a comprehensive summary."),
            new ChatMessage(ChatRole.Assistant,
                $"I'll explore the '{index}' index systematically. Let me follow these steps:\n\n" +
                "1. **Check cluster health** to ensure the cluster is available\n" +
                "2. **Read the index mapping** to understand field names, types, and structure\n" +
                "3. **Get sample documents** to see what the actual data looks like\n" +
                "4. **Count total documents** to understand the index size\n" +
                "5. **Run key aggregations** on interesting fields to understand data distributions\n\n" +
                "Let me start by reading the mapping and sample documents."),
            new ChatMessage(ChatRole.User,
                "Go ahead. After exploring, provide:\n" +
                "- A summary of what this index contains\n" +
                "- Key fields and their types\n" +
                "- Data volume and distribution insights\n" +
                "- Suggested queries a user might want to run")
        ];
    }
}
