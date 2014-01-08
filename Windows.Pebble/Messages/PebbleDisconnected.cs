using System;

namespace Windows.Pebble.Messages
{
    public class PebbleDisconnected
    {
        private readonly flint.Pebble _pebble;

        public PebbleDisconnected( flint.Pebble pebble )
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