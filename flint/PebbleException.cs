using System;

namespace flint
{
    class PebbleNotFoundException : Exception
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

        public PebbleNotFoundException(string message, Exception inner, string pebbleId = "0000")
            : base(message, inner)
        {
            PebbleID = pebbleId;
        }
    }
}
