# Querying JSON with SelectToken

`Argon.JToken.SelectToken` provides a method to query LINQ to JSON using a single string path to a desired `Argon.JToken`. SelectToken makes dynamic queries easy because the entire query is defined in a string.


## SelectToken

SelectToken is a method on JToken and takes a string path to a child token. SelectToken returns the child token or a null reference if a token couldn't be found at the path's location.

The path is made up of property names and array indexes separated by periods, e.g. `Manufacturers[0].Name`.

<!-- snippet: SelectTokenComplex -->
<a id='snippet-SelectTokenComplex'></a>
```cs
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

var name = (string) o.SelectToken("Manufacturers[0].Name");
// Acme Co

var productPrice = (decimal) o.SelectToken("Manufacturers[0].Products[0].Price");
// 50

var productName = (string) o.SelectToken("Manufacturers[1].Products[0].Name");
// Elbow Grease
```
<sup><a href='/src/ArgonTests/Documentation/LinqToJsonTests.cs#L466-L511' title='Snippet source file'>snippet source</a> | <a href='#snippet-SelectTokenComplex' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## SelectToken with JSONPath

SelectToken supports JSONPath queries. See: https://goessner.net/articles/JsonPath/

<!-- snippet: QueryJsonSelectTokenJsonPath -->
<a id='snippet-QueryJsonSelectTokenJsonPath'></a>
```cs
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

// manufacturer with the name 'Acme Co'
var acme = o.SelectToken("$.Manufacturers[?(@.Name == 'Acme Co')]");

Console.WriteLine(acme);
// { "Name": "Acme Co", Products: [{ "Name": "Anvil", "Price": 50 }] }

// name of all products priced 50 and above
var priceyProducts = o.SelectTokens("$..Products[?(@.Price >= 50)].Name")
    .ToList();

foreach (var item in priceyProducts)
{
    Console.WriteLine(item);
}

// Anvil
// Elbow Grease
```
<sup><a href='/src/ArgonTests/Documentation/Samples/JsonPath/QueryJsonSelectTokenJsonPath.cs#L11-L65' title='Snippet source file'>snippet source</a> | <a href='#snippet-QueryJsonSelectTokenJsonPath' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## SelectToken with LINQ

SelectToken can be used in combination with standard LINQ methods.

<!-- snippet: SelectTokenLinq -->
<a id='snippet-SelectTokenLinq'></a>
```cs
var storeNames = o
    .SelectToken("Stores")!
    .Select(s => (string) s)
    .ToList();
// Lambton Quay
// Willis Street

var firstProductNames = o["Manufacturers"]!
    .Select(m => (string) m.SelectToken("Products[1].Name"))
    .ToList();
// null
// Headlight Fluid

var totalPrice = o["Manufacturers"]
    .Sum(m => (decimal) m.SelectToken("Products[0].Price"));
// 149.95
```
<sup><a href='/src/ArgonTests/Documentation/LinqToJsonTests.cs#L555-L574' title='Snippet source file'>snippet source</a> | <a href='#snippet-SelectTokenLinq' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->


## Related Topics

 * LINQtoJSON

      `Argon.JToken.SelectToken`
