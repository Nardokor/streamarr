using System;
using System.Collections.Generic;

namespace Streamarr.Core.MetadataSource.Twitch
{
    public interface ITwitchApiClient
    {
        string GetAccessToken(string clientId, string clientSecret);
        void TestCredentials(string clientId, string clientSecret);

        TwitchUser GetUserByLogin(string clientId, string accessToken, string login);
        TwitchUser GetUserById(string clientId, string accessToken, string userId);

        List<TwitchSearchChannel> SearchChannels(string clientId, string accessToken, string query, int first = 5);

        List<TwitchVideo> GetVideos(string clientId, string accessToken, string userId, DateTime? since = null);

        TwitchVideo GetVideo(string clientId, string accessToken, string videoId);

        TwitchStream GetLiveStream(string clientId, string accessToken, string userLogin);

        TwitchChannelInfo GetChannelInfo(string clientId, string accessToken, string userId);

        List<TwitchClip> GetClips(string clientId, string accessToken, string userId, DateTime? since = null);
    }
}
