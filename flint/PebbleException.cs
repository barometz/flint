using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace flint
{
    class PebbleNotFoundException : Exception
    {
        public String PebbleID { get; private set; }
        public PebbleNotFoundException(String pebbleid = "0000")
        {
            PebbleID = pebbleid;
        }

        public PebbleNotFoundException(String message, String pebbleid = "0000")
            : base(message)
        {
            PebbleID = pebbleid;
        }

        public PebbleNotFoundException(String message, Exception inner, String pebbleid = "0000")
            : base(message, inner)
        {
            PebbleID = pebbleid;
        }
    }
}
