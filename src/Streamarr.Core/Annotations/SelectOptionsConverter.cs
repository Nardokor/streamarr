using System.Collections.Generic;

namespace Streamarr.Core.Annotations
{
    public interface ISelectOptionsConverter
    {
        List<SelectOption> GetSelectOptions();
    }
}
