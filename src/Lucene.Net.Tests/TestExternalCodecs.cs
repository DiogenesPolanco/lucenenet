using System;
using Lucene.Net.Codecs;
using Lucene.Net.Support;
using NUnit.Framework;
using Console = Lucene.Net.Support.SystemConsole;

namespace Lucene.Net
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
    using PostingsFormat = Lucene.Net.Codecs.PostingsFormat;
    using Lucene46Codec = Lucene.Net.Codecs.Lucene46.Lucene46Codec;
    using Document = Lucene.Net.Documents.Document;
    using Field = Lucene.Net.Documents.Field;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using Term = Lucene.Net.Index.Term;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;


    /* Intentionally outside of oal.index to verify fully
	   external codecs work fine */

    public class TestExternalCodecs : LuceneTestCase
    {
        private sealed class CustomPerFieldCodec : Lucene46Codec
        {

            internal readonly PostingsFormat RamFormat = Codecs.PostingsFormat.ForName("RAMOnly");
            internal readonly PostingsFormat DefaultFormat = Codecs.PostingsFormat.ForName("Lucene41");
            internal readonly PostingsFormat PulsingFormat = Codecs.PostingsFormat.ForName("Pulsing41");

            public override PostingsFormat GetPostingsFormatForField(string field)
            {
                if (field.Equals("field2", StringComparison.Ordinal) || field.Equals("id", StringComparison.Ordinal))
                {
                    return PulsingFormat;
                }
                else if (field.Equals("field1", StringComparison.Ordinal))
                {
                    return DefaultFormat;
                }
                else
                {
                    return RamFormat;
                }
            }
        }

        // tests storing "id" and "field2" fields as pulsing codec,
        // whose term sort is backwards unicode code point, and
        // storing "field1" as a custom entirely-in-RAM codec
        [Test]
        public virtual void TestPerFieldCodec()
        {

            int NUM_DOCS = AtLeast(173);
            if (VERBOSE)
            {
                Console.WriteLine("TEST: NUM_DOCS=" + NUM_DOCS);
            }

            using (BaseDirectoryWrapper dir = NewDirectory())
            {
                dir.CheckIndexOnDispose = false; // we use a custom codec provider
                using (IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetCodec(new CustomPerFieldCodec()).SetMergePolicy(NewLogMergePolicy(3))))
                {
                    Documents.Document doc = new Documents.Document();
                    // uses default codec:
                    doc.Add(NewTextField("field1", "this field uses the standard codec as the test", Field.Store.NO));
                    // uses pulsing codec:
                    Field field2 = NewTextField("field2", "this field uses the pulsing codec as the test", Field.Store.NO);
                    doc.Add(field2);

                    Field idField = NewStringField("id", "", Field.Store.NO);

                    doc.Add(idField);
                    for (int i = 0; i < NUM_DOCS; i++)
                    {
                        idField.SetStringValue("" + i);
                        w.AddDocument(doc);
                        if ((i + 1) % 10 == 0)
                        {
                            w.Commit();
                        }
                    }
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: now delete id=77");
                    }
                    w.DeleteDocuments(new Term("id", "77"));

                    using (IndexReader r = DirectoryReader.Open(w, true))
                    {
                        Assert.AreEqual(NUM_DOCS - 1, r.NumDocs);
                        IndexSearcher s = NewSearcher(r);
                        Assert.AreEqual(NUM_DOCS - 1, s.Search(new TermQuery(new Term("field1", "standard")), 1).TotalHits);
                        Assert.AreEqual(NUM_DOCS - 1, s.Search(new TermQuery(new Term("field2", "pulsing")), 1).TotalHits);
                    }

                    if (VERBOSE)
                    {
                        Console.WriteLine("\nTEST: now delete 2nd doc");
                    }
                    w.DeleteDocuments(new Term("id", "44"));

                    if (VERBOSE)
                    {
                        Console.WriteLine("\nTEST: now force merge");
                    }
                    w.ForceMerge(1);
                    if (VERBOSE)
                    {
                        Console.WriteLine("\nTEST: now open reader");
                    }
                    using (IndexReader r = DirectoryReader.Open(w, true))
                    {
                        Assert.AreEqual(NUM_DOCS - 2, r.MaxDoc);
                        Assert.AreEqual(NUM_DOCS - 2, r.NumDocs);
                        IndexSearcher s = NewSearcher(r);
                        Assert.AreEqual(NUM_DOCS - 2, s.Search(new TermQuery(new Term("field1", "standard")), 1).TotalHits);
                        Assert.AreEqual(NUM_DOCS - 2, s.Search(new TermQuery(new Term("field2", "pulsing")), 1).TotalHits);
                        Assert.AreEqual(1, s.Search(new TermQuery(new Term("id", "76")), 1).TotalHits);
                        Assert.AreEqual(0, s.Search(new TermQuery(new Term("id", "77")), 1).TotalHits);
                        Assert.AreEqual(0, s.Search(new TermQuery(new Term("id", "44")), 1).TotalHits);

                        if (VERBOSE)
                        {
                            Console.WriteLine("\nTEST: now close NRT reader");
                        }
                    }
                }
            }
        }
    }
}