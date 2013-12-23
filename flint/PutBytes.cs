using System;
using System.Linq;

namespace flint
{
    public partial class Pebble
    {
        public enum PutBytesType : byte
        {
            Firmware = 1,
            Recovery = 2,
            SystemResources = 3,
            Resources = 4,
            Binary = 5
        }

        private enum PutBytesState
        {
            NotStarted,
            WaitForToken,
            InProgress,
            Commit,
            Complete,
            Failed
        }

        byte[] putBytesBuffer = { };
        private const PutBytesState putBytesState = PutBytesState.NotStarted;


        void PutBytes(byte[] data, int index, PutBytesType type)
        {
            if (putBytesState != PutBytesState.NotStarted)
            {
                // Probably not the best way to handle this, should look up mutex locks or somesuch.
                throw new InvalidOperationException("PUTBYTES operation in progress.");
            }
            putBytesBuffer = new byte[data.Count()];
            data.CopyTo(putBytesBuffer, 0);
        }

        void PutBytesReceived(object sender, MessageReceivedEventArgs e)
        {
            
        }


    }
}
