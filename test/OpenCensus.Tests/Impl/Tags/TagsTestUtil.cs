﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCensus.Tags.Test
{
    internal static class TagsTestUtil
    {
        public static ICollection<ITag> TagContextToList(ITagContext tags)
        {
            return tags.ToList();
        }
    }
}
