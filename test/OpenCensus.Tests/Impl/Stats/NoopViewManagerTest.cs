﻿using OpenCensus.Common;
using OpenCensus.Stats.Aggregations;
using OpenCensus.Stats.Measures;
using OpenCensus.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OpenCensus.Stats.Test
{
    public class NoopViewManagerTest
    {
        private static readonly IMeasureDouble MEASURE = MeasureDouble.Create("my measure", "description", "s");
        private static readonly ITagKey KEY = TagKey.Create("KEY");
        private static readonly IViewName VIEW_NAME = ViewName.Create("my view");
        private static readonly String VIEW_DESCRIPTION = "view description";
        private static readonly ISum AGGREGATION = Sum.Create();
        //private static readonly Cumulative CUMULATIVE = Cumulative.create();
        private static readonly IDuration TEN_SECONDS = Duration.Create(10, 0);
        //private static readonly Interval INTERVAL = Interval.create(TEN_SECONDS);

        //@Rule public readonly ExpectedException thrown = ExpectedException.none();

        [Fact]
        public void NoopViewManager_RegisterView_DisallowRegisteringDifferentViewWithSameName()
        {
            IView view1 =
                View.Create(
                    VIEW_NAME, "description 1", MEASURE, AGGREGATION, new List<ITagKey> { KEY });
            IView view2 =
                View.Create(
                    VIEW_NAME, "description 2", MEASURE, AGGREGATION, new List<ITagKey> { KEY });
            IViewManager viewManager = NoopStats.NewNoopViewManager();
            viewManager.RegisterView(view1);

            try
            {
                Assert.Throws<ArgumentException>(() =>viewManager.RegisterView(view2));
            }
            finally
            {
                Assert.Equal(view1, viewManager.GetView(VIEW_NAME).View);
            }
        }

        [Fact]
        public void NoopViewManager_RegisterView_AllowRegisteringSameViewTwice()
        {
            IView view =
                View.Create(
                    VIEW_NAME, VIEW_DESCRIPTION, MEASURE, AGGREGATION, new List<ITagKey> { KEY });
            IViewManager viewManager = NoopStats.NewNoopViewManager();
            viewManager.RegisterView(view);
            viewManager.RegisterView(view);
        }

        [Fact]
        public void NoopViewManager_RegisterView_DisallowNull()
        {
            IViewManager viewManager = NoopStats.NewNoopViewManager();
            Assert.Throws<ArgumentNullException>(() => viewManager.RegisterView(null));
        }

        [Fact]
        public void NoopViewManager_GetView_GettingNonExistentViewReturnsNull()
        {
            IViewManager viewManager = NoopStats.NewNoopViewManager();
            Assert.Null(viewManager.GetView(VIEW_NAME));
        }

        [Fact]
        public void NoopViewManager_GetView_Cumulative()
        {
            IView view =
                View.Create(
                    VIEW_NAME, VIEW_DESCRIPTION, MEASURE, AGGREGATION, new List<ITagKey> { KEY });
            IViewManager viewManager = NoopStats.NewNoopViewManager();
            viewManager.RegisterView(view);

            IViewData viewData = viewManager.GetView(VIEW_NAME);
            Assert.Equal(view, viewData.View);
            Assert.Empty(viewData.AggregationMap);
            Assert.Equal(Timestamp.Create(0, 0), viewData.Start);
            Assert.Equal(Timestamp.Create(0, 0), viewData.End);

        }

        [Fact]
        public void noopViewManager_GetView_Interval()
        {
            IView view =
                View.Create(
                    VIEW_NAME, VIEW_DESCRIPTION, MEASURE, AGGREGATION, new List<ITagKey> { KEY });
            IViewManager viewManager = NoopStats.NewNoopViewManager();
            viewManager.RegisterView(view);

            IViewData viewData = viewManager.GetView(VIEW_NAME);
            Assert.Equal(view, viewData.View);
            Assert.Empty(viewData.AggregationMap);
            Assert.Equal(Timestamp.Create(0, 0), viewData.Start);
            Assert.Equal(Timestamp.Create(0, 0), viewData.End);

        }

        [Fact]
        public void NoopViewManager_GetView_DisallowNull()
        {
            IViewManager viewManager = NoopStats.NewNoopViewManager();
            Assert.Throws<ArgumentNullException>(() =>viewManager.GetView(null));
        }

        [Fact]
        public void GetAllExportedViews()
        {
            IViewManager viewManager = NoopStats.NewNoopViewManager();
            Assert.Empty(viewManager.AllExportedViews);
            IView cumulativeView1 =
                View.Create(
                    ViewName.Create("View 1"),
                    VIEW_DESCRIPTION,
                    MEASURE,
                    AGGREGATION,
                    new List<ITagKey> { KEY });
            IView cumulativeView2 =
                View.Create(
                    ViewName.Create("View 2"),
                    VIEW_DESCRIPTION,
                    MEASURE,
                    AGGREGATION,
                    new List<ITagKey> { KEY });


            viewManager.RegisterView(cumulativeView1);
            viewManager.RegisterView(cumulativeView2);

            // Only cumulative views should be exported.
            Assert.Equal(2, viewManager.AllExportedViews.Count);
            Assert.Contains(cumulativeView1, viewManager.AllExportedViews);
            Assert.Contains(cumulativeView2, viewManager.AllExportedViews);
        }

        [Fact]
        public void GetAllExportedViews_ResultIsUnmodifiable()
        {
            IViewManager viewManager = NoopStats.NewNoopViewManager();
            IView view1 =
                View.Create(
                    ViewName.Create("View 1"), VIEW_DESCRIPTION, MEASURE, AGGREGATION, new List<ITagKey> { KEY });
            viewManager.RegisterView(view1);
            ISet<IView> exported = viewManager.AllExportedViews;

            IView view2 =
                View.Create(
                    ViewName.Create("View 2"), VIEW_DESCRIPTION, MEASURE, AGGREGATION, new List<ITagKey> { KEY });
            Assert.Throws<NotSupportedException>(() => exported.Add(view2));
        }
    }
}
