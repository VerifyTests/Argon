// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using TestObjects;

public class DelegateFactoryTests : TestFixtureBase
{
    [Fact]
    public void ConstructorWithInString()
    {
        var constructor = typeof(InTestClass).GetConstructors().Single(_ => _.GetParameters().Length == 1);

        var creator = DelegateFactory.CreateParameterizedConstructor(constructor);

        var args = new object[]
        {
            "Value"
        };
        var o = (InTestClass) creator(args);
        Assert.NotNull(o);
        Assert.Equal("Value", o.Value);
    }

    [Fact]
    public void ConstructorWithInStringAndBool()
    {
        var constructor = typeof(InTestClass).GetConstructors().Single(_ => _.GetParameters().Length == 2);

        var creator = DelegateFactory.CreateParameterizedConstructor(constructor);

        var args = new object[]
        {
            "Value",
            true
        };
        var o = (InTestClass) creator(args);
        Assert.NotNull(o);
        Assert.Equal("Value", o.Value);
        Assert.True(o.B1);
    }

    [Fact]
    public void ConstructorWithRefString()
    {
        var constructor = typeof(OutAndRefTestClass).GetConstructors().Single(_ => _.GetParameters().Length == 1);

        var creator = DelegateFactory.CreateParameterizedConstructor(constructor);

        var o = (OutAndRefTestClass) creator("Input");
        Assert.NotNull(o);
        Assert.Equal("Input", o.Input);
    }

    [Fact]
    public void ConstructorWithRefStringAndOutBool()
    {
        var constructor = typeof(OutAndRefTestClass).GetConstructors().Single(_ => _.GetParameters().Length == 2);

        var creator = DelegateFactory.CreateParameterizedConstructor(constructor);

        var o = (OutAndRefTestClass) creator("Input", false);
        Assert.NotNull(o);
        Assert.Equal("Input", o.Input);
        Assert.True(o.B1);
    }

    [Fact]
    public void ConstructorWithRefStringAndRefBoolAndRefBool()
    {
        var constructor = typeof(OutAndRefTestClass).GetConstructors().Single(_ => _.GetParameters().Length == 3);

        var creator = DelegateFactory.CreateParameterizedConstructor(constructor);

        var o = (OutAndRefTestClass) creator("Input", true, null);
        Assert.NotNull(o);
        Assert.Equal("Input", o.Input);
        Assert.True(o.B1);
        Assert.False( o.B2);
    }

    [Fact]
    public void CreateGetWithBadObjectTarget()
    {
        var p = new Person
        {
            Name = "Hi"
        };
        var setter = DelegateFactory.CreateGet<object>(typeof(Movie).GetProperty("Name"));

        var exception = Assert.Throws<InvalidCastException>(() => setter(p));
        Assert.Equal("Unable to cast object of type 'TestObjects.Person' to type 'TestObjects.Movie'.", exception.Message);
    }

    [Fact]
    public void CreateSetWithBadObjectTarget()
    {
        var exception = Assert.Throws<InvalidCastException>(() =>
        {
            var p = new Person();
            var m = new Movie();

            var setter = DelegateFactory.CreateSet<object>(typeof(Movie).GetProperty("Name"));

            setter(m, "Hi");

            Assert.Equal("Hi", m.Name);

            setter(p, "Hi");

            Assert.Equal("Hi", p.Name);
        });
        Assert.Equal("Unable to cast object of type 'TestObjects.Person' to type 'TestObjects.Movie'.", exception.Message);
    }

    [Fact]
    public void CreateSetWithBadTarget()
    {
        object structTest = new StructTest();

        var setter = DelegateFactory.CreateSet<object>(typeof(StructTest).GetProperty("StringProperty"));

        setter(structTest, "Hi");

        Assert.Equal("Hi", ((StructTest) structTest).StringProperty);

        Assert.Throws<InvalidCastException>(() => setter(new TimeSpan(), "Hi"));
    }

    [Fact]
    public void CreateSetWithBadObjectValue()
    {
        var exception = Assert.Throws<InvalidCastException>(() =>
        {
            var m = new Movie();

            var setter = DelegateFactory.CreateSet<object>(typeof(Movie).GetProperty("Name"));

            setter(m, new Version("1.1.1.1"));
        });
        Assert.Equal("Unable to cast object of type 'System.Version' to type 'System.String'.", exception.Message);
    }

    [Fact]
    public void CreateStaticMethodCall()
    {
        var castMethodInfo = typeof(DictionaryKey)
            .GetMethod(
                "op_Implicit",
                [typeof(string)]);

        Assert.NotNull(castMethodInfo);

        var call = DelegateFactory.CreateMethodCall<object>(castMethodInfo);

        var result = call(null, "First!");
        Assert.NotNull(result);

        var key = (DictionaryKey) result;
        Assert.Equal("First!", key.Value);
    }

    [Fact]
    public void CreatePropertyGetter()
    {
        var namePropertyInfo = typeof(Person).GetProperty(nameof(Person.Name));

        Assert.NotNull(namePropertyInfo);

        var call = DelegateFactory.CreateGet<Person>(namePropertyInfo);

        var p = new Person
        {
            Name = "Name!"
        };

        var result = call(p);
        Assert.NotNull(result);

        Assert.Equal("Name!", (string) result);
    }

    [Fact]
    public void ConstructorStruct()
    {
        var creator1 = DelegateFactory.CreateDefaultConstructor<object>(typeof(MyStruct));
        var myStruct1 = (MyStruct) creator1.Invoke();
        Assert.Equal(0, myStruct1.IntProperty);

        var creator2 = DelegateFactory.CreateDefaultConstructor<MyStruct>(typeof(MyStruct));
        var myStruct2 = creator2.Invoke();
        Assert.Equal(0, myStruct2.IntProperty);
    }

    public struct TestStruct(int i)
    {
        public int Value { get; } = i;
    }

    public static TestStruct StructMethod(TestStruct s) =>
        new(s.Value + s.Value);

    [Fact]
    public void CreateStructMethodCall()
    {
        var methodInfo = typeof(DelegateFactoryTests).GetMethod(
            nameof(StructMethod),
            [typeof(TestStruct)]);

        Assert.NotNull(methodInfo);

        var call = DelegateFactory.CreateMethodCall<object>(methodInfo);

        var result = call(null, new TestStruct(123));
        Assert.NotNull(result);

        var s = (TestStruct) result;
        Assert.Equal(246, s.Value);
    }
}
public class OutAndRefTestClass
{
    public string Input { get; set; }
    public bool B1 { get; set; }
    public bool B2 { get; set; }

    public OutAndRefTestClass(ref string value)
    {
        Input = value;
        value = "Output";
    }

    public OutAndRefTestClass(ref string value, out bool b1)
        : this(ref value)
    {
        b1 = true;
        B1 = true;
    }

    public OutAndRefTestClass(ref string value, ref bool b1, ref bool b2)
        : this(ref value)
    {
        B1 = b1;
        B2 = b2;
    }
}

public class InTestClass(in string value)
{
    public string Value { get; } = value;
    public bool B1 { get; }

    public InTestClass(in string value, in bool b1)
        : this(in value) =>
        B1 = b1;
}

public struct MyStruct
{
    public int IntProperty { get; set; }
}