namespace Flint.Core.Responses
{
    [Endpoint( Endpoint.PutBytes )]
    internal class PutBytesResponse : ResponseBase
    {
        public byte[] Response { get; private set; }

        protected override void Load( byte[] payload )
        {
            if (payload.Length == 0 || payload[0] != 1)
            {
                SetError("PutBytes failed");
            }
            Response = payload;
        }
    }
}