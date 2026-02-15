using FluentValidation;
using Streamarr.Core.Annotations;
using Streamarr.Core.Validation;

namespace Streamarr.Core.ImportLists.Rss.Plex
{
    public class PlexRssImportSettingsValidator : AbstractValidator<PlexRssImportSettings>
    {
        public PlexRssImportSettingsValidator()
        {
            RuleFor(c => c.Url).NotEmpty();
        }
    }

    public class PlexRssImportSettings : RssImportBaseSettings<PlexRssImportSettings>
    {
        private static readonly PlexRssImportSettingsValidator Validator = new();

        [FieldDefinition(0, Label = "ImportListsSettingsRssUrl", Type = FieldType.Textbox, HelpLink = "https://app.plex.tv/desktop/#!/settings/watchlist")]
        public override string Url { get; set; }

        public override StreamarrValidationResult Validate()
        {
            return new StreamarrValidationResult(Validator.Validate(this));
        }
    }
}
