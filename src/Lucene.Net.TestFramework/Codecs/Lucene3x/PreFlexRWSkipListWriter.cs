namespace Lucene.Net.Codecs.Lucene3x
{
    using Lucene.Net.Support;

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

    using IndexOutput = Lucene.Net.Store.IndexOutput;

    /// <summary>
    /// PreFlexRW skiplist implementation.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class PreFlexRWSkipListWriter : MultiLevelSkipListWriter
    {
        private int[] lastSkipDoc;
        private int[] lastSkipPayloadLength;
        private long[] lastSkipFreqPointer;
        private long[] lastSkipProxPointer;

        private IndexOutput freqOutput;
        private IndexOutput proxOutput;

        private int curDoc;
        private bool curStorePayloads;
        private int curPayloadLength;
        private long curFreqPointer;
        private long curProxPointer;

        public PreFlexRWSkipListWriter(int skipInterval, int numberOfSkipLevels, int docCount, IndexOutput freqOutput, IndexOutput proxOutput)
            : base(skipInterval, numberOfSkipLevels, docCount)
        {
            this.freqOutput = freqOutput;
            this.proxOutput = proxOutput;

            lastSkipDoc = new int[numberOfSkipLevels];
            lastSkipPayloadLength = new int[numberOfSkipLevels];
            lastSkipFreqPointer = new long[numberOfSkipLevels];
            lastSkipProxPointer = new long[numberOfSkipLevels];
        }

        /// <summary>
        /// Sets the values for the current skip data.
        /// </summary>
        public virtual void SetSkipData(int doc, bool storePayloads, int payloadLength)
        {
            this.curDoc = doc;
            this.curStorePayloads = storePayloads;
            this.curPayloadLength = payloadLength;
            this.curFreqPointer = freqOutput.GetFilePointer();
            if (proxOutput != null)
            {
                this.curProxPointer = proxOutput.GetFilePointer();
            }
        }

        public override void ResetSkip()
        {
            base.ResetSkip();
            Arrays.Fill(lastSkipDoc, 0);
            Arrays.Fill(lastSkipPayloadLength, -1); // we don't have to write the first length in the skip list
            Arrays.Fill(lastSkipFreqPointer, freqOutput.GetFilePointer());
            if (proxOutput != null)
            {
                Arrays.Fill(lastSkipProxPointer, proxOutput.GetFilePointer());
            }
        }

        protected override void WriteSkipData(int level, IndexOutput skipBuffer)
        {
            // To efficiently store payloads in the posting lists we do not store the length of
            // every payload. Instead we omit the length for a payload if the previous payload had
            // the same length.
            // However, in order to support skipping the payload length at every skip point must be known.
            // So we use the same length encoding that we use for the posting lists for the skip data as well:
            // Case 1: current field does not store payloads
            //           SkipDatum                 --> DocSkip, FreqSkip, ProxSkip
            //           DocSkip,FreqSkip,ProxSkip --> VInt
            //           DocSkip records the document number before every SkipInterval th  document in TermFreqs.
            //           Document numbers are represented as differences from the previous value in the sequence.
            // Case 2: current field stores payloads
            //           SkipDatum                 --> DocSkip, PayloadLength?, FreqSkip,ProxSkip
            //           DocSkip,FreqSkip,ProxSkip --> VInt
            //           PayloadLength             --> VInt
            //         In this case DocSkip/2 is the difference between
            //         the current and the previous value. If DocSkip
            //         is odd, then a PayloadLength encoded as VInt follows,
            //         if DocSkip is even, then it is assumed that the
            //         current payload length equals the length at the previous
            //         skip point
            if (curStorePayloads)
            {
                int delta = curDoc - lastSkipDoc[level];
                if (curPayloadLength == lastSkipPayloadLength[level])
                {
                    // the current payload length equals the length at the previous skip point,
                    // so we don't store the length again
                    skipBuffer.WriteVInt32(delta * 2);
                }
                else
                {
                    // the payload length is different from the previous one. We shift the DocSkip,
                    // set the lowest bit and store the current payload length as VInt.
                    skipBuffer.WriteVInt32(delta * 2 + 1);
                    skipBuffer.WriteVInt32(curPayloadLength);
                    lastSkipPayloadLength[level] = curPayloadLength;
                }
            }
            else
            {
                // current field does not store payloads
                skipBuffer.WriteVInt32(curDoc - lastSkipDoc[level]);
            }

            skipBuffer.WriteVInt32((int)(curFreqPointer - lastSkipFreqPointer[level]));
            skipBuffer.WriteVInt32((int)(curProxPointer - lastSkipProxPointer[level]));

            lastSkipDoc[level] = curDoc;

            lastSkipFreqPointer[level] = curFreqPointer;
            lastSkipProxPointer[level] = curProxPointer;
        }
    }
}