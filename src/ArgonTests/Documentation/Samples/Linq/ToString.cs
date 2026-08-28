// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class ToString : TestFixtureBase
{
    [Fact]
    public void Example()
    {
        #region ToString

        var o = JObject.Parse("{'string1':'value','integer2':99,'datetime3':'2000-05-23T00:00:00'}");

        Console.WriteLine(o.ToString());
        // {
        //   "string1": "value",
        //   "integer2": 99,
        //   "datetime3": "2000-05-23T00:00:00"
        // }

        Console.WriteLine(o.ToString(Formatting.None));
        // {"string1":"value","integer2":99,"datetime3":"2000-05-23T00:00:00"}

        #endregion
    }
}