using System.Linq;

namespace Flint.Core.Responses
{
    [Endpoint(Endpoint.SystemMessage)]
    public class SystemMessageResponse : ResponseBase
    {
        public byte[] ResponseData { get; set; }

        protected override void Load(byte[] payload)
        {
            if (payload.Length >= 2)
            {
                ResponseData = payload.Skip(1).ToArray();
            }
            else
            {
                SetError("Got unknown system message response");
            }
        }
    }
}