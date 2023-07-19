
using System.Text.Json.Nodes;

namespace WindowsEventLogChatGPTPlugIn;

public record EventEntry
{
    public int Id { get; set; }
    public JsonNode Info { get; init; } = new JsonObject();
}