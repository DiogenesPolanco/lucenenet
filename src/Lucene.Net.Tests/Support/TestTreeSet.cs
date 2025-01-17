﻿/*
 Copyright (c) 2003-2016 Niels Kokholm, Peter Sestoft, and Rasmus Lystrøm
 Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:
 
 The above copyright notice and this permission notice shall be included in
 all copies or substantial portions of the Software.
 
 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 SOFTWARE.
*/

using System;
using System.Globalization;
using Lucene.Net.Attributes;
using Lucene.Net.Support.C5;
using NUnit.Framework;
using SCG = System.Collections.Generic;

namespace Lucene.Net.Support.RBTreeSet
{
    using CollectionOfInt = TreeSet<int>;

    [TestFixture]
    public class GenericTesters
    {
        [Test, LuceneNetSpecific]
        public void TestEvents()
        {
            Func<CollectionOfInt> factory = delegate () { return new CollectionOfInt(TenEqualityComparer.Default); };
            new Templates.Events.SortedIndexedTester<CollectionOfInt>().Test(factory, MemoryType.Normal);
        }

        //[Test, LuceneNetSpecific]
        //public void Extensible()
        //{
        //    C5UnitTests.Templates.Extensible.Clone.Tester<CollectionOfInt>();
        //    C5UnitTests.Templates.Extensible.Serialization.Tester<CollectionOfInt>();
        //}
    }

    static class Factory
    {
        public static ICollection<T> New<T>() { return new TreeSet<T>(); }
    }


    [TestFixture]
    public class Formatting
    {
        ICollection<int> coll;
        IFormatProvider rad16;
        [SetUp]
        public void Init() { coll = Factory.New<int>(); rad16 = new RadixFormatProvider(16); }
        [TearDown]
        public void Dispose() { coll = null; rad16 = null; }
        [Test, LuceneNetSpecific]
        public void Format()
        {
            Assert.AreEqual("{  }", coll.ToString());
            coll.AddAll(new int[] { -4, 28, 129, 65530 });
            // LUCENENET specific - LuceneTestCase swaps cultures at random, therefore
            // we require the IFormatProvider to be passed to make this test stable.
            Assert.AreEqual("{ -4, 28, 129, 65530 }", coll.ToString(null, CultureInfo.InvariantCulture));
            Assert.AreEqual("{ -4, 1C, 81, FFFA }", coll.ToString(null, rad16));
            // LUCENENET specific - LuceneTestCase swaps cultures at random, therefore
            // passing a null here will have random results, making the comparison fail under
            // certain cultures. We fix this by passing CultureInfo.InvariantCulture instead of null.
            Assert.AreEqual("{ -4, 28, 129... }", coll.ToString("L14", CultureInfo.InvariantCulture));
            Assert.AreEqual("{ -4, 1C, 81... }", coll.ToString("L14", rad16));
        }
    }

    [TestFixture]
    public class Combined
    {
        private IIndexedSorted<KeyValuePair<int, int>> lst;


        [SetUp]
        public void Init()
        {
            lst = new TreeSet<KeyValuePair<int, int>>(new KeyValuePairComparer<int, int>(new IC()));
            for (int i = 0; i < 10; i++)
                lst.Add(new KeyValuePair<int, int>(i, i + 30));
        }


        [TearDown]
        public void Dispose() { lst = null; }


        [Test, LuceneNetSpecific]
        public void Find()
        {
            KeyValuePair<int, int> p = new KeyValuePair<int, int>(3, 78);

            Assert.IsTrue(lst.Find(ref p));
            Assert.AreEqual(3, p.Key);
            Assert.AreEqual(33, p.Value);
            p = new KeyValuePair<int, int>(13, 78);
            Assert.IsFalse(lst.Find(ref p));
        }


        [Test, LuceneNetSpecific]
        public void FindOrAdd()
        {
            KeyValuePair<int, int> p = new KeyValuePair<int, int>(3, 78);

            Assert.IsTrue(lst.FindOrAdd(ref p));
            Assert.AreEqual(3, p.Key);
            Assert.AreEqual(33, p.Value);
            p = new KeyValuePair<int, int>(13, 79);
            Assert.IsFalse(lst.FindOrAdd(ref p));
            Assert.AreEqual(13, lst[10].Key);
            Assert.AreEqual(79, lst[10].Value);
        }


        [Test, LuceneNetSpecific]
        public void Update()
        {
            KeyValuePair<int, int> p = new KeyValuePair<int, int>(3, 78);

            Assert.IsTrue(lst.Update(p));
            Assert.AreEqual(3, lst[3].Key);
            Assert.AreEqual(78, lst[3].Value);
            p = new KeyValuePair<int, int>(13, 78);
            Assert.IsFalse(lst.Update(p));
        }


        [Test, LuceneNetSpecific]
        public void UpdateOrAdd1()
        {
            KeyValuePair<int, int> p = new KeyValuePair<int, int>(3, 78);

            Assert.IsTrue(lst.UpdateOrAdd(p));
            Assert.AreEqual(3, lst[3].Key);
            Assert.AreEqual(78, lst[3].Value);
            p = new KeyValuePair<int, int>(13, 79);
            Assert.IsFalse(lst.UpdateOrAdd(p));
            Assert.AreEqual(13, lst[10].Key);
            Assert.AreEqual(79, lst[10].Value);
        }

        [Test, LuceneNetSpecific]
        public void UpdateOrAdd2()
        {
            ICollection<String> coll = new TreeSet<String>();
            // s1 and s2 are distinct objects but contain the same text:
            String old, s1 = "abc", s2 = ("def" + s1).Substring(3);
            Assert.IsFalse(coll.UpdateOrAdd(s1, out old));
            Assert.AreEqual(null, old);
            Assert.IsTrue(coll.UpdateOrAdd(s2, out old));
            Assert.IsTrue(Object.ReferenceEquals(s1, old));
            Assert.IsFalse(Object.ReferenceEquals(s2, old));
        }

        [Test, LuceneNetSpecific]
        public void RemoveWithReturn()
        {
            KeyValuePair<int, int> p = new KeyValuePair<int, int>(3, 78);

            Assert.IsTrue(lst.Remove(p, out p));
            Assert.AreEqual(3, p.Key);
            Assert.AreEqual(33, p.Value);
            Assert.AreEqual(4, lst[3].Key);
            Assert.AreEqual(34, lst[3].Value);
            p = new KeyValuePair<int, int>(13, 78);
            Assert.IsFalse(lst.Remove(p, out p));
        }
    }


    [TestFixture]
    public class Ranges
    {
        private TreeSet<int> tree;

        private SCG.IComparer<int> c;


        [SetUp]
        public void Init()
        {
            c = new IC();
            tree = new TreeSet<int>(c);
            for (int i = 1; i <= 10; i++)
            {
                tree.Add(i * 2);
            }
        }


        [Test, LuceneNetSpecific]
        public void Enumerator()
        {
            SCG.IEnumerator<int> e = tree.RangeFromTo(5, 17).GetEnumerator();
            int i = 3;

            while (e.MoveNext())
            {
                Assert.AreEqual(2 * i++, e.Current);
            }

            Assert.AreEqual(9, i);
        }


        [Test, LuceneNetSpecific]
        public void Enumerator2()
        {
            SCG.IEnumerator<int> e = tree.RangeFromTo(5, 17).GetEnumerator();
            Assert.Throws<InvalidOperationException>(() => { int i = e.Current; });
        }


        [Test, LuceneNetSpecific]
        public void Enumerator3()
        {
            SCG.IEnumerator<int> e = tree.RangeFromTo(5, 17).GetEnumerator();

            while (e.MoveNext()) ;

            Assert.Throws<InvalidOperationException>(() => { int i = e.Current; });
        }


        [Test, LuceneNetSpecific]
        public void Remove()
        {
            int[] all = new int[] { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 };

            tree.RemoveRangeFrom(18);
            Assert.IsTrue(IC.eq(tree, new int[] { 2, 4, 6, 8, 10, 12, 14, 16 }));
            tree.RemoveRangeFrom(28);
            Assert.IsTrue(IC.eq(tree, new int[] { 2, 4, 6, 8, 10, 12, 14, 16 }));
            tree.RemoveRangeFrom(2);
            Assert.IsTrue(IC.eq(tree));
            foreach (int i in all) tree.Add(i);

            tree.RemoveRangeTo(10);
            Assert.IsTrue(IC.eq(tree, new int[] { 10, 12, 14, 16, 18, 20 }));
            tree.RemoveRangeTo(2);
            Assert.IsTrue(IC.eq(tree, new int[] { 10, 12, 14, 16, 18, 20 }));
            tree.RemoveRangeTo(21);
            Assert.IsTrue(IC.eq(tree));
            foreach (int i in all) tree.Add(i);

            tree.RemoveRangeFromTo(4, 8);
            Assert.IsTrue(IC.eq(tree, 2, 8, 10, 12, 14, 16, 18, 20));
            tree.RemoveRangeFromTo(14, 28);
            Assert.IsTrue(IC.eq(tree, 2, 8, 10, 12));
            tree.RemoveRangeFromTo(0, 9);
            Assert.IsTrue(IC.eq(tree, 10, 12));
            tree.RemoveRangeFromTo(0, 81);
            Assert.IsTrue(IC.eq(tree));
        }

        [Test, LuceneNetSpecific]
        public void Normal()
        {
            int[] all = new int[] { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 };

            Assert.IsTrue(IC.eq(tree, all));
            Assert.IsTrue(IC.eq(tree.RangeAll(), all));
            Assert.AreEqual(10, tree.RangeAll().Count);
            Assert.IsTrue(IC.eq(tree.RangeFrom(11), new int[] { 12, 14, 16, 18, 20 }));
            Assert.AreEqual(5, tree.RangeFrom(11).Count);
            Assert.IsTrue(IC.eq(tree.RangeFrom(12), new int[] { 12, 14, 16, 18, 20 }));
            Assert.IsTrue(IC.eq(tree.RangeFrom(2), all));
            Assert.IsTrue(IC.eq(tree.RangeFrom(1), all));
            Assert.IsTrue(IC.eq(tree.RangeFrom(21), new int[] { }));
            Assert.IsTrue(IC.eq(tree.RangeFrom(20), new int[] { 20 }));
            Assert.IsTrue(IC.eq(tree.RangeTo(8), new int[] { 2, 4, 6 }));
            Assert.IsTrue(IC.eq(tree.RangeTo(7), new int[] { 2, 4, 6 }));
            Assert.AreEqual(3, tree.RangeTo(7).Count);
            Assert.IsTrue(IC.eq(tree.RangeTo(2), new int[] { }));
            Assert.IsTrue(IC.eq(tree.RangeTo(1), new int[] { }));
            Assert.IsTrue(IC.eq(tree.RangeTo(3), new int[] { 2 }));
            Assert.IsTrue(IC.eq(tree.RangeTo(20), new int[] { 2, 4, 6, 8, 10, 12, 14, 16, 18 }));
            Assert.IsTrue(IC.eq(tree.RangeTo(21), all));
            Assert.IsTrue(IC.eq(tree.RangeFromTo(7, 12), new int[] { 8, 10 }));
            Assert.IsTrue(IC.eq(tree.RangeFromTo(6, 11), new int[] { 6, 8, 10 }));
            Assert.IsTrue(IC.eq(tree.RangeFromTo(1, 12), new int[] { 2, 4, 6, 8, 10 }));
            Assert.AreEqual(5, tree.RangeFromTo(1, 12).Count);
            Assert.IsTrue(IC.eq(tree.RangeFromTo(2, 12), new int[] { 2, 4, 6, 8, 10 }));
            Assert.IsTrue(IC.eq(tree.RangeFromTo(6, 21), new int[] { 6, 8, 10, 12, 14, 16, 18, 20 }));
            Assert.IsTrue(IC.eq(tree.RangeFromTo(6, 20), new int[] { 6, 8, 10, 12, 14, 16, 18 }));
        }


        [Test, LuceneNetSpecific]
        public void Backwards()
        {
            int[] all = new int[] { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 };
            int[] lla = new int[] { 20, 18, 16, 14, 12, 10, 8, 6, 4, 2 };

            Assert.IsTrue(IC.eq(tree, all));
            Assert.IsTrue(IC.eq(tree.RangeAll().Backwards(), lla));
            Assert.IsTrue(IC.eq(tree.RangeFrom(11).Backwards(), new int[] { 20, 18, 16, 14, 12 }));
            Assert.IsTrue(IC.eq(tree.RangeFrom(12).Backwards(), new int[] { 20, 18, 16, 14, 12 }));
            Assert.IsTrue(IC.eq(tree.RangeFrom(2).Backwards(), lla));
            Assert.IsTrue(IC.eq(tree.RangeFrom(1).Backwards(), lla));
            Assert.IsTrue(IC.eq(tree.RangeFrom(21).Backwards(), new int[] { }));
            Assert.IsTrue(IC.eq(tree.RangeFrom(20).Backwards(), new int[] { 20 }));
            Assert.IsTrue(IC.eq(tree.RangeTo(8).Backwards(), new int[] { 6, 4, 2 }));
            Assert.IsTrue(IC.eq(tree.RangeTo(7).Backwards(), new int[] { 6, 4, 2 }));
            Assert.IsTrue(IC.eq(tree.RangeTo(2).Backwards(), new int[] { }));
            Assert.IsTrue(IC.eq(tree.RangeTo(1).Backwards(), new int[] { }));
            Assert.IsTrue(IC.eq(tree.RangeTo(3).Backwards(), new int[] { 2 }));
            Assert.IsTrue(IC.eq(tree.RangeTo(20).Backwards(), new int[] { 18, 16, 14, 12, 10, 8, 6, 4, 2 }));
            Assert.IsTrue(IC.eq(tree.RangeTo(21).Backwards(), lla));
            Assert.IsTrue(IC.eq(tree.RangeFromTo(7, 12).Backwards(), new int[] { 10, 8 }));
            Assert.IsTrue(IC.eq(tree.RangeFromTo(6, 11).Backwards(), new int[] { 10, 8, 6 }));
            Assert.IsTrue(IC.eq(tree.RangeFromTo(1, 12).Backwards(), new int[] { 10, 8, 6, 4, 2 }));
            Assert.IsTrue(IC.eq(tree.RangeFromTo(2, 12).Backwards(), new int[] { 10, 8, 6, 4, 2 }));
            Assert.IsTrue(IC.eq(tree.RangeFromTo(6, 21).Backwards(), new int[] { 20, 18, 16, 14, 12, 10, 8, 6 }));
            Assert.IsTrue(IC.eq(tree.RangeFromTo(6, 20).Backwards(), new int[] { 18, 16, 14, 12, 10, 8, 6 }));
        }

        [Test, LuceneNetSpecific]
        public void Direction()
        {
            Assert.AreEqual(EnumerationDirection.Forwards, tree.Direction);
            Assert.AreEqual(EnumerationDirection.Forwards, tree.RangeFrom(20).Direction);
            Assert.AreEqual(EnumerationDirection.Forwards, tree.RangeTo(7).Direction);
            Assert.AreEqual(EnumerationDirection.Forwards, tree.RangeFromTo(1, 12).Direction);
            Assert.AreEqual(EnumerationDirection.Forwards, tree.RangeAll().Direction);
            Assert.AreEqual(EnumerationDirection.Backwards, tree.Backwards().Direction);
            Assert.AreEqual(EnumerationDirection.Backwards, tree.RangeFrom(20).Backwards().Direction);
            Assert.AreEqual(EnumerationDirection.Backwards, tree.RangeTo(7).Backwards().Direction);
            Assert.AreEqual(EnumerationDirection.Backwards, tree.RangeFromTo(1, 12).Backwards().Direction);
            Assert.AreEqual(EnumerationDirection.Backwards, tree.RangeAll().Backwards().Direction);
        }


        [TearDown]
        public void Dispose()
        {
            tree = null;
            c = null;
        }
    }

    [TestFixture]
    public class BagItf
    {
        private TreeSet<int> tree;


        [SetUp]
        public void Init()
        {
            tree = new TreeSet<int>(new IC());
            for (int i = 10; i < 20; i++)
            {
                tree.Add(i);
                tree.Add(i + 10);
            }
        }


        [Test, LuceneNetSpecific]
        public void Both()
        {
            Assert.AreEqual(0, tree.ContainsCount(7));
            Assert.AreEqual(1, tree.ContainsCount(10));
            tree.RemoveAllCopies(10);
            Assert.AreEqual(0, tree.ContainsCount(10));
            tree.RemoveAllCopies(7);
        }


        [TearDown]
        public void Dispose()
        {
            tree = null;
        }
    }


    [TestFixture]
    public class Div
    {
        private TreeSet<int> tree;


        [SetUp]
        public void Init()
        {
            tree = new TreeSet<int>(new IC());
        }


        private void loadup()
        {
            for (int i = 10; i < 20; i++)
            {
                tree.Add(i);
                tree.Add(i + 10);
            }
        }

        [Test, LuceneNetSpecific]
        public void NullEqualityComparerinConstructor1()
        {
            Assert.Throws<NullReferenceException>(() => new TreeSet<int>(null));
        }

        [Test, LuceneNetSpecific]
        public void NullEqualityComparerinConstructor3()
        {
            Assert.Throws<NullReferenceException>(() => new TreeSet<int>(null, EqualityComparer<int>.Default));
        }

        [Test, LuceneNetSpecific]
        public void NullEqualityComparerinConstructor4()
        {
            Assert.Throws<NullReferenceException>(() => new TreeSet<int>(SCG.Comparer<int>.Default, null));
        }

        [Test, LuceneNetSpecific]
        public void NullEqualityComparerinConstructor5()
        {
            Assert.Throws<NullReferenceException>(() => new TreeSet<int>(null, null));
        }

        [Test, LuceneNetSpecific]
        public void Choose()
        {
            tree.Add(7);
            Assert.AreEqual(7, tree.Choose());
        }

        [Test, LuceneNetSpecific]
        public void BadChoose()
        {
            Assert.Throws<NoSuchItemException>(() => tree.Choose());
        }


        [Test, LuceneNetSpecific]
        public void NoDuplicates()
        {
            Assert.IsFalse(tree.AllowsDuplicates);
            loadup();
            Assert.IsFalse(tree.AllowsDuplicates);
        }

        [Test, LuceneNetSpecific]
        public void Add()
        {
            Assert.IsTrue(tree.Add(17));
            Assert.IsFalse(tree.Add(17));
            Assert.IsTrue(tree.Add(18));
            Assert.IsFalse(tree.Add(18));
            Assert.AreEqual(2, tree.Count);
        }


        [TearDown]
        public void Dispose()
        {
            tree = null;
        }
    }


    [TestFixture]
    public class FindOrAdd
    {
        private TreeSet<KeyValuePair<int, string>> bag;


        [SetUp]
        public void Init()
        {
            bag = new TreeSet<KeyValuePair<int, string>>(new KeyValuePairComparer<int, string>(new IC()));
        }


        [TearDown]
        public void Dispose()
        {
            bag = null;
        }


        [Test, LuceneNetSpecific]
        public void Test()
        {
            KeyValuePair<int, string> p = new KeyValuePair<int, string>(3, "tre");

            Assert.IsFalse(bag.FindOrAdd(ref p));
            p.Value = "drei";
            Assert.IsTrue(bag.FindOrAdd(ref p));
            Assert.AreEqual("tre", p.Value);
            p.Value = "three";
            Assert.AreEqual(1, bag.ContainsCount(p));
            Assert.AreEqual("tre", bag[0].Value);
        }
    }

    [TestFixture]
    public class FindPredicate
    {
        private TreeSet<int> list;
        Func<int, bool> pred;

        [SetUp]
        public void Init()
        {
            list = new TreeSet<int>(TenEqualityComparer.Default);
            pred = delegate (int i) { return i % 5 == 0; };
        }

        [TearDown]
        public void Dispose() { list = null; }

        [Test, LuceneNetSpecific]
        public void Find()
        {
            int i;
            Assert.IsFalse(list.Find(pred, out i));
            list.AddAll(new int[] { 4, 22, 67, 37 });
            Assert.IsFalse(list.Find(pred, out i));
            list.AddAll(new int[] { 45, 122, 675, 137 });
            Assert.IsTrue(list.Find(pred, out i));
            Assert.AreEqual(45, i);
        }

        [Test, LuceneNetSpecific]
        public void FindLast()
        {
            int i;
            Assert.IsFalse(list.FindLast(pred, out i));
            list.AddAll(new int[] { 4, 22, 67, 37 });
            Assert.IsFalse(list.FindLast(pred, out i));
            list.AddAll(new int[] { 45, 122, 675, 137 });
            Assert.IsTrue(list.FindLast(pred, out i));
            Assert.AreEqual(675, i);
        }

        [Test, LuceneNetSpecific]
        public void FindIndex()
        {
            Assert.IsFalse(0 <= list.FindIndex(pred));
            list.AddAll(new int[] { 4, 22, 67, 37 });
            Assert.IsFalse(0 <= list.FindIndex(pred));
            list.AddAll(new int[] { 45, 122, 675, 137 });
            Assert.AreEqual(3, list.FindIndex(pred));
        }

        [Test, LuceneNetSpecific]
        public void FindLastIndex()
        {
            Assert.IsFalse(0 <= list.FindLastIndex(pred));
            list.AddAll(new int[] { 4, 22, 67, 37 });
            Assert.IsFalse(0 <= list.FindLastIndex(pred));
            list.AddAll(new int[] { 45, 122, 675, 137 });
            Assert.AreEqual(7, list.FindLastIndex(pred));
        }
    }

    [TestFixture]
    public class UniqueItems
    {
        private TreeSet<int> list;

        [SetUp]
        public void Init() { list = new TreeSet<int>(); }

        [TearDown]
        public void Dispose() { list = null; }

        [Test, LuceneNetSpecific]
        public void Test()
        {
            Assert.IsTrue(IC.seteq(list.UniqueItems()));
            Assert.IsTrue(IC.seteq(list.ItemMultiplicities()));
            list.AddAll(new int[] { 7, 9, 7 });
            Assert.IsTrue(IC.seteq(list.UniqueItems(), 7, 9));
            Assert.IsTrue(IC.seteq(list.ItemMultiplicities(), 7, 1, 9, 1));
        }
    }

    [TestFixture]
    public class ArrayTest
    {
        private TreeSet<int> tree;

        int[] a;


        [SetUp]
        public void Init()
        {
            tree = new TreeSet<int>(new IC());
            a = new int[10];
            for (int i = 0; i < 10; i++)
                a[i] = 1000 + i;
        }


        [TearDown]
        public void Dispose() { tree = null; }


        private string aeq(int[] a, params int[] b)
        {
            if (a.Length != b.Length)
            {
                return "Lengths differ: " + a.Length + " != " + b.Length;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return string.Format("{0}'th elements differ: {1} != {2}", i, a[i], b[i]);
                }
            }
            return "Alles klar";
        }


        [Test, LuceneNetSpecific]
        public void ToArray()
        {
            Assert.AreEqual("Alles klar", aeq(tree.ToArray()));
            tree.Add(7);
            tree.Add(4);
            Assert.AreEqual("Alles klar", aeq(tree.ToArray(), 4, 7));
        }


        [Test, LuceneNetSpecific]
        public void CopyTo()
        {
            tree.CopyTo(a, 1);
            Assert.AreEqual("Alles klar", aeq(a, 1000, 1001, 1002, 1003, 1004, 1005, 1006, 1007, 1008, 1009));
            tree.Add(6);
            tree.CopyTo(a, 2);
            Assert.AreEqual("Alles klar", aeq(a, 1000, 1001, 6, 1003, 1004, 1005, 1006, 1007, 1008, 1009));
            tree.Add(4);
            tree.Add(9);
            tree.CopyTo(a, 4);
            Assert.AreEqual("Alles klar", aeq(a, 1000, 1001, 6, 1003, 4, 6, 9, 1007, 1008, 1009));
            tree.Clear();
            tree.Add(7);
            tree.CopyTo(a, 9);
            Assert.AreEqual("Alles klar", aeq(a, 1000, 1001, 6, 1003, 4, 6, 9, 1007, 1008, 7));
        }


        [Test, LuceneNetSpecific]
        public void CopyToBad()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => tree.CopyTo(a, 11));
        }

        [Test, LuceneNetSpecific]
        public void CopyToBad2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => tree.CopyTo(a, -1));
        }

        [Test, LuceneNetSpecific]
        public void CopyToTooFar()
        {
            tree.Add(3);
            tree.Add(4);
            Assert.Throws<ArgumentOutOfRangeException>(() => tree.CopyTo(a, 9));
        }
    }

    [TestFixture]
    public class Remove
    {
        private TreeSet<int> tree;

        [SetUp]
        public void Init()
        {
            tree = new TreeSet<int>(new IC());
            for (int i = 10; i < 20; i++)
            {
                tree.Add(i);
                tree.Add(i + 10);
            }
        }


        [Test, LuceneNetSpecific]
        public void SmallTrees()
        {
            tree.Clear();
            tree.Add(7);
            tree.Add(9);
            Assert.IsTrue(tree.Remove(7));
            Assert.IsTrue(tree.Check(""));
        }


        [Test, LuceneNetSpecific]
        public void ByIndex()
        {
            //Remove root!
            int n = tree.Count;
            int i = tree[10];

            tree.RemoveAt(10);
            Assert.IsTrue(tree.Check(""));
            Assert.IsFalse(tree.Contains(i));
            Assert.AreEqual(n - 1, tree.Count);

            //Low end
            i = tree.FindMin();
            tree.RemoveAt(0);
            Assert.IsTrue(tree.Check(""));
            Assert.IsFalse(tree.Contains(i));
            Assert.AreEqual(n - 2, tree.Count);

            //high end
            i = tree.FindMax();
            tree.RemoveAt(tree.Count - 1);
            Assert.IsTrue(tree.Check(""));
            Assert.IsFalse(tree.Contains(i));
            Assert.AreEqual(n - 3, tree.Count);

            //Some leaf
            i = 18;
            tree.RemoveAt(7);
            Assert.IsTrue(tree.Check(""));
            Assert.IsFalse(tree.Contains(i));
            Assert.AreEqual(n - 4, tree.Count);
        }


        [Test, LuceneNetSpecific]
        public void AlmostEmpty()
        {
            //Almost empty
            tree.Clear();
            tree.Add(3);
            tree.RemoveAt(0);
            Assert.IsTrue(tree.Check(""));
            Assert.IsFalse(tree.Contains(3));
            Assert.AreEqual(0, tree.Count);
        }


        [Test, LuceneNetSpecific]
        public void Empty()
        {
            tree.Clear();
            var exception = Assert.Throws<IndexOutOfRangeException>(() => tree.RemoveAt(0));
            Assert.AreEqual("Index out of range for sequenced collectionvalue", exception.Message);
        }


        [Test, LuceneNetSpecific]
        public void HighIndex()
        {
            var exception = Assert.Throws<IndexOutOfRangeException>(() => tree.RemoveAt(tree.Count));
            Assert.AreEqual("Index out of range for sequenced collectionvalue", exception.Message);
        }


        [Test, LuceneNetSpecific]
        public void LowIndex()
        {
            var exception = Assert.Throws<IndexOutOfRangeException>(() => tree.RemoveAt(-1));
            Assert.AreEqual("Index out of range for sequenced collectionvalue", exception.Message);
        }


        [Test, LuceneNetSpecific]
        public void Normal()
        {
            Assert.IsFalse(tree.Remove(-20));

            //No demote case, with move_item
            Assert.IsTrue(tree.Remove(20));
            Assert.IsTrue(tree.Check("T1"));
            Assert.IsFalse(tree.Remove(20));

            //plain case 2
            Assert.IsTrue(tree.Remove(14));
            Assert.IsTrue(tree.Check("Normal test 1"), "Bad tree");

            //case 1b
            Assert.IsTrue(tree.Remove(25));
            Assert.IsTrue(tree.Check("Normal test 1"), "Bad tree");

            //case 1c
            Assert.IsTrue(tree.Remove(29));
            Assert.IsTrue(tree.Check("Normal test 1"), "Bad tree");

            //1a (terminating)
            Assert.IsTrue(tree.Remove(10));
            Assert.IsTrue(tree.Check("Normal test 1"), "Bad tree");

            //2+1b
            Assert.IsTrue(tree.Remove(12));
            Assert.IsTrue(tree.Remove(11));

            //1a+1b
            Assert.IsTrue(tree.Remove(18));
            Assert.IsTrue(tree.Remove(13));
            Assert.IsTrue(tree.Remove(15));

            //2+1c
            for (int i = 0; i < 10; i++)
                tree.Add(50 - 2 * i);

            Assert.IsTrue(tree.Remove(42));
            Assert.IsTrue(tree.Remove(38));
            Assert.IsTrue(tree.Remove(28));
            Assert.IsTrue(tree.Remove(40));

            //
            Assert.IsTrue(tree.Remove(16));
            Assert.IsTrue(tree.Remove(23));
            Assert.IsTrue(tree.Remove(17));
            Assert.IsTrue(tree.Remove(19));
            Assert.IsTrue(tree.Remove(50));
            Assert.IsTrue(tree.Remove(26));
            Assert.IsTrue(tree.Remove(21));
            Assert.IsTrue(tree.Remove(22));
            Assert.IsTrue(tree.Remove(24));
            for (int i = 0; i < 48; i++)
                tree.Remove(i);

            //Almost empty tree:
            Assert.IsFalse(tree.Remove(26));
            Assert.IsTrue(tree.Remove(48));
            Assert.IsTrue(tree.Check("Normal test 1"), "Bad tree");

            //Empty tree:
            Assert.IsFalse(tree.Remove(26));
            Assert.IsTrue(tree.Check("Normal test 1"), "Bad tree");
        }


        [TearDown]
        public void Dispose()
        {
            tree = null;
        }
    }



    [TestFixture]
    public class PredecessorStructure
    {
        private TreeSet<int> tree;


        [SetUp]
        public void Init()
        {
            tree = new TreeSet<int>(new IC());
        }


        private void loadup()
        {
            for (int i = 0; i < 20; i++)
                tree.Add(2 * i);
        }

        [Test, LuceneNetSpecific]
        public void FindPredecessor()
        {
            loadup();
            int res;
            Assert.IsTrue(tree.TryPredecessor(7, out res) && res == 6);
            Assert.IsTrue(tree.TryPredecessor(8, out res) && res == 6);

            //The bottom
            Assert.IsTrue(tree.TryPredecessor(1, out res) && res == 0);

            //The top
            Assert.IsTrue(tree.TryPredecessor(39, out res) && res == 38);
        }

        [Test, LuceneNetSpecific]
        public void FindPredecessorTooLow1()
        {
            int res;
            Assert.IsFalse(tree.TryPredecessor(-2, out res));
            Assert.AreEqual(0, res);
            Assert.IsFalse(tree.TryPredecessor(0, out res));
            Assert.AreEqual(0, res);
        }

        [Test, LuceneNetSpecific]
        public void FindWeakPredecessor()
        {
            loadup();
            int res;
            Assert.IsTrue(tree.TryWeakPredecessor(7, out res) && res == 6);
            Assert.IsTrue(tree.TryWeakPredecessor(8, out res) && res == 8);

            //The bottom
            Assert.IsTrue(tree.TryWeakPredecessor(1, out res) && res == 0);
            Assert.IsTrue(tree.TryWeakPredecessor(0, out res) && res == 0);

            //The top
            Assert.IsTrue(tree.TryWeakPredecessor(39, out res) && res == 38);
            Assert.IsTrue(tree.TryWeakPredecessor(38, out res) && res == 38);
        }

        [Test, LuceneNetSpecific]
        public void FindWeakPredecessorTooLow1()
        {
            int res;
            Assert.IsFalse(tree.TryWeakPredecessor(-1, out res));
            Assert.AreEqual(0, res);
        }

        [Test, LuceneNetSpecific]
        public void FindSuccessor()
        {
            loadup();
            int res;
            Assert.IsTrue(tree.TrySuccessor(7, out res) && res == 8);
            Assert.IsTrue(tree.TrySuccessor(8, out res) && res == 10);

            //The bottom
            Assert.IsTrue(tree.TrySuccessor(0, out res) && res == 2);
            Assert.IsTrue(tree.TrySuccessor(-1, out res) && res == 0);

            //The top
            Assert.IsTrue(tree.TrySuccessor(37, out res) && res == 38);
        }

        [Test, LuceneNetSpecific]
        public void FindSuccessorTooHigh()
        {
            int res;
            Assert.IsFalse(tree.TrySuccessor(38, out res));
            Assert.AreEqual(0, res);
            Assert.IsFalse(tree.TrySuccessor(39, out res));
            Assert.AreEqual(0, res);
        }

        [Test, LuceneNetSpecific]
        public void FindWeakSuccessor()
        {
            loadup();
            int res;
            Assert.IsTrue(tree.TryWeakSuccessor(6, out res) && res == 6);
            Assert.IsTrue(tree.TryWeakSuccessor(7, out res) && res == 8);

            //The bottom
            Assert.IsTrue(tree.TryWeakSuccessor(-1, out res) && res == 0);
            Assert.IsTrue(tree.TryWeakSuccessor(0, out res) && res == 0);

            //The top
            Assert.IsTrue(tree.TryWeakSuccessor(37, out res) && res == 38);
            Assert.IsTrue(tree.TryWeakSuccessor(38, out res) && res == 38);
        }

        [Test, LuceneNetSpecific]
        public void FindWeakSuccessorTooHigh1()
        {
            int res;
            Assert.IsFalse(tree.TryWeakSuccessor(39, out res));
            Assert.AreEqual(0, res);
        }


        [Test, LuceneNetSpecific]
        public void Predecessor()
        {
            loadup();
            Assert.AreEqual(6, tree.Predecessor(7));
            Assert.AreEqual(6, tree.Predecessor(8));

            //The bottom
            Assert.AreEqual(0, tree.Predecessor(1));

            //The top
            Assert.AreEqual(38, tree.Predecessor(39));
        }


        [Test, LuceneNetSpecific]
        public void PredecessorTooLow1()
        {
            Assert.Throws<NoSuchItemException>(() => tree.Predecessor(-2));
        }


        [Test, LuceneNetSpecific]
        public void PredecessorTooLow2()
        {
            Assert.Throws<NoSuchItemException>(() => tree.Predecessor(0));
        }

        [Test, LuceneNetSpecific]
        public void WeakPredecessor()
        {
            loadup();
            Assert.AreEqual(6, tree.WeakPredecessor(7));
            Assert.AreEqual(8, tree.WeakPredecessor(8));

            //The bottom
            Assert.AreEqual(0, tree.WeakPredecessor(1));
            Assert.AreEqual(0, tree.WeakPredecessor(0));

            //The top
            Assert.AreEqual(38, tree.WeakPredecessor(39));
            Assert.AreEqual(38, tree.WeakPredecessor(38));
        }

        [Test, LuceneNetSpecific]
        public void WeakPredecessorTooLow1()
        {
            Assert.Throws<NoSuchItemException>(() => tree.WeakPredecessor(-2));
        }


        [Test, LuceneNetSpecific]
        public void Successor()
        {
            loadup();
            Assert.AreEqual(8, tree.Successor(7));
            Assert.AreEqual(10, tree.Successor(8));

            //The bottom
            Assert.AreEqual(2, tree.Successor(0));
            Assert.AreEqual(0, tree.Successor(-1));

            //The top
            Assert.AreEqual(38, tree.Successor(37));
        }


        [Test, LuceneNetSpecific]
        public void SuccessorTooHigh1()
        {
            Assert.Throws<NoSuchItemException>(() => tree.Successor(38));
        }

        [Test, LuceneNetSpecific]
        public void SuccessorTooHigh2()
        {
            Assert.Throws<NoSuchItemException>(() => tree.Successor(39));
        }

        [Test, LuceneNetSpecific]
        public void WeakSuccessor()
        {
            loadup();
            Assert.AreEqual(6, tree.WeakSuccessor(6));
            Assert.AreEqual(8, tree.WeakSuccessor(7));

            //The bottom
            Assert.AreEqual(0, tree.WeakSuccessor(-1));
            Assert.AreEqual(0, tree.WeakSuccessor(0));

            //The top
            Assert.AreEqual(38, tree.WeakSuccessor(37));
            Assert.AreEqual(38, tree.WeakSuccessor(38));
        }


        [Test, LuceneNetSpecific]
        public void WeakSuccessorTooHigh1()
        {
            Assert.Throws<NoSuchItemException>(() => tree.WeakSuccessor(39));
        }


        [TearDown]
        public void Dispose()
        {
            tree = null;
        }
    }

    [TestFixture]
    public class PriorityQueue
    {
        private TreeSet<int> tree;


        [SetUp]
        public void Init()
        {
            tree = new TreeSet<int>(new IC());
        }

        private void loadup()
        {
            foreach (int i in new int[] { 1, 2, 3, 4 })
                tree.Add(i);
        }

        [Test, LuceneNetSpecific]
        public void Normal()
        {
            loadup();
            Assert.AreEqual(1, tree.FindMin());
            Assert.AreEqual(4, tree.FindMax());
            Assert.AreEqual(1, tree.DeleteMin());
            Assert.AreEqual(4, tree.DeleteMax());
            Assert.IsTrue(tree.Check("Normal test 1"), "Bad tree");
            Assert.AreEqual(2, tree.FindMin());
            Assert.AreEqual(3, tree.FindMax());
            Assert.AreEqual(2, tree.DeleteMin());
            Assert.AreEqual(3, tree.DeleteMax());
            Assert.IsTrue(tree.Check("Normal test 2"), "Bad tree");
        }

        [Test, LuceneNetSpecific]
        public void Empty1()
        {
            Assert.Throws<NoSuchItemException>(() => tree.FindMin());
        }

        [Test, LuceneNetSpecific]
        public void Empty2()
        {
            Assert.Throws<NoSuchItemException>(() => tree.FindMax());
        }

        [Test, LuceneNetSpecific]
        public void Empty3()
        {
            Assert.Throws<NoSuchItemException>(() => tree.DeleteMin());
        }

        [Test, LuceneNetSpecific]
        public void Empty4()
        {
            Assert.Throws<NoSuchItemException>(() => tree.DeleteMax());
        }

        [TearDown]
        public void Dispose()
        {
            tree = null;
        }
    }

    [TestFixture]
    public class IndexingAndCounting
    {
        private TreeSet<int> tree;

        [SetUp]
        public void Init()
        {
            tree = new TreeSet<int>(new IC());
        }

        private void populate()
        {
            tree.Add(30);
            tree.Add(50);
            tree.Add(10);
            tree.Add(70);
        }

        [Test, LuceneNetSpecific]
        public void ToArray()
        {
            populate();

            int[] a = tree.ToArray();

            Assert.AreEqual(4, a.Length);
            Assert.AreEqual(10, a[0]);
            Assert.AreEqual(30, a[1]);
            Assert.AreEqual(50, a[2]);
            Assert.AreEqual(70, a[3]);
        }


        [Test, LuceneNetSpecific]
        public void GoodIndex()
        {
            Assert.AreEqual(-1, tree.IndexOf(20));
            Assert.AreEqual(-1, tree.LastIndexOf(20));
            populate();
            Assert.AreEqual(10, tree[0]);
            Assert.AreEqual(30, tree[1]);
            Assert.AreEqual(50, tree[2]);
            Assert.AreEqual(70, tree[3]);
            Assert.AreEqual(0, tree.IndexOf(10));
            Assert.AreEqual(1, tree.IndexOf(30));
            Assert.AreEqual(2, tree.IndexOf(50));
            Assert.AreEqual(3, tree.IndexOf(70));
            Assert.AreEqual(~1, tree.IndexOf(20));
            Assert.AreEqual(~0, tree.IndexOf(0));
            Assert.AreEqual(~4, tree.IndexOf(90));
            Assert.AreEqual(0, tree.LastIndexOf(10));
            Assert.AreEqual(1, tree.LastIndexOf(30));
            Assert.AreEqual(2, tree.LastIndexOf(50));
            Assert.AreEqual(3, tree.LastIndexOf(70));
            Assert.AreEqual(~1, tree.LastIndexOf(20));
            Assert.AreEqual(~0, tree.LastIndexOf(0));
            Assert.AreEqual(~4, tree.LastIndexOf(90));
        }


        [Test, LuceneNetSpecific]
        public void IndexTooLarge()
        {
            populate();
            Assert.Throws<IndexOutOfRangeException>(() => Console.WriteLine(tree[4]));
        }


        [Test, LuceneNetSpecific]
        public void IndexTooSmall()
        {
            populate();
            Assert.Throws<IndexOutOfRangeException>(() => Console.WriteLine(tree[-1]));
        }


        [Test, LuceneNetSpecific]
        public void FilledTreeOutsideInput()
        {
            populate();
            Assert.AreEqual(0, tree.CountFrom(90));
            Assert.AreEqual(0, tree.CountFromTo(-20, 0));
            Assert.AreEqual(0, tree.CountFromTo(80, 100));
            Assert.AreEqual(0, tree.CountTo(0));
            Assert.AreEqual(4, tree.CountTo(90));
            Assert.AreEqual(4, tree.CountFromTo(-20, 90));
            Assert.AreEqual(4, tree.CountFrom(0));
        }


        [Test, LuceneNetSpecific]
        public void FilledTreeIntermediateInput()
        {
            populate();
            Assert.AreEqual(3, tree.CountFrom(20));
            Assert.AreEqual(1, tree.CountFromTo(20, 40));
            Assert.AreEqual(2, tree.CountTo(40));
        }


        [Test, LuceneNetSpecific]
        public void FilledTreeMatchingInput()
        {
            populate();
            Assert.AreEqual(3, tree.CountFrom(30));
            Assert.AreEqual(2, tree.CountFromTo(30, 70));
            Assert.AreEqual(0, tree.CountFromTo(50, 30));
            Assert.AreEqual(0, tree.CountFromTo(50, 50));
            Assert.AreEqual(0, tree.CountTo(10));
            Assert.AreEqual(2, tree.CountTo(50));
        }

        [Test, LuceneNetSpecific]
        public void CountEmptyTree()
        {
            Assert.AreEqual(0, tree.CountFrom(20));
            Assert.AreEqual(0, tree.CountFromTo(20, 40));
            Assert.AreEqual(0, tree.CountTo(40));
        }

        [TearDown]
        public void Dispose()
        {
            tree = null;
        }
    }

    namespace ModificationCheck
    {
        [TestFixture]
        public class Enumerator
        {
            private TreeSet<int> tree;

            private SCG.IEnumerator<int> e;


            [SetUp]
            public void Init()
            {
                tree = new TreeSet<int>(new IC());
                for (int i = 0; i < 10; i++)
                    tree.Add(i);

                e = tree.GetEnumerator();
            }


            [Test, LuceneNetSpecific]
            public void CurrentAfterModification()
            {
                e.MoveNext();
                tree.Add(34);
                Assert.AreEqual(0, e.Current);
            }


            [Test, LuceneNetSpecific]
            public void MoveNextAfterAdd()
            {
                tree.Add(34);

                Assert.Throws<CollectionModifiedException>(() => e.MoveNext());
            }

            [Test, LuceneNetSpecific]
            public void MoveNextAfterRemove()
            {
                tree.Remove(34);

                Assert.Throws<CollectionModifiedException>(() => e.MoveNext());
            }


            [Test, LuceneNetSpecific]
            public void MoveNextAfterClear()
            {
                tree.Clear();

                Assert.Throws<CollectionModifiedException>(() => e.MoveNext());
            }


            [TearDown]
            public void Dispose()
            {
                tree = null;
                e = null;
            }
        }

        [TestFixture]
        public class RangeEnumerator
        {
            private TreeSet<int> tree;

            private SCG.IEnumerator<int> e;

            [SetUp]
            public void Init()
            {
                tree = new TreeSet<int>(new IC());
                for (int i = 0; i < 10; i++)
                    tree.Add(i);

                e = tree.RangeFromTo(3, 7).GetEnumerator();
            }

            [Test, LuceneNetSpecific]
            public void CurrentAfterModification()
            {
                e.MoveNext();
                tree.Add(34);
                Assert.AreEqual(3, e.Current);
            }


            [Test, LuceneNetSpecific]
            public void MoveNextAfterAdd()
            {
                tree.Add(34);

                Assert.Throws<CollectionModifiedException>(() => e.MoveNext());
            }

            [Test, LuceneNetSpecific]
            public void MoveNextAfterRemove()
            {
                tree.Remove(34);

                Assert.Throws<CollectionModifiedException>(() => e.MoveNext());
            }


            [Test, LuceneNetSpecific]
            public void MoveNextAfterClear()
            {
                tree.Clear();

                Assert.Throws<CollectionModifiedException>(() => e.MoveNext());
            }


            [TearDown]
            public void Dispose()
            {
                tree = null;
                e = null;
            }
        }
    }

    namespace PathcopyPersistence
    {
        [TestFixture]
        public class Navigation
        {
            private TreeSet<int> tree, snap;

            private SCG.IComparer<int> ic;

            [SetUp]
            public void Init()
            {
                ic = new IC();
                tree = new TreeSet<int>(ic);
                for (int i = 0; i <= 20; i++)
                    tree.Add(2 * i + 1);

                snap = (TreeSet<int>)tree.Snapshot();
                for (int i = 0; i <= 10; i++)
                    tree.Remove(4 * i + 1);
            }


            private bool twomodeleven(int i)
            {
                return i % 11 == 2;
            }


            [Test, LuceneNetSpecific]
            public void InternalEnum()
            {
                Assert.IsTrue(IC.eq(snap.FindAll(new Func<int, bool>(twomodeleven)), 13, 35));
            }


            public void MoreCut() { }

            [Test, LuceneNetSpecific]
            public void Cut()
            {
                int lo, hi;
                bool lv, hv;

                Assert.IsFalse(snap.Cut(new HigherOrder.CubeRoot(64), out lo, out lv, out hi, out hv));
                Assert.IsTrue(lv && hv);
                Assert.AreEqual(5, hi);
                Assert.AreEqual(3, lo);
                Assert.IsTrue(snap.Cut(new HigherOrder.CubeRoot(125), out lo, out lv, out hi, out hv));
                Assert.IsTrue(lv && hv);
                Assert.AreEqual(7, hi);
                Assert.AreEqual(3, lo);
                Assert.IsFalse(snap.Cut(new HigherOrder.CubeRoot(125000), out lo, out lv, out hi, out hv));
                Assert.IsTrue(lv && !hv);
                Assert.AreEqual(41, lo);
                Assert.IsFalse(snap.Cut(new HigherOrder.CubeRoot(-27), out lo, out lv, out hi, out hv));
                Assert.IsTrue(!lv && hv);
                Assert.AreEqual(1, hi);
            }


            [Test, LuceneNetSpecific]
            public void Range()
            {
                Assert.IsTrue(IC.eq(snap.RangeFromTo(5, 16), 5, 7, 9, 11, 13, 15));
                Assert.IsTrue(IC.eq(snap.RangeFromTo(5, 17), 5, 7, 9, 11, 13, 15));
                Assert.IsTrue(IC.eq(snap.RangeFromTo(6, 16), 7, 9, 11, 13, 15));
                //Assert.AreEqual(snap.RangeFromTo(6, 16).Count, 5);
            }

            [Test, LuceneNetSpecific]
            public void Contains()
            {
                Assert.IsTrue(snap.Contains(5));
            }

            [Test, LuceneNetSpecific]
            public void FindMin()
            {
                Assert.AreEqual(1, snap.FindMin());
            }

            [Test, LuceneNetSpecific]
            public void FindMax()
            {
                Assert.AreEqual(41, snap.FindMax());
            }

            [Test, LuceneNetSpecific]
            public void FindPredecessor()
            {
                int res;
                Assert.IsTrue(snap.TryPredecessor(15, out res) && res == 13);
                Assert.IsTrue(snap.TryPredecessor(16, out res) && res == 15);
                Assert.IsTrue(snap.TryPredecessor(17, out res) && res == 15);
                Assert.IsTrue(snap.TryPredecessor(18, out res) && res == 17);

                Assert.IsTrue(snap.TryPredecessor(2, out res) && res == 1);

                Assert.IsFalse(snap.TryPredecessor(1, out res));
                Assert.AreEqual(0, res);
            }


            [Test, LuceneNetSpecific]
            public void FindSuccessor()
            {
                int res;
                Assert.IsTrue(snap.TrySuccessor(15, out res) && res == 17);
                Assert.IsTrue(snap.TrySuccessor(16, out res) && res == 17);
                Assert.IsTrue(snap.TrySuccessor(17, out res) && res == 19);
                Assert.IsTrue(snap.TrySuccessor(18, out res) && res == 19);

                Assert.IsTrue(snap.TrySuccessor(40, out res) && res == 41);

                Assert.IsFalse(snap.TrySuccessor(41, out res));
                Assert.AreEqual(0, res);
            }


            [Test, LuceneNetSpecific]
            public void FindWeakPredecessor()
            {
                int res;
                Assert.IsTrue(snap.TryWeakPredecessor(15, out res) && res == 15);
                Assert.IsTrue(snap.TryWeakPredecessor(16, out res) && res == 15);
                Assert.IsTrue(snap.TryWeakPredecessor(17, out res) && res == 17);
                Assert.IsTrue(snap.TryWeakPredecessor(18, out res) && res == 17);

                Assert.IsTrue(snap.TryWeakPredecessor(1, out res) && res == 1);

                Assert.IsFalse(snap.TryWeakPredecessor(0, out res));
                Assert.AreEqual(0, res);
            }


            [Test, LuceneNetSpecific]
            public void FindWeakSuccessor()
            {
                int res;
                Assert.IsTrue(snap.TryWeakSuccessor(15, out res) && res == 15);
                Assert.IsTrue(snap.TryWeakSuccessor(16, out res) && res == 17);
                Assert.IsTrue(snap.TryWeakSuccessor(17, out res) && res == 17);
                Assert.IsTrue(snap.TryWeakSuccessor(18, out res) && res == 19);

                Assert.IsTrue(snap.TryWeakSuccessor(41, out res) && res == 41);

                Assert.IsFalse(snap.TryWeakSuccessor(42, out res));
                Assert.AreEqual(0, res);
            }


            [Test, LuceneNetSpecific]
            public void Predecessor()
            {
                Assert.AreEqual(13, snap.Predecessor(15));
                Assert.AreEqual(15, snap.Predecessor(16));
                Assert.AreEqual(15, snap.Predecessor(17));
                Assert.AreEqual(17, snap.Predecessor(18));
            }


            [Test, LuceneNetSpecific]
            public void Successor()
            {
                Assert.AreEqual(17, snap.Successor(15));
                Assert.AreEqual(17, snap.Successor(16));
                Assert.AreEqual(19, snap.Successor(17));
                Assert.AreEqual(19, snap.Successor(18));
            }


            [Test, LuceneNetSpecific]
            public void WeakPredecessor()
            {
                Assert.AreEqual(15, snap.WeakPredecessor(15));
                Assert.AreEqual(15, snap.WeakPredecessor(16));
                Assert.AreEqual(17, snap.WeakPredecessor(17));
                Assert.AreEqual(17, snap.WeakPredecessor(18));
            }


            [Test, LuceneNetSpecific]
            public void WeakSuccessor()
            {
                Assert.AreEqual(15, snap.WeakSuccessor(15));
                Assert.AreEqual(17, snap.WeakSuccessor(16));
                Assert.AreEqual(17, snap.WeakSuccessor(17));
                Assert.AreEqual(19, snap.WeakSuccessor(18));
            }


            [Test, LuceneNetSpecific]
            public void CountTo()
            {
                var exception = Assert.Throws<NotSupportedException>(() =>
                {
                    int j = snap.CountTo(15);
                });
                Assert.AreEqual("Indexing not supported for snapshots", exception.Message);
            }

            [Test, LuceneNetSpecific]
            public void Indexing()
            {
                var exception = Assert.Throws<NotSupportedException>(() =>
                {
                    int j = snap[4];
                });
                Assert.AreEqual("Indexing not supported for snapshots", exception.Message);
            }

            [Test, LuceneNetSpecific]
            public void Indexing2()
            {
                var exception = Assert.Throws<NotSupportedException>(() =>
                {
                    int j = snap.IndexOf(5);
                });
                Assert.AreEqual("Indexing not supported for snapshots", exception.Message);
            }

            [TearDown]
            public void Dispose()
            {
                tree = null;
                ic = null;
            }
        }

        [TestFixture]
        public class Single
        {
            private TreeSet<int> tree;

            private SCG.IComparer<int> ic;


            [SetUp]
            public void Init()
            {
                ic = new IC();
                tree = new TreeSet<int>(ic);
                for (int i = 0; i < 10; i++)
                    tree.Add(2 * i + 1);
            }


            [Test, LuceneNetSpecific]
            public void EnumerationWithAdd()
            {
                int[] orig = new int[] { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19 };
                int i = 0;
                TreeSet<int> snap = (TreeSet<int>)tree.Snapshot();

                foreach (int j in snap)
                {
                    Assert.AreEqual(1 + 2 * i++, j);
                    tree.Add(21 - j);
                    Assert.IsTrue(snap.Check("M"), "Bad snap!");
                    Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");
                    Assert.IsTrue(tree.Check("Tree"), "Bad tree!");
                }
            }


            [Test, LuceneNetSpecific]
            public void Remove()
            {
                int[] orig = new int[] { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19 };
                TreeSet<int> snap = (TreeSet<int>)tree.Snapshot();

                Assert.IsTrue(snap.Check("Snap"), "Bad snap!");
                Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");
                tree.Remove(19);
                Assert.IsTrue(snap.Check("Snap"), "Bad snap!");
                Assert.IsTrue(tree.Check("Tree"), "Bad tree!");
                Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");
            }


            [Test, LuceneNetSpecific]
            public void RemoveNormal()
            {
                tree.Clear();
                for (int i = 10; i < 20; i++)
                {
                    tree.Add(i);
                    tree.Add(i + 10);
                }

                int[] orig = new int[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29 };
                TreeSet<int> snap = (TreeSet<int>)tree.Snapshot();

                Assert.IsFalse(tree.Remove(-20));
                Assert.IsTrue(snap.Check("Snap"), "Bad snap!");
                Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");

                //No demote case, with move_item
                Assert.IsTrue(tree.Remove(20));
                Assert.IsTrue(tree.Check("T1"));
                Assert.IsTrue(snap.Check("Snap"), "Bad snap!");
                Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");
                Assert.IsFalse(tree.Remove(20));

                //plain case 2
                tree.Snapshot();
                Assert.IsTrue(tree.Remove(14));
                Assert.IsTrue(tree.Check("Normal test 1"), "Bad tree");
                Assert.IsTrue(snap.Check("Snap"), "Bad snap!");
                Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");

                //case 1b
                Assert.IsTrue(tree.Remove(25));
                Assert.IsTrue(tree.Check("Normal test 1"), "Bad tree");
                Assert.IsTrue(snap.Check("Snap"), "Bad snap!");
                Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");

                //case 1c
                Assert.IsTrue(tree.Remove(29));
                Assert.IsTrue(tree.Check("Normal test 1"), "Bad tree");
                Assert.IsTrue(snap.Check("Snap"), "Bad snap!");
                Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");

                //1a (terminating)
                Assert.IsTrue(tree.Remove(10));
                Assert.IsTrue(tree.Check("Normal test 1"), "Bad tree");
                Assert.IsTrue(snap.Check("Snap"), "Bad snap!");
                Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");

                //2+1b
                Assert.IsTrue(tree.Remove(12));
                tree.Snapshot();
                Assert.IsTrue(tree.Remove(11));
                Assert.IsTrue(tree.Check("Normal test 1"), "Bad tree");
                Assert.IsTrue(snap.Check("Snap"), "Bad snap!");
                Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");

                //1a+1b
                Assert.IsTrue(tree.Remove(18));
                Assert.IsTrue(tree.Remove(13));
                Assert.IsTrue(tree.Remove(15));
                Assert.IsTrue(tree.Check("Normal test 1"), "Bad tree");
                Assert.IsTrue(snap.Check("Snap"), "Bad snap!");
                Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");

                //2+1c
                for (int i = 0; i < 10; i++)
                    tree.Add(50 - 2 * i);

                Assert.IsTrue(tree.Remove(42));
                Assert.IsTrue(tree.Remove(38));
                Assert.IsTrue(tree.Remove(28));
                Assert.IsTrue(tree.Remove(40));
                Assert.IsTrue(tree.Check("Normal test 1"), "Bad tree");
                Assert.IsTrue(snap.Check("Snap"), "Bad snap!");
                Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");

                //
                Assert.IsTrue(tree.Remove(16));
                Assert.IsTrue(tree.Remove(23));
                Assert.IsTrue(tree.Remove(17));
                Assert.IsTrue(tree.Remove(19));
                Assert.IsTrue(tree.Remove(50));
                Assert.IsTrue(tree.Remove(26));
                Assert.IsTrue(tree.Remove(21));
                Assert.IsTrue(tree.Remove(22));
                Assert.IsTrue(tree.Remove(24));
                for (int i = 0; i < 48; i++)
                    tree.Remove(i);

                //Almost empty tree:
                Assert.IsFalse(tree.Remove(26));
                Assert.IsTrue(tree.Remove(48));
                Assert.IsTrue(tree.Check("Normal test 1"), "Bad tree");
                Assert.IsTrue(snap.Check("Snap"), "Bad snap!");
                Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");

                //Empty tree:
                Assert.IsFalse(tree.Remove(26));
                Assert.IsTrue(tree.Check("Normal test 1"), "Bad tree");
                Assert.IsTrue(snap.Check("Snap"), "Bad snap!");
                Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");
            }


            [Test, LuceneNetSpecific]
            public void Add()
            {
                int[] orig = new int[] { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19 };
                TreeSet<int> snap = (TreeSet<int>)tree.Snapshot();

                Assert.IsTrue(snap.Check("M"), "Bad snap!");
                Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");
                Assert.IsTrue(tree.Check("Tree"), "Bad tree!");
                tree.Add(10);
                Assert.IsTrue(snap.Check("M"), "Bad snap!");
                Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");
                Assert.IsTrue(tree.Check("Tree"), "Bad tree!");
                tree.Add(16);
                Assert.IsTrue(snap.Check("M"), "Bad snap!");
                Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");
                Assert.IsTrue(tree.Check("Tree"), "Bad tree!");

                //Promote+zigzig
                tree.Add(40);
                Assert.IsTrue(snap.Check("M"), "Bad snap!");
                Assert.IsTrue(tree.Check("Tree"), "Bad tree!");
                Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");
                for (int i = 1; i < 4; i++)
                    tree.Add(40 - 2 * i);

                Assert.IsTrue(snap.Check("M"), "Bad snap!");
                Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");
                Assert.IsTrue(tree.Check("Tree"), "Bad tree!");

                //Zigzag:
                tree.Add(32);
                Assert.IsTrue(snap.Check("M"), "Bad snap!");
                Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");
                Assert.IsTrue(tree.Check("Tree"), "Bad tree!");
            }


            [Test, LuceneNetSpecific]
            public void Clear()
            {
                int[] orig = new int[] { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19 };
                TreeSet<int> snap = (TreeSet<int>)tree.Snapshot();

                tree.Clear();
                Assert.IsTrue(snap.Check("Snap"), "Bad snap!");
                Assert.IsTrue(tree.Check("Tree"), "Bad tree!");
                Assert.IsTrue(IC.eq(snap, orig), "Snap was changed!");
                Assert.AreEqual(0, tree.Count);
            }


            [Test, LuceneNetSpecific]
            public void SnapSnap()
            {
                TreeSet<int> snap = (TreeSet<int>)tree.Snapshot();

                var exception = Assert.Throws<InvalidOperationException>(() => snap.Snapshot());
                Assert.AreEqual("Cannot snapshot a snapshot", exception.Message);
            }

            [TearDown]
            public void Dispose()
            {
                tree = null;
                ic = null;
            }
        }

        [TestFixture]
        public class Multiple
        {
            private TreeSet<int> tree;

            private SCG.IComparer<int> ic;

            private bool eq(SCG.IEnumerable<int> me, int[] that)
            {
                int i = 0, maxind = that.Length - 1;

                foreach (int item in me)
                    if (i > maxind || ic.Compare(item, that[i++]) != 0)
                        return false;

                return true;
            }

            [SetUp]
            public void Init()
            {
                ic = new IC();
                tree = new TreeSet<int>(ic);
                for (int i = 0; i < 10; i++)
                    tree.Add(2 * i + 1);
            }

            [Test, LuceneNetSpecific]
            public void First()
            {
                TreeSet<int>[] snaps = new TreeSet<int>[10];

                for (int i = 0; i < 10; i++)
                {
                    snaps[i] = (TreeSet<int>)(tree.Snapshot());
                    tree.Add(2 * i);
                }

                for (int i = 0; i < 10; i++)
                {
                    Assert.AreEqual(i + 10, snaps[i].Count);
                }

                snaps[5] = null;
                snaps[9] = null;
                GC.Collect();
                snaps[8].Dispose();
                tree.Remove(14);

                int[] res = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 15, 16, 17, 18, 19 };
                int[] snap7 = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 15, 17, 19 };
                int[] snap3 = new int[] { 0, 1, 2, 3, 4, 5, 7, 9, 11, 13, 15, 17, 19 };

                Assert.IsTrue(IC.eq(snaps[3], snap3), "Snap 3 was changed!");
                Assert.IsTrue(IC.eq(snaps[7], snap7), "Snap 7 was changed!");
                Assert.IsTrue(IC.eq(tree, res));
                Assert.IsTrue(tree.Check("B"));
                Assert.IsTrue(snaps[3].Check("B"));
                Assert.IsTrue(snaps[7].Check("B"));
            }


            [Test, LuceneNetSpecific]
            public void CollectingTheMaster()
            {
                TreeSet<int>[] snaps = new TreeSet<int>[10];

                for (int i = 0; i < 10; i++)
                {
                    snaps[i] = (TreeSet<int>)(tree.Snapshot());
                    tree.Add(2 * i);
                }

                tree = null;
                GC.Collect();
                for (int i = 0; i < 10; i++)
                {
                    Assert.AreEqual(i + 10, snaps[i].Count);
                }

                snaps[5] = null;
                snaps[9] = null;
                GC.Collect();
                snaps[8].Dispose();

                int[] snap7 = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 15, 17, 19 };
                int[] snap3 = new int[] { 0, 1, 2, 3, 4, 5, 7, 9, 11, 13, 15, 17, 19 };

                Assert.IsTrue(IC.eq(snaps[3], snap3), "Snap 3 was changed!");
                Assert.IsTrue(IC.eq(snaps[7], snap7), "Snap 7 was changed!");
                Assert.IsTrue(snaps[3].Check("B"));
                Assert.IsTrue(snaps[7].Check("B"));
            }


            [TearDown]
            public void Dispose()
            {
                tree = null;
                ic = null;
            }
        }
    }




    namespace HigherOrder
    {
        internal class CubeRoot : IComparable<int>
        {
            private int c;


            internal CubeRoot(int c) { this.c = c; }


            public int CompareTo(int that) { return c - that * that * that; }
            public bool Equals(int that) { return c == that * that * that; }
        }



        class Interval : IComparable<int>
        {
            private int b, t;


            internal Interval(int b, int t) { this.b = b; this.t = t; }


            public int CompareTo(int that) { return that < b ? 1 : that > t ? -1 : 0; }
            public bool Equals(int that) { return that >= b && that <= t; }
        }



        [TestFixture]
        public class Simple
        {
            private TreeSet<int> tree;

            private SCG.IComparer<int> ic;


            [SetUp]
            public void Init()
            {
                ic = new IC();
                tree = new TreeSet<int>(ic);
            }


            private bool never(int i) { return false; }


            private bool always(int i) { return true; }


            private bool even(int i) { return i % 2 == 0; }


            private string themap(int i) { return string.Format("AA {0,4} BB", i); }


            private string badmap(int i) { return string.Format("AA {0} BB", i); }


            private int appfield1;

            private int appfield2;


            private void apply(int i) { appfield1++; appfield2 += i * i; }


            [Test, LuceneNetSpecific]
            public void Apply()
            {
                Simple simple1 = new Simple();

                tree.Apply(new Action<int>(simple1.apply));
                Assert.AreEqual(0, simple1.appfield1);
                Assert.AreEqual(0, simple1.appfield2);

                Simple simple2 = new Simple();

                for (int i = 0; i < 10; i++) tree.Add(i);

                tree.Apply(new Action<int>(simple2.apply));
                Assert.AreEqual(10, simple2.appfield1);
                Assert.AreEqual(285, simple2.appfield2);
            }


            [Test, LuceneNetSpecific]
            public void All()
            {
                Assert.IsTrue(tree.All(new Func<int, bool>(never)));
                Assert.IsTrue(tree.All(new Func<int, bool>(even)));
                Assert.IsTrue(tree.All(new Func<int, bool>(always)));
                for (int i = 0; i < 10; i++) tree.Add(i);

                Assert.IsFalse(tree.All(new Func<int, bool>(never)));
                Assert.IsFalse(tree.All(new Func<int, bool>(even)));
                Assert.IsTrue(tree.All(new Func<int, bool>(always)));
                tree.Clear();
                for (int i = 0; i < 10; i++) tree.Add(i * 2);

                Assert.IsFalse(tree.All(new Func<int, bool>(never)));
                Assert.IsTrue(tree.All(new Func<int, bool>(even)));
                Assert.IsTrue(tree.All(new Func<int, bool>(always)));
                tree.Clear();
                for (int i = 0; i < 10; i++) tree.Add(i * 2 + 1);

                Assert.IsFalse(tree.All(new Func<int, bool>(never)));
                Assert.IsFalse(tree.All(new Func<int, bool>(even)));
                Assert.IsTrue(tree.All(new Func<int, bool>(always)));
            }


            [Test, LuceneNetSpecific]
            public void Exists()
            {
                Assert.IsFalse(tree.Exists(new Func<int, bool>(never)));
                Assert.IsFalse(tree.Exists(new Func<int, bool>(even)));
                Assert.IsFalse(tree.Exists(new Func<int, bool>(always)));
                for (int i = 0; i < 10; i++) tree.Add(i);

                Assert.IsFalse(tree.Exists(new Func<int, bool>(never)));
                Assert.IsTrue(tree.Exists(new Func<int, bool>(even)));
                Assert.IsTrue(tree.Exists(new Func<int, bool>(always)));
                tree.Clear();
                for (int i = 0; i < 10; i++) tree.Add(i * 2);

                Assert.IsFalse(tree.Exists(new Func<int, bool>(never)));
                Assert.IsTrue(tree.Exists(new Func<int, bool>(even)));
                Assert.IsTrue(tree.Exists(new Func<int, bool>(always)));
                tree.Clear();
                for (int i = 0; i < 10; i++) tree.Add(i * 2 + 1);

                Assert.IsFalse(tree.Exists(new Func<int, bool>(never)));
                Assert.IsFalse(tree.Exists(new Func<int, bool>(even)));
                Assert.IsTrue(tree.Exists(new Func<int, bool>(always)));
            }


            [Test, LuceneNetSpecific]
            public void FindAll()
            {
                Assert.AreEqual(0, tree.FindAll(new Func<int, bool>(never)).Count);
                for (int i = 0; i < 10; i++)
                    tree.Add(i);

                Assert.AreEqual(0, tree.FindAll(new Func<int, bool>(never)).Count);
                Assert.AreEqual(10, tree.FindAll(new Func<int, bool>(always)).Count);
                Assert.AreEqual(5, tree.FindAll(new Func<int, bool>(even)).Count);
                Assert.IsTrue(((TreeSet<int>)tree.FindAll(new Func<int, bool>(even))).Check("R"));
            }


            [Test, LuceneNetSpecific]
            public void Map()
            {
                Assert.AreEqual(0, tree.Map(new Func<int, string>(themap), new SC()).Count);
                for (int i = 0; i < 11; i++)
                    tree.Add(i * i * i);

                IIndexedSorted<string> res = tree.Map(new Func<int, string>(themap), new SC());

                Assert.IsTrue(((TreeSet<string>)res).Check("R"));
                Assert.AreEqual(11, res.Count);
                Assert.AreEqual("AA    0 BB", res[0]);
                Assert.AreEqual("AA   27 BB", res[3]);
                Assert.AreEqual("AA  125 BB", res[5]);
                Assert.AreEqual("AA 1000 BB", res[10]);
            }


            [Test, LuceneNetSpecific]
            public void BadMap()
            {
                for (int i = 0; i < 11; i++)
                {
                    tree.Add(i * i * i);
                }

                var exception = Assert.Throws<ArgumentException>(() =>
                {
                    ISorted<string> res = tree.Map(new Func<int, string>(badmap), new SC());
                });
                Assert.AreEqual("mapper not monotonic", exception.Message);
            }


            [Test, LuceneNetSpecific]
            public void Cut()
            {
                for (int i = 0; i < 10; i++)
                    tree.Add(i);

                int low, high;
                bool lval, hval;

                Assert.IsTrue(tree.Cut(new CubeRoot(27), out low, out lval, out high, out hval));
                Assert.IsTrue(lval && hval);
                Assert.AreEqual(4, high);
                Assert.AreEqual(2, low);
                Assert.IsFalse(tree.Cut(new CubeRoot(30), out low, out lval, out high, out hval));
                Assert.IsTrue(lval && hval);
                Assert.AreEqual(4, high);
                Assert.AreEqual(3, low);
            }


            [Test, LuceneNetSpecific]
            public void CutInt()
            {
                for (int i = 0; i < 10; i++)
                    tree.Add(2 * i);

                int low, high;
                bool lval, hval;

                Assert.IsFalse(tree.Cut(new IC(3), out low, out lval, out high, out hval));
                Assert.IsTrue(lval && hval);
                Assert.AreEqual(4, high);
                Assert.AreEqual(2, low);
                Assert.IsTrue(tree.Cut(new IC(6), out low, out lval, out high, out hval));
                Assert.IsTrue(lval && hval);
                Assert.AreEqual(8, high);
                Assert.AreEqual(4, low);
            }


            [Test, LuceneNetSpecific]
            public void CutInterval()
            {
                for (int i = 0; i < 10; i++)
                    tree.Add(2 * i);

                int lo, hi;
                bool lv, hv;

                Assert.IsTrue(tree.Cut(new Interval(5, 9), out lo, out lv, out hi, out hv));
                Assert.IsTrue(lv && hv);
                Assert.AreEqual(10, hi);
                Assert.AreEqual(4, lo);
                Assert.IsTrue(tree.Cut(new Interval(6, 10), out lo, out lv, out hi, out hv));
                Assert.IsTrue(lv && hv);
                Assert.AreEqual(12, hi);
                Assert.AreEqual(4, lo);
                for (int i = 0; i < 100; i++)
                    tree.Add(2 * i);

                tree.Cut(new Interval(77, 105), out lo, out lv, out hi, out hv);
                Assert.IsTrue(lv && hv);
                Assert.AreEqual(106, hi);
                Assert.AreEqual(76, lo);
                tree.Cut(new Interval(5, 7), out lo, out lv, out hi, out hv);
                Assert.IsTrue(lv && hv);
                Assert.AreEqual(8, hi);
                Assert.AreEqual(4, lo);
                tree.Cut(new Interval(80, 110), out lo, out lv, out hi, out hv);
                Assert.IsTrue(lv && hv);
                Assert.AreEqual(112, hi);
                Assert.AreEqual(78, lo);
            }


            [Test, LuceneNetSpecific]
            public void UpperCut()
            {
                for (int i = 0; i < 10; i++)
                    tree.Add(i);

                int l, h;
                bool lv, hv;

                Assert.IsFalse(tree.Cut(new CubeRoot(1000), out l, out lv, out h, out hv));
                Assert.IsTrue(lv && !hv);
                Assert.AreEqual(9, l);
                Assert.IsFalse(tree.Cut(new CubeRoot(-50), out l, out lv, out h, out hv));
                Assert.IsTrue(!lv && hv);
                Assert.AreEqual(0, h);
            }


            [TearDown]
            public void Dispose() { ic = null; tree = null; }
        }
    }




    namespace MultiOps
    {
        [TestFixture]
        public class AddAll
        {
            private int sqr(int i) { return i * i; }


            TreeSet<int> tree;


            [SetUp]
            public void Init() { tree = new TreeSet<int>(new IC()); }


            [Test, LuceneNetSpecific]
            public void EmptyEmpty()
            {
                tree.AddAll(new FunEnumerable(0, new Func<int, int>(sqr)));
                Assert.AreEqual(0, tree.Count);
                Assert.IsTrue(tree.Check());
            }


            [Test, LuceneNetSpecific]
            public void SomeEmpty()
            {
                for (int i = 4; i < 9; i++) tree.Add(i);

                tree.AddAll(new FunEnumerable(0, new Func<int, int>(sqr)));
                Assert.AreEqual(5, tree.Count);
                Assert.IsTrue(tree.Check());
            }


            [Test, LuceneNetSpecific]
            public void EmptySome()
            {
                tree.AddAll(new FunEnumerable(4, new Func<int, int>(sqr)));
                Assert.AreEqual(4, tree.Count);
                Assert.IsTrue(tree.Check());
                Assert.AreEqual(0, tree[0]);
                Assert.AreEqual(1, tree[1]);
                Assert.AreEqual(4, tree[2]);
                Assert.AreEqual(9, tree[3]);
            }


            [Test, LuceneNetSpecific]
            public void SomeSome()
            {
                for (int i = 5; i < 9; i++) tree.Add(i);

                tree.AddAll(new FunEnumerable(4, new Func<int, int>(sqr)));
                Assert.AreEqual(8, tree.Count);
                Assert.IsTrue(tree.Check());
                Assert.IsTrue(IC.eq(tree, 0, 1, 4, 5, 6, 7, 8, 9));
            }


            [TearDown]
            public void Dispose() { tree = null; }
        }



        [TestFixture]
        public class AddSorted
        {
            private int sqr(int i) { return i * i; }


            private int bad(int i) { return i * (5 - i); }


            TreeSet<int> tree;


            [SetUp]
            public void Init() { tree = new TreeSet<int>(new IC()); }


            [Test, LuceneNetSpecific]
            public void EmptyEmpty()
            {
                tree.AddSorted(new FunEnumerable(0, new Func<int, int>(sqr)));
                Assert.AreEqual(0, tree.Count);
                Assert.IsTrue(tree.Check());
            }



            [Test, LuceneNetSpecific]
            public void SomeEmpty()
            {
                for (int i = 4; i < 9; i++) tree.Add(i);

                tree.AddSorted(new FunEnumerable(0, new Func<int, int>(sqr)));
                Assert.AreEqual(5, tree.Count);
                Assert.IsTrue(tree.Check());
            }



            [Test, LuceneNetSpecific]
            public void EmptySome()
            {
                tree.AddSorted(new FunEnumerable(4, new Func<int, int>(sqr)));
                Assert.AreEqual(4, tree.Count);
                Assert.IsTrue(tree.Check());
                Assert.AreEqual(0, tree[0]);
                Assert.AreEqual(1, tree[1]);
                Assert.AreEqual(4, tree[2]);
                Assert.AreEqual(9, tree[3]);
            }



            [Test, LuceneNetSpecific]
            public void SomeSome()
            {
                for (int i = 5; i < 9; i++) tree.Add(i);

                tree.AddSorted(new FunEnumerable(4, new Func<int, int>(sqr)));
                Assert.AreEqual(8, tree.Count);
                Assert.IsTrue(tree.Check());
                Assert.IsTrue(IC.eq(tree, 0, 1, 4, 5, 6, 7, 8, 9));
            }

            [Test, LuceneNetSpecific]
            public void EmptyBad()
            {
                var exception = Assert.Throws<ArgumentException>(() => tree.AddSorted(new FunEnumerable(9, new Func<int, int>(bad))));
                Assert.AreEqual("Argument not sorted", exception.Message);
            }


            [TearDown]
            public void Dispose() { tree = null; }
        }

        [TestFixture]
        public class Rest
        {
            TreeSet<int> tree, tree2;


            [SetUp]
            public void Init()
            {
                tree = new TreeSet<int>(new IC());
                tree2 = new TreeSet<int>(new IC());
                for (int i = 0; i < 10; i++)
                    tree.Add(i);

                for (int i = 0; i < 10; i++)
                    tree2.Add(2 * i);
            }


            [Test, LuceneNetSpecific]
            public void RemoveAll()
            {
                tree.RemoveAll(tree2.RangeFromTo(3, 7));
                Assert.AreEqual(8, tree.Count);
                Assert.IsTrue(tree.Check());
                Assert.IsTrue(IC.eq(tree, 0, 1, 2, 3, 5, 7, 8, 9));
                tree.RemoveAll(tree2.RangeFromTo(3, 7));
                Assert.AreEqual(8, tree.Count);
                Assert.IsTrue(tree.Check());
                Assert.IsTrue(IC.eq(tree, 0, 1, 2, 3, 5, 7, 8, 9));
                tree.RemoveAll(tree2.RangeFromTo(13, 17));
                Assert.AreEqual(8, tree.Count);
                Assert.IsTrue(tree.Check());
                Assert.IsTrue(IC.eq(tree, 0, 1, 2, 3, 5, 7, 8, 9));
                tree.RemoveAll(tree2.RangeFromTo(3, 17));
                Assert.AreEqual(7, tree.Count);
                Assert.IsTrue(tree.Check());
                Assert.IsTrue(IC.eq(tree, 0, 1, 2, 3, 5, 7, 9));
                for (int i = 0; i < 10; i++) tree2.Add(i);

                tree.RemoveAll(tree2.RangeFromTo(-1, 10));
                Assert.AreEqual(0, tree.Count);
                Assert.IsTrue(tree.Check());
                Assert.IsTrue(IC.eq(tree));
            }


            [Test, LuceneNetSpecific]
            public void RetainAll()
            {
                tree.RetainAll(tree2.RangeFromTo(3, 17));
                Assert.AreEqual(3, tree.Count);
                Assert.IsTrue(tree.Check());
                Assert.IsTrue(IC.eq(tree, 4, 6, 8));
                tree.RetainAll(tree2.RangeFromTo(1, 17));
                Assert.AreEqual(3, tree.Count);
                Assert.IsTrue(tree.Check());
                Assert.IsTrue(IC.eq(tree, 4, 6, 8));
                tree.RetainAll(tree2.RangeFromTo(3, 5));
                Assert.AreEqual(1, tree.Count);
                Assert.IsTrue(tree.Check());
                Assert.IsTrue(IC.eq(tree, 4));
                tree.RetainAll(tree2.RangeFromTo(7, 17));
                Assert.AreEqual(0, tree.Count);
                Assert.IsTrue(tree.Check());
                Assert.IsTrue(IC.eq(tree));
                for (int i = 0; i < 10; i++) tree.Add(i);

                tree.RetainAll(tree2.RangeFromTo(5, 5));
                Assert.AreEqual(0, tree.Count);
                Assert.IsTrue(tree.Check());
                Assert.IsTrue(IC.eq(tree));
                for (int i = 0; i < 10; i++) tree.Add(i);

                tree.RetainAll(tree2.RangeFromTo(15, 25));
                Assert.AreEqual(0, tree.Count);
                Assert.IsTrue(tree.Check());
                Assert.IsTrue(IC.eq(tree));
            }


            [Test, LuceneNetSpecific]
            public void ContainsAll()
            {
                Assert.IsFalse(tree.ContainsAll(tree2));
                Assert.IsTrue(tree.ContainsAll(tree));
                tree2.Clear();
                Assert.IsTrue(tree.ContainsAll(tree2));
                tree.Clear();
                Assert.IsTrue(tree.ContainsAll(tree2));
                tree2.Add(8);
                Assert.IsFalse(tree.ContainsAll(tree2));
            }


            [Test, LuceneNetSpecific]
            public void RemoveInterval()
            {
                tree.RemoveInterval(3, 4);
                Assert.IsTrue(tree.Check());
                Assert.AreEqual(6, tree.Count);
                Assert.IsTrue(IC.eq(tree, 0, 1, 2, 7, 8, 9));
                tree.RemoveInterval(2, 3);
                Assert.IsTrue(tree.Check());
                Assert.AreEqual(3, tree.Count);
                Assert.IsTrue(IC.eq(tree, 0, 1, 9));
                tree.RemoveInterval(0, 3);
                Assert.IsTrue(tree.Check());
                Assert.AreEqual(0, tree.Count);
                Assert.IsTrue(IC.eq(tree));
            }


            [Test, LuceneNetSpecific]
            public void RemoveRangeBad1()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => tree.RemoveInterval(-3, 8));
            }


            [Test, LuceneNetSpecific]
            public void RemoveRangeBad2()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => tree.RemoveInterval(3, -8));
            }


            [Test, LuceneNetSpecific]
            public void RemoveRangeBad3()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => tree.RemoveInterval(3, 8));
            }


            [Test, LuceneNetSpecific]
            public void GetRange()
            {
                SCG.IEnumerable<int> e = tree[3, 3];
                Assert.IsTrue(IC.eq(e, 3, 4, 5));
                e = tree[3, 0];
                Assert.IsTrue(IC.eq(e));
            }

            [Test, LuceneNetSpecific]
            public void GetRangeBad1()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    object foo = tree[-3, 0];
                });
            }

            [Test, LuceneNetSpecific]
            public void GetRangeBad2()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    object foo = tree[3, -1];
                });
            }

            [Test, LuceneNetSpecific]
            public void GetRangeBad3()
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    object foo = tree[3, 8];
                });
            }

            [TearDown]
            public void Dispose() { tree = null; tree2 = null; }
        }
    }




    namespace Sync
    {
        [TestFixture]
        public class SyncRoot
        {
            private TreeSet<int> tree;
            private readonly Object mySyncRoot = new Object();
            int sz = 5000;


            [Test, LuceneNetSpecific]
            public void Safe()
            {
                System.Threading.Thread t1 = new System.Threading.Thread(new System.Threading.ThreadStart(safe1));
                System.Threading.Thread t2 = new System.Threading.Thread(new System.Threading.ThreadStart(safe2));

                t1.Start();
                t2.Start();
                t1.Join();
                t2.Join();
                Assert.AreEqual(2 * sz + 1, tree.Count);
                Assert.IsTrue(tree.Check());
            }


            //[Test, LuceneNetSpecific]
            public void UnSafe()
            {
                bool bad = false;

                for (int i = 0; i < 10; i++)
                {
                    System.Threading.Thread t1 = new System.Threading.Thread(new System.Threading.ThreadStart(unsafe1));
                    System.Threading.Thread t2 = new System.Threading.Thread(new System.Threading.ThreadStart(unsafe2));

                    t1.Start();
                    t2.Start();
                    t1.Join();
                    t2.Join();
                    if (bad = 2 * sz + 1 != tree.Count)
                    {
                        Console.WriteLine("{0}::Unsafe(): bad at {1}", GetType(), i);
                        break;
                    }
                }

                Assert.IsTrue(bad, "No sync problems!");
            }


            [Test, LuceneNetSpecific]
            public void SafeUnSafe()
            {
                System.Threading.Thread t1 = new System.Threading.Thread(new System.Threading.ThreadStart(unsafe1));
                System.Threading.Thread t2 = new System.Threading.Thread(new System.Threading.ThreadStart(unsafe2));

                t1.Start();
                t1.Join();
                t2.Start();
                t2.Join();
                Assert.AreEqual(2 * sz + 1, tree.Count);
            }


            [SetUp]
            public void Init() { tree = new TreeSet<int>(new IC()); }


            private void unsafe1()
            {
                for (int i = 0; i < 2 * sz; i++)
                    tree.Add(i * 2);

                for (int i = 1; i < sz; i++)
                    tree.Remove(i * 4);
            }


            private void safe1()
            {
                for (int i = 0; i < 2 * sz; i++)
                    lock (mySyncRoot)
                        tree.Add(i * 2);

                for (int i = 1; i < sz; i++)
                    lock (mySyncRoot)
                        tree.Remove(i * 4);
            }


            private void unsafe2()
            {
                for (int i = 2 * sz; i > 0; i--)
                    tree.Add(i * 2 + 1);

                for (int i = sz; i > 0; i--)
                    tree.Remove(i * 4 + 1);
            }


            private void safe2()
            {
                for (int i = 2 * sz; i > 0; i--)
                    lock (mySyncRoot)
                        tree.Add(i * 2 + 1);

                for (int i = sz; i > 0; i--)
                    lock (mySyncRoot)
                        tree.Remove(i * 4 + 1);
            }


            [TearDown]
            public void Dispose() { tree = null; }
        }



        //[TestFixture]
        public class ConcurrentQueries
        {
            private TreeSet<int> tree;

            int sz = 500000;


            [SetUp]
            public void Init()
            {
                tree = new TreeSet<int>(new IC());
                for (int i = 0; i < sz; i++)
                {
                    tree.Add(i);
                }
            }



            class A
            {
                public int count = 0;

                TreeSet<int> t;


                public A(TreeSet<int> t) { this.t = t; }


                public void a(int i) { count++; }


                public void traverse() { t.Apply(new Action<int>(a)); }
            }




            [Test, LuceneNetSpecific]
            public void Safe()
            {
                A a = new A(tree);

                a.traverse();
                Assert.AreEqual(sz, a.count);
            }


            [Test, LuceneNetSpecific]
            public void RegrettablyUnsafe()
            {
                System.Threading.Thread[] t = new System.Threading.Thread[10];
                A[] a = new A[10];
                for (int i = 0; i < 10; i++)
                {
                    a[i] = new A(tree);
                    t[i] = new System.Threading.Thread(new System.Threading.ThreadStart(a[i].traverse));
                }

                for (int i = 0; i < 10; i++)
                    t[i].Start();
                for (int i = 0; i < 10; i++)
                    t[i].Join();
                for (int i = 0; i < 10; i++)
                    Assert.AreEqual(sz, a[i].count);

            }


            [TearDown]
            public void Dispose() { tree = null; }
        }
    }




    namespace Hashing
    {
        [TestFixture]
        public class ISequenced
        {
            private ISequenced<int> dit, dat, dut;


            [SetUp]
            public void Init()
            {
                dit = new TreeSet<int>(SCG.Comparer<int>.Default, EqualityComparer<int>.Default);
                dat = new TreeSet<int>(SCG.Comparer<int>.Default, EqualityComparer<int>.Default);
                dut = new TreeSet<int>(new RevIC(), EqualityComparer<int>.Default);
            }


            [Test, LuceneNetSpecific]
            public void EmptyEmpty()
            {
                Assert.IsTrue(dit.SequencedEquals(dat));
            }


            [Test, LuceneNetSpecific]
            public void EmptyNonEmpty()
            {
                dit.Add(3);
                Assert.IsFalse(dit.SequencedEquals(dat));
                Assert.IsFalse(dat.SequencedEquals(dit));
            }

            [Test, LuceneNetSpecific]
            public void HashVal()
            {
                Assert.AreEqual(CHC.sequencedhashcode(), dit.GetSequencedHashCode());
                dit.Add(3);
                Assert.AreEqual(CHC.sequencedhashcode(3), dit.GetSequencedHashCode());
                dit.Add(7);
                Assert.AreEqual(CHC.sequencedhashcode(3, 7), dit.GetSequencedHashCode());
                Assert.AreEqual(CHC.sequencedhashcode(), dut.GetSequencedHashCode());
                dut.Add(3);
                Assert.AreEqual(CHC.sequencedhashcode(3), dut.GetSequencedHashCode());
                dut.Add(7);
                Assert.AreEqual(CHC.sequencedhashcode(7, 3), dut.GetSequencedHashCode());
            }


            [Test, LuceneNetSpecific]
            public void Normal()
            {
                dit.Add(3);
                dit.Add(7);
                dat.Add(3);
                Assert.IsFalse(dit.SequencedEquals(dat));
                Assert.IsFalse(dat.SequencedEquals(dit));
                dat.Add(7);
                Assert.IsTrue(dit.SequencedEquals(dat));
                Assert.IsTrue(dat.SequencedEquals(dit));
            }


            [Test, LuceneNetSpecific]
            public void WrongOrder()
            {
                dit.Add(3);
                dut.Add(3);
                Assert.IsTrue(dit.SequencedEquals(dut));
                Assert.IsTrue(dut.SequencedEquals(dit));
                dit.Add(7);
                dut.Add(7);
                Assert.IsFalse(dit.SequencedEquals(dut));
                Assert.IsFalse(dut.SequencedEquals(dit));
            }


            [Test, LuceneNetSpecific]
            public void Reflexive()
            {
                Assert.IsTrue(dit.SequencedEquals(dit));
                dit.Add(3);
                Assert.IsTrue(dit.SequencedEquals(dit));
                dit.Add(7);
                Assert.IsTrue(dit.SequencedEquals(dit));
            }


            [TearDown]
            public void Dispose()
            {
                dit = null;
                dat = null;
                dut = null;
            }
        }



        [TestFixture]
        public class IEditableCollection
        {
            private ICollection<int> dit, dat, dut;


            [SetUp]
            public void Init()
            {
                dit = new TreeSet<int>(SCG.Comparer<int>.Default, EqualityComparer<int>.Default);
                dat = new TreeSet<int>(SCG.Comparer<int>.Default, EqualityComparer<int>.Default);
                dut = new TreeSet<int>(new RevIC(), EqualityComparer<int>.Default);
            }


            [Test, LuceneNetSpecific]
            public void EmptyEmpty()
            {
                Assert.IsTrue(dit.UnsequencedEquals(dat));
            }


            [Test, LuceneNetSpecific]
            public void EmptyNonEmpty()
            {
                dit.Add(3);
                Assert.IsFalse(dit.UnsequencedEquals(dat));
                Assert.IsFalse(dat.UnsequencedEquals(dit));
            }


            [Test, LuceneNetSpecific]
            public void HashVal()
            {
                Assert.AreEqual(CHC.unsequencedhashcode(), dit.GetUnsequencedHashCode());
                dit.Add(3);
                Assert.AreEqual(CHC.unsequencedhashcode(3), dit.GetUnsequencedHashCode());
                dit.Add(7);
                Assert.AreEqual(CHC.unsequencedhashcode(3, 7), dit.GetUnsequencedHashCode());
                Assert.AreEqual(CHC.unsequencedhashcode(), dut.GetUnsequencedHashCode());
                dut.Add(3);
                Assert.AreEqual(CHC.unsequencedhashcode(3), dut.GetUnsequencedHashCode());
                dut.Add(7);
                Assert.AreEqual(CHC.unsequencedhashcode(7, 3), dut.GetUnsequencedHashCode());
            }


            [Test, LuceneNetSpecific]
            public void Normal()
            {
                dit.Add(3);
                dit.Add(7);
                dat.Add(3);
                Assert.IsFalse(dit.UnsequencedEquals(dat));
                Assert.IsFalse(dat.UnsequencedEquals(dit));
                dat.Add(7);
                Assert.IsTrue(dit.UnsequencedEquals(dat));
                Assert.IsTrue(dat.UnsequencedEquals(dit));
            }


            [Test, LuceneNetSpecific]
            public void WrongOrder()
            {
                dit.Add(3);
                dut.Add(3);
                Assert.IsTrue(dit.UnsequencedEquals(dut));
                Assert.IsTrue(dut.UnsequencedEquals(dit));
                dit.Add(7);
                dut.Add(7);
                Assert.IsTrue(dit.UnsequencedEquals(dut));
                Assert.IsTrue(dut.UnsequencedEquals(dit));
            }


            [Test, LuceneNetSpecific]
            public void Reflexive()
            {
                Assert.IsTrue(dit.UnsequencedEquals(dit));
                dit.Add(3);
                Assert.IsTrue(dit.UnsequencedEquals(dit));
                dit.Add(7);
                Assert.IsTrue(dit.UnsequencedEquals(dit));
            }


            [TearDown]
            public void Dispose()
            {
                dit = null;
                dat = null;
                dut = null;
            }
        }

    }
}