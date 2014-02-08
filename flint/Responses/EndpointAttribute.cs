using System;

namespace flint.Responses
{
    [AttributeUsage( AttributeTargets.Class )]
    internal class EndpointAttribute : Attribute
    {
        public Endpoint Endpoint { get; set; }
        public byte PayloadType { get; set; } 

        public EndpointAttribute()
        { }

        public EndpointAttribute( Endpoint endpoint )
        {
            Endpoint = endpoint;
        }

        public EndpointAttribute( Endpoint endpoint , byte payloadType)
        {
            Endpoint = endpoint;
            PayloadType = payloadType;
        }

        public Func<byte[], bool> GetPredicate()
        {
            return bytes => bytes != null && bytes.Length > 0 && bytes[0] == PayloadType;
        }
    }
}