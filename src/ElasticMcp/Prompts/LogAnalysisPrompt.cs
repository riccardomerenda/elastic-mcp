using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace ElasticMcp.Prompts;

[McpServerPromptType]
public class LogAnalysisPrompt
{
    [McpServerPrompt(Name = "log_analysis")]
    [Description("Analyze log data in an Elasticsearch index. " +
                 "Identifies error patterns, top services, time distributions, and anomalies.")]
    public static IEnumerable<ChatMessage> LogAnalysis(
        [Description("The log index name or pattern (e.g. 'server-logs', 'logs-*')")] string index,
        [Description("Time range to analyze (e.g. 'last 24 hours', 'last 7 days')")] string? time_range = null)
    {
        var timeContext = time_range != null
            ? $" Focus on the {time_range} time range."
            : "";

        return [
            new ChatMessage(ChatRole.User,
                $"Analyze the logs in '{index}'.{timeContext} " +
                "I need to understand error patterns, service health, and any anomalies."),
            new ChatMessage(ChatRole.Assistant,
                "I'll perform a comprehensive log analysis. Here's my plan:\n\n" +
                "1. **Read the mapping** to identify log fields (timestamp, level, service, message, etc.)\n" +
                "2. **Count total logs** and count by severity level (error, warn, info, debug)\n" +
                "3. **Aggregate by log level** to see the error rate\n" +
                "4. **Aggregate by service** to find which services generate the most errors\n" +
                "5. **Date histogram** to see trends over time\n" +
                "6. **Search for errors** to examine specific error messages\n\n" +
                "Let me start the analysis."),
            new ChatMessage(ChatRole.User,
                "Proceed with the analysis. Provide:\n" +
                "- Overall health summary (error rate, total volume)\n" +
                "- Top error-producing services\n" +
                "- Time-based trends (any spikes?)\n" +
                "- Specific error messages that need attention\n" +
                "- Actionable recommendations")
        ];
    }
}
