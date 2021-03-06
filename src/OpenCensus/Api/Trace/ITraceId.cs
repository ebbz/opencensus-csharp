﻿using System;
using System.Collections.Generic;
using System.Text;

namespace OpenCensus.Trace
{
    public interface ITraceId : IComparable<ITraceId>
    {
        byte[] Bytes { get; }
        bool IsValid { get; }
        long LowerLong { get; }
        void CopyBytesTo(byte[] dest, int destOffset);
        string ToLowerBase16();

    }
}
