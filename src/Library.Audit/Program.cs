using Library.Contracts.Options;
using Library.Data;
using Library.Audit;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Thiếu cấu hình ConnectionStrings:Postgres");

builder.Services.AddDbContext<LibraryDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddHostedService<AuditConsumer>();

var host = builder.Build();
host.Run();
