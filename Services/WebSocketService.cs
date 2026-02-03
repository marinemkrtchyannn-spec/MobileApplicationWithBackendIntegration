using System.Net.WebSockets;
using System.Text;

namespace MyMobileApplication.Services;

public class WebSocketService
{
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private string _serverUrl = "wss://echo.websocket.org"; // Default echo server
    private bool _isDisconnecting = false; // Prevent multiple simultaneous disconnects
    
    public event EventHandler<string>? MessageReceived;
    public event EventHandler<string>? ConnectionStatusChanged;

    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    public void SetServerUrl(string url)
    {
        _serverUrl = url;
    }

    public string GetServerUrl() => _serverUrl;

    public async Task ConnectAsync()
    {
        try
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                ConnectionStatusChanged?.Invoke(this, "Already connected");
                return;
            }

            // Clean up any previous connection
            if (_webSocket != null)
            {
                _webSocket.Dispose();
                _webSocket = null;
            }

            _cancellationTokenSource?.Dispose();
            _isDisconnecting = false;

            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();

            ConnectionStatusChanged?.Invoke(this, "Connecting...");
            await _webSocket.ConnectAsync(new Uri(_serverUrl), _cancellationTokenSource.Token);
            ConnectionStatusChanged?.Invoke(this, "Connected");

            // Start receiving messages
            _ = Task.Run(async () => await ReceiveMessagesAsync());
        }
        catch (Exception ex)
        {
            ConnectionStatusChanged?.Invoke(this, $"Connection failed: {ex.Message}");
            _isDisconnecting = false;
        }
    }

    public async Task DisconnectAsync()
    {
        // Prevent multiple simultaneous disconnect attempts
        if (_isDisconnecting)
        {
            return;
        }

        _isDisconnecting = true;

        try
        {
            if (_webSocket != null)
            {
                _cancellationTokenSource?.Cancel();

                if (_webSocket.State == WebSocketState.Open || 
                    _webSocket.State == WebSocketState.CloseReceived ||
                    _webSocket.State == WebSocketState.CloseSent)
                {
                    try
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    }
                    catch (WebSocketException)
                    {
                        // Connection already closed/aborted, ignore
                    }
                }

                ConnectionStatusChanged?.Invoke(this, "Disconnected");
            }
        }
        catch (Exception ex)
        {
            ConnectionStatusChanged?.Invoke(this, $"Disconnect error: {ex.Message}");
        }
        finally
        {
            _webSocket?.Dispose();
            _cancellationTokenSource?.Dispose();
            _webSocket = null;
            _cancellationTokenSource = null;
            _isDisconnecting = false;
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket is not connected");
        }

        try
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            ConnectionStatusChanged?.Invoke(this, $"Send error: {ex.Message}");
        }
    }

    private async Task ReceiveMessagesAsync()
    {
        var buffer = new byte[1024 * 4];

        try
        {
            while (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    _cancellationTokenSource?.Token ?? CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    ConnectionStatusChanged?.Invoke(this, "Connection closed by server");
                    await DisconnectAsync();
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                MessageReceived?.Invoke(this, message);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            ConnectionStatusChanged?.Invoke(this, $"Receive error: {ex.Message}");
        }
    }
}
