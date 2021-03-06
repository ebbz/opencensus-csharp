﻿using System;
using System.Collections.Generic;
using System.Text;

namespace OpenCensus.Stats.Aggregations
{
    public interface IMeanData : IAggregationData
    {
        double Mean { get; }
        long Count { get; }
        double Max { get; }
        double Min { get; }
    }
}
