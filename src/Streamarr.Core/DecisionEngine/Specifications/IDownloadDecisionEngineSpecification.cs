using Streamarr.Core.Parser.Model;

namespace Streamarr.Core.DecisionEngine.Specifications
{
    public interface IDownloadDecisionEngineSpecification
    {
        RejectionType Type { get; }

        SpecificationPriority Priority { get; }

        DownloadSpecDecision IsSatisfiedBy(RemoteEpisode subject, ReleaseDecisionInformation information);
    }
}
