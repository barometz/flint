using System;

namespace Flint.Core
{
    public class PebbleNotFoundException : PebbleException
    {
        public PebbleNotFoundException( string pebbleId = "0000" )
        {
            PebbleID = pebbleId;
        }

        public PebbleNotFoundException( string message, string pebbleId = "0000" )
            : base(message)
        {
            PebbleID = pebbleId;
        }

        public PebbleNotFoundException( string message, Exception innerException, string pebbleId = "0000" )
            : base(message, innerException)
        {
            PebbleID = pebbleId;
        }

        public string PebbleID { get; private set; }
    }
}