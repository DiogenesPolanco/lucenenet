﻿
<!--
 Licensed to the Apache Software Foundation (ASF) under one or more
 contributor license agreements.  See the NOTICE file distributed with
 this work for additional information regarding copyright ownership.
 The ASF licenses this file to You under the Apache License, Version 2.0
 (the "License"); you may not use this file except in compliance with
 the License.  You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
-->

## Search Quality Benchmarking.

This package allows to benchmark search quality of a Lucene application.

In order to use this package you should provide:

*   A [IndexSearcher]({@docRoot}/../core/org/apache/lucene/search/IndexSearcher.html).
*   [Quality queries](QualityQuery.html).
*   [Judging object](Judge.html).
*   [Reporting object](utils/SubmissionReport.html).

For benchmarking TREC collections with TREC QRels, take a look at the 
[trec package](trec/package-summary.html).

Here is a sample code used to run the TREC 2006 queries 701-850 on the .Gov2 collection:

        File topicsFile = new File("topics-701-850.txt");
        File qrelsFile = new File("qrels-701-850.txt");
        IndexReader ir = DirectoryReader.open(directory):
        IndexSearcher searcher = new IndexSearcher(ir);
    
    int maxResults = 1000;
        String docNameField = "docname"; 

        PrintWriter logger = new PrintWriter(System.out,true); 
    
    // use trec utilities to read trec topics into quality queries
        TrecTopicsReader qReader = new TrecTopicsReader();
        QualityQuery qqs[] = qReader.readQueries(new BufferedReader(new FileReader(topicsFile)));

        // prepare judge, with trec utilities that read from a QRels file
        Judge judge = new TrecJudge(new BufferedReader(new FileReader(qrelsFile)));

        // validate topics & judgments match each other
        judge.validateData(qqs, logger);

        // set the parsing of quality queries into Lucene queries.
        QualityQueryParser qqParser = new SimpleQQParser("title", "body");

        // run the benchmark
        QualityBenchmark qrun = new QualityBenchmark(qqs, qqParser, searcher, docNameField);
        SubmissionReport submitLog = null;
        QualityStats stats[] = qrun.execute(maxResults, judge, submitLog, logger);

        // print an avarage sum of the results
        QualityStats avg = QualityStats.average(stats);
        avg.log("SUMMARY",2,logger, "  ");

Some immediate ways to modify this program to your needs are:

*   To run on different formats of queries and judgements provide your own 
      [Judge](Judge.html) and 
      [Quality queries](QualityQuery.html).
*   Create sophisticated Lucene queries by supplying a different 
  [Quality query parser](QualityQueryParser.html).