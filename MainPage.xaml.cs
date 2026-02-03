using MyMobileApplication.Services;
using MyMobileApplication.Models;
using System.Text.Json;

namespace MyMobileApplication
{
    public partial class MainPage : ContentPage
    {
        private readonly RestApiService _restApiService;
        private readonly WebSocketService _webSocketService;
        private readonly AIService _aiService;
        private readonly List<string> _messages = new();
        private readonly List<string> _aiChatHistory = new();

        public MainPage(RestApiService restApiService, WebSocketService webSocketService, AIService aiService)
        {
            InitializeComponent();
            
            _restApiService = restApiService;
            _webSocketService = webSocketService;
            _aiService = aiService;

            // Set default values
            RestUrlEntry.Text = _restApiService.GetBaseUrl();
            RestEndpointEntry.Text = "/posts/1";
            WebSocketUrlEntry.Text = _webSocketService.GetServerUrl();
            OllamaUrlEntry.Text = _aiService.GetOllamaUrl();

            // Subscribe to WebSocket events
            _webSocketService.MessageReceived += OnWebSocketMessageReceived;
            _webSocketService.ConnectionStatusChanged += OnWebSocketStatusChanged;
        }

        // REST API Methods
        private async void OnGetClicked(object? sender, EventArgs e)
        {
            try
            {
                RestResponseLabel.Text = "Loading...";
                
                if (!string.IsNullOrWhiteSpace(RestUrlEntry.Text))
                {
                    _restApiService.SetBaseUrl(RestUrlEntry.Text.Trim());
                }

                var endpoint = string.IsNullOrWhiteSpace(RestEndpointEntry.Text) 
                    ? "/posts/1" 
                    : RestEndpointEntry.Text.Trim();

                var response = await _restApiService.GetAsync(endpoint);
                
                try
                {
                    var jsonDoc = JsonDocument.Parse(response);
                    var formatted = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    RestResponseLabel.Text = formatted;
                }
                catch
                {
                    RestResponseLabel.Text = response;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"GET Request failed: {ex.Message}", "OK");
                RestResponseLabel.Text = $"Error: {ex.Message}";
            }
        }

        private async void OnPostClicked(object? sender, EventArgs e)
        {
            try
            {
                RestResponseLabel.Text = "Posting...";
                
                if (!string.IsNullOrWhiteSpace(RestUrlEntry.Text))
                {
                    _restApiService.SetBaseUrl(RestUrlEntry.Text.Trim());
                }

                var endpoint = string.IsNullOrWhiteSpace(RestEndpointEntry.Text) 
                    ? "/posts" 
                    : RestEndpointEntry.Text.Trim();

                var newPost = new
                {
                    title = "Test Post from MAUI App",
                    body = "This is a test post created from the mobile application",
                    userId = 1
                };

                var response = await _restApiService.PostAsync(endpoint, newPost);
                
                try
                {
                    var jsonDoc = JsonDocument.Parse(response);
                    var formatted = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    RestResponseLabel.Text = formatted;
                }
                catch
                {
                    RestResponseLabel.Text = response;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"POST Request failed: {ex.Message}", "OK");
                RestResponseLabel.Text = $"Error: {ex.Message}";
            }
        }

        private async void OnConnectClicked(object? sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(WebSocketUrlEntry.Text))
                {
                    _webSocketService.SetServerUrl(WebSocketUrlEntry.Text.Trim());
                }

                await _webSocketService.ConnectAsync();
                
                ConnectButton.IsEnabled = false;
                DisconnectButton.IsEnabled = true;
                SendButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Connection failed: {ex.Message}", "OK");
            }
        }

        private async void OnDisconnectClicked(object? sender, EventArgs e)
        {
            try
            {
                await _webSocketService.DisconnectAsync();
                
                ConnectButton.IsEnabled = true;
                DisconnectButton.IsEnabled = false;
                SendButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Disconnect failed: {ex.Message}", "OK");
            }
        }

        private async void OnSendMessageClicked(object? sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(MessageEntry.Text))
                {
                    await DisplayAlertAsync("Error", "Please enter a message", "OK");
                    return;
                }

                await _webSocketService.SendMessageAsync(MessageEntry.Text);
                AddMessage($"[SENT] {MessageEntry.Text}");
                MessageEntry.Text = string.Empty;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Send failed: {ex.Message}", "OK");
            }
        }

        // WebSocket Event Handlers
        private void OnWebSocketMessageReceived(object? sender, string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AddMessage($"[RECEIVED] {message}");
            });
        }

        private void OnWebSocketStatusChanged(object? sender, string status)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ConnectionStatusLabel.Text = $"Status: {status}";
                
                if (status.Contains("Connected") && !status.Contains("Disconnected"))
                {
                    ConnectionStatusLabel.TextColor = Colors.Green;
                }
                else if (status.Contains("Disconnected") || status.Contains("failed"))
                {
                    ConnectionStatusLabel.TextColor = Colors.Red;
                    ConnectButton.IsEnabled = true;
                    DisconnectButton.IsEnabled = false;
                    SendButton.IsEnabled = false;
                }
                else
                {
                    ConnectionStatusLabel.TextColor = Colors.Orange;
                }
            });
        }

        private void AddMessage(string message)
        {
            _messages.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            
            // Keep only last 50 messages
            if (_messages.Count > 50)
            {
                _messages.RemoveAt(0);
            }
            
            MessagesLabel.Text = string.Join("\n\n", _messages);
        }

        private async void OnTestConnectionClicked(object? sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(OllamaUrlEntry.Text))
                {
                    _aiService.SetOllamaUrl(OllamaUrlEntry.Text.Trim());
                }

                TestConnectionButton.IsEnabled = false;
                TestConnectionButton.Text = "Testing...";
                AIConnectionStatusLabel.Text = "Testing connection...";
                AIConnectionStatusLabel.TextColor = Colors.Orange;

                var isConnected = await _aiService.TestConnectionAsync();

                if (isConnected)
                {
                    AIConnectionStatusLabel.Text = "✅ Connected! Ollama is ready. You can start chatting!";
                    AIConnectionStatusLabel.TextColor = Colors.Green;
                    
                    // Try to get available models
                    var models = await _aiService.GetAvailableModelsAsync();
                    if (models.Count > 0)
                    {
                        AIConnectionStatusLabel.Text = $"✅ Connected! Available models: {string.Join(", ", models)}";
                    }
                }
                else
                {
                    AIConnectionStatusLabel.Text = "❌ Cannot connect.";
                    AIConnectionStatusLabel.TextColor = Colors.Red;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Connection Error", ex.Message, "OK");
                AIConnectionStatusLabel.Text = "❌ Connection failed";
                AIConnectionStatusLabel.TextColor = Colors.Red;
            }
            finally
            {
                TestConnectionButton.IsEnabled = true;
                TestConnectionButton.Text = "Test Connection";
            }
        }

        private async void OnSendToAIClicked(object? sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(AIChatEntry.Text))
                {
                    await DisplayAlertAsync("Error", "Please enter a message", "OK");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(OllamaUrlEntry.Text))
                {
                    _aiService.SetOllamaUrl(OllamaUrlEntry.Text.Trim());
                }

                var userMessage = AIChatEntry.Text;
                
                SendAIButton.IsEnabled = false;
                SendAIButton.Text = "AI is thinking...";

                AddAIMessage($"You: {userMessage}");

                // Get AI response from Ollama (free, no API key!)
                var aiResponse = await _aiService.SendMessageAsync(userMessage);

                AddAIMessage($"AI: {aiResponse}");

                AIChatEntry.Text = string.Empty;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
                AddAIMessage($"❌ Error: {ex.Message}");
            }
            finally
            {
                SendAIButton.IsEnabled = true;
                SendAIButton.Text = "Send to AI";
            }
        }

        private void AddAIMessage(string message)
        {
            _aiChatHistory.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            
            // Keep only last 20 messages
            if (_aiChatHistory.Count > 20)
            {
                _aiChatHistory.RemoveAt(0);
            }
            
            AIChatLabel.Text = string.Join("\n\n", _aiChatHistory);
        }
    }
}
