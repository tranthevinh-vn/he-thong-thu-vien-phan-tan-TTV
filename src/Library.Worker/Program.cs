using Library.Contracts.Options;
using Library.Data;
using Library.Worker;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Thiếu cấu hình ConnectionStrings:Postgres");

builder.Services.AddDbContext<LibraryDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddHostedService<ReservationConsumer>();

try
{
    var host = builder.Build();
    await host.RunAsync();
}
finally
{
    Log.CloseAndFlush();
}
