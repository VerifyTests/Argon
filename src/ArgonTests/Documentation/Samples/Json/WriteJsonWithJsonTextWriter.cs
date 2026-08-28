// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class WriteJsonWithJsonTextWriter : TestFixtureBase
{
    [Fact]
    public void Example()
    {
        #region WriteJsonWithJsonTextWriter

        var stringWriter = new StringWriter();

        using (JsonWriter writer = new JsonTextWriter(stringWriter))
        {
            writer.Formatting = Formatting.Indented;

            writer.WriteStartObject();
            writer.WritePropertyName("CPU");
            writer.WriteValue("Intel");
            writer.WritePropertyName("PSU");
            writer.WriteValue("500W");
            writer.WritePropertyName("Drives");
            writer.WriteStartArray();
            writer.WriteValue("DVD read/writer");
            writer.WriteComment("(broken)");
            writer.WriteValue("500 gigabyte hard drive");
            writer.WriteValue("200 gigabyte hard drive");
            writer.WriteEnd();
            writer.WriteEndObject();
        }

        Console.WriteLine(stringWriter.ToString());
        // {
        //   "CPU": "Intel",
        //   "PSU": "500W",
        //   "Drives": [
        //     "DVD read/writer"
        //     /*(broken)*/,
        //     "500 gigabyte hard drive",
        //     "200 gigabyte hard drive"
        //   ]
        // }

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "CPU": "Intel",
              "PSU": "500W",
              "Drives": [
                "DVD read/writer"
                /*(broken)*/,
                "500 gigabyte hard drive",
                "200 gigabyte hard drive"
              ]
            }
            """,
            stringWriter.ToString());
    }
}