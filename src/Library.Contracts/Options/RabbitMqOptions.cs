namespace Library.Contracts.Options;

public class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "library";
    public string Password { get; set; } = "library_pass";
    public string VirtualHost { get; set; } = "/";
    public string Exchange { get; set; } = "library.events";
    public string ReservationQueue { get; set; } = "reservation-worker";
    public string AuditQueue { get; set; } = "reservation-audit";
    public string DeadLetterExchange { get; set; } = "library.dlx";
    public string DeadLetterQueue { get; set; } = "reservation-worker.dlq";
}
