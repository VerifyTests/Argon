// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using System.Data;
using Argon.DataSets;
using TestObjects;

public class DataSetConverterTests : TestFixtureBase
{
    [Fact]
    public void DeserializeInvalidDataTable()
    {
        var settings = new JsonSerializerSettings();

        settings.AddDataSetConverters();
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<DataSet>("{\"pending_count\":23,\"completed_count\":45}", settings));
        Assert.Equal("Unexpected JSON token when reading DataTable. Expected StartArray, got Integer. Path 'pending_count', line 1, position 19.", exception.Message);

        Assert.Equal(1, exception.LineNumber);
        Assert.Equal(19, exception.LinePosition);
        Assert.Equal("pending_count", exception.Path);
    }

    [Fact]
    public void SerializeAndDeserialize()
    {
        var dataSet = new DataSet("dataSet")
        {
            Namespace = "NetFrameWork"
        };
        var table = new DataTable();
        var idColumn = new DataColumn("id", typeof(int))
        {
            AutoIncrement = true
        };

        var itemColumn = new DataColumn("item");
        table.Columns.Add(idColumn);
        table.Columns.Add(itemColumn);
        dataSet.Tables.Add(table);

        for (var i = 0; i < 2; i++)
        {
            var newRow = table.NewRow();
            newRow["item"] = $"item {i}";
            table.Rows.Add(newRow);
        }

        dataSet.AcceptChanges();

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        settings.AddDataSetConverters();
        var json = JsonConvert.SerializeObject(dataSet, settings);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Table1": [
                {
                  "id": 0,
                  "item": "item 0"
                },
                {
                  "id": 1,
                  "item": "item 1"
                }
              ]
            }
            """,
            json);

        var deserializedDataSet = JsonConvert.DeserializeObject<DataSet>(json, settings);
        Assert.NotNull(deserializedDataSet);

        Assert.Single(deserializedDataSet.Tables);

        var dt = deserializedDataSet.Tables[0];

        Assert.Equal("Table1", dt.TableName);
        Assert.Equal(2, dt.Columns.Count);
        Assert.Equal("id", dt.Columns[0].ColumnName);
        Assert.Equal(typeof(long), dt.Columns[0].DataType);
        Assert.Equal("item", dt.Columns[1].ColumnName);
        Assert.Equal(typeof(string), dt.Columns[1].DataType);

        Assert.Equal(2, dt.Rows.Count);
    }

    public class DataSetTestClass
    {
        public DataSet Set { get; set; }
    }

    [Fact]
    public void SerializeNull()
    {
        var c1 = new DataSetTestClass
        {
            Set = null
        };

        var json = JsonConvert.SerializeObject(c1, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Set": null
            }
            """,
            json);

        var c2 = JsonConvert.DeserializeObject<DataSetTestClass>(json);

        Assert.Null(c2.Set);
    }

    [Fact]
    public void SerializeNullRoot()
    {
        var json = JsonConvert.SerializeObject(null, typeof(DataSet), new()
        {
            Formatting = Formatting.Indented
        });

        XUnitAssert.AreEqualNormalized("null", json);
    }

    [Fact]
    public void DeserializeNullTable()
    {
        var json = """
            {
              "TableName": null
            }
            """;

        var settings = new JsonSerializerSettings();

        settings.AddDataSetConverters();
        var ds = JsonConvert.DeserializeObject<DataSet>(json, settings);

        Assert.True(ds.Tables.Contains("TableName"));
    }

    [Fact]
    public void SerializeMultiTableDataSet()
    {
        var ds = new DataSet();
        ds.Tables.Add(CreateDataTable("FirstTable", 2));
        ds.Tables.Add(CreateDataTable("SecondTable", 1));

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        settings.AddDataSetConverters();
        settings.Converters.Add(new IsoDateTimeConverter());

        var json = JsonConvert.SerializeObject(ds, settings);
        // {
        //   "FirstTable": [
        //     {
        //       "StringCol": "Item Name",
        //       "Int32Col": 1,
        //       "BooleanCol": true,
        //       "TimeSpanCol": "10.22:10:15.1000000",
        //       "DateTimeCol": "2000-12-29T00:00:00Z",
        //       "DecimalCol": 64.0021
        //     },
        //     {
        //       "StringCol": "Item Name",
        //       "Int32Col": 2,
        //       "BooleanCol": true,
        //       "TimeSpanCol": "10.22:10:15.1000000",
        //       "DateTimeCol": "2000-12-29T00:00:00Z",
        //       "DecimalCol": 64.0021
        //     }
        //   ],
        //   "SecondTable": [
        //     {
        //       "StringCol": "Item Name",
        //       "Int32Col": 1,
        //       "BooleanCol": true,
        //       "TimeSpanCol": "10.22:10:15.1000000",
        //       "DateTimeCol": "2000-12-29T00:00:00Z",
        //       "DecimalCol": 64.0021
        //     }
        //   ]
        // }

        var deserializedDs = JsonConvert.DeserializeObject<DataSet>(json, settings);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "FirstTable": [
                {
                  "StringCol": "Item Name",
                  "Int32Col": 1,
                  "BooleanCol": true,
                  "TimeSpanCol": "10.22:10:15.1000000",
                  "DateTimeCol": "2000-12-29T00:00:00Z",
                  "DecimalCol": 64.0021
                },
                {
                  "StringCol": "Item Name",
                  "Int32Col": 2,
                  "BooleanCol": true,
                  "TimeSpanCol": "10.22:10:15.1000000",
                  "DateTimeCol": "2000-12-29T00:00:00Z",
                  "DecimalCol": 64.0021
                }
              ],
              "SecondTable": [
                {
                  "StringCol": "Item Name",
                  "Int32Col": 1,
                  "BooleanCol": true,
                  "TimeSpanCol": "10.22:10:15.1000000",
                  "DateTimeCol": "2000-12-29T00:00:00Z",
                  "DecimalCol": 64.0021
                }
              ]
            }
            """,
            json);

        Assert.NotNull(deserializedDs);
    }

    [Fact]
    public void DeserializeMultiTableDataSet()
    {
        var json = """
            {
              "FirstTable": [
                {
                  "StringCol": "Item Name",
                  "Int32Col": 2147483647,
                  "BooleanCol": true,
                  "TimeSpanCol": "10.22:10:15.1000000",
                  "DateTimeCol": "2000-12-29T00:00:00Z",
                  "DecimalCol": 64.0021
                }
              ],
              "SecondTable": [
                {
                  "StringCol": "Item Name",
                  "Int32Col": 2147483647,
                  "BooleanCol": true,
                  "TimeSpanCol": "10.22:10:15.1000000",
                  "DateTimeCol": "2000-12-29T00:00:00Z",
                  "DecimalCol": 64.0021
                }
              ]
            }
            """;

        var settings = new JsonSerializerSettings();

        settings.AddDataSetConverters();
        var ds = JsonConvert.DeserializeObject<DataSet>(json, settings);
        Assert.NotNull(ds);

        Assert.Equal(2, ds.Tables.Count);
        Assert.Equal("FirstTable", ds.Tables[0].TableName);
        Assert.Equal("SecondTable", ds.Tables[1].TableName);

        var dt = ds.Tables[0];
        Assert.Equal("StringCol", dt.Columns[0].ColumnName);
        Assert.Equal(typeof(string), dt.Columns[0].DataType);
        Assert.Equal("Int32Col", dt.Columns[1].ColumnName);
        Assert.Equal(typeof(long), dt.Columns[1].DataType);
        Assert.Equal("BooleanCol", dt.Columns[2].ColumnName);
        Assert.Equal(typeof(bool), dt.Columns[2].DataType);
        Assert.Equal("TimeSpanCol", dt.Columns[3].ColumnName);
        Assert.Equal(typeof(string), dt.Columns[3].DataType);
        Assert.Equal("DateTimeCol", dt.Columns[4].ColumnName);
        Assert.Equal(typeof(string), dt.Columns[4].DataType);
        Assert.Equal("DecimalCol", dt.Columns[5].ColumnName);
        Assert.Equal(typeof(double), dt.Columns[5].DataType);

        Assert.Equal(1, ds.Tables[0].Rows.Count);
        Assert.Equal(1, ds.Tables[1].Rows.Count);
    }

    static DataTable CreateDataTable(string dataTableName, int rows)
    {
        // create a new DataTable.
        var myTable = new DataTable(dataTableName);

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

        for (var i = 1; i <= rows; i++)
        {
            var myNewRow = myTable.NewRow();

            myNewRow["StringCol"] = "Item Name";
            myNewRow["Int32Col"] = i;
            myNewRow["BooleanCol"] = true;
            myNewRow["TimeSpanCol"] = new TimeSpan(10, 22, 10, 15, 100);
            myNewRow["DateTimeCol"] = new DateTime(2000, 12, 29, 0, 0, 0, DateTimeKind.Utc);
            // a decimal literal, not a double: the column is typed decimal, and converting a
            // double to decimal is correctly rounded from .NET 11 on rather than truncated
            myNewRow["DecimalCol"] = 64.0021m;
            myTable.Rows.Add(myNewRow);
        }

        return myTable;
    }

    public class DataSetAndTableTestClass
    {
        public string Before { get; set; }
        public DataSet Set { get; set; }
        public string Middle { get; set; }
        public DataTable Table { get; set; }
        public string After { get; set; }
    }

    [Fact]
    public void SerializeWithCamelCaseResolver()
    {
        var ds = new DataSet();
        ds.Tables.Add(CreateDataTable("FirstTable", 2));
        ds.Tables.Add(CreateDataTable("SecondTable", 1));

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        settings.AddDataSetConverters();
        settings.Converters.Add(new IsoDateTimeConverter());
        var json = JsonConvert.SerializeObject(ds, settings);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "firstTable": [
                {
                  "stringCol": "Item Name",
                  "int32Col": 1,
                  "booleanCol": true,
                  "timeSpanCol": "10.22:10:15.1000000",
                  "dateTimeCol": "2000-12-29T00:00:00Z",
                  "decimalCol": 64.0021
                },
                {
                  "stringCol": "Item Name",
                  "int32Col": 2,
                  "booleanCol": true,
                  "timeSpanCol": "10.22:10:15.1000000",
                  "dateTimeCol": "2000-12-29T00:00:00Z",
                  "decimalCol": 64.0021
                }
              ],
              "secondTable": [
                {
                  "stringCol": "Item Name",
                  "int32Col": 1,
                  "booleanCol": true,
                  "timeSpanCol": "10.22:10:15.1000000",
                  "dateTimeCol": "2000-12-29T00:00:00Z",
                  "decimalCol": 64.0021
                }
              ]
            }
            """,
            json);
    }

    [Fact]
    public void SerializeDataSetProperty()
    {
        var ds = new DataSet();
        ds.Tables.Add(CreateDataTable("FirstTable", 2));
        ds.Tables.Add(CreateDataTable("SecondTable", 1));

        var c = new DataSetAndTableTestClass
        {
            Before = "Before",
            Set = ds,
            Middle = "Middle",
            Table = CreateDataTable("LoneTable", 2),
            After = "After"
        };
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        settings.AddDataSetConverters();
        settings.Converters.Add(new IsoDateTimeConverter());

        var json = JsonConvert.SerializeObject(c, settings);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Before": "Before",
              "Set": {
                "FirstTable": [
                  {
                    "StringCol": "Item Name",
                    "Int32Col": 1,
                    "BooleanCol": true,
                    "TimeSpanCol": "10.22:10:15.1000000",
                    "DateTimeCol": "2000-12-29T00:00:00Z",
                    "DecimalCol": 64.0021
                  },
                  {
                    "StringCol": "Item Name",
                    "Int32Col": 2,
                    "BooleanCol": true,
                    "TimeSpanCol": "10.22:10:15.1000000",
                    "DateTimeCol": "2000-12-29T00:00:00Z",
                    "DecimalCol": 64.0021
                  }
                ],
                "SecondTable": [
                  {
                    "StringCol": "Item Name",
                    "Int32Col": 1,
                    "BooleanCol": true,
                    "TimeSpanCol": "10.22:10:15.1000000",
                    "DateTimeCol": "2000-12-29T00:00:00Z",
                    "DecimalCol": 64.0021
                  }
                ]
              },
              "Middle": "Middle",
              "Table": [
                {
                  "StringCol": "Item Name",
                  "Int32Col": 1,
                  "BooleanCol": true,
                  "TimeSpanCol": "10.22:10:15.1000000",
                  "DateTimeCol": "2000-12-29T00:00:00Z",
                  "DecimalCol": 64.0021
                },
                {
                  "StringCol": "Item Name",
                  "Int32Col": 2,
                  "BooleanCol": true,
                  "TimeSpanCol": "10.22:10:15.1000000",
                  "DateTimeCol": "2000-12-29T00:00:00Z",
                  "DecimalCol": 64.0021
                }
              ],
              "After": "After"
            }
            """,
            json);

        var c2 = JsonConvert.DeserializeObject<DataSetAndTableTestClass>(json, settings);

        Assert.Equal(c.Before, c2.Before);
        Assert.Equal(c.Set.Tables.Count, c2.Set.Tables.Count);
        Assert.Equal(c.Middle, c2.Middle);
        Assert.Equal(c.Table.Rows.Count, c2.Table.Rows.Count);
        Assert.Equal(c.After, c2.After);
    }

    [Fact]
    public void SerializedTypedDataSet()
    {
        var ds = new CustomerDataSet();
        ds.Customers.AddCustomersRow("234");

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        settings.AddDataSetConverters();
        var json1 = JsonConvert.SerializeObject(ds, settings);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Customers": [
                {
                  "CustomerID": "234"
                }
              ]
            }
            """,
            json1);

        var ds1 = new CustomerDataSet();
        var table = ds1.Tables["Customers"];
        var row = ds1.Tables["Customers"].NewRow();
        row["CustomerID"] = "234";

        table.Rows.Add(row);

        var json = JsonConvert.SerializeObject(ds1, settings);

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Customers": [
                {
                  "CustomerID": "234"
                }
              ]
            }
            """,
            json);
    }

    [Fact]
    public void DeserializedTypedDataSet()
    {
        var settings = new JsonSerializerSettings();

        settings.AddDataSetConverters();

        var json = """
            {
              "Customers": [
                {
                  "CustomerID": "234"
                }
              ]
            }
            """;

        var ds = JsonConvert.DeserializeObject<CustomerDataSet>(json, settings);

        Assert.Equal("234", ds.Customers[0].CustomerID);
    }

    [Fact]
    public void ContractResolverInsideConverter()
    {
        var test = new MultipleDataTablesJsonTest
        {
            TableWrapper1 = new() {DataTableProperty = CreateDataTable(3, "Table1Col")},
            TableWrapper2 = new() {DataTableProperty = CreateDataTable(3, "Table2Col")}
        };

        var json = JsonConvert.SerializeObject(test, Formatting.Indented, new LowercaseDataTableConverter());

        XUnitAssert.AreEqualNormalized(
            """
            {
              "TableWrapper1": {
                "DataTableProperty": [
                  {
                    "table1col1": "1",
                    "table1col2": "2",
                    "table1col3": "3"
                  }
                ],
                "StringProperty": null,
                "IntProperty": 0
              },
              "TableWrapper2": {
                "DataTableProperty": [
                  {
                    "table2col1": "1",
                    "table2col2": "2",
                    "table2col3": "3"
                  }
                ],
                "StringProperty": null,
                "IntProperty": 0
              }
            }
            """,
            json);
    }

    static DataTable CreateDataTable(int cols, string colNamePrefix)
    {
        var table = new DataTable();
        for (var i = 1; i <= cols; i++)
        {
            table.Columns.Add(new DataColumn {ColumnName = colNamePrefix + i, DefaultValue = i});
        }

        table.Rows.Add(table.NewRow());
        return table;
    }

    public class DataTableWrapper
    {
        public DataTable DataTableProperty { get; set; }
        public string StringProperty { get; set; }
        public int IntProperty { get; set; }
    }

    public class MultipleDataTablesJsonTest
    {
        public DataTableWrapper TableWrapper1 { get; set; }
        public DataTableWrapper TableWrapper2 { get; set; }
    }

    public class LowercaseDataTableConverter : DataTableConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dataTableSerializer = new JsonSerializer {ContractResolver = new LowercaseContractResolver()};

            base.WriteJson(writer, value, dataTableSerializer);
        }
    }

    public class LowercaseContractResolver : DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName) =>
            propertyName.ToLower();
    }
}