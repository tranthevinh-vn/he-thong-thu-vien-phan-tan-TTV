namespace Library.Data.Entities;

public class ProcessedMessage
{
    public string Consumer { get; set; } = string.Empty;
    public Guid MessageId { get; set; }
    public DateTime ProcessedAt { get; set; }
}
