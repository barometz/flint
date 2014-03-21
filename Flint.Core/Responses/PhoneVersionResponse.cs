namespace Flint.Core.Responses
{
    [Endpoint( Endpoint.PhoneVersion )]
    public class PhoneVersionResponse : ResponseBase
    {
        protected override void Load( byte[] payload )
        {
        }
    }
}