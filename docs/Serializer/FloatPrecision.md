# Float Precision

Controls how many decimal points to use when serializing floats and doubles.

<!-- snippet: FloatPrecision -->
<a id='snippet-FloatPrecision'></a>
```cs
[Fact]
public void FloatPrecision()
{
    var numbers = new List<object>
    {
        1.1234567f,
        1.1234567d,
    };

    var json = JsonConvert.SerializeObject(
        numbers,
        new JsonSerializerSettings
        {
            FloatPrecision = 3
        });
    Assert.Equal("[1.123,1.123]", json);
}
```
<sup><a href='/src/ArgonTests/Serialization/JsonSerializerTest.cs#L6349-L6369' title='Snippet source file'>snippet source</a> | <a href='#snippet-FloatPrecision' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
