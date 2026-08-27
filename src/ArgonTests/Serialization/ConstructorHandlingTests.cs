// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using System.ComponentModel;
using TestObjects;

public class ConstructorHandlingTests : TestFixtureBase
{
    [Fact]
    public void ImmutableStructWithMultipleConstructorsRoundTrips()
    {
        var json = JsonConvert.SerializeObject(new MultiConstructorImmutable(42));
        var result = JsonConvert.DeserializeObject<MultiConstructorImmutable>(json);
        Assert.Equal(42, result.Value);
    }

    public readonly struct MultiConstructorImmutable
    {
        // declared before the matching constructor: before the fix a non-matching first
        // constructor aborted the whole search
        // ReSharper disable once UnusedParameter.Local
        public MultiConstructorImmutable(string ignored) =>
            Value = -1;

        public MultiConstructorImmutable(int value) =>
            Value = value;

        public int Value { get; }
    }

    [Fact]
    public void UsePrivateConstructorIfThereAreMultipleConstructorsWithParametersAndNothingToFallbackTo()
    {
        var json = """{Name:"Name!"}""";

        var c = JsonConvert.DeserializeObject<PrivateConstructorTestClass>(json);

        Assert.Equal("Name!", c.Name);
    }

    [Fact]
    public void SuccessWithPrivateConstructorAndAllowNonPublic()
    {
        var json = """{Name:"Name!"}""";

        var c = JsonConvert.DeserializeObject<PrivateConstructorTestClass>(json,
            new JsonSerializerSettings
            {
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
            });
        Assert.NotNull(c);
        Assert.Equal("Name!", c.Name);
    }

    [Fact]
    public void FailWithPrivateConstructorPlusParameterizedAndDefault()
    {
        var json = """{Name:"Name!"}""";
        Assert.Throws<Exception>(() => JsonConvert.DeserializeObject<PrivateConstructorWithPublicParameterizedConstructorTestClass>(json));
    }

    [Fact]
    public void SuccessWithPrivateConstructorPlusParameterizedAndAllowNonPublic()
    {
        var json = """{Name:"Name!"}""";

        var c = JsonConvert.DeserializeObject<PrivateConstructorWithPublicParameterizedConstructorTestClass>(json,
            new JsonSerializerSettings
            {
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
            });
        Assert.NotNull(c);
        Assert.Equal("Name!", c.Name);
        Assert.Equal(1, c.Age);
    }

    [Fact]
    public void SuccessWithPublicParameterizedConstructor()
    {
        var json = """{Name:"Name!"}""";

        var c = JsonConvert.DeserializeObject<PublicParameterizedConstructorTestClass>(json);
        Assert.NotNull(c);
        Assert.Equal("Name!", c.Name);
    }

    [Fact]
    public void SuccessWithPublicParameterizedConstructorWhenParameterIsNotAProperty()
    {
        var json = """{nameParameter:"Name!"}""";

        var c = JsonConvert.DeserializeObject<PublicParameterizedConstructorWithNonPropertyParameterTestClass>(json);
        Assert.NotNull(c);
        Assert.Equal("Name!", c.Name);
    }

    [Fact]
    public void SuccessWithPublicParameterizedConstructorWhenParameterRequiresAConverter()
    {
        var json = """{nameParameter:"Name!"}""";

        var c = JsonConvert.DeserializeObject<PublicParameterizedConstructorRequiringConverterTestClass>(json, new NameContainerConverter());
        Assert.NotNull(c);
        Assert.Equal("Name!", c.Name.Value);
    }

    [Fact]
    public void SuccessWithPublicParameterizedConstructorWhenParameterRequiresAConverterWithParameterAttribute()
    {
        var json = """{nameParameter:"Name!"}""";

        var c = JsonConvert.DeserializeObject<PublicParameterizedConstructorRequiringConverterWithParameterAttributeTestClass>(json);
        Assert.NotNull(c);
        Assert.Equal("Name!", c.Name.Value);
    }

    [Fact]
    public void SuccessWithPublicParameterizedConstructorWhenParameterRequiresAConverterWithPropertyAttribute()
    {
        var json = """{name:"Name!"}""";

        var c = JsonConvert.DeserializeObject<PublicParameterizedConstructorRequiringConverterWithPropertyAttributeTestClass>(json);
        Assert.NotNull(c);
        Assert.Equal("Name!", c.Name.Value);
    }

    [Fact]
    public void SuccessWithPublicParameterizedConstructorWhenParameterNameConflictsWithPropertyName()
    {
        var json = """{name:"1"}""";

        var c = JsonConvert.DeserializeObject<PublicParameterizedConstructorWithPropertyNameConflict>(json);
        Assert.NotNull(c);
        Assert.Equal(1, c.Name);
    }

    [Fact]
    public void PublicParameterizedConstructorWithPropertyNameConflictWithAttribute()
    {
        var json = """{name:"1"}""";

        var c = JsonConvert.DeserializeObject<PublicParameterizedConstructorWithPropertyNameConflictWithAttribute>(json);
        Assert.NotNull(c);
        Assert.Equal(1, c.Name);
    }

    public class ConstructorParametersRespectDefaultValueAttributes(string parameter1, string parameter2, string parameter3)
    {
        [DefaultValue("parameter1_default")]
        public string Parameter1 { get; } = parameter1;

        [DefaultValue("parameter2_default")]
        public string Parameter2 { get; } = parameter2;

        [DefaultValue("parameter3_default")]
        public string Parameter3 { get; set; } = parameter3;

        [DefaultValue("parameter4_default")]
        public string Parameter4 { get; set; }
    }

    [Fact]
    public void ConstructorParametersRespectDefaultValueTest_Attributes()
    {
        var testObject = JsonConvert.DeserializeObject<ConstructorParametersRespectDefaultValueAttributes>("{'Parameter2':'value!'}", new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Populate
        });

        Assert.Equal("parameter1_default", testObject.Parameter1);
        Assert.Equal("value!", testObject.Parameter2);
        Assert.Equal("parameter3_default", testObject.Parameter3);
        Assert.Equal("parameter4_default", testObject.Parameter4);
    }

    [Fact]
    public void ConstructorParametersRespectDefaultValueTest()
    {
        var testObject = JsonConvert.DeserializeObject<ConstructorParametersRespectDefaultValue>("{}", new JsonSerializerSettings
        {
            ContractResolver = ConstructorParameterDefaultStringValueContractResolver.Instance
        });

        Assert.Equal("Default Value", testObject.Parameter1);
        Assert.Equal("Default Value", testObject.Parameter2);
    }

    public class ConstructorParametersRespectDefaultValue(string parameter1, string parameter2)
    {
        public const string DefaultValue = "Default Value";

        public string Parameter1 { get; } = parameter1;
        public string Parameter2 { get; } = parameter2;
    }

    public class ConstructorParameterDefaultStringValueContractResolver : DefaultContractResolver
    {
        public new static ConstructorParameterDefaultStringValueContractResolver Instance = new();

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);

            foreach (var property in properties.Where(_ => _.PropertyType == typeof(string)))
            {
                property.DefaultValue = ConstructorParametersRespectDefaultValue.DefaultValue;
                property.DefaultValueHandling = DefaultValueHandling.Populate;
            }

            return properties;
        }
    }
}
