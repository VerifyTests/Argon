using System.Data;
using System.Xml;
using Argon;
using Argon.NodaTime;
using NodaTime;

// Test models
public class Person :
    IJsonOnDeserialized
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public Address? Address { get; set; }
    public bool WasDeserialized { get; private set; }

    public void OnDeserialized() => WasDeserialized = true;
}

public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
}

public class Order
{
    [JsonProperty("order_id")]
    public int OrderId { get; set; }

    public List<OrderItem> Items { get; set; } = [];

    [JsonIgnore]
    public string InternalNote { get; set; } = "";
}

public class OrderItem
{
    public string ProductName { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

// Generated serializer context
[ArgonSerializerContext]
[ArgonSerializable(typeof(Person))]
[ArgonSerializable(typeof(Address))]
[ArgonSerializable(typeof(Order))]
[ArgonSerializable(typeof(OrderItem))]
public partial class TestSerializerContext : ArgonSerializerContext
{
}

public static class Program
{
    public static int Main()
    {
        try
        {
            // AOT-compatible tests using source generation
            TestBasicSerialization();
            TestNestedObjectSerialization();
            TestJsonPropertyAttribute();
            TestDeserialization();

            // Verify non-AOT libraries are referenceable (types accessible)
            TestDataSetTypesAccessible();
            TestFSharpTypesAccessible();
            TestInterfaceCallbacksAccessible();
            TestJsonPath();
            TestNodaTimeTypesAccessible();
            TestXmlTypesAccessible();

            Console.WriteLine("All Argon AOT tests passed!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Test failed: {ex}");
            return 1;
        }
    }

    static void TestBasicSerialization()
    {
        var person = new Person
        {
            Name = "John Doe",
            Age = 30
        };

        var context = TestSerializerContext.Default;
        var json = context.Serialize(person);

        Console.WriteLine($"Basic serialization result: {json}");

        if (!json.Contains("\"Name\"") || !json.Contains("John Doe"))
        {
            throw new Exception("Expected Name property in JSON");
        }

        if (!json.Contains("\"Age\"") || !json.Contains("30"))
        {
            throw new Exception("Expected Age property in JSON");
        }
    }

    static void TestNestedObjectSerialization()
    {
        var person = new Person
        {
            Name = "Jane Smith",
            Age = 25,
            Address = new Address
            {
                Street = "123 Main St",
                City = "Springfield",
                Country = "USA"
            }
        };

        var context = TestSerializerContext.Default;
        var json = context.Serialize(person, Argon.Formatting.Indented);

        Console.WriteLine($"Nested serialization result:\n{json}");

        if (!json.Contains("\"Street\"") || !json.Contains("123 Main St"))
        {
            throw new Exception("Expected Address.Street in JSON");
        }
    }

    static void TestJsonPropertyAttribute()
    {
        var order = new Order
        {
            OrderId = 12345,
            Items = [
                new OrderItem { ProductName = "Widget", Price = 9.99m, Quantity = 2 }
            ],
            InternalNote = "This should be ignored"
        };

        var context = TestSerializerContext.Default;
        var json = context.Serialize(order);

        Console.WriteLine($"JsonProperty test result: {json}");

        // Check that order_id is used (from JsonProperty attribute)
        if (!json.Contains("\"order_id\""))
        {
            throw new Exception("Expected 'order_id' property name from JsonProperty attribute");
        }

        // Check that InternalNote is not present (JsonIgnore)
        if (json.Contains("InternalNote") || json.Contains("This should be ignored"))
        {
            throw new Exception("InternalNote should be ignored");
        }
    }

    static void TestDeserialization()
    {
        var json = """
        {
            "Name": "Bob Wilson",
            "Age": 42,
            "Address": {
                "Street": "456 Oak Ave",
                "City": "Portland",
                "Country": "USA"
            }
        }
        """;

        var context = TestSerializerContext.Default;
        var person = context.Deserialize<Person>(json);

        Console.WriteLine($"Deserialization result: Name={person?.Name}, Age={person?.Age}");

        if (person == null)
        {
            throw new Exception("Deserialization returned null");
        }

        if (person.Name != "Bob Wilson")
        {
            throw new Exception($"Expected Name='Bob Wilson', got '{person.Name}'");
        }

        if (person.Age != 42)
        {
            throw new Exception($"Expected Age=42, got {person.Age}");
        }

        if (person.Address?.City != "Portland")
        {
            throw new Exception($"Expected Address.City='Portland', got '{person.Address?.City}'");
        }
    }

    // Argon.DataSets: Verify types are accessible
    static void TestDataSetTypesAccessible()
    {
        // Verify DataSet converter types are accessible
        var settings = new JsonSerializerSettings();
        settings.AddDataSetConverters();

        Console.WriteLine($"DataSets: {settings.Converters.Count} converters registered");

        if (settings.Converters.Count == 0)
        {
            throw new Exception("Expected DataSet converters to be added");
        }
    }

    // Argon.FSharp: Verify types are accessible
    static void TestFSharpTypesAccessible()
    {
        var settings = new JsonSerializerSettings();
        settings.AddFSharpConverters();

        Console.WriteLine($"FSharp: {settings.Converters.Count} converters registered");

        if (settings.Converters.Count == 0)
        {
            throw new Exception("Expected FSharp converters to be added");
        }
    }

    // Argon.InterfaceCallbacks: Verify callbacks can be registered
    static void TestInterfaceCallbacksAccessible()
    {
        var settings = new JsonSerializerSettings();
        settings.AddInterfaceCallbacks();

        // Verify the IJsonOnDeserialized interface is accessible
        var person = new Person();
        if (person is not IJsonOnDeserialized)
        {
            throw new Exception("Expected Person to implement IJsonOnDeserialized");
        }

        Console.WriteLine("InterfaceCallbacks: callbacks registered successfully");
    }

    // Argon.JsonPath: JSONPath query support (works in AOT)
    static void TestJsonPath()
    {
        var json = """
        {
            "store": {
                "book": [
                    { "title": "Book One", "price": 10 },
                    { "title": "Book Two", "price": 20 }
                ]
            }
        }
        """;

        var jObject = JObject.Parse(json);
        var title = jObject.SelectToken("$.store.book[0].title");

        Console.WriteLine($"JsonPath test result: {title}");

        if (title?.ToString() != "Book One")
        {
            throw new Exception($"Expected 'Book One', got '{title}'");
        }

        var allTitles = jObject.SelectTokens("$..title").ToList();
        if (allTitles.Count != 2)
        {
            throw new Exception($"Expected 2 titles, got {allTitles.Count}");
        }
    }

    // Argon.NodaTime: Verify converters are accessible
    static void TestNodaTimeTypesAccessible()
    {
        var settings = new JsonSerializerSettings();
        settings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

        Console.WriteLine($"NodaTime: {settings.Converters.Count} converters registered");

        if (settings.Converters.Count == 0)
        {
            throw new Exception("Expected NodaTime converters to be added");
        }

        // Verify NodaTime types are usable
        var instant = Instant.FromUtc(2024, 6, 15, 10, 30, 0);
        var localDate = new LocalDate(2024, 6, 15);
        var duration = Duration.FromHours(2);

        Console.WriteLine($"NodaTime types: Instant={instant}, LocalDate={localDate}, Duration={duration}");
    }

    // Argon.Xml: Verify XmlNodeConverter is accessible
    static void TestXmlTypesAccessible()
    {
        // Verify XmlNodeConverter can be instantiated
        var converter = new XmlNodeConverter
        {
            DeserializeRootElementName = "root",
            WriteArrayAttribute = false,
            OmitRootObject = false
        };

        Console.WriteLine($"XmlConverter: DeserializeRootElementName={converter.DeserializeRootElementName}");

        // Verify it can be added to settings
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(converter);

        if (settings.Converters.Count == 0)
        {
            throw new Exception("Expected XmlNodeConverter to be added");
        }
    }
}
