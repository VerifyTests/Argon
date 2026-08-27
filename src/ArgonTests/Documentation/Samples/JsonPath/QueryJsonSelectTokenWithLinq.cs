// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class QueryJsonSelectTokenWithLinq : TestFixtureBase
{
    [Fact]
    public void Example()
    {
        #region QueryJsonSelectTokenWithLinq

        var o = JObject.Parse(
            """
            {
              'Stores': [
                'Lambton Quay',
                'Willis Street'
              ],
              'Manufacturers': [
                {
                  'Name': 'Acme Co',
                  'Products': [
                    {
                      'Name': 'Anvil',
                      'Price': 50
                    }
                  ]
                },
                {
                  'Name': 'Contoso',
                  'Products': [
                    {
                      'Name': 'Elbow Grease',
                      'Price': 99.95
                    },
                    {
                      'Name': 'Headlight Fluid',
                      'Price': 4
                    }
                  ]
                }
              ]
            }
            """);

        var storeNames = o.SelectToken("Stores").Select(s => (string) s).ToArray();

        Console.WriteLine(string.Join(", ", storeNames));
        // Lambton Quay, Willis Street

        var firstProductNames = o["Manufacturers"].Select(m => (string) m.SelectToken("Products[1].Name"))
            .Where(n => n != null).ToArray();

        Console.WriteLine(string.Join(", ", firstProductNames));
        // Headlight Fluid

        var totalPrice = o["Manufacturers"].Sum(m => (decimal) m.SelectToken("Products[0].Price"));

        Console.WriteLine(totalPrice);
        // 149.95

        #endregion

        Assert.Equal(149.95m, totalPrice, 2);
    }
}