// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class SerializeRawJson : TestFixtureBase
{
    #region SerializeRawJsonTypes

    public class JavaScriptSettings
    {
        public JRaw OnLoadFunction { get; set; }
        public JRaw OnUnloadFunction { get; set; }
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region SerializeRawJsonUsage

        var settings = new JavaScriptSettings
        {
            OnLoadFunction = [with("OnLoad")],
            OnUnloadFunction = [with("function(e) { alert(e); }")]
        };

        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);

        Console.WriteLine(json);
        // {
        //   "OnLoadFunction": OnLoad,
        //   "OnUnloadFunction": function(e) { alert(e); }
        // }

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "OnLoadFunction": OnLoad,
              "OnUnloadFunction": function(e) { alert(e); }
            }
            """,
            json);
    }
}
