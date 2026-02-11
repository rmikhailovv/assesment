namespace Assesment.Domain;

public class ExceptionJournal
{
    public long Id { get; set; }
    public long EventId { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string ExceptionType { get; set; }
    public required string Message { get; set; }
    public required string StackTrace { get; set; }
    public required string QueryParameters { get; set; }
    public required string BodyParameters { get; set; }
    public required string Endpoint { get; set; }
}
