using System.Text;
using System.Text.Json;

namespace MyMobileApplication.Services
{
    public class AIService
    {
        private readonly HttpClient _httpClient;
        private string _ollamaUrl = "http://192.168.1.100:11434"; // User will change this to their PC's IP

        public AIService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
        }

        public void SetOllamaUrl(string url)
        {
            _ollamaUrl = url.TrimEnd('/');
        }

        public string GetOllamaUrl()
        {
            return _ollamaUrl;
        }

        /// <summary>
        /// Send message to Ollama AI (completely free, no API key needed!)
        /// </summary>
        public async Task<string> SendMessageAsync(string userMessage, string model = "llama3.2")
        {
            try
            {
                var requestBody = new
                {
                    model = model,
                    prompt = userMessage,
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_ollamaUrl}/api/generate", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Ollama error: {response.StatusCode} - {error}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
                
                if (result.TryGetProperty("response", out var responseText))
                {
                    return responseText.GetString() ?? "No response from AI";
                }

                return "Error: Invalid response format";
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Cannot connect to Ollama.: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"AI Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if Ollama is running and accessible
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_ollamaUrl}/api/tags");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get list of available models
        /// </summary>
        public async Task<List<string>> GetAvailableModelsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_ollamaUrl}/api/tags");
                
                if (!response.IsSuccessStatusCode)
                {
                    return new List<string> { "llama3.2" };
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(json);
                
                var models = new List<string>();
                if (result.TryGetProperty("models", out var modelsArray))
                {
                    foreach (var model in modelsArray.EnumerateArray())
                    {
                        if (model.TryGetProperty("name", out var name))
                        {
                            models.Add(name.GetString() ?? "unknown");
                        }
                    }
                }

                return models.Count > 0 ? models : new List<string> { "llama3.2" };
            }
            catch
            {
                return new List<string> { "llama3.2" };
            }
        }
    }
}
