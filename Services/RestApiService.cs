using System.Net.Http.Json;
using System.Text.Json;

namespace MyMobileApplication.Services;

public class RestApiService
{
    private readonly HttpClient _httpClient;
    private string _baseUrl = "https://jsonplaceholder.typicode.com"; // Default test API

    public RestApiService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public void SetBaseUrl(string url)
    {
        _baseUrl = url;
    }

    public string GetBaseUrl() => _baseUrl;

    public async Task<string> GetAsync(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public async Task<string> PostAsync(string endpoint, object data)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}{endpoint}", data);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object data)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}{endpoint}", data);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception ex)
        {
            throw new Exception($"POST request failed: {ex.Message}", ex);
        }
    }

    public async Task<string> PutAsync(string endpoint, object data)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}{endpoint}", data);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public async Task<string> DeleteAsync(string endpoint)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}{endpoint}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
