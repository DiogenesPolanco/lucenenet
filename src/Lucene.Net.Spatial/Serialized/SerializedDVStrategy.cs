﻿using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Spatial.Util;
using Lucene.Net.Support.IO;
using Lucene.Net.Util;
using Spatial4n.Core.Context;
using Spatial4n.Core.IO;
using Spatial4n.Core.Shapes;
using System;
using System.Collections;
using System.IO;

namespace Lucene.Net.Spatial.Serialized
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

    /// <summary>
    /// A <see cref="SpatialStrategy"/> based on serializing a Shape stored into BinaryDocValues.
    /// This is not at all fast; it's designed to be used in conjuction with another index based
    /// SpatialStrategy that is approximated(like <see cref="Prefix.RecursivePrefixTreeStrategy"/>)
    /// to add precision or eventually make more specific / advanced calculations on the per-document
    /// geometry.
    /// The serialization uses Spatial4j's <see cref="BinaryCodec"/>.
    ///
    /// @lucene.experimental
    /// </summary>
    public class SerializedDVStrategy : SpatialStrategy
    {
        /// <summary>
        /// A cache heuristic for the buf size based on the last shape size.
        /// </summary>
        //TODO do we make this non-volatile since it's merely a heuristic?
        private volatile int indexLastBufSize = 8 * 1024;//8KB default on first run

        /// <summary>
        /// Constructs the spatial strategy with its mandatory arguments.
        /// </summary>
        public SerializedDVStrategy(SpatialContext ctx, string fieldName)
            : base(ctx, fieldName)
        {
        }

        public override Field[] CreateIndexableFields(IShape shape)
        {
            int bufSize = Math.Max(128, (int)(this.indexLastBufSize * 1.5));//50% headroom over last
            ByteArrayOutputStream byteStream = new ByteArrayOutputStream(bufSize);
            BytesRef bytesRef = new BytesRef();//receiver of byteStream's bytes
            try
            {
                m_ctx.BinaryCodec.WriteShape(new BinaryWriter(byteStream), shape);

                //this is a hack to avoid redundant byte array copying by byteStream.toByteArray()
                byteStream.WriteTo(new OutputStreamAnonymousHelper(bytesRef));
            }
            catch (IOException e)
            {
                throw new Exception(e.ToString(), e);
            }
            this.indexLastBufSize = bytesRef.Length;//cache heuristic
            return new Field[] { new BinaryDocValuesField(FieldName, bytesRef) };
        }

        internal class OutputStreamAnonymousHelper : MemoryStream
        {
            private readonly BytesRef bytesRef;

            public OutputStreamAnonymousHelper(BytesRef bytesRef)
            {
                this.bytesRef = bytesRef;
            }

            public override void Write(byte[] buffer, int index, int count)
            {
                bytesRef.Bytes = buffer;
                bytesRef.Offset = index;
                bytesRef.Length = count;
            }
        }

        public override ValueSource MakeDistanceValueSource(IPoint queryPoint, double multiplier)
        {
            //TODO if makeShapeValueSource gets lifted to the top; this could become a generic impl.
            return new DistanceToShapeValueSource(MakeShapeValueSource(), queryPoint, multiplier, m_ctx);
        }

        public override ConstantScoreQuery MakeQuery(SpatialArgs args)
        {
            throw new NotSupportedException("This strategy can't return a query that operates" +
                " efficiently. Instead try a Filter or ValueSource.");
        }

        /// <summary>
        /// Returns a <see cref="Filter"/> that should be used with <see cref="FilteredQuery.QUERY_FIRST_FILTER_STRATEGY"/>.
        /// Use in another manner is likely to result in an <see cref="NotSupportedException"/>
        /// to prevent misuse because the filter can't efficiently work via iteration.
        /// </summary>
        public override Filter MakeFilter(SpatialArgs args)
        {
            ValueSource shapeValueSource = MakeShapeValueSource();
            ShapePredicateValueSource predicateValueSource = new ShapePredicateValueSource(
                shapeValueSource, args.Operation, args.Shape);
            return new PredicateValueSourceFilter(predicateValueSource);
        }

        /// <summary>
        /// Provides access to each shape per document as a ValueSource in which
        /// <see cref="FunctionValues.ObjectVal(int)"/> returns a <see cref="IShape"/>.
        /// </summary>
        //TODO raise to SpatialStrategy
        public virtual ValueSource MakeShapeValueSource()
        {
            return new ShapeDocValueSource(this, FieldName, m_ctx.BinaryCodec);
        }

        /// <summary>
        /// This filter only supports returning a DocSet with a GetBits(). If you try to grab the
        /// iterator then you'll get a <see cref="NotSupportedException"/>.
        /// </summary>
        internal class PredicateValueSourceFilter : Filter
        {
            private readonly ValueSource predicateValueSource;//we call boolVal(doc)

            public PredicateValueSourceFilter(ValueSource predicateValueSource)
            {
                this.predicateValueSource = predicateValueSource;
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                return new DocIdSetAnonymousHelper(this, context, acceptDocs);
            }

            internal class DocIdSetAnonymousHelper : DocIdSet
            {
                private readonly PredicateValueSourceFilter outerInstance;
                private readonly AtomicReaderContext context;
                private readonly IBits acceptDocs;

                public DocIdSetAnonymousHelper(PredicateValueSourceFilter outerInstance, AtomicReaderContext context, IBits acceptDocs)
                {
                    this.outerInstance = outerInstance;
                    this.context = context;
                    this.acceptDocs = acceptDocs;
                }

                public override DocIdSetIterator GetIterator()
                {
                    throw new NotSupportedException(
                        "Iteration is too slow; instead try FilteredQuery.QUERY_FIRST_FILTER_STRATEGY");
                        //Note that if you're truly bent on doing this, then see FunctionValues.getRangeScorer
                }

                public override IBits Bits
                {
                    get
                    {
                        //null Map context -- we simply don't have one. That's ok.
                        FunctionValues predFuncValues = outerInstance.predicateValueSource.GetValues(null, context);

                        return new BitsAnonymousHelper(this, predFuncValues, context, acceptDocs);
                    }
                }

                internal class BitsAnonymousHelper : IBits
                {
                    private readonly DocIdSetAnonymousHelper outerInstance;
                    private readonly FunctionValues predFuncValues;
                    private readonly AtomicReaderContext context;
                    private readonly IBits acceptDocs;

                    public BitsAnonymousHelper(DocIdSetAnonymousHelper outerInstance, FunctionValues predFuncValues, AtomicReaderContext context, IBits acceptDocs)
                    {
                        this.outerInstance = outerInstance;
                        this.predFuncValues = predFuncValues;
                        this.context = context;
                        this.acceptDocs = acceptDocs;
                    }

                    public virtual bool Get(int index)
                    {
                        if (acceptDocs != null && !acceptDocs.Get(index))
                            return false;
                        return predFuncValues.BoolVal(index);
                    }

                    public virtual int Length
                    {
                        get { return context.Reader.MaxDoc; }
                    }
                }
            }

            public override bool Equals(object o)
            {
                if (this == o) return true;
                if (o == null || GetType() != o.GetType()) return false;

                PredicateValueSourceFilter that = (PredicateValueSourceFilter)o;

                if (!predicateValueSource.Equals(that.predicateValueSource)) return false;

                return true;
            }


            public override int GetHashCode()
            {
                return predicateValueSource.GetHashCode();
            }
        }//PredicateValueSourceFilter

        /// <summary>
        /// Implements a <see cref="ValueSource"/> by deserializing a <see cref="IShape"/> in from <see cref="BinaryDocValues"/> using <see cref="BinaryCodec"/>.
        /// </summary>
        /// <seealso cref="MakeShapeValueSource()"/>
        internal class ShapeDocValueSource : ValueSource
        {
            private readonly SerializedDVStrategy outerInstance;
            private readonly string fieldName;
            private readonly BinaryCodec binaryCodec;//spatial4n

            internal ShapeDocValueSource(SerializedDVStrategy outerInstance, string fieldName, BinaryCodec binaryCodec)
            {
                this.outerInstance = outerInstance;
                this.fieldName = fieldName;
                this.binaryCodec = binaryCodec;
            }

            public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
            {
                BinaryDocValues docValues = readerContext.AtomicReader.GetBinaryDocValues(fieldName);

                return new FuctionValuesAnonymousHelper(this, docValues);
            }

            internal class FuctionValuesAnonymousHelper : FunctionValues
            {
                private readonly ShapeDocValueSource outerInstance;
                private readonly BinaryDocValues docValues;

                public FuctionValuesAnonymousHelper(ShapeDocValueSource outerInstance, BinaryDocValues docValues)
                {
                    this.outerInstance = outerInstance;
                    this.docValues = docValues;
                }

                private int bytesRefDoc = -1;
                private BytesRef bytesRef = new BytesRef();//scratch

                internal bool FillBytes(int doc)
                {
                    if (bytesRefDoc != doc)
                    {
                        docValues.Get(doc, bytesRef);
                        bytesRefDoc = doc;
                    }
                    return bytesRef.Length != 0;
                }

                public override bool Exists(int doc)
                {
                    return FillBytes(doc);
                }

                public override bool BytesVal(int doc, BytesRef target)
                {
                    if (FillBytes(doc))
                    {
                        target.Bytes = bytesRef.Bytes;
                        target.Offset = bytesRef.Offset;
                        target.Length = bytesRef.Length;
                        return true;
                    }
                    else
                    {
                        target.Length = 0;
                        return false;
                    }
                }

                public override object ObjectVal(int docId)
                {
                    if (!FillBytes(docId))
                        return null;
                    BinaryReader dataInput = new BinaryReader(
                        new MemoryStream(bytesRef.Bytes, bytesRef.Offset, bytesRef.Length));
                    try
                    {
                        return outerInstance.binaryCodec.ReadShape(dataInput);
                    }
                    catch (IOException e)
                    {
                        throw new Exception(e.ToString(), e);
                    }
                }

                public override Explanation Explain(int doc)
                {
                    return new Explanation(float.NaN, ToString(doc));
                }

                public override string ToString(int doc)
                {
                    return outerInstance.GetDescription() + "=" + ObjectVal(doc);//TODO truncate?
                }
            }

            public override bool Equals(object o)
            {
                if (this == o) return true;
                if (o == null || GetType() != o.GetType()) return false;

                ShapeDocValueSource that = (ShapeDocValueSource)o;

                if (!fieldName.Equals(that.fieldName, StringComparison.Ordinal)) return false;

                return true;
            }

            public override int GetHashCode()
            {
                int result = fieldName.GetHashCode();
                return result;
            }

            public override string GetDescription()
            {
                return "shapeDocVal(" + fieldName + ")";
            }

        }//ShapeDocValueSource
    }
}
