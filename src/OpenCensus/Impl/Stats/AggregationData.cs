﻿using OpenCensus.Stats.Aggregations;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenCensus.Stats
{
    public abstract class AggregationData : IAggregationData
    {
        public abstract M Match<M>(
            Func<ISumDataDouble, M> p0, 
            Func<ISumDataLong, M> p1, 
            Func<ICountData, M> p2, 
            Func<IMeanData, M> p3, 
            Func<IDistributionData, M> p4,
            Func<ILastValueDataDouble, M> p5,
            Func<ILastValueDataLong, M> p6,
            Func<IAggregationData, M> defaultFunction);
    }
}
