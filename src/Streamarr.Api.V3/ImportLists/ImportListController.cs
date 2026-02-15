using FluentValidation;
using Streamarr.Core.ImportLists;
using Streamarr.Core.Validation;
using Streamarr.Core.Validation.Paths;
using Streamarr.Http;
using Streamarr.SignalR;

namespace Streamarr.Api.V3.ImportLists
{
    [V3ApiController]
    public class ImportListController : ProviderControllerBase<ImportListResource, ImportListBulkResource, IImportList, ImportListDefinition>
    {
        public static readonly ImportListResourceMapper ResourceMapper = new();
        public static readonly ImportListBulkResourceMapper BulkResourceMapper = new();

        public ImportListController(IBroadcastSignalRMessage signalRBroadcaster,
            IImportListFactory importListFactory,
            RootFolderExistsValidator rootFolderExistsValidator,
            QualityProfileExistsValidator qualityProfileExistsValidator)
            : base(signalRBroadcaster, importListFactory, "importlist", ResourceMapper, BulkResourceMapper)
        {
            SharedValidator.RuleFor(c => c.RootFolderPath).Cascade(CascadeMode.Stop)
                .IsValidPath()
                .SetValidator(rootFolderExistsValidator);

            SharedValidator.RuleFor(c => c.QualityProfileId).Cascade(CascadeMode.Stop)
                .ValidId()
                .SetValidator(qualityProfileExistsValidator);
        }
    }
}
