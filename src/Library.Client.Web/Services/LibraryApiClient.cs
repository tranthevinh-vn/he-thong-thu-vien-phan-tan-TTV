using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Library.Contracts.Dtos;

namespace Library.Client.Web.Services;

public class LibraryApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;

    public LibraryApiClient(HttpClient http) => _http = http;

    public async Task<PagedResult<BookDto>?> GetBooksAsync(string? q, int page, int pageSize, CancellationToken ct = default)
    {
        var url = $"/books?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(q))
        {
            url += $"&q={Uri.EscapeDataString(q)}";
        }

        return await _http.GetFromJsonAsync<PagedResult<BookDto>>(url, JsonOptions, ct);
    }

    public async Task<BookDetailDto?> GetBookAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/books/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BookDetailDto>(JsonOptions, ct);
    }
}
