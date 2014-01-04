namespace flint.Responses
{
    public abstract class ResponseBase : IResponse
    {
        public abstract void Load( byte[] payload );

        public void SetError( string message )
        {
            ErrorMessage = message;
        }

        public bool Success
        {
            get { return string.IsNullOrEmpty(ErrorMessage); }
        }

        public string ErrorMessage { get; private set; }
    }
}