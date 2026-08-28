// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;
using Formatting = Argon.Formatting;

public class SerializeComparisonBenchmarks
{
    static readonly TestClass TestClass = CreateSerializationObject();

    static TestClass CreateSerializationObject()
    {
        var test = new TestClass
        {
            dictionary = new()
            {
                {"Val & asd1", 1},
                {"Val2 & asd1", 3},
                {"Val3 & asd1", 4}
            },
            Address1 =
            {
                Street = "fff Street",
                Entered = DateTime.Now.AddDays(20)
            },
            BigNumber = 34123123123.121M,
            Now = DateTime.Now.AddHours(1),
            strings =
            [
                null,
                "Markus egger ]><[, (2nd)",
                null
            ]
        };

        var address = new Address
        {
            Entered = DateTime.Now.AddDays(-1),
            Street = "\u001farray\u003caddress"
        };

        test.Addresses.Add(address);

        address = new()
        {
            Entered = DateTime.Now.AddDays(-2),
            Street = "array 2 address"
        };
        test.Addresses.Add(address);
        return test;
    }

    [Benchmark]
    public string DataContractSerializer() =>
        SerializeDataContract(TestClass);

    static string SerializeDataContract(object value)
    {
        var dataContractSerializer = new DataContractSerializer(value.GetType());

        var ms = new MemoryStream();
        dataContractSerializer.WriteObject(ms, value);

        ms.Seek(0, SeekOrigin.Begin);

        using var sr = new StreamReader(ms);
        return sr.ReadToEnd();
    }

    [Benchmark]
    public string DataContractJsonSerializer() =>
        SerializeDataContractJson(TestClass);

    public string SerializeDataContractJson(object value)
    {
        var dataContractSerializer = new DataContractJsonSerializer(value.GetType());

        var ms = new MemoryStream();
        dataContractSerializer.WriteObject(ms, value);

        ms.Seek(0, SeekOrigin.Begin);

        using var sr = new StreamReader(ms);
        return sr.ReadToEnd();
    }

    [Benchmark]
    public string JsonNet() =>
        JsonConvert.SerializeObject(TestClass);

    [Benchmark]
    public string JsonNetLinq() =>
        SerializeJsonNetLinq(TestClass);

    #region SerializeJsonNetManual

    static string SerializeJsonNetLinq(TestClass c)
    {
        var o = new JObject(
            new JProperty("strings", new JArray(c.strings)),
            new JProperty("dictionary", new JObject(c.dictionary.Select(d => new JProperty(d.Key, d.Value)))),
            new JProperty("Name", c.Name),
            new JProperty("Now", c.Now),
            new JProperty("BigNumber", c.BigNumber),
            new JProperty("Address1", new JObject(
                new JProperty("Street", c.Address1.Street),
                new JProperty("Phone", c.Address1.Phone),
                new JProperty("Entered", c.Address1.Entered))),
            new JProperty("Addresses", new JArray(c.Addresses.Select(a =>
                new JObject(
                    new JProperty("Street", a.Street),
                    new JProperty("Phone", a.Phone),
                    new JProperty("Entered", a.Entered)))))
        );

        return o.ToString(Formatting.None);
    }

    #endregion

    [Benchmark]
    public string JsonNetManual() =>
        SerializeJsonNetManual(TestClass);

    #region SerializeJsonNetManual

    static string SerializeJsonNetManual(TestClass c)
    {
        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter);
        jsonWriter.WriteStartObject();
        jsonWriter.WritePropertyName("strings");
        jsonWriter.WriteStartArray();
        foreach (var s in c.strings)
        {
            jsonWriter.WriteValue(s);
        }

        jsonWriter.WriteEndArray();
        jsonWriter.WritePropertyName("dictionary");
        jsonWriter.WriteStartObject();
        foreach (var keyValuePair in c.dictionary)
        {
            jsonWriter.WritePropertyName(keyValuePair.Key);
            jsonWriter.WriteValue(keyValuePair.Value);
        }

        jsonWriter.WriteEndObject();
        jsonWriter.WritePropertyName("Name");
        jsonWriter.WriteValue(c.Name);
        jsonWriter.WritePropertyName("Now");
        jsonWriter.WriteValue(c.Now);
        jsonWriter.WritePropertyName("BigNumber");
        jsonWriter.WriteValue(c.BigNumber);
        jsonWriter.WritePropertyName("Address1");
        jsonWriter.WriteStartObject();
        jsonWriter.WritePropertyName("Street");
        jsonWriter.WriteValue(c.BigNumber);
        jsonWriter.WritePropertyName("Street");
        jsonWriter.WriteValue(c.BigNumber);
        jsonWriter.WritePropertyName("Street");
        jsonWriter.WriteValue(c.BigNumber);
        jsonWriter.WriteEndObject();
        jsonWriter.WritePropertyName("Addresses");
        jsonWriter.WriteStartArray();
        foreach (var address in c.Addresses)
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("Street");
            jsonWriter.WriteValue(address.Street);
            jsonWriter.WritePropertyName("Phone");
            jsonWriter.WriteValue(address.Phone);
            jsonWriter.WritePropertyName("Entered");
            jsonWriter.WriteValue(address.Entered);
            jsonWriter.WriteEndObject();
        }

        jsonWriter.WriteEndArray();
        jsonWriter.WriteEndObject();

        jsonWriter.Flush();
        return stringWriter.ToString();
    }

    #endregion
}