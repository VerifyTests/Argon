// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class FromObject : TestFixtureBase
{
    #region FromObjectTypes

    public class Computer
    {
        public string Cpu { get; set; }
        public int Memory { get; set; }
        public IList<string> Drives { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region FromObjectUsage

        var i = (JValue) JToken.FromObject(12345);

        Console.WriteLine(i.Type);
        // Integer
        Console.WriteLine(i.ToString());
        // 12345

        var s = (JValue) JToken.FromObject("A string");

        Console.WriteLine(s.Type);
        // String
        Console.WriteLine(s.ToString());
        // A string

        var computer = new Computer
        {
            Cpu = "Intel",
            Memory = 32,
            Drives =
            [
                "DVD",
                "SSD"
            ]
        };

        var o = (JObject) JToken.FromObject(computer);

        Console.WriteLine(o.ToString());
        // {
        //   "Cpu": "Intel",
        //   "Memory": 32,
        //   "Drives": [
        //     "DVD",
        //     "SSD"
        //   ]
        // }

        var a = (JArray) JToken.FromObject(computer.Drives);

        Console.WriteLine(a.ToString());
        // [
        //   "DVD",
        //   "SSD"
        // ]

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            [
              "DVD",
              "SSD"
            ]
            """,
            a.ToString());
    }
}