using System;
using Streamarr.Common.Exceptions;

namespace Streamarr.Core.Datastore
{
    public class ModelConflictException : StreamarrException
    {
        public ModelConflictException(Type modelType, int modelId)
            : base("{0} with ID {1} cannot be modified", modelType.Name, modelId)
        {
        }

        public ModelConflictException(Type modelType, int modelId, string message)
            : base("{0} with ID {1} {2}", modelType.Name, modelId, message)
        {
        }
    }
}
