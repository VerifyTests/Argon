// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using System.Xml;
using System.Xml.Linq;
using BenchmarkDotNet.Attributes;

public class XmlNodeConverterBenchmarks
{
    [Benchmark]
    public void ConvertXmlNode()
    {
        var doc = new XmlDocument();
        using (var file = ProjectFiles.large_sample_xml.OpenRead())
        {
            doc.Load(file);
        }

        JsonXmlConvert.SerializeXmlNode(doc);
    }

    [Benchmark]
    public void ConvertXNode()
    {
        XDocument doc;
        using (var file = File.OpenRead("large_sample.xml"))
        {
            doc = XDocument.Load(file);
        }

        JsonXmlConvert.SerializeXNode(doc);
    }
}