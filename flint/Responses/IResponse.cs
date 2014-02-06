namespace flint.Responses
{
    public interface IResponse
    {
        void SetPayload( byte[] payload );
        void SetError( string message );
        void SetError( byte[] errorPayload );

        bool Success { get; }
        string ErrorMessage { get; }
    }
}