using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Threading;
using Console = Lucene.Net.Support.SystemConsole;

namespace Lucene.Net.Index
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

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;

    [TestFixture]
    public class TestStressIndexing : LuceneTestCase
    {
        private abstract class TimedThread : ThreadClass
        {
            internal volatile bool Failed;
            internal int Count;
            internal static int RUN_TIME_MSEC = AtLeast(1000);
            internal TimedThread[] AllThreads;

            public abstract void DoWork();

            internal TimedThread(TimedThread[] threads)
            {
                this.AllThreads = threads;
            }

            public override void Run()
            {
                long stopTime = Environment.TickCount + RUN_TIME_MSEC;

                Count = 0;

                try
                {
                    do
                    {
                        if (AnyErrors())
                        {
                            break;
                        }
                        DoWork();
                        Count++;
                    } while (Environment.TickCount < stopTime);
                }
                catch (Exception e)
                {
                    Console.WriteLine(Thread.CurrentThread + ": exc");
                    Console.WriteLine(e.StackTrace);
                    Failed = true;
                }
            }

            internal virtual bool AnyErrors()
            {
                for (int i = 0; i < AllThreads.Length; i++)
                {
                    if (AllThreads[i] != null && AllThreads[i].Failed)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private class IndexerThread : TimedThread
        {
            private readonly Func<string, string, Field.Store, Field> NewStringFieldFunc;
            private readonly Func<string, string, Field.Store, Field> NewTextFieldFunc;

            internal IndexWriter Writer;
            internal int NextID;

            /// <param name="newStringField">
            /// LUCENENET specific
            /// Passed in because <see cref="LuceneTestCase.NewStringField(string, string, Field.Store)"/>
            /// is no longer static.
            /// </param>
            /// <param name="newTextField">
            /// LUCENENET specific
            /// Passed in because <see cref="LuceneTestCase.NewTextField(string, string, Field.Store)"/>
            /// is no longer static.
            /// </param>
            public IndexerThread(IndexWriter writer, TimedThread[] threads,
                Func<string, string, Field.Store, Field> newStringField,
                Func<string, string, Field.Store, Field> newTextField)
                : base(threads)
            {
                this.Writer = writer;
                NewStringFieldFunc = newStringField;
                NewTextFieldFunc = newTextField;
            }

            public override void DoWork()
            {
                // Add 10 docs:
                for (int j = 0; j < 10; j++)
                {
                    Documents.Document d = new Documents.Document();
                    int n = Random.Next();
                    d.Add(NewStringFieldFunc("id", Convert.ToString(NextID++), Field.Store.YES));
                    d.Add(NewTextFieldFunc("contents", English.Int32ToEnglish(n), Field.Store.NO));
                    Writer.AddDocument(d);
                }

                // Delete 5 docs:
                int deleteID = NextID - 1;
                for (int j = 0; j < 5; j++)
                {
                    Writer.DeleteDocuments(new Term("id", "" + deleteID));
                    deleteID -= 2;
                }
            }
        }

        private class SearcherThread : TimedThread
        {
            internal Directory Directory;
            private readonly LuceneTestCase OuterInstance;

            /// <param name="outerInstance">
            /// LUCENENET specific
            /// Passed in because <see cref="LuceneTestCase.NewSearcher(IndexReader)"/>
            /// is no longer static.
            /// </param>
            public SearcherThread(Directory directory, TimedThread[] threads, LuceneTestCase outerInstance)
                : base(threads)
            {
                OuterInstance = outerInstance;
                this.Directory = directory;
            }

            public override void DoWork()
            {
                for (int i = 0; i < 100; i++)
                {
                    IndexReader ir = DirectoryReader.Open(Directory);
                    IndexSearcher @is =
#if FEATURE_INSTANCE_TESTDATA_INITIALIZATION
                        OuterInstance.
#endif
                        NewSearcher(ir);
                    ir.Dispose();
                }
                Count += 100;
            }
        }

        /*
          Run one indexer and 2 searchers against single index as
          stress test.
        */

        public virtual void RunStressTest(Directory directory, IConcurrentMergeScheduler mergeScheduler)
        {
            IndexWriter modifier = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE).SetMaxBufferedDocs(10).SetMergeScheduler(mergeScheduler));
            modifier.Commit();

            TimedThread[] threads = new TimedThread[4];
            int numThread = 0;

            // One modifier that writes 10 docs then removes 5, over
            // and over:
            IndexerThread indexerThread = new IndexerThread(modifier, threads, NewStringField, NewTextField);
            threads[numThread++] = indexerThread;
            indexerThread.Start();

            IndexerThread indexerThread2 = new IndexerThread(modifier, threads, NewStringField, NewTextField);
            threads[numThread++] = indexerThread2;
            indexerThread2.Start();

            // Two searchers that constantly just re-instantiate the
            // searcher:
            SearcherThread searcherThread1 = new SearcherThread(directory, threads, this);
            threads[numThread++] = searcherThread1;
            searcherThread1.Start();

            SearcherThread searcherThread2 = new SearcherThread(directory, threads, this);
            threads[numThread++] = searcherThread2;
            searcherThread2.Start();

            for (int i = 0; i < numThread; i++)
            {
                threads[i].Join();
            }

            modifier.Dispose();

            for (int i = 0; i < numThread; i++)
            {
                Assert.IsTrue(!threads[i].Failed);
            }

            //System.out.println("    Writer: " + indexerThread.count + " iterations");
            //System.out.println("Searcher 1: " + searcherThread1.count + " searchers created");
            //System.out.println("Searcher 2: " + searcherThread2.count + " searchers created");
        }

        /*
          Run above stress test against RAMDirectory and then
          FSDirectory.
        */

        [Test]
        public virtual void TestStressIndexAndSearching([ValueSource(typeof(ConcurrentMergeSchedulerFactories), "Values")]Func<IConcurrentMergeScheduler> newScheduler)
        {
            Directory directory = NewDirectory();
            MockDirectoryWrapper wrapper = directory as MockDirectoryWrapper;
            if (wrapper != null)
            {
                wrapper.AssertNoUnreferencedFilesOnClose = true;
            }

            RunStressTest(directory, newScheduler());
            directory.Dispose();
        }
    }
}