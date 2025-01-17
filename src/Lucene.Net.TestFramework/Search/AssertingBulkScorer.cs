using Lucene.Net.Util;
using System;
using Debug = Lucene.Net.Diagnostics.Debug; // LUCENENET NOTE: We cannot use System.Diagnostics.Debug because those calls will be optimized out of the release!

namespace Lucene.Net.Search
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    using DocsEnum = Lucene.Net.Index.DocsEnum;

    /// <summary>
    /// Wraps a <see cref="Scorer"/> with additional checks. </summary>
    public class AssertingBulkScorer : BulkScorer
    {
        private static readonly VirtualMethod SCORE_COLLECTOR = new VirtualMethod(typeof(BulkScorer), "Score", typeof(ICollector));
        private static readonly VirtualMethod SCORE_COLLECTOR_RANGE = new VirtualMethod(typeof(BulkScorer), "Score", typeof(ICollector), typeof(int));

        public static BulkScorer Wrap(Random random, BulkScorer other)
        {
            if (other == null || other is AssertingBulkScorer)
            {
                return other;
            }
            return new AssertingBulkScorer(random, other);
        }

        public static bool ShouldWrap(BulkScorer inScorer)
        {
            return SCORE_COLLECTOR.IsOverriddenAsOf(inScorer.GetType()) || SCORE_COLLECTOR_RANGE.IsOverriddenAsOf(inScorer.GetType());
        }

        internal readonly Random random;
        internal readonly BulkScorer @in;

        private AssertingBulkScorer(Random random, BulkScorer @in)
        {
            this.random = random;
            this.@in = @in;
        }

        public virtual BulkScorer In
        {
            get
            {
                return @in;
            }
        }

        public override void Score(ICollector collector)
        {
            if (random.NextBoolean())
            {
                try
                {
                    bool remaining = @in.Score(collector, DocsEnum.NO_MORE_DOCS);
                    Debug.Assert(!remaining);
                }
#pragma warning disable 168
                catch (System.NotSupportedException e)
#pragma warning restore 168
                {
                    @in.Score(collector);
                }
            }
            else
            {
                @in.Score(collector);
            }
        }

        public override bool Score(ICollector collector, int max)
        {
            return @in.Score(collector, max);
        }

        public override string ToString()
        {
            return "AssertingBulkScorer(" + @in + ")";
        }
    }
}