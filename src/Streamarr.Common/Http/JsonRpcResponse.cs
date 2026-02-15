using Newtonsoft.Json.Linq;

namespace Streamarr.Common.Http
{
    public class JsonRpcResponse<T>
    {
        public string Id { get; set; }
        public T Result { get; set; }
        public JToken Error { get; set; }
    }
}
