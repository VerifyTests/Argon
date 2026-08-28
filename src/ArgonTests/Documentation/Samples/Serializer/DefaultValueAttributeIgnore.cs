// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using System.ComponentModel;
using Formatting = Argon.Formatting;

public class DefaultValueAttributeIgnore : TestFixtureBase
{
    #region DefaultValueAttributeIgnoreTypes

    public class Customer
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        [DefaultValue(" ")] public string FullName => $"{FirstName} {LastName}";
    }

    #endregion

    [Fact]
    public void Example()
    {
        #region DefaultValueAttributeIgnoreUsage

        var customer = new Customer();

        var jsonIncludeDefaultValues = JsonConvert.SerializeObject(customer, Formatting.Indented);

        Console.WriteLine(jsonIncludeDefaultValues);
        // {
        //   "FirstName": null,
        //   "LastName": null,
        //   "FullName": " "
        // }

        var jsonIgnoreDefaultValues = JsonConvert.SerializeObject(customer, Formatting.Indented, new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore
        });

        Console.WriteLine(jsonIgnoreDefaultValues);
        // {}

        #endregion

        Assert.Equal("{}", jsonIgnoreDefaultValues);
    }
}