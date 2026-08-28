// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class DataContractAndDataMember : TestFixtureBase
{
    #region DataContractAndDataMemberTypes

    [DataContract]
    public class File
    {
        // excluded from serialization
        // does not have DataMemberAttribute
        public Guid Id { get; set; }

        [DataMember] public string Name { get; set; }

        [DataMember] public int Size { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region DataContractAndDataMemberUsage

        var file = new File
        {
            Id = Guid.NewGuid(),
            Name = "ImportantLegalDocuments.docx",
            Size = 50 * 1024
        };

        var json = JsonConvert.SerializeObject(file, Formatting.Indented);

        Console.WriteLine(json);
        // {
        //   "Name": "ImportantLegalDocuments.docx",
        //   "Size": 51200
        // }

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Name": "ImportantLegalDocuments.docx",
              "Size": 51200
            }
            """,
            json);
    }
}