using System.Collections.Concurrent;
using System.Net;
using TwitchLib.Api.Auth;

namespace DeathCounterNETShared.Twitch
{
    internal class TwitchAccessTokenAPI : TwitchPublicAPI
    {
        protected ConcurrentDictionary<string, string> _broadCasterIdCache;
        public TwitchCredentials Credentials { get; init; }

        public TwitchAccessTokenAPI(TwitchCredentials credentials) 
        {
            _api.Settings.ClientId = credentials.ClientID;
            _api.Settings.Secret = credentials.ClientSecret;
            _api.Settings.AccessToken = credentials.UserAccessToken;

            Credentials = credentials;

            _broadCasterIdCache = new();
        }
        public async Task<Result<string>> GetBroadcasterIdAsync(string channel)
        {
            if (_broadCasterIdCache.ContainsKey(channel))
            {
                return new GoodResult<string>(_broadCasterIdCache[channel]);
            }

            Func<Task<Result<string>>> action = async () =>
            {
                var resp = await _api.Helix.Users.GetUsersAsync(null, new List<string> { channel });

                if (resp.Users.Length == 0)
                {
                    return new BadResult<string>($"no users with name [{channel} found");
                }

                string id = resp.Users[0].Id;
                _broadCasterIdCache.AddOrUpdate(
                    channel, 
                    key => id,
                    (key, currentValue) => id);

                return new GoodResult<string>(id);
            };

            return await _executor.ExecuteAsync(action);
        }
        public async Task<Result<AuthCodeResponse>> GetUserAccessToken(string? authCode)
        {
            if(string.IsNullOrEmpty(authCode)) { return new BadResult<AuthCodeResponse>("authcode is empty"); }
            if(string.IsNullOrEmpty(Credentials.RedirectUri)) { return new BadResult<AuthCodeResponse>("redirectUri is empty"); }

            Func<Task<Result<AuthCodeResponse>>> action = async () =>
            {
                var resp = await _api.Auth.GetAccessTokenFromCodeAsync(authCode, _api.Settings.Secret, Credentials.RedirectUri);

                if (resp is null)
                {
                    return new BadResult<AuthCodeResponse>($"response is null");
                }

                return new GoodResult<AuthCodeResponse>(resp);
            };

            return await _executor.ExecuteAsync(action);
        }

        public async Task<Result<RefreshResponse>> RefreshTokenAsync(string? token)
        {
            if (token is null) return new BadResult<RefreshResponse>("refresh token is null or empty");

            Func<Task<Result<RefreshResponse>>> action = async () =>
            {
                var res = await _api.Auth.RefreshAuthTokenAsync(token, _api.Settings.Secret, _api.Settings.ClientId);
                return new GoodResult<RefreshResponse>(res);
            };

            return await _executor.ExecuteAsync(action);
        }
    }
}
