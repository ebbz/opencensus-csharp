﻿using OpenCensus.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenCensus.Trace
{
    public interface ISpanBuilder
    {
        ISpanBuilder SetSampler(ISampler sampler);
        ISpanBuilder SetParentLinks(IList<ISpan> parentLinks);
        ISpanBuilder SetRecordEvents(bool recordEvents);
        ISpan StartSpan();
        IScope StartScopedSpan();
    }
}
