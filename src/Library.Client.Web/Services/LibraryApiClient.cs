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

    public async Task<List<UserDto>?> GetUsersAsync(CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<List<UserDto>>("/users", JsonOptions, ct);
    }

    public async Task<ReservationDto?> CreateReservationAsync(Guid userId, Guid bookId, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(
            "/reservations",
            new CreateReservationRequest { UserId = userId, BookId = bookId },
            JsonOptions,
            ct);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ReservationDto>(JsonOptions, ct);
    }

    public async Task<ReservationDto?> GetReservationAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/reservations/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ReservationDto>(JsonOptions, ct);
    }

    public async Task<List<ReservationDto>?> GetUserReservationsAsync(Guid userId, string? status = null, CancellationToken ct = default)
    {
        var url = $"/users/{userId}/reservations";
        if (!string.IsNullOrWhiteSpace(status))
        {
            url += $"?status={Uri.EscapeDataString(status)}";
        }

        return await _http.GetFromJsonAsync<List<ReservationDto>>(url, JsonOptions, ct);
    }

    public async Task<ReservationDto?> BorrowAsync(Guid id, CancellationToken ct = default)
    {
        return await PostReservationActionAsync($"/reservations/{id}/borrow", ct);
    }

    public async Task<ReservationDto?> ReturnAsync(Guid id, CancellationToken ct = default)
    {
        return await PostReservationActionAsync($"/reservations/{id}/return", ct);
    }

    private async Task<ReservationDto?> PostReservationActionAsync(string url, CancellationToken ct)
    {
        var response = await _http.PostAsync(url, content: null, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ReservationDto>(JsonOptions, ct);
    }
}
