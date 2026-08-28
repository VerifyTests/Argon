// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using System.Data.SqlTypes;
using TestObjects;
using Formatting = Argon.Formatting;

public class DataTableConverterTests : TestFixtureBase
{
    [Fact]
    public void DeserializeEmptyNestedArray()
    {
        var jsonString2 = """[{"col1": []}]""";

        var settings = new JsonSerializerSettings();

        settings.AddDataSetConverters();
        var dt = JsonConvert.DeserializeObject<DataTable>(jsonString2, settings);

        Assert.Single(dt.Columns);
        Assert.Equal(typeof(string[]), dt.Columns["col1"].DataType);

        Assert.Equal(1, dt.Rows.Count);
        Assert.NotNull(dt.Rows[0]["col1"]);

        var value = (object[]) dt.Rows[0]["col1"];
        Assert.Empty(value);
    }

    [Fact]
    public void SerializeNullValues()
    {
        var dt = new DataTable();
        var types = new List<Type>
        {
            typeof(TimeSpan),
            typeof(char[]),
            typeof(Type),
            typeof(object),
            typeof(byte[]),
            typeof(Uri),
            typeof(Guid),
            typeof(BigInteger)
        };

        foreach (var ss in types)
        {
            dt.Columns.Add(ss.Name, ss);
        }

        dt.Rows.Add(types.Select(object (_) => null).ToArray());

        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter)
        {
            Formatting = Formatting.Indented
        };

        var converter = new DataTableConverter();
        converter.WriteJson(jsonWriter, dt, new());

        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "TimeSpan": null,
                "Char[]": null,
                "Type": null,
                "Object": null,
                "Byte[]": null,
                "Uri": null,
                "Guid": null,
                "BigInteger": null
              }
            ]
            """,
            stringWriter.ToString());
    }

    [Fact]
    public void SerializeValues()
    {
        var dt = new DataTable();
        var types = new Dictionary<Type, object>
        {
            [typeof(TimeSpan)] = TimeSpan.Zero,
            [typeof(char[])] = new[] {'a', 'b', 'c'},
            [typeof(Type)] = typeof(string),
            [typeof(object)] = new(),
            [typeof(byte[])] = new byte[] {1, 2, 3, 4, 5, 6, 7, 8},
            [typeof(Uri)] = new Uri("http://localhost"),
            [typeof(Guid)] = new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11),
            [typeof(BigInteger)] = BigInteger.Parse("10000000000000000000000000000000000")
        };

        foreach (var ss in types)
        {
            dt.Columns.Add(ss.Key.Name, ss.Key);
        }

        dt.Rows.Add(types.Select(t => t.Value).ToArray());

        var stringWriter = new StringWriter();
        var jsonWriter = new JsonTextWriter(stringWriter)
        {
            Formatting = Formatting.Indented
        };

        var converter = new DataTableConverter();
        converter.WriteJson(jsonWriter, dt, new());

        var stringName = typeof(string).AssemblyQualifiedName;

        XUnitAssert.AreEqualNormalized(
            $$"""
            [
              {
                "TimeSpan": "00:00:00",
                "Char[]": [
                  "a",
                  "b",
                  "c"
                ],
                "Type": "{{stringName}}",
                "Object": {},
                "Byte[]": "AQIDBAUGBwg=",
                "Uri": "http://localhost",
                "Guid": "00000001-0002-0003-0405-060708090a0b",
                "BigInteger": 10000000000000000000000000000000000
              }
            ]
            """,
            stringWriter.ToString());
    }

    [Fact]
    public void Deserialize()
    {
        var json = """
            [
              {
                "id": 0,
                "item": "item 0",
                "DataTableCol": [
                  {
                    "NestedStringCol": "0!"
                  }
                ],
                "ArrayCol": [
                  0
                ],
                "DateCol": "2000-12-29T00:00:00Z"
              },
              {
                "id": 1,
                "item": "item 1",
                "DataTableCol": [
                  {
                    "NestedStringCol": "1!"
                  }
                ],
                "ArrayCol": [
                  1
                ],
                "DateCol": "2000-12-29T00:00:00Z"
              }
            ]
            """;

        var settings = new JsonSerializerSettings();
        settings.AddDataSetConverters();
        var deserializedDataTable = JsonConvert.DeserializeObject<DataTable>(json, settings);
        Assert.NotNull(deserializedDataTable);

        Assert.Equal(string.Empty, deserializedDataTable.TableName);
        Assert.Equal(5, deserializedDataTable.Columns.Count);
        Assert.Equal("id", deserializedDataTable.Columns[0].ColumnName);
        Assert.Equal(typeof(long), deserializedDataTable.Columns[0].DataType);
        Assert.Equal("item", deserializedDataTable.Columns[1].ColumnName);
        Assert.Equal(typeof(string), deserializedDataTable.Columns[1].DataType);
        Assert.Equal("DataTableCol", deserializedDataTable.Columns[2].ColumnName);
        Assert.Equal(typeof(DataTable), deserializedDataTable.Columns[2].DataType);
        Assert.Equal("ArrayCol", deserializedDataTable.Columns[3].ColumnName);
        Assert.Equal(typeof(long[]), deserializedDataTable.Columns[3].DataType);
        Assert.Equal("DateCol", deserializedDataTable.Columns[4].ColumnName);
        Assert.Equal(typeof(string), deserializedDataTable.Columns[4].DataType);

        Assert.Equal(2, deserializedDataTable.Rows.Count);

        var dr1 = deserializedDataTable.Rows[0];
        Assert.Equal(0L, dr1["id"]);
        Assert.Equal("item 0", dr1["item"]);
        Assert.Equal("0!", ((DataTable) dr1["DataTableCol"]).Rows[0]["NestedStringCol"]);
        Assert.Equal(0L, ((long[]) dr1["ArrayCol"])[0]);
        Assert.Equal("2000-12-29T00:00:00Z", dr1["DateCol"]);

        var dr2 = deserializedDataTable.Rows[1];
        Assert.Equal(1L, dr2["id"]);
        Assert.Equal("item 1", dr2["item"]);
        Assert.Equal("1!", ((DataTable) dr2["DataTableCol"]).Rows[0]["NestedStringCol"]);
        Assert.Equal(1L, ((long[]) dr2["ArrayCol"])[0]);
        Assert.Equal("2000-12-29T00:00:00Z", dr2["DateCol"]);
    }

    [Fact]
    public void DeserializeParseHandling()
    {
        var json = """
            [
              {
                "DateCol": "2000-12-29T00:00:00Z",
                "FloatCol": 99.9999999999999999999
              },
              {
                "DateCol": "2000-12-29T00:00:00Z",
                "FloatCol": 99.9999999999999999999
              }
            ]
            """;

        var settings = new JsonSerializerSettings
        {
            FloatParseHandling = FloatParseHandling.Decimal
        };

        settings.AddDataSetConverters();
        var deserializedDataTable = JsonConvert.DeserializeObject<DataTable>(json, settings);
        Assert.NotNull(deserializedDataTable);

        Assert.Equal(string.Empty, deserializedDataTable.TableName);
        Assert.Equal(2, deserializedDataTable.Columns.Count);
        Assert.Equal("DateCol", deserializedDataTable.Columns[0].ColumnName);
        Assert.Equal(typeof(string), deserializedDataTable.Columns[0].DataType);
        Assert.Equal("FloatCol", deserializedDataTable.Columns[1].ColumnName);
        Assert.Equal(typeof(decimal), deserializedDataTable.Columns[1].DataType);

        Assert.Equal(2, deserializedDataTable.Rows.Count);

        var dr1 = deserializedDataTable.Rows[0];
        Assert.Equal("2000-12-29T00:00:00Z", dr1["DateCol"]);
        Assert.Equal(99.9999999999999999999m, dr1["FloatCol"]);

        var dr2 = deserializedDataTable.Rows[1];
        Assert.Equal("2000-12-29T00:00:00Z", dr2["DateCol"]);
        Assert.Equal(99.9999999999999999999m, dr2["FloatCol"]);
    }

    [Fact]
    public void Serialize()
    {
        // create a new DataTable.
        var myTable = new DataTable("blah");

        // create DataColumn objects of data types.
        var colString = new DataColumn("StringCol")
        {
            DataType = typeof(string)
        };
        myTable.Columns.Add(colString);

        var colInt32 = new DataColumn("Int32Col")
        {
            DataType = typeof(int)
        };
        myTable.Columns.Add(colInt32);

        var colBoolean = new DataColumn("BooleanCol")
        {
            DataType = typeof(bool)
        };
        myTable.Columns.Add(colBoolean);

        var colTimeSpan = new DataColumn("TimeSpanCol")
        {
            DataType = typeof(TimeSpan)
        };
        myTable.Columns.Add(colTimeSpan);

        var colDateTime = new DataColumn("DateTimeCol")
        {
            DataType = typeof(DateTime),
            DateTimeMode = DataSetDateTime.Utc
        };
        myTable.Columns.Add(colDateTime);

        var colDecimal = new DataColumn("DecimalCol")
        {
            DataType = typeof(decimal)
        };
        myTable.Columns.Add(colDecimal);

        var colDataTable = new DataColumn("DataTableCol")
        {
            DataType = typeof(DataTable)
        };
        myTable.Columns.Add(colDataTable);

        var colArray = new DataColumn("ArrayCol")
        {
            DataType = typeof(int[])
        };
        myTable.Columns.Add(colArray);

        var colBytes = new DataColumn("BytesCol")
        {
            DataType = typeof(byte[])
        };
        myTable.Columns.Add(colBytes);

        // populate one row with values.
        var myNewRow = myTable.NewRow();

        myNewRow["StringCol"] = "Item Name";
        myNewRow["Int32Col"] = 2147483647;
        myNewRow["BooleanCol"] = true;
        myNewRow["TimeSpanCol"] = new TimeSpan(10, 22, 10, 15, 100);
        myNewRow["DateTimeCol"] = new DateTime(2000, 12, 29, 0, 0, 0, DateTimeKind.Utc);
        myNewRow["DecimalCol"] = 64.0021m;
        myNewRow["ArrayCol"] = new[] {1};
        myNewRow["BytesCol"] = "Hello world"u8.ToArray();

        var nestedTable = new DataTable("Nested");
        var nestedColString = new DataColumn("NestedStringCol")
        {
            DataType = typeof(string)
        };
        nestedTable.Columns.Add(nestedColString);
        var myNewNestedRow = nestedTable.NewRow();
        myNewNestedRow["NestedStringCol"] = "Nested!";
        nestedTable.Rows.Add(myNewNestedRow);

        myNewRow["DataTableCol"] = nestedTable;
        myTable.Rows.Add(myNewRow);

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        settings.AddDataSetConverters();
        var json = JsonConvert.SerializeObject(myTable, settings);
        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "StringCol": "Item Name",
                "Int32Col": 2147483647,
                "BooleanCol": true,
                "TimeSpanCol": "10.22:10:15.1000000",
                "DateTimeCol": "2000-12-29T00:00:00Z",
                "DecimalCol": 64.0021,
                "DataTableCol": [
                  {
                    "NestedStringCol": "Nested!"
                  }
                ],
                "ArrayCol": [
                  1
                ],
                "BytesCol": "SGVsbG8gd29ybGQ="
              }
            ]
            """,
            json);
    }

    public class TestDataTableConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var d = (DataTable) value;
            writer.WriteValue(d.TableName);
        }

        public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer)
        {
            //reader.Read();
            var d = new DataTable((string) reader.Value);

            return d;
        }

        public override bool CanConvert(Type type) =>
            type == typeof(DataTable);
    }

    [Fact]
    public void PassedInJsonConverterOverridesInternalConverter()
    {
        var t1 = new DataTable("Custom");

        var json = JsonConvert.SerializeObject(t1, Formatting.Indented, new TestDataTableConverter());
        Assert.Equal(
            """
            "Custom"
            """,
            json);

        var t2 = JsonConvert.DeserializeObject<DataTable>(json, new TestDataTableConverter());
        Assert.Equal(t1.TableName, t2.TableName);
    }

    [Fact]
    public void SerializeDataTableWithNull()
    {
        var table = new DataTable();
        table.Columns.Add("item");
        table.Columns.Add("price", typeof(double));
        table.Rows.Add("shirt", 49.99);
        table.Rows.Add("pants", 54.99);
        table.Rows.Add("shoes"); // no price

        var settings = new JsonSerializerSettings();

        settings.AddDataSetConverters();
        var json = JsonConvert.SerializeObject(table, settings);
        Assert.Equal("["
                     + """{"item":"shirt","price":49.99},"""
                     + """{"item":"pants","price":54.99},"""
                     + """{"item":"shoes","price":null}]""", json);
    }

    [Fact]
    public void SerializeDataTableWithNullAndIgnoreNullHandling()
    {
        var table = new DataTable();
        table.Columns.Add("item");
        table.Columns.Add("price", typeof(double));
        table.Rows.Add("shirt", 49.99);
        table.Rows.Add("pants", 54.99);
        table.Rows.Add("shoes"); // no price

        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        settings.AddDataSetConverters();
        var json = JsonConvert.SerializeObject(table, Formatting.None, settings);
        Assert.Equal("["
                     + """{"item":"shirt","price":49.99},"""
                     + """{"item":"pants","price":54.99},"""
                     + """{"item":"shoes"}]""",
            json);
    }

    [Fact]
    public void DeserializeDataTableWithImplicitNull()
    {
        var settings = new JsonSerializerSettings();

        settings.AddDataSetConverters();
        const string json = "["
                            + """{"item":"shirt","price":49.99},"""
                            + """{"item":"pants","price":54.99},"""
                            + """{"item":"shoes"}]""";
        var table = JsonConvert.DeserializeObject<DataTable>(json, settings);
        Assert.Equal("shirt", table.Rows[0]["item"]);
        Assert.Equal("pants", table.Rows[1]["item"]);
        Assert.Equal("shoes", table.Rows[2]["item"]);
        Assert.Equal(49.99, (double) table.Rows[0]["price"], 0.01);
        Assert.Equal(54.99, (double) table.Rows[1]["price"], 0.01);
        Assert.IsType<DBNull>(table.Rows[2]["price"]);
    }

    [Fact]
    public void DeserializeDataTableWithExplicitNull()
    {
        const string json = "["
                            + """{"item":"shirt","price":49.99},"""
                            + """{"item":"pants","price":54.99},"""
                            + """{"item":"shoes","price":null}]""";
        var settings = new JsonSerializerSettings();

        settings.AddDataSetConverters();
        var table = JsonConvert.DeserializeObject<DataTable>(json, settings);
        Assert.Equal("shirt", table.Rows[0]["item"]);
        Assert.Equal("pants", table.Rows[1]["item"]);
        Assert.Equal("shoes", table.Rows[2]["item"]);
        Assert.Equal(49.99, (double) table.Rows[0]["price"], 0.01);
        Assert.Equal(54.99, (double) table.Rows[1]["price"], 0.01);
        Assert.IsType<DBNull>(table.Rows[2]["price"]);
    }

    [Fact]
    public void SerializeKeyValuePairWithDataTableKey()
    {
        var table = new DataTable();
        var idColumn = new DataColumn("id", typeof(int))
        {
            AutoIncrement = true
        };

        var itemColumn = new DataColumn("item");
        table.Columns.Add(idColumn);
        table.Columns.Add(itemColumn);

        var r = table.NewRow();
        r["item"] = "item!";
        r.EndEdit();
        table.Rows.Add(r);

        var pair = new KeyValuePair<DataTable, int>(table, 1);
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        settings.AddDataSetConverters();

        var serialized = JsonConvert.SerializeObject(pair, settings);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Key": [
                {
                  "id": 0,
                  "item": "item!"
                }
              ],
              "Value": 1
            }
            """,
            serialized);

        var pair2 = (KeyValuePair<DataTable, int>) JsonConvert.DeserializeObject(serialized, typeof(KeyValuePair<DataTable, int>), settings);

        Assert.Equal(1, pair2.Value);
        Assert.Equal(1, pair2.Key.Rows.Count);
        Assert.Equal("item!", pair2.Key.Rows[0]["item"]);
    }

    [Fact]
    public void SerializedTypedDataTable()
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        settings.AddDataSetConverters();
        var dt = new CustomerDataSet.CustomersDataTable();
        dt.AddCustomersRow("432");

        var json = JsonConvert.SerializeObject(dt, settings);

        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "CustomerID": "432"
              }
            ]
            """,
            json);
    }

    [Fact]
    public void DeserializedTypedDataTable()
    {
        var json = """
            [
              {
                "CustomerID": "432"
              }
            ]
            """;

        var settings = new JsonSerializerSettings();

        settings.AddDataSetConverters();
        var dt = JsonConvert.DeserializeObject<CustomerDataSet.CustomersDataTable>(json, settings);

        Assert.Equal("432", dt[0].CustomerID);
    }

    public class DataTableTestClass
    {
        public DataTable Table { get; set; }
    }

    [Fact]
    public void SerializeNull()
    {
        var c1 = new DataTableTestClass
        {
            Table = null
        };

        var json = JsonConvert.SerializeObject(c1, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Table": null
            }
            """,
            json);

        var c2 = JsonConvert.DeserializeObject<DataTableTestClass>(json);

        Assert.Null(c2.Table);
    }

    [Fact]
    public void SerializeNullRoot()
    {
        var json = JsonConvert.SerializeObject(
            null,
            typeof(DataTable),
            new()
            {
                Formatting = Formatting.Indented
            });

        XUnitAssert.AreEqualNormalized("null", json);
    }

    [Fact]
    public void DeserializedTypedDataTableWithConverter()
    {
        var json = """
            {
              "TestTable": [
                {
                  "DateTimeValue": "2015-11-28T00:00:00"
                },
                {
                  "DateTimeValue": null
                }
              ]
            }
            """;

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        settings.AddDataSetConverters();
        settings.Converters.Add(new SqlDateTimeConverter());
        var ds = JsonConvert.DeserializeObject<SqlTypesDataSet>(json, settings);

        Assert.Equal(new(2015, 11, 28), ds.TestTable[0].DateTimeValue);
        Assert.Equal(SqlDateTime.Null, ds.TestTable[1].DateTimeValue);

        var json2 = JsonConvert.SerializeObject(ds, settings);

        XUnitAssert.AreEqualNormalized(json, json2);
    }

    internal class SqlDateTimeConverter : JsonConverter
    {
        public override bool CanConvert(Type type) =>
            typeof(SqlDateTime).IsAssignableFrom(type);

        public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null ||
                reader.Value == DBNull.Value)
            {
                return SqlDateTime.Null;
            }

            var stringValue = (string) serializer.Deserialize(reader);
            return new SqlDateTime(DateTime.Parse(stringValue));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (((SqlDateTime) value).IsNull)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(((SqlDateTime) value).Value);
            }
        }
    }

    [Fact]
    public void HandleColumnOnError()
    {
        var json = "[{\"timeCol\":\"\"}]";
        DataTable table = JsonConvert.DeserializeObject<CustomDataTable>(json);

        Assert.Equal(DBNull.Value, table.Rows[0]["timeCol"]);
    }

    [JsonConverter(typeof(DataTableConverterTest))]
    public class CustomDataTable : DataTable;

    public class DataTableConverterTest :
        DataTableConverter,
        IJsonOnDeserializeError
    {
        protected DataTable CreateTable()
        {
            var table = new CustomDataTable();
            table.Columns.Add("timeCol", typeof(DateTime));
            return table;
        }

        public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer)
        {
            existingValue ??= CreateTable();

            var previousError = serializer.DeserializeError;
            serializer.DeserializeError = (_, _, _, _, _, markAsHandled) => markAsHandled();
            try
            {
                return base.ReadJson(reader, type, existingValue, serializer);
            }
            finally
            {
                serializer.DeserializeError = previousError;
            }
        }

        public void OnDeserializeError(object originalObject, string path, object member, Exception exception, Action markAsHandled) =>
            markAsHandled();
    }
}
