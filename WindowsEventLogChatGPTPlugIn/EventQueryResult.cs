namespace WindowsEventLogChatGPTPlugIn;

public record EventQueryResult
{
    public List<EventEntry> Events { get; init; } = new();
    public bool HasMore { get; set; }
    public int PageNumber { get; init; }
    public bool HasPageSizeTruncated { get; set; }
    public int PageSize { get; set; }
}