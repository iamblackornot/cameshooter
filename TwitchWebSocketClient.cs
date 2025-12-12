using DeathCounterNETShared;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;

class TwitchWebSocketClient
{
    public event EventHandler<OnMessageReceivedArgs>? OnMessageReceived;
    public event EventHandler<OnErrorEventArgs>? OnError;
    public event EventHandler<OnConnectionErrorArgs>? OnConnectionError;
    public event EventHandler? OnTokenUpdate;
    public TwitchWebSocketClient(string username, string userAccessToken, string channel)
    {
        this.channel = channel;

        ConnectionCredentials credentials = new ConnectionCredentials(username, userAccessToken);
        var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
        WebSocketClient customClient = new WebSocketClient(clientOptions);
        client = new TwitchClient(customClient);
        client.Initialize(credentials, channel);

        client.OnLog += Client_OnLog;
        client.OnJoinedChannel += Client_OnJoinedChannel;
        client.OnMessageReceived += Client_OnMessageReceived;
        client.OnConnected += Client_OnConnected;
        client.OnError += Client_OnError;
        client.OnConnectionError += Client_OnConnectionError;
    }

    public bool IsConnected => client.IsConnected;
    public void Connect() => client.Connect();
    public void Disconnect() => client.Disconnect();

    public void Reconnect()
    {
        if(client.IsConnected)
        {
           client.Reconnect();
        }
        else
        {
            client.Connect();
        }
    }

    public void SendMessage(string message)
    {
        client.SendMessage(channel, message, false);
    }

    private void Client_OnConnectionError(object? sender, OnConnectionErrorArgs e)
    {
        OnTokenUpdate?.Invoke(this, EventArgs.Empty);
        OnConnectionError?.Invoke(sender, e);
    } 
    private void Client_OnError(object? sender, OnErrorEventArgs e)
    {
        OnTokenUpdate?.Invoke(this, EventArgs.Empty);
        OnError?.Invoke(sender, e);
    } 
    private void Client_OnLog(object? sender, OnLogArgs e)
    {
        //ConsoleHelper.PrintInfo($"{e.DateTime}: {e.BotUsername} - {e.Data}");
        OnTokenUpdate?.Invoke(this, EventArgs.Empty);
    }

    private void Client_OnConnected(object? sender, OnConnectedArgs e)
    {
        ConsoleHelper.PrintInfo($"Connected");
    }

    private void Client_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        ConsoleHelper.PrintInfo($"Joined [{channel}] channel");
    }

    private void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        OnMessageReceived?.Invoke(sender, e);
        OnTokenUpdate?.Invoke(this, EventArgs.Empty);
    }

    private readonly TwitchClient client;
    private readonly string channel;
}