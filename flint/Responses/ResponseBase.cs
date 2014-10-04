using System;

namespace flint.Responses
{
    public abstract class ResponseBase : IResponse
    {
        protected ResponseBase()
        {
            ErrorMessage = "Response not set";
        }

        protected abstract void Load( byte[] payload );

        public void SetPayload( byte[] payload )
        {
            ErrorMessage = null;
            try
            {
                Load( payload );
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }
        }

        public void SetError( string message )
        {
            ErrorMessage = message;
        }

        public void SetError( byte[] errorPayload )
        {
            if (errorPayload == null || errorPayload.Length < 24)
            {
                SetError("The error payload is not large enough to be valid");
                return;
            }

            /* 
             * Unpack the metadata.  Eight bytes:
             * 0..3 -> integer timestamp
             * 4    -> Message level (severity)
             * 5    -> Size of the message
             * 6..7 -> Line number (?)
             */

            DateTime timestamp = Util.GetDateTimeFromTimestamp( Util.GetUInt32( errorPayload, 0 ) );
            byte level = errorPayload[4];
            byte messageSize = errorPayload[5];
            ushort lineNumber = Util.GetUInt16( errorPayload, 6 );

            // Now to extract the actual data
            string fileName = Util.GetString( errorPayload, 8, 16 );
            string message = Util.GetString( errorPayload, 24, messageSize );


            ErrorDetails = new ErrorDetails
                               {
                                   Timestamp = timestamp,
                                   Level = level,
                                   LineNumber = lineNumber,
                                   Filename = fileName,
                                   Message = message
                               };
            ErrorMessage = ErrorDetails.ToString();
        }

        public bool Success
        {
            get { return string.IsNullOrEmpty( ErrorMessage ) && ErrorDetails == null; }
        }

        public string ErrorMessage { get; private set; }

        public ErrorDetails ErrorDetails { get; private set; }
    }

    public class ErrorDetails
    {
        public DateTime Timestamp { get; set; }
        public byte Level { get; set; }
        public ushort LineNumber { get; set; }
        public string Filename { get; set; }
        public string Message { get; set; }

        public LogLevel GetLogLevel()
        {
            return Util.GetEnum( Level, LogLevel.Unknown );
        }

        public override string ToString()
        {
            return string.Format( "{1} - {2}: {0} in {3} ({4})", Message, GetLogLevel(), Timestamp, Filename, LineNumber );
        }
    }
}