using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure;
using Azure.AI.Inference;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

/// <summary>
///  API routes for the LLM backend.
///  POST /api/chat streams tokens back as Server-Sent Events (SSE).
/// </summary>
[ApiController]
[Route("api")]
public sealed class ApiController : ControllerBase
{
    private readonly ILogger<ApiController> _logger;
    private readonly ChatCompletionsClient          _client;

    public ApiController(ILogger<ApiController> logger,
                         IConfiguration cfg)
    {
        _logger     = logger;
        var endpoint = new Uri(cfg["AI:Endpoint"]);
        var credential = new AzureKeyCredential(cfg["AI:Key"]);
        var model = cfg["AI:Model"];
        _client     = new ChatCompletionsClient(
            endpoint,
            credential,
            new AzureAIInferenceClientOptions());;
       
    }

// one item in the content array
    public sealed record ContentItemDto(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string Text);

// one message
    public sealed record MsgDto(
        [property: JsonPropertyName("role")]    string Role,
        [property: JsonPropertyName("content")] List<ContentItemDto> Content);

// whole request body
    public sealed record ChatRequest(
        [property: JsonPropertyName("messages")] List<MsgDto> Messages,
        [property: JsonPropertyName("system")]   string?      System,
        [property: JsonPropertyName("tools")]    Dictionary<string,ToolDef>? Tools,
        [property: JsonPropertyName("runConfig")] JsonElement?              RunConfig,
        [property: JsonPropertyName("unstable_assistantMessageId")]
        string? AssistantMsgId);

    public sealed record ToolDef(
        [property: JsonPropertyName("parameters")] JsonElement Parameters);

    /*──────────────────────────────  /api/chat  ────────────────────────────────*/
    [HttpPost("chat")]
    public async Task Chat([FromBody] ChatRequest req)
    {
        Response.Headers.ContentType = "text/event-stream";
        var requestOptions = new ChatCompletionsOptions()
        {
            Messages =
            {
                new ChatRequestSystemMessage("You are a helpful assistant."),
                new ChatRequestUserMessage("How many feet are in a mile?"),
            },
        };

        StreamingResponse<StreamingChatCompletionsUpdate> response = await _client.CompleteStreamingAsync(requestOptions);

        StringBuilder contentBuilder = new();
        await foreach (StreamingChatCompletionsUpdate chatUpdate in response)
        {
            if (!string.IsNullOrEmpty(chatUpdate.ContentUpdate))
            {
                contentBuilder.Append(chatUpdate.ContentUpdate);
                await Response.WriteAsync($"data: {chatUpdate.ContentUpdate}\n\n");
                await Response.Body.FlushAsync();
            }
        }

        System.Console.WriteLine(contentBuilder.ToString());
        await Response.WriteAsync("data: [DONE]\n\n");
    }
    
    [HttpPost("chat2")]
    public async Task<IActionResult>  Chat2([FromBody] ChatRequest req)
    {
        var requestOptions = new ChatCompletionsOptions()
        {
            Messages =
            {
                new ChatRequestSystemMessage("You are a helpful assistant."),
                new ChatRequestUserMessage(req.Messages[0].Content[0].Text),
            },
        };

        StreamingResponse<StreamingChatCompletionsUpdate> response = await _client.CompleteStreamingAsync(requestOptions);

        StringBuilder contentBuilder = new();
        await foreach (StreamingChatCompletionsUpdate chatUpdate in response)
        {
            if (!string.IsNullOrEmpty(chatUpdate.ContentUpdate))
            {
                contentBuilder.Append(chatUpdate.ContentUpdate);
            }
        }
        string answer = contentBuilder.ToString();
        System.Console.WriteLine(contentBuilder.ToString());
        return Ok(new { text = answer });
    }
}
