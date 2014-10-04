using System;

namespace Windows.Pebble.Messages
{
    public class PebbleConnected
    {
        private readonly Flint.Core.Pebble _pebble;

        public PebbleConnected( Flint.Core.Pebble pebble )
        {
            if (pebble == null) throw new ArgumentNullException("pebble");
            _pebble = pebble;
        }

        public Flint.Core.Pebble Pebble
        {
            get { return _pebble; }
        }
    }
}