using System;

namespace Windows.Pebble.Messages
{
    public class PebbleDisconnected
    {
        private readonly Flint.Core.Pebble _pebble;

        public PebbleDisconnected( Flint.Core.Pebble pebble )
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