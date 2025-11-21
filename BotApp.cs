using DeathCounterNETShared;
using DeathCounterNETShared.Twitch;
using TwitchLib.Api.Auth;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Events;

class BotApp
{
    static readonly string USER_ACCESS_TOKEN_FILE = "uat";
    static readonly string REFRESH_USER_ACCESS_TOKEN_FILE = "ruat";
    public async Task Run()
    {
        {
            var refreshTokenRes = await RefreshTokenIfNeeded();

            if(!refreshTokenRes.IsSuccessful)
            {
                ConsoleHelper.PrintError($"Failed to refresh UserAccessToken, reason: {refreshTokenRes.ErrorMessage}");
                return;
            }
        }

        client.Connect();
        Console.ReadLine();
    }
    public static BotApp? CreateIntance()
    {
        DotNetEnv.Env.TraversePath().Load();

        TwitchCredentials credentials = TwitchCredentials.FromEnv();
        bool requiredParamIsMissing = false;

        foreach (string missing in credentials.Validate())
        {
            requiredParamIsMissing = true;
            ConsoleHelper.PrintError($"{missing} is required");
        }

        if(requiredParamIsMissing) { return null; }

        if(string.IsNullOrEmpty(DotNetEnv.Env.GetString("USERNAME")))
        {
            ConsoleHelper.PrintError($"USERNAME is required");
            return null;
        }

        if(string.IsNullOrEmpty(DotNetEnv.Env.GetString("CHANNEL")))
        {
            ConsoleHelper.PrintError($"CHANNEL is required");
            return null;
        }

        if(File.Exists(USER_ACCESS_TOKEN_FILE))
        {
            credentials.UserAccessToken = File.ReadAllText(USER_ACCESS_TOKEN_FILE).Trim();
        }

        if(File.Exists(REFRESH_USER_ACCESS_TOKEN_FILE))
        {
            credentials.RefreshUserAccessToken = File.ReadAllText(REFRESH_USER_ACCESS_TOKEN_FILE).Trim();
        }

        return new BotApp(credentials);
    }

    public async Task<Result> RefreshTokenIfNeeded()
    {
        DateTime now = DateTime.Now;

        if((updateTokenDeadline - now) >= TimeSpan.FromMinutes(5))
        {
            return new GoodResult();
        }

        var validateRes = await api.ValidateTokenAsync(api.Credentials.UserAccessToken);

        if(!validateRes.IsSuccessful)
        {
            return new BadResult(validateRes.ErrorMessage);
        }

        if(validateRes.Data.IsValid)
        {
            return new GoodResult();
        }

        var validateRefreshRes = await api.ValidateTokenAsync(api.Credentials.RefreshUserAccessToken);

        if(!validateRefreshRes.IsSuccessful)
        {
            return new BadResult(validateRes.ErrorMessage);
        }

        if(validateRefreshRes.Data.IsValid)
        {
            var refreshRes = await api.RefreshTokenAsync(api.Credentials.RefreshUserAccessToken);

            if(!refreshRes.IsSuccessful)
            {
                return new BadResult(refreshRes.ErrorMessage);
            }

            UpdateTokens(refreshRes.Data);
            ConsoleHelper.PrintInfo("UserAccessToken was refreshed successfully");

            return new GoodResult();
        }

        var getTokenRes = await api.GetUserAccessToken(api.Credentials.AuthorizationCode);
        
        if(!getTokenRes.IsSuccessful)
        {
            ConsoleHelper.PrintError("You need to provide a new Authorization Code");
            return new BadResult(getTokenRes.ErrorMessage);
        }

        UpdateTokens(getTokenRes.Data);
        ConsoleHelper.PrintInfo("new UserAccessToken was acquired successfully");

        return new GoodResult();
    }

    protected void UpdateTokens(AuthCodeResponse response)
    {
        UpdateTokens(response.AccessToken, response.RefreshToken, response.ExpiresIn);
    }
    protected void UpdateTokens(RefreshResponse response)
    {
        UpdateTokens(response.AccessToken, response.RefreshToken, response.ExpiresIn);
    }
    protected void UpdateTokens(string userAccessToken, string refreshToken, int expiresInSeconds)
    {
        
        api.Credentials.UserAccessToken = userAccessToken;
        api.Credentials.RefreshUserAccessToken = refreshToken;
        updateTokenDeadline = DateTime.Now + TimeSpan.FromSeconds(expiresInSeconds);

        File.WriteAllText(USER_ACCESS_TOKEN_FILE, userAccessToken);
        File.WriteAllText(REFRESH_USER_ACCESS_TOKEN_FILE, refreshToken);
    }
    protected BotApp(TwitchCredentials credentials)
    {
        api = new(credentials);

        client = new TwitchWebSocketClient(
            DotNetEnv.Env.GetString("USERNAME"),
            api.Credentials.UserAccessToken!,
            DotNetEnv.Env.GetString("CHANNEL")
        );

        stateManager.OnAction += OnAction;
        client.OnMessageReceived += OnMessageReceived;
        client.OnError += OnError;
        client.OnConnectionError += OnConnectionError;
    }
    private async void OnConnectionError(object? sender, OnConnectionErrorArgs e)
    {
        ConsoleHelper.PrintError($"WebSocketConnectionError: {e.Error}");
        await RefreshTokenIfNeeded();
        client.Reconnect();
    }

    private async void OnError(object? sender, OnErrorEventArgs e)
    {
        ConsoleHelper.PrintError($"WebSocketError: {e.Exception}");
        await RefreshTokenIfNeeded();
        client.Reconnect();
    }

    private void OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        ConsoleHelper.PrintInfo($"{e.ChatMessage.DisplayName}: {e.ChatMessage.Message}");
        stateManager.ProcessMessage(e.ChatMessage.Message);
    }
    private void OnAction(object? sender, StateActionEventArgs e)
    {
        if(client.IsConnected)
        {
            client.SendMessage(e.Message);
        }
    }

    private readonly TwitchUserAccessTokenAPI api;
    private readonly TwitchWebSocketClient client;
    private readonly StateManager stateManager = new ();
    private DateTime updateTokenDeadline = DateTime.MinValue;

}