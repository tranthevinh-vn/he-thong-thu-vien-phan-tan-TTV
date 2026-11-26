using System.Text.Json.Serialization;
using Library.Api.Endpoints;
using Library.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=localhost;Port=5432;Database=library_db;Username=library;Password=library_pass";

builder.Services.AddDbContext<LibraryDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Ok(new { name = "Library API", status = "ok" }));
app.MapBooksEndpoints();

app.Run();
