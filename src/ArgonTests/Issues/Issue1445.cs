// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class Issue1445 : TestFixtureBase
{
    [Fact]
    public void Test()
    {
        var dt = new DataTable();
        dt.Columns.Add("First", typeof(string));
        dt.Columns.Add("Second", typeof(string));

        dt.Rows.Add("string1", "string2");
        dt.Rows.Add("string1", null);

        var data = dt.Select().Select(r => r.ItemArray).ToArray();

        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        XUnitAssert.AreEqualNormalized(
            """
            [
              [
                "string1",
                "string2"
              ],
              [
                "string1",
                null
              ]
            ]
            """,
            json);
    }
}