// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class DeserializeDataSet : TestFixtureBase
{
    [Fact]
    public void Example()
    {
        #region DeserializeDataSet

        var json = """
            {
              'Table1': [
                {
                  'id': 0,
                  'item': 'item 0'
                },
                {
                  'id': 1,
                  'item': 'item 1'
                }
              ]
            }
            """;

        var settings = new JsonSerializerSettings();

        settings.AddDataSetConverters();
        var dataSet = JsonConvert.DeserializeObject<DataSet>(json, settings);

        var dataTable = dataSet.Tables["Table1"];

        Console.WriteLine(dataTable.Rows.Count);
        // 2

        foreach (DataRow row in dataTable.Rows)
        {
            Console.WriteLine($"{row["id"]} - {row["item"]}");
        }

        // 0 - item 0
        // 1 - item 1

        #endregion

        Assert.Equal(2, dataTable.Rows.Count);
    }
}