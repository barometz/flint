using System;

namespace flint
{
    public class PebbleNotFoundException : PebbleException
    {
        public string PebbleID { get; private set; }
        public PebbleNotFoundException(string pebbleId = "0000")
        {
            PebbleID = pebbleId;
        }

        public PebbleNotFoundException(string message, string pebbleId = "0000")
            : base(message)
        {
            PebbleID = pebbleId;
        }

        public PebbleNotFoundException(string message, Exception innerException, string pebbleId = "0000")
            : base(message, innerException)
        {
            PebbleID = pebbleId;
        }
    }
}
