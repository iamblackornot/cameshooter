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

        if(client is null || !client.IsConnected)
        {
            InitTwitchClient();
            client?.Connect();
        }

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
            credentials.UserAccessToken = File.ReadAllText(USER_ACCESS_TOKEN_FILE).TrimExtended();
        }

        if(File.Exists(REFRESH_USER_ACCESS_TOKEN_FILE))
        {
            credentials.RefreshUserAccessToken = File.ReadAllText(REFRESH_USER_ACCESS_TOKEN_FILE).TrimExtended();
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

        var refreshRes = await RefreshToken();

        if(refreshRes.IsSuccessful)
        {
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

    protected async Task<Result> RefreshToken()
    {
        var refreshRes = await api.RefreshTokenAsync(api.Credentials.RefreshUserAccessToken);

        if(!refreshRes.IsSuccessful)
        {
            return new BadResult(refreshRes.ErrorMessage);
        }

        UpdateTokens(refreshRes.Data);

        InitTwitchClient();
        client?.Connect();

        ConsoleHelper.PrintInfo("UserAccessToken was refreshed successfully");

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
        api.Credentials.UserAccessToken = userAccessToken.TrimExtended();
        api.Credentials.RefreshUserAccessToken = refreshToken.TrimExtended();
        updateTokenDeadline = DateTime.Now + TimeSpan.FromSeconds(expiresInSeconds);

        File.WriteAllText(USER_ACCESS_TOKEN_FILE, userAccessToken.TrimExtended());
        File.WriteAllText(REFRESH_USER_ACCESS_TOKEN_FILE, refreshToken.TrimExtended());
    }
    protected BotApp(TwitchCredentials credentials)
    {
        api = new(credentials);
    }

    protected void InitTwitchClient()
    {
        if(client is not null && client.IsConnected)
        {
            client.Disconnect();
        }

        client = new TwitchWebSocketClient(
            DotNetEnv.Env.GetString("USERNAME"),
            api.Credentials.UserAccessToken!,
            DotNetEnv.Env.GetString("CHANNEL")
        );

        stateManager.OnAction += OnAction;
        client.OnTokenUpdate += OnTokenUpdate;
        client.OnMessageReceived += OnMessageReceived;
        client.OnError += OnError;
        client.OnConnectionError += OnConnectionError;

        ConsoleHelper.PrintInfo("Initialized a new TwitchClient");
    }
    private async void OnTokenUpdate(object? sender, EventArgs e)
    {
        if (!tokenUpdateSemaphore.Wait(0)) return;

        try
        {
            await RefreshTokenIfNeeded();
        }
        catch (Exception ex)
        {
            Logger.AddToLogs("OnTokenUpdate", ex.Message);
            ConsoleHelper.PrintError($"OnTokenUpdateException: {ex.Message}");
        }
        finally
        {
            tokenUpdateSemaphore.Release();
        }
    }

    private void OnConnectionError(object? sender, OnConnectionErrorArgs e)
    {
        ConsoleHelper.PrintError($"WebSocketConnectionError: {e.Error.Message}");
    }

    private void OnError(object? sender, OnErrorEventArgs e)
    {
        ConsoleHelper.PrintError($"WebSocketError: {e.Exception}");
    }

    private void OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        //ConsoleHelper.PrintInfo($"{e.ChatMessage.DisplayName}: {e.ChatMessage.Message}");
        stateManager.ProcessMessage(e.ChatMessage.Message);
    }
    private void OnAction(object? sender, StateActionEventArgs e)
    {
        if(client is not null && client.IsConnected)
        {
            client.SendMessage(e.Message);
        }
    }

    private readonly TwitchUserAccessTokenAPI api;
    private TwitchWebSocketClient? client;
    private readonly StateManager stateManager = new ();
    private DateTime updateTokenDeadline = DateTime.MinValue;
    private readonly SemaphoreSlim tokenUpdateSemaphore = new(1, 1);


}