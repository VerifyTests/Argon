// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class SerializeDataSet : TestFixtureBase
{
    [Fact]
    public void Example()
    {
        #region SerializeDataSet

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

        Console.WriteLine(json);
        // {
        //   "Table1": [
        //     {
        //       "id": 0,
        //       "item": "item 0"
        //     },
        //     {
        //       "id": 1,
        //       "item": "item 1"
        //     }
        //   ]
        // }

        #endregion

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
    }
}