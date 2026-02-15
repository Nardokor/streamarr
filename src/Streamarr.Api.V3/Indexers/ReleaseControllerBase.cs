using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.DecisionEngine;
using Streamarr.Core.Profiles.Qualities;
using Streamarr.Http.REST;

namespace Streamarr.Api.V3.Indexers
{
    public abstract class ReleaseControllerBase : RestController<ReleaseResource>
    {
        private readonly QualityProfile _qualityProfile;

        public ReleaseControllerBase(IQualityProfileService qualityProfileService)
        {
            _qualityProfile = qualityProfileService.GetDefaultProfile(string.Empty);
        }

        [NonAction]
        public override ActionResult<ReleaseResource> GetResourceByIdWithErrorHandler(int id)
        {
            return base.GetResourceByIdWithErrorHandler(id);
        }

        protected override ReleaseResource GetResourceById(int id)
        {
            throw new NotImplementedException();
        }

        protected virtual List<ReleaseResource> MapDecisions(IEnumerable<DownloadDecision> decisions)
        {
            var result = new List<ReleaseResource>();

            foreach (var downloadDecision in decisions)
            {
                var release = MapDecision(downloadDecision, result.Count);

                result.Add(release);
            }

            return result;
        }

        protected virtual ReleaseResource MapDecision(DownloadDecision decision, int initialWeight)
        {
            var release = decision.ToResource();

            release.ReleaseWeight = initialWeight;

            release.QualityWeight = _qualityProfile.GetIndex(release.Quality.Quality).Index * 100;

            release.QualityWeight += release.Quality.Revision.Real * 10;
            release.QualityWeight += release.Quality.Revision.Version;

            return release;
        }
    }
}
