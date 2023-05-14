using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System.Collections.Generic;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;

namespace WireMockInspector.ViewModels;

public class SearchHelper
{
    const LuceneVersion luceneVersion = LuceneVersion.LUCENE_48;

    private readonly IndexWriter indexWriter;
    private readonly StandardAnalyzer standardAnalyzer;

    public SearchHelper()
    {
        this.standardAnalyzer = new StandardAnalyzer(luceneVersion);
        this.indexWriter = new IndexWriter(new RAMDirectory(), new IndexWriterConfig(luceneVersion, standardAnalyzer));
    }

    public void Load(IReadOnlyList<RequestViewModel> requests)
    {
        this.indexWriter.DeleteAll();

        foreach (var request in requests)
        {
            var doc = new Document
            {
                new StringField("_id", request.Raw.Guid.ToString(), Field.Store.YES),
                new StringField("clientip", request.Raw.Request.ClientIP.ToLower(), Field.Store.NO),
                new StringField("method", request.Method.ToLower(), Field.Store.NO),
                new StringField("url", request.Raw.Request.Url.ToLower(), Field.Store.NO),
                new StringField("path", request.Path.ToLower(), Field.Store.NO)
            };

            if (request.Raw.Request.Headers is { } headers)
                foreach (var (key, val) in headers)
                {
                    doc.Add(new StringField("request.header", key.ToLower(), Field.Store.NO));
                    foreach (var v in val)
                    {
                        doc.Add(new StringField(ToFieldName("request.header", key), v.ToLower(), Field.Store.NO));
                    }
                }

            if (request.Raw.Request.Cookies is { } cookies)
                foreach (var (key, val) in cookies)
                {
                    doc.Add(new StringField("request.cookie", key.ToLower(), Field.Store.NO));
                    doc.Add(new StringField(ToFieldName("request.cookie", key), val.ToLower(), Field.Store.NO));
                }

            if (request.Raw.Request.Query is { } query)
                foreach (var (key, val) in query)
                {
                    doc.Add(new StringField("param", key.ToLower(), Field.Store.NO));
                    foreach (var v in val)
                    {
                        doc.Add(new StringField( ToFieldName("param",key), v.ToLower(), Field.Store.NO));
                    }
                }

            if (request.Raw.Request.Body is { } requestBody)
            {
                doc.Add(new StringField("request.body", requestBody.ToLower(), Field.Store.NO));
            }

            doc.Add(new StringField("status", request.Raw.Response.StatusCode?.ToString()?.ToLower() ?? "", Field.Store.NO));

            if (request.Raw.Response.Headers is { } resHeaders)
                foreach (var (key, val) in resHeaders)
                {
                    doc.Add(new StringField("response.header", key.ToLower(), Field.Store.NO));
                    foreach (var v in val)
                    {
                        doc.Add(new StringField(ToFieldName("response.header", key), v.ToLower(), Field.Store.NO));
                    }
                }

            if (request.Raw.Response.Body is { } responseBody)
            {
                doc.Add(new StringField("response.body", responseBody.ToLower(), Field.Store.NO));
            }
            else if (request.Raw.Response.BodyAsJson is { } responseBodyAsJson)
            {
                doc.Add(new StringField("response.body", responseBodyAsJson.ToString()?.ToLower() ?? string.Empty, Field.Store.NO));
            }

            indexWriter.AddDocument(doc);
        }
        indexWriter.Commit();
    }

    private static string ToFieldName(string prefix, string name) => $"{prefix}.{name.Replace("-", "")}";

    public HashSet<string> Search(string query)
    {
        using var reader = indexWriter.GetReader(applyAllDeletes: true);
        var searcher = new IndexSearcher(reader);


        var parser = new QueryParser(luceneVersion, "url", standardAnalyzer)
        {
            AllowLeadingWildcard = true
        };

        var lquery = parser.Parse(query);
        var topDocs = searcher.Search(lquery, n: int.MaxValue);
        var results = new HashSet<string>();
        for (var i = 0; i < topDocs.TotalHits; i++)
        {
            var resultDoc = searcher.Doc(topDocs.ScoreDocs[i].Doc);

            results.Add(resultDoc.Get("_id"));
        }
        return results;
    }
}