namespace Streamarr.Core.Validation
{
    public class StreamarrValidationState
    {
        public static StreamarrValidationState Warning = new StreamarrValidationState { IsWarning = true };

        public bool IsWarning { get; set; }
    }
}
