using Streamarr.Common.Http;

namespace Streamarr.Common.Cloud
{
    public interface IStreamarrCloudRequestBuilder
    {
        IHttpRequestBuilderFactory Services { get; }
        IHttpRequestBuilderFactory SkyHookTvdb { get; }
    }

    public class StreamarrCloudRequestBuilder : IStreamarrCloudRequestBuilder
    {
        public StreamarrCloudRequestBuilder()
        {
            Services = new HttpRequestBuilder("https://services.sonarr.tv/v1/")
                .CreateFactory();

            SkyHookTvdb = new HttpRequestBuilder("https://skyhook.sonarr.tv/v1/tvdb/{route}/{language}/")
                .SetSegment("language", "en")
                .CreateFactory();
        }

        public IHttpRequestBuilderFactory Services { get; }

        public IHttpRequestBuilderFactory SkyHookTvdb { get; }
    }
}
