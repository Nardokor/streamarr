using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Datastore
{
    public class ModelNotFoundException : StreamarrException
    {
        public ModelNotFoundException(Type modelType, int modelId)
            : base("{0} with ID {1} does not exist", modelType.Name, modelId)
        {
        }
    }
}
