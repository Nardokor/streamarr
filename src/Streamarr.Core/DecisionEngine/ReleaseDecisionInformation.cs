using Streamarr.Core.IndexerSearch.Definitions;

namespace Streamarr.Core.DecisionEngine;

public class ReleaseDecisionInformation
{
    public bool PushedRelease { get; set; }
    public SearchCriteriaBase SearchCriteria { get; set; }

    public ReleaseDecisionInformation()
    {
        PushedRelease = false;
        SearchCriteria = null;
    }

    public ReleaseDecisionInformation(bool pushedRelease, SearchCriteriaBase searchCriteria)
    {
        PushedRelease = pushedRelease;
        SearchCriteria = searchCriteria;
    }
}
