namespace DeathCounterNETShared.Twitch
{
    internal class TwitchCredentials
    {
        public string? AuthorizationCode { get; init; }
        public string? ClientID { get; init; }
        public string? ClientSecret { get; init; }
        public string? RedirectUri { get; init; }
        public string? UserAccessToken { get; set; }
        public string? RefreshUserAccessToken { get; set; }

        public static TwitchCredentials FromEnv()
        {
            return new TwitchCredentials
            {
                AuthorizationCode = DotNetEnv.Env.GetString("AUTHORIZATION_CODE"),
                ClientID          = DotNetEnv.Env.GetString("CLIENT_ID"),
                ClientSecret      = DotNetEnv.Env.GetString("CLIENT_SECRET"),
                RedirectUri       = DotNetEnv.Env.GetString("REDIRECT_URI"),
                UserAccessToken   = DotNetEnv.Env.GetString("USER_ACCESS_TOKEN"),
            };
        }

        public IEnumerable<string> Validate()
        {
            if (string.IsNullOrWhiteSpace(AuthorizationCode)) yield return "AUTHORIZATION_CODE";
            if (string.IsNullOrWhiteSpace(ClientID))          yield return "CLIENT_ID";
            if (string.IsNullOrWhiteSpace(ClientSecret))      yield return "CLIENT_SECRET";
            if (string.IsNullOrWhiteSpace(RedirectUri))       yield return "REDIRECT_URI";
        }
    }
}
