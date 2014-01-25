﻿namespace flint.Responses
{
    internal class PutBytesResponse : ResponseBase
    {
        public byte[] Response { get; private set; }

        public override void Load( byte[] payload )
        {
            if (payload.Length == 0 || payload[0] != 1)
            {
                SetError("PutBytes failed");
            }
            Response = payload;
        }
    }
}