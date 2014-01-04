using System;

namespace flint
{
    public class ProgressValue
    {
        private readonly string _message;
        private readonly int _progressPercentage;

        internal ProgressValue( string message, int progressPercentage )
        {
            if (string.IsNullOrEmpty(message)) throw new ArgumentException("Message is required", "message");
            if ( progressPercentage < 0 || progressPercentage > 100 ) throw new ArgumentOutOfRangeException( "progressPercentage", "Progress percentage must be between 0 and 100");
            _message = message;
            _progressPercentage = progressPercentage;
        }

        public string Message
        {
            get { return _message; }
        }

        public int ProgressPercentage
        {
            get { return _progressPercentage; }
        }
    }
}