using Formatting = Argon.Formatting;

public class KeyValuePairConverterTests : TestFixtureBase
{
    [Fact]
    public void SerializeUsingInternalConverter()
    {
        var contractResolver = new DefaultContractResolver();
        var contract = (JsonObjectContract) contractResolver.ResolveContract(typeof(KeyValuePair<string, int>));

        Assert.Equal(typeof(KeyValuePairConverter), contract.InternalConverter.GetType());

        var values = new List<KeyValuePair<string, int>>
        {
            new("123", 123),
            new("456", 456)
        };

        var json = JsonConvert.SerializeObject(values, Formatting.Indented);

        XUnitAssert.AreEqualNormalized(
            """
            [
              {
                "Key": "123",
                "Value": 123
              },
              {
                "Key": "456",
                "Value": 456
              }
            ]
            """,
            json);

        var v2 = JsonConvert.DeserializeObject<IList<KeyValuePair<string, int>>>(json);

        Assert.Equal(2, v2.Count);
        Assert.Equal("123", v2[0].Key);
        Assert.Equal(123, v2[0].Value);
        Assert.Equal("456", v2[1].Key);
        Assert.Equal(456, v2[1].Value);
    }

    [Fact]
    public void DeserializeUnexpectedEnd()
    {
        var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<KeyValuePair<string, int>>("""{"Key": "123","""));
        Assert.Equal("Unexpected end when reading JSON. Path 'Key', line 1, position 14.", exception.Message);
    }
}