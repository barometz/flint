namespace Flint.Core.Responses
{
    public interface IResponse
    {
        bool Success { get; }
        string ErrorMessage { get; }
        void SetPayload( byte[] payload );
        void SetError( string message );
        void SetError( byte[] errorPayload );
    }
}