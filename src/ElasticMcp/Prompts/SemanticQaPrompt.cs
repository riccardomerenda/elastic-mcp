using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace ElasticMcp.Prompts;

[McpServerPromptType]
public class SemanticQaPrompt
{
    [McpServerPrompt(Name = "semantic_qa")]
    [Description("Answer a question using semantic search over a vector-indexed knowledge base. " +
                 "Finds relevant documents via kNN and synthesizes an answer.")]
    public static IEnumerable<ChatMessage> SemanticQa(
        [Description("The index containing vector embeddings")] string index,
        [Description("The question to answer")] string question,
        [Description("The dense_vector field name (default: 'embedding')")] string? vector_field = null)
    {
        var fieldNote = vector_field != null
            ? $" The vector field is '{vector_field}'."
            : "";

        return [
            new ChatMessage(ChatRole.User,
                $"Answer this question using the knowledge base in '{index}': {question}"),
            new ChatMessage(ChatRole.Assistant,
                $"I'll answer your question using semantic search.{fieldNote} Here's my approach:\n\n" +
                "1. **Read the index mapping** to identify the vector field and content fields\n" +
                "2. **Get sample documents** to understand the document structure\n" +
                "3. **Perform semantic search** with a query vector matching your question\n" +
                "4. **Synthesize an answer** from the most relevant documents\n\n" +
                "Note: I'll need to generate an embedding vector for your question to perform " +
                "the semantic search. Let me start by examining the index structure."),
            new ChatMessage(ChatRole.User,
                "Go ahead. When presenting the answer:\n" +
                "- Cite the specific documents that support your answer\n" +
                "- Include relevance scores so I can judge confidence\n" +
                "- If the knowledge base doesn't contain relevant information, say so clearly\n" +
                "- Suggest follow-up questions if appropriate")
        ];
    }
}
