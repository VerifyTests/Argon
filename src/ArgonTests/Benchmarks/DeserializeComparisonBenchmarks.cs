// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;
#if !NET6_0_OR_GREATER
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.Script.Serialization;
#endif

public class DeserializeComparisonBenchmarks
{
    [Benchmark]
    public TestClass DataContractSerializer() =>
        DeserializeDataContract<TestClass>(BenchmarkConstants.XmlText);

    static T DeserializeDataContract<T>(string xml)
    {
        var serializer = new DataContractSerializer(typeof(T));
        var ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        return (T) serializer.ReadObject(ms);
    }

    static T DeserializeDataContractJson<T>(string json)
    {
        var dataContractSerializer = new DataContractJsonSerializer(typeof(T));
        var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));

        return (T) dataContractSerializer.ReadObject(ms);
    }

#if !NET6_0_OR_GREATER

    static readonly byte[] BinaryFormatterData = BenchmarkConstants.BinaryFormatterHex.HexToBytes();

    [Benchmark]
    public TestClass BinaryFormatter() =>
        DeserializeBinaryFormatter<TestClass>(BinaryFormatterData);

    static T DeserializeBinaryFormatter<T>(byte[] bytes)
    {
        var formatter = new BinaryFormatter();
        return (T) formatter.Deserialize(new MemoryStream(bytes));
    }

    [Benchmark]
    public TestClass JavaScriptSerializer() =>
        DeserializeWebExtensions<TestClass>(BenchmarkConstants.JsonText);

    public T DeserializeWebExtensions<T>(string json)
    {
        var ser = new JavaScriptSerializer {MaxJsonLength = int.MaxValue};

        return ser.Deserialize<T>(json);
    }
#endif

    [Benchmark]
    public TestClass DataContractJsonSerializer() =>
        DeserializeDataContractJson<TestClass>(BenchmarkConstants.JsonText);

    [Benchmark]
    public TestClass JsonNet() =>
        JsonConvert.DeserializeObject<TestClass>(BenchmarkConstants.JsonText);

    [Benchmark]
    public TestClass JsonNetIso() =>
        JsonConvert.DeserializeObject<TestClass>(BenchmarkConstants.JsonIsoText);

    [Benchmark]
    public TestClass JsonNetManual() =>
        DeserializeJsonNetManual(BenchmarkConstants.JsonText);

    #region DeserializeJsonNetManual

    static TestClass DeserializeJsonNetManual(string json)
    {
        var c = new TestClass();

        var reader = new JsonTextReader(new StringReader(json));
        reader.Read();
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.PropertyName)
            {
                var propertyName = (string) reader.Value;
                switch (propertyName)
                {
                    case "strings":
                        reader.Read();
                        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                        {
                            c.strings.Add((string) reader.Value);
                        }

                        break;
                    case "dictionary":
                        reader.Read();
                        while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                        {
                            var key = (string) reader.Value;
                            c.dictionary.Add(key, reader.ReadAsInt32().GetValueOrDefault());
                        }

                        break;
                    case "Name":
                        c.Name = reader.ReadAsString();
                        break;
                    case "Now":
                        c.Now = reader.ReadAsDateTime().GetValueOrDefault();
                        break;
                    case "BigNumber":
                        c.BigNumber = reader.ReadAsDecimal().GetValueOrDefault();
                        break;
                    case "Address1":
                        reader.Read();
                        c.Address1 = CreateAddress(reader);
                        break;
                    case "Addresses":
                        reader.Read();
                        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                        {
                            var address = CreateAddress(reader);
                            c.Addresses.Add(address);
                        }

                        break;
                }
            }
            else
            {
                break;
            }
        }

        return c;
    }

    static Address CreateAddress(JsonTextReader reader)
    {
        var a = new Address();
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.PropertyName)
            {
                switch ((string) reader.Value)
                {
                    case "Street":
                        a.Street = reader.ReadAsString();
                        break;
                    case "Phone":
                        a.Phone = reader.ReadAsString();
                        break;
                    case "Entered":
                        a.Entered = reader.ReadAsDateTime().GetValueOrDefault();
                        break;
                }
            }
            else
            {
                break;
            }
        }

        return a;
    }

    #endregion
}
