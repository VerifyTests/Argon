// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class JsonObjectAttributeOverrideIEnumerable : TestFixtureBase
{
    #region JsonObjectAttributeOverrideIEnumerableTypes

    [JsonObject]
    public class Directory : IEnumerable<string>
    {
        public string Name { get; set; }
        public IList<string> Files { get; set; } = [];

        public IEnumerator<string> GetEnumerator() =>
            Files.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region JsonObjectAttributeOverrideIEnumerableUsage

        var directory = new Directory
        {
            Name = "My Documents",
            Files =
            {
                "ImportantLegalDocuments.docx",
                "WiseFinancalAdvice.xlsx"
            }
        };

        var json = JsonConvert.SerializeObject(directory, Formatting.Indented);

        Console.WriteLine(json);
        // {
        //   "Name": "My Documents",
        //   "Files": [
        //     "ImportantLegalDocuments.docx",
        //     "WiseFinancalAdvice.xlsx"
        //   ]
        // }

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Name": "My Documents",
              "Files": [
                "ImportantLegalDocuments.docx",
                "WiseFinancalAdvice.xlsx"
              ]
            }
            """,
            json);
    }
}