namespace Flint.Core
{
    //TODO: Better namespace
    public class BytesReceivedEventArgs
    {
        public BytesReceivedEventArgs()
        { }

        public BytesReceivedEventArgs( byte[] bytes )
        {
            Bytes = bytes;
        }

        public byte[] Bytes { get; set; }
    }
}