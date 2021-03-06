﻿using OpenCensus.Common;
using OpenCensus.Internal;
using OpenCensus.Stats.Aggregations;
using OpenCensus.Stats.Measures;
using OpenCensus.Tags;
using OpenCensus.Tags.Unsafe;
using OpenCensus.Testing.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OpenCensus.Stats.Test
{
    public class StatsRecorderTest
    {
        private static readonly ITagKey KEY = TagKey.Create("KEY");
        private static readonly ITagValue VALUE = TagValue.Create("VALUE");
        private static readonly ITagValue VALUE_2 = TagValue.Create("VALUE_2");
        private static readonly IMeasureDouble MEASURE_DOUBLE = MeasureDouble.Create("my measurement", "description", "us");
        private static readonly IMeasureDouble MEASURE_DOUBLE_NO_VIEW_1 = MeasureDouble.Create("my measurement no view 1", "description", "us");
        private static readonly IMeasureDouble MEASURE_DOUBLE_NO_VIEW_2 = MeasureDouble.Create("my measurement no view 2", "description", "us");
        private static readonly IViewName VIEW_NAME = ViewName.Create("my view");

        private StatsComponent statsComponent;
        private IViewManager viewManager;
        private IStatsRecorder statsRecorder;

        static readonly ITimestamp ZERO_TIMESTAMP = Timestamp.Create(0, 0);

        public StatsRecorderTest()
        {
            statsComponent = new StatsComponent(new SimpleEventQueue(), TestClock.Create());
            viewManager = statsComponent.ViewManager;
            statsRecorder = statsComponent.StatsRecorder;
        }

        [Fact]
        public void Record_CurrentContextNotSet()
        {
            IView view =
                View.Create(
                    VIEW_NAME,
                    "description",
                    MEASURE_DOUBLE,
                    Sum.Create(),
                    new List<ITagKey>() { KEY });
            viewManager.RegisterView(view);
            statsRecorder.NewMeasureMap().Put(MEASURE_DOUBLE, 1.0).Record();
            IViewData viewData = viewManager.GetView(VIEW_NAME);

            // record() should have used the default TagContext, so the tag value should be null.
            ICollection<TagValues> expected = new List<TagValues>() { TagValues.Create(new List<ITagValue>() { null }) };
            Assert.Equal(expected, viewData.AggregationMap.Keys);
          
        }

        [Fact]
        public void Record_CurrentContextSet()
        {
            IView view =
                View.Create(
                    VIEW_NAME,
                    "description",
                    MEASURE_DOUBLE,
                    Sum.Create(),
               new List<ITagKey>() { KEY });
            viewManager.RegisterView(view);
            var orig = AsyncLocalContext.CurrentTagContext;
            AsyncLocalContext.CurrentTagContext = new SimpleTagContext(Tag.Create(KEY, VALUE));
 
            try
            {
                statsRecorder.NewMeasureMap().Put(MEASURE_DOUBLE, 1.0).Record();
            }
            finally
            {
                AsyncLocalContext.CurrentTagContext = orig;
            }
            IViewData viewData = viewManager.GetView(VIEW_NAME);

            // record() should have used the given TagContext.
            ICollection<TagValues> expected = new List<TagValues>() { TagValues.Create(new List<ITagValue>() { VALUE }) };
            Assert.Equal(expected, viewData.AggregationMap.Keys);
        }

        [Fact]
        public void Record_UnregisteredMeasure()
        {
            IView view =
                View.Create(
                    VIEW_NAME,
                    "description",
                    MEASURE_DOUBLE,
                    Sum.Create(),
                    new List<ITagKey>() { KEY });
            viewManager.RegisterView(view);
            statsRecorder
                .NewMeasureMap()
                .Put(MEASURE_DOUBLE_NO_VIEW_1, 1.0)
                .Put(MEASURE_DOUBLE, 2.0)
                .Put(MEASURE_DOUBLE_NO_VIEW_2, 3.0)
                .Record(new SimpleTagContext(Tag.Create(KEY, VALUE)));

            IViewData viewData = viewManager.GetView(VIEW_NAME);

            // There should be one entry.
            var tv = TagValues.Create(new List<ITagValue>() { VALUE });
            StatsTestUtil.AssertAggregationMapEquals(
                viewData.AggregationMap,
                new Dictionary<TagValues, IAggregationData>() {
                    { tv, StatsTestUtil.CreateAggregationData(Sum.Create(), MEASURE_DOUBLE, 2.0) }
                },
                1e-6);
        }

        [Fact]
        public void RecordTwice()
        {
            IView view =
                View.Create(
                    VIEW_NAME,
                    "description",
                    MEASURE_DOUBLE,
                    Sum.Create(),
                    new List<ITagKey>() { KEY });

            viewManager.RegisterView(view);
            IMeasureMap statsRecord = statsRecorder.NewMeasureMap().Put(MEASURE_DOUBLE, 1.0);
            statsRecord.Record(new SimpleTagContext(Tag.Create(KEY, VALUE)));
            statsRecord.Record(new SimpleTagContext(Tag.Create(KEY, VALUE_2)));
            IViewData viewData = viewManager.GetView(VIEW_NAME);

            // There should be two entries.
            var tv = TagValues.Create(new List<ITagValue>() { VALUE });
            var tv2 = TagValues.Create(new List<ITagValue>() { VALUE_2 });

            StatsTestUtil.AssertAggregationMapEquals(
                viewData.AggregationMap,
                new Dictionary<TagValues, IAggregationData>() {
                    { tv, StatsTestUtil.CreateAggregationData(Sum.Create(), MEASURE_DOUBLE, 1.0) },
                    { tv2, StatsTestUtil.CreateAggregationData(Sum.Create(), MEASURE_DOUBLE, 1.0) }
                },
                1e-6);
        }

        [Fact]
        public void Record_StatsDisabled()
        {
            IView view =
                View.Create(
                    VIEW_NAME,
                    "description",
                    MEASURE_DOUBLE,
                    Sum.Create(),
              new List<ITagKey>() { KEY });

            viewManager.RegisterView(view);
            statsComponent.State = StatsCollectionState.DISABLED;
            statsRecorder
                .NewMeasureMap()
                .Put(MEASURE_DOUBLE, 1.0)
                .Record(new SimpleTagContext(Tag.Create(KEY, VALUE)));
            Assert.Equal(CreateEmptyViewData(view), viewManager.GetView(VIEW_NAME));
        }

        [Fact]
        public void Record_StatsReenabled()
        {
            IView view =
                View.Create(
                    VIEW_NAME,
                    "description",
                    MEASURE_DOUBLE,
                    Sum.Create(),
                    new List<ITagKey>() { KEY });

            viewManager.RegisterView(view);

            statsComponent.State = StatsCollectionState.DISABLED;
            statsRecorder
                .NewMeasureMap()
                .Put(MEASURE_DOUBLE, 1.0)
                .Record(new SimpleTagContext(Tag.Create(KEY, VALUE)));
            Assert.Equal(CreateEmptyViewData(view), viewManager.GetView(VIEW_NAME));

            statsComponent.State = StatsCollectionState.ENABLED;
            Assert.Empty(viewManager.GetView(VIEW_NAME).AggregationMap);
            //assertThat(viewManager.getView(VIEW_NAME).getWindowData())
            //    .isNotEqualTo(CumulativeData.Create(ZERO_TIMESTAMP, ZERO_TIMESTAMP));
            statsRecorder
                .NewMeasureMap()
                .Put(MEASURE_DOUBLE, 4.0)
                .Record(new SimpleTagContext(Tag.Create(KEY, VALUE)));
            TagValues tv = TagValues.Create(new List<ITagValue>() { VALUE });
            StatsTestUtil.AssertAggregationMapEquals(
                viewManager.GetView(VIEW_NAME).AggregationMap,
                new Dictionary<TagValues, IAggregationData>()
                {
                    { tv,  StatsTestUtil.CreateAggregationData(Sum.Create(), MEASURE_DOUBLE, 4.0) }
                },
                1e-6);
        }

        // Create an empty ViewData with the given View.
        static IViewData CreateEmptyViewData(IView view)
        {
            return ViewData.Create(
                view,
                new Dictionary<TagValues, IAggregationData>(),
                ZERO_TIMESTAMP, ZERO_TIMESTAMP);

        }
        class SimpleTagContext : TagContextBase
        {
            private readonly IList<ITag> tags;

            public SimpleTagContext(params ITag[] tags)
            {
                this.tags = new List<ITag>(tags);
            }

            public override IEnumerator<ITag> GetEnumerator()
            {
                return tags.GetEnumerator();
            }

        }
    }
}
