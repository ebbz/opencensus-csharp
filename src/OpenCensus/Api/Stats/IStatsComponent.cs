﻿using System;
using System.Collections.Generic;
using System.Text;

namespace OpenCensus.Stats
{
    public interface IStatsComponent
    {
        IViewManager ViewManager { get; }
        IStatsRecorder StatsRecorder { get; }
        StatsCollectionState State { get; }
    }
}
