﻿using System;

namespace Flint.Core.Tests.Responses
{
    public static class ResponseGenerator
    {
        public static BytesReceivedEventArgs GetBytesReceivedResponse(Endpoint endpoint)
        {
            switch (endpoint)
            {
                case Endpoint.PutBytes:
                    return new BytesReceivedEventArgs(Util.CombineArrays(Util.GetBytes((ushort)1), Util.GetBytes((ushort)Endpoint.PutBytes), new byte[] { 1 }));
                case Endpoint.SystemMessage:
                    return new BytesReceivedEventArgs(Util.CombineArrays(Util.GetBytes((ushort)2), Util.GetBytes((ushort)Endpoint.SystemMessage), new byte[] { 0, 0 }));
                default:
                    throw new InvalidOperationException();
            }        
        }
    }
}