using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using Streamarr.Common.Disk;
using Streamarr.Common.Extensions;
using Streamarr.Common.Processes;
using Streamarr.Core.Configuration;
using Streamarr.Core.MediaFiles.MediaInfo;
using Streamarr.Core.Parser;
using Streamarr.Core.Parser.Model;
using Streamarr.Core.Tags;

namespace Streamarr.Core.MediaFiles
{
    public interface IImportScript
    {
        public ScriptImportDecision TryImport(string sourcePath, string destinationFilePath, LocalEpisode localEpisode, EpisodeFile episodeFile, TransferMode mode);
    }

    public class ImportScriptService : IImportScript
    {
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IVideoFileInfoReader _videoFileInfoReader;
        private readonly IProcessProvider _processProvider;
        private readonly IConfigService _configService;
        private readonly ITagRepository _tagRepository;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public ImportScriptService(IProcessProvider processProvider,
                                   IVideoFileInfoReader videoFileInfoReader,
                                   IConfigService configService,
                                   IConfigFileProvider configFileProvider,
                                   ITagRepository tagRepository,
                                   IDiskProvider diskProvider,
                                   Logger logger)
        {
            _processProvider = processProvider;
            _videoFileInfoReader = videoFileInfoReader;
            _configService = configService;
            _configFileProvider = configFileProvider;
            _tagRepository = tagRepository;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        private static readonly Regex OutputRegex = new Regex(@"^(?:\[(?:(?<mediaFile>MediaFile)|(?<extraFile>ExtraFile))\]\s?(?<fileName>.+)|(?<preventExtraImport>\[PreventExtraImport\])|\[MoveStatus\]\s?(?:(?<deferMove>DeferMove)|(?<moveComplete>MoveComplete)|(?<renameRequested>RenameRequested)))$", RegexOptions.Compiled);

        private ScriptImportInfo ProcessOutput(List<ProcessOutputLine> processOutputLines)
        {
            var possibleExtraFiles = new List<string>();
            string mediaFile = null;
            var decision = ScriptImportDecision.MoveComplete;
            var importExtraFiles = true;

            foreach (var line in processOutputLines)
            {
                var match = OutputRegex.Match(line.Content);

                if (match.Groups["mediaFile"].Success)
                {
                    if (mediaFile is not null)
                    {
                        throw new ScriptImportException("Script output contains multiple media files. Only one media file can be returned.");
                    }

                    mediaFile = match.Groups["fileName"].Value;

                    if (!MediaFileExtensions.Extensions.Contains(Path.GetExtension(mediaFile)))
                    {
                        throw new ScriptImportException("Script output contains invalid media file: {0}", mediaFile);
                    }
                    else if (!_diskProvider.FileExists(mediaFile))
                    {
                        throw new ScriptImportException("Script output contains non-existent media file: {0}", mediaFile);
                    }
                }
                else if (match.Groups["extraFile"].Success)
                {
                    var fileName = match.Groups["fileName"].Value;

                    if (!_diskProvider.FileExists(fileName))
                    {
                        _logger.Warn("Script output contains non-existent possible extra file: {0}", fileName);
                    }

                    possibleExtraFiles.Add(fileName);
                }
                else if (match.Groups["moveComplete"].Success)
                {
                    decision = ScriptImportDecision.MoveComplete;
                }
                else if (match.Groups["renameRequested"].Success)
                {
                    decision = ScriptImportDecision.RenameRequested;
                }
                else if (match.Groups["deferMove"].Success)
                {
                    decision = ScriptImportDecision.DeferMove;
                }
                else if (match.Groups["preventExtraImport"].Success)
                {
                    importExtraFiles = false;
                }
            }

            return new ScriptImportInfo(possibleExtraFiles, mediaFile, decision, importExtraFiles);
        }

        public ScriptImportDecision TryImport(string sourcePath, string destinationFilePath, LocalEpisode localEpisode, EpisodeFile episodeFile, TransferMode mode)
        {
            var series = localEpisode.Series;
            var oldFiles = localEpisode.OldFiles;
            var downloadClientInfo = localEpisode.DownloadItem?.DownloadClientInfo;
            var downloadId = localEpisode.DownloadItem?.DownloadId;

            if (!_configService.UseScriptImport)
            {
                return ScriptImportDecision.DeferMove;
            }

            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Streamarr_SourcePath", sourcePath);
            environmentVariables.Add("Streamarr_DestinationPath", destinationFilePath);

            environmentVariables.Add("Streamarr_InstanceName", _configFileProvider.InstanceName);
            environmentVariables.Add("Streamarr_ApplicationUrl", _configService.ApplicationUrl);
            environmentVariables.Add("Streamarr_TransferMode", mode.ToString());

            environmentVariables.Add("Streamarr_Series_Id", series.Id.ToString());
            environmentVariables.Add("Streamarr_Series_Title", series.Title);
            environmentVariables.Add("Streamarr_Series_TitleSlug", series.TitleSlug);
            environmentVariables.Add("Streamarr_Series_Path", series.Path);
            environmentVariables.Add("Streamarr_Series_TvdbId", series.TvdbId.ToString());
            environmentVariables.Add("Streamarr_Series_TvMazeId", series.TvMazeId.ToString());
            environmentVariables.Add("Streamarr_Series_TmdbId", series.TmdbId.ToString());
            environmentVariables.Add("Streamarr_Series_ImdbId", series.ImdbId ?? string.Empty);
            environmentVariables.Add("Streamarr_Series_Type", series.SeriesType.ToString());
            environmentVariables.Add("Streamarr_Series_OriginalLanguage", IsoLanguages.Get(series.OriginalLanguage).ThreeLetterCode);
            environmentVariables.Add("Streamarr_Series_Genres", string.Join("|", series.Genres));
            environmentVariables.Add("Streamarr_Series_Tags", string.Join("|", series.Tags.Select(t => _tagRepository.Get(t).Label)));

            environmentVariables.Add("Streamarr_EpisodeFile_EpisodeCount", localEpisode.Episodes.Count.ToString());
            environmentVariables.Add("Streamarr_EpisodeFile_EpisodeIds", string.Join(",", localEpisode.Episodes.Select(e => e.Id)));
            environmentVariables.Add("Streamarr_EpisodeFile_SeasonNumber", localEpisode.SeasonNumber.ToString());
            environmentVariables.Add("Streamarr_EpisodeFile_EpisodeNumbers", string.Join(",", localEpisode.Episodes.Select(e => e.EpisodeNumber)));
            environmentVariables.Add("Streamarr_EpisodeFile_EpisodeAirDates", string.Join(",", localEpisode.Episodes.Select(e => e.AirDate)));
            environmentVariables.Add("Streamarr_EpisodeFile_EpisodeAirDatesUtc", string.Join(",", localEpisode.Episodes.Select(e => e.AirDateUtc)));
            environmentVariables.Add("Streamarr_EpisodeFile_EpisodeTitles", string.Join("|", localEpisode.Episodes.Select(e => e.Title)));
            environmentVariables.Add("Streamarr_EpisodeFile_EpisodeOverviews", string.Join("|", localEpisode.Episodes.Select(e => e.Overview)));
            environmentVariables.Add("Streamarr_EpisodeFile_Quality", localEpisode.Quality.Quality.Name);
            environmentVariables.Add("Streamarr_EpisodeFile_QualityVersion", localEpisode.Quality.Revision.Version.ToString());
            environmentVariables.Add("Streamarr_EpisodeFile_ReleaseGroup", localEpisode.ReleaseGroup ?? string.Empty);
            environmentVariables.Add("Streamarr_EpisodeFile_SceneName", localEpisode.SceneName ?? string.Empty);

            environmentVariables.Add("Streamarr_Download_Client", downloadClientInfo?.Name ?? string.Empty);
            environmentVariables.Add("Streamarr_Download_Client_Type", downloadClientInfo?.Type ?? string.Empty);
            environmentVariables.Add("Streamarr_Download_Id", downloadId ?? string.Empty);

            if (localEpisode.MediaInfo == null)
            {
                _logger.Trace("MediaInfo is null for episode file import. This may cause issues with the import script.");
            }
            else
            {
                environmentVariables.Add("Streamarr_EpisodeFile_MediaInfo_AudioChannels",
                    MediaInfoFormatter.FormatAudioChannels(localEpisode.MediaInfo.PrimaryAudioStream).ToString());
                environmentVariables.Add("Streamarr_EpisodeFile_MediaInfo_AudioCodec",
                    MediaInfoFormatter.FormatAudioCodec(localEpisode.MediaInfo.PrimaryAudioStream, null));
                environmentVariables.Add("Streamarr_EpisodeFile_MediaInfo_AudioLanguages",
                    localEpisode.MediaInfo.AudioStreams?.Select(l => l.Language).Distinct().ConcatToString(" / "));
                environmentVariables.Add("Streamarr_EpisodeFile_MediaInfo_Languages",
                    localEpisode.MediaInfo.AudioStreams?.Select(l => l.Language).ConcatToString(" / "));
                environmentVariables.Add("Streamarr_EpisodeFile_MediaInfo_Height",
                    localEpisode.MediaInfo.Height.ToString());
                environmentVariables.Add("Streamarr_EpisodeFile_MediaInfo_Width", localEpisode.MediaInfo.Width.ToString());
                environmentVariables.Add("Streamarr_EpisodeFile_MediaInfo_Subtitles",
                    localEpisode.MediaInfo.SubtitleStreams?.Select(l => l.Language).ConcatToString(" / "));
                environmentVariables.Add("Streamarr_EpisodeFile_MediaInfo_VideoCodec",
                    MediaInfoFormatter.FormatVideoCodec(localEpisode.MediaInfo, null));
                environmentVariables.Add("Streamarr_EpisodeFile_MediaInfo_VideoDynamicRangeType",
                    MediaInfoFormatter.FormatVideoDynamicRangeType(localEpisode.MediaInfo));
            }

            environmentVariables.Add("Streamarr_EpisodeFile_CustomFormat", string.Join("|", localEpisode.CustomFormats));
            environmentVariables.Add("Streamarr_EpisodeFile_CustomFormatScore", localEpisode.CustomFormatScore.ToString());

            if (oldFiles.Any())
            {
                environmentVariables.Add("Streamarr_DeletedRelativePaths", string.Join("|", oldFiles.Select(e => e.EpisodeFile.RelativePath)));
                environmentVariables.Add("Streamarr_DeletedPaths", string.Join("|", oldFiles.Select(e => Path.Combine(series.Path, e.EpisodeFile.RelativePath))));
                environmentVariables.Add("Streamarr_DeletedDateAdded", string.Join("|", oldFiles.Select(e => e.EpisodeFile.DateAdded)));
                environmentVariables.Add("Streamarr_DeletedRecycleBinPaths", string.Join("|", oldFiles.Select(e => e.RecycleBinPath ?? string.Empty)));
            }

            _logger.Debug("Executing external script: {0}", _configService.ScriptImportPath);

            var processOutput = _processProvider.StartAndCapture(_configService.ScriptImportPath, $"\"{sourcePath}\" \"{destinationFilePath}\"", environmentVariables);

            _logger.Debug("Script Output: \r\n{0}", string.Join("\r\n", processOutput.Lines));

            if (processOutput.ExitCode != 0)
            {
                throw new ScriptImportException("Script exited with non-zero exit code: {0}", processOutput.ExitCode);
            }

            var scriptImportInfo = ProcessOutput(processOutput.Lines);

            var mediaFile = scriptImportInfo.MediaFile ?? destinationFilePath;
            localEpisode.PossibleExtraFiles = scriptImportInfo.PossibleExtraFiles;

            episodeFile.RelativePath = series.Path.GetRelativePath(mediaFile);
            episodeFile.Path = mediaFile;

            var exitCode = processOutput.ExitCode;

            localEpisode.ShouldImportExtras = scriptImportInfo.ImportExtraFiles;

            if (scriptImportInfo.Decision != ScriptImportDecision.DeferMove)
            {
                localEpisode.ScriptImported = true;
            }

            if (scriptImportInfo.Decision == ScriptImportDecision.RenameRequested)
            {
                episodeFile.MediaInfo = _videoFileInfoReader.GetMediaInfo(mediaFile);
                episodeFile.Path = null;
            }

            return scriptImportInfo.Decision;
        }
    }
}
