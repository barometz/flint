namespace flint.Responses
{
    public interface IResponse
    {
        void Load( byte[] payload );
        void SetError( string message );

        bool Success { get; }
        string ErrorMessage { get; }
    }
}