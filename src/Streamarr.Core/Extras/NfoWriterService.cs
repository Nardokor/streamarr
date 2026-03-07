using System;
using System.IO;
using System.Xml.Linq;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Creators;
using ContentEntity = Streamarr.Core.Content.Content;

namespace Streamarr.Core.Extras
{
    public interface INfoWriterService
    {
        void WriteCreatorNfo(Creator creator);
        void WriteContentNfo(ContentEntity content, string absoluteFilePath, Channel channel);
    }

    public class NfoWriterService : INfoWriterService
    {
        private readonly Logger _logger;

        public NfoWriterService(Logger logger)
        {
            _logger = logger;
        }

        public void WriteCreatorNfo(Creator creator)
        {
            if (string.IsNullOrWhiteSpace(creator.Path) || !Directory.Exists(creator.Path))
            {
                return;
            }

            var nfoPath = Path.Combine(creator.Path, "tvshow.nfo");

            try
            {
                var tvshow = new XElement("tvshow",
                    new XElement("title", creator.Title));

                if (!string.IsNullOrWhiteSpace(creator.Description))
                {
                    tvshow.Add(new XElement("plot", creator.Description));
                }

                if (!string.IsNullOrWhiteSpace(creator.ThumbnailUrl))
                {
                    tvshow.Add(new XElement("thumb",
                        new XAttribute("aspect", "poster"),
                        creator.ThumbnailUrl));
                }

                var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), tvshow);
                doc.Save(nfoPath);

                _logger.Debug("Wrote creator NFO: {0}", nfoPath);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to write creator NFO: {0}", nfoPath);
            }
        }

        public void WriteContentNfo(ContentEntity content, string absoluteFilePath, Channel channel)
        {
            var nfoPath = Path.ChangeExtension(absoluteFilePath, ".nfo");

            try
            {
                var platformType = channel.Platform switch
                {
                    PlatformType.YouTube => "youtube",
                    PlatformType.Twitch => "twitch",
                    _ => channel.Platform.ToString().ToLowerInvariant()
                };

                var episode = new XElement("episodedetails",
                    new XElement("title", content.Title),
                    new XElement("studio", channel.Title),
                    new XElement("uniqueid",
                        new XAttribute("type", platformType),
                        new XAttribute("default", "true"),
                        content.PlatformContentId));

                if (!string.IsNullOrWhiteSpace(content.Description))
                {
                    episode.Add(new XElement("plot", content.Description));
                }

                if (content.AirDateUtc.HasValue)
                {
                    episode.Add(new XElement("aired",
                        content.AirDateUtc.Value.ToString("yyyy-MM-dd")));
                }

                if (content.Duration.HasValue)
                {
                    var minutes = (int)Math.Ceiling(content.Duration.Value.TotalMinutes);
                    episode.Add(new XElement("runtime", minutes));
                }

                if (!string.IsNullOrWhiteSpace(content.ThumbnailUrl))
                {
                    episode.Add(new XElement("thumb", content.ThumbnailUrl));
                }

                var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), episode);
                doc.Save(nfoPath);

                _logger.Debug("Wrote content NFO: {0}", nfoPath);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to write content NFO: {0}", nfoPath);
            }
        }
    }
}
