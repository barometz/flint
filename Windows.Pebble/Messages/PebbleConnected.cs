using System;

namespace Windows.Pebble.Messages
{
    public class PebbleConnected
    {
        private readonly flint.Pebble _pebble;

        public PebbleConnected( flint.Pebble pebble )
        {
            if (pebble == null) throw new ArgumentNullException("pebble");
            _pebble = pebble;
        }

        public flint.Pebble Pebble
        {
            get { return _pebble; }
        }
    }
}