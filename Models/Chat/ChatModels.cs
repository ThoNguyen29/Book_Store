namespace Book_Store.Models.Chat;

public class ChatAskRequest
{
    public string? Message { get; set; }
    public List<ChatHistoryItem>? History { get; set; }
}

public class ChatHistoryItem
{
    public string Role { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class ChatAskResponse
{
    public string Reply { get; set; } = string.Empty;
}
