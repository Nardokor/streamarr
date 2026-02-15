using System;

namespace Streamarr.Common.Messaging
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class LifecycleEventAttribute : Attribute
    {
    }
}
