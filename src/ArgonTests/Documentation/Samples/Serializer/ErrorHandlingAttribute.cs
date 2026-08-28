// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using Formatting = Argon.Formatting;

public class ErrorHandlingAttribute : TestFixtureBase
{
    #region ErrorHandlingAttributeTypes

    public class Employee :
        IJsonOnSerializeError
    {
        public string Name { get; set; }
        public int Age { get; set; }

        public List<string> Roles
        {
            get
            {
                if (field == null)
                {
                    throw new("Roles not loaded!");
                }

                return field;
            }
            set;
        }

        public string Title { get; set; }

        public void OnSerializeError(object originalObject, string path, object member, Exception exception, Action markAsHandled) =>
            markAsHandled();
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region ErrorHandlingAttributeUsage

        var person = new Employee
        {
            Name = "George Michael Bluth",
            Age = 16,
            Roles = null,
            Title = "Mister Manager"
        };

        var settings = new JsonSerializerSettings();
        settings.AddInterfaceCallbacks();
        var json = JsonConvert.SerializeObject(person, Formatting.Indented, settings);

        Console.WriteLine(json);
        // {
        //   "Name": "George Michael Bluth",
        //   "Age": 16,
        //   "Title": "Mister Manager"
        // }

        #endregion

        XUnitAssert.AreEqualNormalized(
            """
            {
              "Name": "George Michael Bluth",
              "Age": 16,
              "Title": "Mister Manager"
            }
            """,
            json);
    }
}