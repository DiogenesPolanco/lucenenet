using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Util
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

    using MaxBytesLengthExceededException = Lucene.Net.Util.BytesRefHash.MaxBytesLengthExceededException;

    [TestFixture]
    public class TestBytesRefHash : LuceneTestCase
    {
        internal BytesRefHash Hash;
        internal ByteBlockPool Pool;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Pool = NewPool();
            Hash = NewHash(Pool);
        }

        private ByteBlockPool NewPool()
        {
            return Random.NextBoolean() && Pool != null ? Pool : new ByteBlockPool(new RecyclingByteBlockAllocator(ByteBlockPool.BYTE_BLOCK_SIZE, Random.Next(25)));
        }

        private BytesRefHash NewHash(ByteBlockPool blockPool)
        {
            int initSize = 2 << 1 + Random.Next(5);
            return Random.NextBoolean() ? new BytesRefHash(blockPool) : new BytesRefHash(blockPool, initSize, new BytesRefHash.DirectBytesStartArray(initSize));
        }

        /// <summary>
        /// Test method for <seealso cref="Lucene.Net.Util.BytesRefHash#size()"/>.
        /// </summary>
        [Test]
        public virtual void TestSize()
        {
            BytesRef @ref = new BytesRef();
            int num = AtLeast(2);
            for (int j = 0; j < num; j++)
            {
                int mod = 1 + Random.Next(39);
                for (int i = 0; i < 797; i++)
                {
                    string str;
                    do
                    {
                        str = TestUtil.RandomRealisticUnicodeString(Random, 1000);
                    } while (str.Length == 0);
                    @ref.CopyChars(str);
                    int count = Hash.Count;
                    int key = Hash.Add(@ref);
                    if (key < 0)
                    {
                        Assert.AreEqual(Hash.Count, count);
                    }
                    else
                    {
                        Assert.AreEqual(Hash.Count, count + 1);
                    }
                    if (i % mod == 0)
                    {
                        Hash.Clear();
                        Assert.AreEqual(0, Hash.Count);
                        Hash.Reinit();
                    }
                }
            }
        }

        /// <summary>
        /// Test method for
        /// <seealso cref="Lucene.Net.Util.BytesRefHash#get(int, BytesRef)"/>
        /// .
        /// </summary>
        [Test]
        public virtual void TestGet()
        {
            BytesRef @ref = new BytesRef();
            BytesRef scratch = new BytesRef();
            int num = AtLeast(2);
            for (int j = 0; j < num; j++)
            {
                IDictionary<string, int?> strings = new Dictionary<string, int?>();
                int uniqueCount = 0;
                for (int i = 0; i < 797; i++)
                {
                    string str;
                    do
                    {
                        str = TestUtil.RandomRealisticUnicodeString(Random, 1000);
                    } while (str.Length == 0);
                    @ref.CopyChars(str);
                    int count = Hash.Count;
                    int key = Hash.Add(@ref);
                    if (key >= 0)
                    {
                        Assert.IsFalse(strings.ContainsKey(str));
                        strings[str] = Convert.ToInt32(key);
                        Assert.AreEqual(uniqueCount, key);
                        uniqueCount++;
                        Assert.AreEqual(Hash.Count, count + 1);
                    }
                    else
                    {
                        Assert.IsTrue((-key) - 1 < count);
                        Assert.AreEqual(Hash.Count, count);
                    }
                }
                foreach (KeyValuePair<string, int?> entry in strings)
                {
                    @ref.CopyChars(entry.Key);
                    Assert.AreEqual(@ref, Hash.Get((int)entry.Value, scratch));
                }
                Hash.Clear();
                Assert.AreEqual(0, Hash.Count);
                Hash.Reinit();
            }
        }

        /// <summary>
        /// Test method for <seealso cref="Lucene.Net.Util.BytesRefHash#compact()"/>.
        /// </summary>
        [Test]
        public virtual void TestCompact()
        {
            BytesRef @ref = new BytesRef();
            int num = AtLeast(2);
            for (int j = 0; j < num; j++)
            {
                int numEntries = 0;
                const int size = 797;
                BitArray bits = new BitArray(size);
                for (int i = 0; i < size; i++)
                {
                    string str;
                    do
                    {
                        str = TestUtil.RandomRealisticUnicodeString(Random, 1000);
                    } while (str.Length == 0);
                    @ref.CopyChars(str);
                    int key = Hash.Add(@ref);
                    if (key < 0)
                    {
                        Assert.IsTrue(bits.SafeGet((-key) - 1));
                    }
                    else
                    {
                        Assert.IsFalse(bits.SafeGet(key));
                        bits.SafeSet(key, true);
                        numEntries++;
                    }
                }
                Assert.AreEqual(Hash.Count, bits.Cardinality());
                Assert.AreEqual(numEntries, bits.Cardinality());
                Assert.AreEqual(numEntries, Hash.Count);
                int[] compact = Hash.Compact();
                Assert.IsTrue(numEntries < compact.Length);
                for (int i = 0; i < numEntries; i++)
                {
                    bits.SafeSet(compact[i], false);
                }
                Assert.AreEqual(0, bits.Cardinality());
                Hash.Clear();
                Assert.AreEqual(0, Hash.Count);
                Hash.Reinit();
            }
        }

        /// <summary>
        /// Test method for
        /// <seealso cref="Lucene.Net.Util.BytesRefHash#sort(java.util.Comparer)"/>.
        /// </summary>
        [Test]
        public virtual void TestSort()
        {
            BytesRef @ref = new BytesRef();
            int num = AtLeast(2);
            for (int j = 0; j < num; j++)
            {
                // LUCENENET specific - to ensure sorting strings works the same in the SortedSet,
                // we need to use StringComparer.Ordinal, which compares strings the same
                // way they are done in Java.
                SortedSet<string> strings = new SortedSet<string>(StringComparer.Ordinal);
                for (int k = 0; k < 797; k++)
                {
                    string str;
                    do
                    {
                        str = TestUtil.RandomRealisticUnicodeString(Random, 1000);
                    } while (str.Length == 0);
                    @ref.CopyChars(str);
                    Hash.Add(@ref);
                    strings.Add(str);
                }
                // We use the UTF-16 comparer here, because we need to be able to
                // compare to native String.CompareTo() [UTF-16]:
#pragma warning disable 612, 618
                int[] sort = Hash.Sort(BytesRef.UTF8SortedAsUTF16Comparer);
#pragma warning restore 612, 618
                Assert.IsTrue(strings.Count < sort.Length);
                int i = 0;
                BytesRef scratch = new BytesRef();
                foreach (string @string in strings)
                {
                    @ref.CopyChars(@string);
                    Assert.AreEqual(@ref, Hash.Get(sort[i++], scratch));
                }
                Hash.Clear();
                Assert.AreEqual(0, Hash.Count);
                Hash.Reinit();
            }
        }

        /// <summary>
        /// Test method for
        /// <seealso cref="Lucene.Net.Util.BytesRefHash#add(Lucene.Net.Util.BytesRef)"/>
        /// .
        /// </summary>
        [Test]
        public virtual void TestAdd()
        {
            BytesRef @ref = new BytesRef();
            BytesRef scratch = new BytesRef();
            int num = AtLeast(2);
            for (int j = 0; j < num; j++)
            {
                HashSet<string> strings = new HashSet<string>();
                int uniqueCount = 0;
                for (int i = 0; i < 797; i++)
                {
                    string str;
                    do
                    {
                        str = TestUtil.RandomRealisticUnicodeString(Random, 1000);
                    } while (str.Length == 0);
                    @ref.CopyChars(str);
                    int count = Hash.Count;
                    int key = Hash.Add(@ref);

                    if (key >= 0)
                    {
                        Assert.IsTrue(strings.Add(str));
                        Assert.AreEqual(uniqueCount, key);
                        Assert.AreEqual(Hash.Count, count + 1);
                        uniqueCount++;
                    }
                    else
                    {
                        Assert.IsFalse(strings.Add(str));
                        Assert.IsTrue((-key) - 1 < count);
                        Assert.AreEqual(str, Hash.Get((-key) - 1, scratch).Utf8ToString());
                        Assert.AreEqual(count, Hash.Count);
                    }
                }

                AssertAllIn(strings, Hash);
                Hash.Clear();
                Assert.AreEqual(0, Hash.Count);
                Hash.Reinit();
            }
        }

        [Test]
        public virtual void TestFind()
        {
            BytesRef @ref = new BytesRef();
            BytesRef scratch = new BytesRef();
            int num = AtLeast(2);
            for (int j = 0; j < num; j++)
            {
                HashSet<string> strings = new HashSet<string>();
                int uniqueCount = 0;
                for (int i = 0; i < 797; i++)
                {
                    string str;
                    do
                    {
                        str = TestUtil.RandomRealisticUnicodeString(Random, 1000);
                    } while (str.Length == 0);
                    @ref.CopyChars(str);
                    int count = Hash.Count;
                    int key = Hash.Find(@ref); //hash.Add(ref);
                    if (key >= 0) // string found in hash
                    {
                        Assert.IsFalse(strings.Add(str));
                        Assert.IsTrue(key < count);
                        Assert.AreEqual(str, Hash.Get(key, scratch).Utf8ToString());
                        Assert.AreEqual(count, Hash.Count);
                    }
                    else
                    {
                        key = Hash.Add(@ref);
                        Assert.IsTrue(strings.Add(str));
                        Assert.AreEqual(uniqueCount, key);
                        Assert.AreEqual(Hash.Count, count + 1);
                        uniqueCount++;
                    }
                }

                AssertAllIn(strings, Hash);
                Hash.Clear();
                Assert.AreEqual(0, Hash.Count);
                Hash.Reinit();
            }
        }

        [Test]
        public virtual void TestLargeValue()
        {
            int[] sizes = { Random.Next(5), ByteBlockPool.BYTE_BLOCK_SIZE - 33 + Random.Next(31), ByteBlockPool.BYTE_BLOCK_SIZE - 1 + Random.Next(37) };
            BytesRef @ref = new BytesRef();

            var exceptionThrown = false;

            for (int i = 0; i < sizes.Length; i++)
            {
                @ref.Bytes = new byte[sizes[i]];
                @ref.Offset = 0;
                @ref.Length = sizes[i];
                try
                {
                    Assert.AreEqual(i, Hash.Add(@ref));
                }
#pragma warning disable 168
                catch (MaxBytesLengthExceededException e)
#pragma warning restore 168
                {
                    exceptionThrown = true;
                    if (i < sizes.Length - 1)
                    {
                        Assert.Fail("unexpected exception at size: " + sizes[i]);
                    }
                }
            }

            Assert.True(exceptionThrown, "Expected that MaxBytesLengthExceededException would be thrown at least once.");
        }

        /// <summary>
        /// Test method for
        /// <seealso cref="Lucene.Net.Util.BytesRefHash#addByPoolOffset(int)"/>
        /// .
        /// </summary>
        [Test]
        public virtual void TestAddByPoolOffset()
        {
            BytesRef @ref = new BytesRef();
            BytesRef scratch = new BytesRef();
            BytesRefHash offsetHash = NewHash(Pool);
            int num = AtLeast(2);
            for (int j = 0; j < num; j++)
            {
                HashSet<string> strings = new HashSet<string>();
                int uniqueCount = 0;
                for (int i = 0; i < 797; i++)
                {
                    string str;
                    do
                    {
                        str = TestUtil.RandomRealisticUnicodeString(Random, 1000);
                    } while (str.Length == 0);
                    @ref.CopyChars(str);
                    int count = Hash.Count;
                    int key = Hash.Add(@ref);

                    if (key >= 0)
                    {
                        Assert.IsTrue(strings.Add(str));
                        Assert.AreEqual(uniqueCount, key);
                        Assert.AreEqual(Hash.Count, count + 1);
                        int offsetKey = offsetHash.AddByPoolOffset(Hash.ByteStart(key));
                        Assert.AreEqual(uniqueCount, offsetKey);
                        Assert.AreEqual(offsetHash.Count, count + 1);
                        uniqueCount++;
                    }
                    else
                    {
                        Assert.IsFalse(strings.Add(str));
                        Assert.IsTrue((-key) - 1 < count);
                        Assert.AreEqual(str, Hash.Get((-key) - 1, scratch).Utf8ToString());
                        Assert.AreEqual(count, Hash.Count);
                        int offsetKey = offsetHash.AddByPoolOffset(Hash.ByteStart((-key) - 1));
                        Assert.IsTrue((-offsetKey) - 1 < count);
                        Assert.AreEqual(str, Hash.Get((-offsetKey) - 1, scratch).Utf8ToString());
                        Assert.AreEqual(count, Hash.Count);
                    }
                }

                AssertAllIn(strings, Hash);
                foreach (string @string in strings)
                {
                    @ref.CopyChars(@string);
                    int key = Hash.Add(@ref);
                    BytesRef bytesRef = offsetHash.Get((-key) - 1, scratch);
                    Assert.AreEqual(@ref, bytesRef);
                }

                Hash.Clear();
                Assert.AreEqual(0, Hash.Count);
                offsetHash.Clear();
                Assert.AreEqual(0, offsetHash.Count);
                Hash.Reinit(); // init for the next round
                offsetHash.Reinit();
            }
        }

        private void AssertAllIn(ISet<string> strings, BytesRefHash hash)
        {
            BytesRef @ref = new BytesRef();
            BytesRef scratch = new BytesRef();
            int count = hash.Count;
            foreach (string @string in strings)
            {
                @ref.CopyChars(@string);
                int key = hash.Add(@ref); // add again to check duplicates
                Assert.AreEqual(@string, hash.Get((-key) - 1, scratch).Utf8ToString());
                Assert.AreEqual(count, hash.Count);
                Assert.IsTrue(key < count, "key: " + key + " count: " + count + " string: " + @string);
            }
        }
    }
}