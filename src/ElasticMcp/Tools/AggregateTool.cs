using System.ComponentModel;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using ElasticMcp.Services;
using ModelContextProtocol.Server;

namespace ElasticMcp.Tools;

[McpServerToolType]
public class AggregateTool
{
    [McpServerTool(Name = "aggregate")]
    [Description("Run an aggregation on an Elasticsearch index. Supports terms, date_histogram, avg, sum, min, max, and cardinality.")]
    public static async Task<string> Aggregate(
        ElasticsearchClient client,
        SecurityGuard guard,
        [Description("The Elasticsearch index name or pattern")] string index,
        [Description("Aggregation type: terms, date_histogram, avg, sum, min, max, cardinality")] string aggregation_type,
        [Description("Field to aggregate on")] string field,
        [Description("Optional query string to filter documents before aggregating")] string? query = null,
        [Description("Number of buckets to return for terms/date_histogram (default: 10)")] int size = 10,
        [Description("Calendar interval for date_histogram (e.g. '1d', '1h', '1w')")] string? interval = null,
        CancellationToken cancellationToken = default)
    {
        var accessError = guard.ValidateIndexAccess(index);
        if (accessError != null) return accessError;

        guard.AuditToolCall("aggregate", index, query);

        var aggType = aggregation_type.ToLowerInvariant();
        var aggregation = BuildAggregation(aggType, field, size, interval);

        var response = await client.SearchAsync<JsonElement>(s =>
        {
            s.Indices(index).Size(0);

            if (!string.IsNullOrWhiteSpace(query))
                s.Query(q => q.QueryString(qs => qs.Query(query)));

            s.Aggregations(new Dictionary<string, Aggregation> { ["result"] = aggregation });
        }, cancellationToken);

        if (!response.IsValidResponse)
        {
            return $"Aggregation failed: {response.ElasticsearchServerError?.Error?.Reason ?? "Unknown error"}";
        }

        // Extract raw aggregation result from the response JSON
        var resultJson = ExtractAggregationFromResponse(response);

        var result = new
        {
            index,
            aggregation_type,
            field,
            total_docs = response.Total,
            aggregation = resultJson
        };

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    private static Aggregation BuildAggregation(string type, string field, int size, string? interval)
    {
        return type switch
        {
            "terms" => new Aggregation { Terms = new TermsAggregation { Field = field, Size = size } },
            "date_histogram" => new Aggregation
            {
                DateHistogram = new DateHistogramAggregation
                {
                    Field = field,
                    CalendarInterval = MapCalendarInterval(interval ?? "1d")
                }
            },
            "avg" => new Aggregation { Avg = new AverageAggregation { Field = field } },
            "sum" => new Aggregation { Sum = new SumAggregation { Field = field } },
            "min" => new Aggregation { Min = new MinAggregation { Field = field } },
            "max" => new Aggregation { Max = new MaxAggregation { Field = field } },
            "cardinality" => new Aggregation { Cardinality = new CardinalityAggregation { Field = field } },
            _ => throw new ArgumentException(
                $"Unsupported aggregation type: {type}. Supported: terms, date_histogram, avg, sum, min, max, cardinality.")
        };
    }

    private static CalendarInterval MapCalendarInterval(string interval) => interval switch
    {
        "1m" or "minute" => CalendarInterval.Minute,
        "1h" or "hour" => CalendarInterval.Hour,
        "1d" or "day" => CalendarInterval.Day,
        "1w" or "week" => CalendarInterval.Week,
        "1M" or "month" => CalendarInterval.Month,
        "1q" or "quarter" => CalendarInterval.Quarter,
        "1y" or "year" => CalendarInterval.Year,
        _ => CalendarInterval.Day
    };

    private static object ExtractAggregationFromResponse(SearchResponse<JsonElement> response)
    {
        // The response has an Aggregations property which is an AggregateDictionary
        // We serialize the whole response and extract the "aggregations" section
        try
        {
            var responseJson = JsonSerializer.Serialize(response);
            using var doc = JsonDocument.Parse(responseJson);

            if (doc.RootElement.TryGetProperty("aggregations", out var aggsElement))
            {
                if (aggsElement.TryGetProperty("result", out var resultElement))
                    return resultElement;
            }

            // Fallback: return whatever the aggregations object gives us
            if (response.Aggregations != null)
                return response.Aggregations;
        }
        catch
        {
            // Ignore serialization errors
        }

        return new { error = "Could not extract aggregation result" };
    }
}
