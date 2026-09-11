// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

#nullable enable

using ClosedTypeFixtures;
using ClosedTypeFixtures.Discriminators.First;
using ClosedTypeFixtures.Discriminators.Second;

public class ClosedTypePolymorphismTests : TestFixtureBase
{
    static JsonSerializerSettings Infer =>
        new()
        {
            InferClosedTypePolymorphism = true
        };

    // the discriminator is the simple type name, matching what System.Text.Json infers
    [Fact]
    public void SerializeLeafThroughProperty() =>
        Assert.Equal(
            """{"Event":{"$type":"PaymentAuthorized","Amount":42.5,"PaymentId":"p-123"}}""",
            JsonConvert.SerializeObject(new HasEvent {Event = new PaymentAuthorized("p-123", 42.5m)}, Infer));

    [Fact]
    public void RoundTripLeafThroughProperty()
    {
        var json = JsonConvert.SerializeObject(new HasEvent {Event = new PaymentAuthorized("p-123", 42.5m)}, Infer);

        Assert.Equal(
            new PaymentAuthorized("p-123", 42.5m),
            JsonConvert.DeserializeObject<HasEvent>(json, Infer)!.Event);
    }

    [Fact]
    public void RoundTripRootWithDeclaredType()
    {
        var json = JsonConvert.SerializeObject(new PaymentAuthorized("p-123", 42.5m), typeof(PaymentEvent), Infer);

        Assert.Equal("""{"$type":"PaymentAuthorized","Amount":42.5,"PaymentId":"p-123"}""", json);
        Assert.Equal(
            new PaymentAuthorized("p-123", 42.5m),
            JsonConvert.DeserializeObject<PaymentEvent>(json, Infer));
    }

    // rootType is only set when the caller passes a type, the same limitation TypeNameHandling.Auto
    // has, so a root serialized without one carries no discriminator
    [Fact]
    public void SerializeRootWithoutDeclaredTypeHasNoDiscriminator() =>
        Assert.DoesNotContain("$type", JsonConvert.SerializeObject(new PaymentAuthorized("p-123", 42.5m), Infer));

    [Fact]
    public void RoundTripList()
    {
        List<PaymentEvent> events = [new PaymentAuthorized("p-1", 1m), new PaymentCaptured("p-2", "r-2")];

        var json = JsonConvert.SerializeObject(events, Infer);

        Assert.Contains(@"""$type"":""PaymentAuthorized""", json);
        Assert.Contains(@"""$type"":""PaymentCaptured""", json);
        Assert.Equal(events, JsonConvert.DeserializeObject<List<PaymentEvent>>(json, Infer));
    }

    [Fact]
    public void RoundTripDictionary()
    {
        Dictionary<string, PaymentEvent> events = new()
        {
            {"a", new PaymentAuthorized("p-1", 1m)},
            {"b", new PaymentCaptured("p-2", "r-2")}
        };

        var json = JsonConvert.SerializeObject(events, Infer);

        Assert.Equal(events, JsonConvert.DeserializeObject<Dictionary<string, PaymentEvent>>(json, Infer));
    }

    // only terminal leaves carry a discriminator, so the intermediate closed type is expanded through
    [Fact]
    public void RoundTripNestedHierarchy()
    {
        var json = JsonConvert.SerializeObject(new HasNode {Node = new NamedLeaf("x")}, Infer);

        Assert.Contains(@"""$type"":""NamedLeaf""", json);
        Assert.Equal(new NamedLeaf("x"), JsonConvert.DeserializeObject<HasNode>(json, Infer)!.Node);
    }

    // a closed type is always implicitly abstract, so a closed base or an intermediate closed type
    // can never itself be an instance to serialize
    [Fact]
    public void ClosedTypesAreAlwaysAbstract()
    {
        Assert.True(typeof(PaymentEvent).IsAbstract);
        Assert.True(typeof(Branch).IsAbstract);
    }

    [Fact]
    public void DeserializeUnknownDiscriminator()
    {
        var exception = Assert.Throws<JsonSerializationException>(
            () => JsonConvert.DeserializeObject<HasEvent>("""{"Event":{"$type":"Nonexistent"}}""", Infer));

        Assert.Contains("'Nonexistent' is not a known derived type of closed type", exception.Message);
    }

    // OpenBranch is not closed, so it is the terminal leaf and anything below it is out of reach
    [Fact]
    public void SerializeBelowNonClosedBranch()
    {
        var exception = Assert.Throws<JsonSerializationException>(
            () => JsonConvert.SerializeObject(new HasRoot {Root = new OpenLeaf()}, Infer));

        Assert.Contains("is not a terminal derived type of closed type", exception.Message);
    }

    [Fact]
    public void DuplicateDiscriminatorsThrowOnWrite()
    {
        var exception = Assert.Throws<JsonSerializationException>(
            () => JsonConvert.SerializeObject(new HasAmbiguous {Value = new ClosedTypeFixtures.Discriminators.First.Circle()}, Infer));

        Assert.Contains("two derived types with the type discriminator 'Circle'", exception.Message);
    }

    [Fact]
    public void DuplicateDiscriminatorsThrowOnRead()
    {
        var exception = Assert.Throws<JsonSerializationException>(
            () => JsonConvert.DeserializeObject<HasAmbiguous>("""{"Value":{"$type":"Circle"}}""", Infer));

        Assert.Contains("two derived types with the type discriminator 'Circle'", exception.Message);
    }

    // the marker is matched by full name, so an unrelated attribute of the same simple name is not a
    // closed type
    [Fact]
    public void DecoyAttributeIsNotClosed() =>
        Assert.DoesNotContain("$type", JsonConvert.SerializeObject(new HasDecoy {Value = new DecoyDerived()}, Infer));

    [Fact]
    public void PlainAbstractBaseIsNotInferred() =>
        Assert.DoesNotContain("$type", JsonConvert.SerializeObject(new HasPlain {Value = new PlainDerived("p")}, Infer));

    [Fact]
    public void SettingOffWritesNoDiscriminator() =>
        Assert.DoesNotContain(
            "$type",
            JsonConvert.SerializeObject(new HasEvent {Event = new PaymentAuthorized("p-123", 42.5m)}));

    // with inference off an abstract closed base cannot be constructed, and the failure points at
    // the setting that would fix it
    [Fact]
    public void SettingOffFailsWithHint()
    {
        var exception = Assert.Throws<JsonSerializationException>(
            () => JsonConvert.DeserializeObject<HasClosedShape>("""{"Value":{"Radius":3.0}}"""));

        Assert.Contains("InferClosedTypePolymorphism", exception.Message);
    }

    // inference owns the $type slot for a closed base, otherwise Auto would emit an assembly
    // qualified name for exactly the values this feature exists to shorten
    [Fact]
    public void InferenceWinsOverTypeNameHandlingAuto()
    {
        var settings = Infer;
        settings.TypeNameHandling = TypeNameHandling.Auto;

        var json = JsonConvert.SerializeObject(new HasEvent {Event = new PaymentAuthorized("p-123", 42.5m)}, settings);

        Assert.Equal("""{"Event":{"$type":"PaymentAuthorized","Amount":42.5,"PaymentId":"p-123"}}""", json);
        Assert.Equal(
            new PaymentAuthorized("p-123", 42.5m),
            JsonConvert.DeserializeObject<HasEvent>(json, settings)!.Event);
    }

    // a non closed type is untouched by inference and still uses the binder
    [Fact]
    public void NonClosedTypeStillUsesTypeNameHandling()
    {
        var settings = Infer;
        settings.TypeNameHandling = TypeNameHandling.Auto;

        var json = JsonConvert.SerializeObject(new HasPlain {Value = new PlainDerived("p")}, settings);

        Assert.Contains("PlainDerived, ArgonTests", json);
    }

    [Fact]
    public void GenericClosedHierarchy()
    {
        var json = JsonConvert.SerializeObject(new HasContainer {Value = new Wrapper<int>(7)}, Infer);

        Assert.Contains(@"""$type"":""Wrapper""", json);
        Assert.Equal(new Wrapper<int>(7), JsonConvert.DeserializeObject<HasContainer>(json, Infer)!.Value);
    }

    // the default metadata handling only inspects the first property, which is where the writer puts
    // the discriminator. ReadAhead is what lifts that restriction
    [Fact]
    public void DiscriminatorMustBeFirstPropertyUnderDefaultHandling()
    {
        var json = """{"Event":{"Amount":42.5,"PaymentId":"p-123","$type":"PaymentAuthorized"}}""";

        Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<HasEvent>(json, Infer));

        var readAhead = Infer;
        readAhead.MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead;

        Assert.Equal(
            new PaymentAuthorized("p-123", 42.5m),
            JsonConvert.DeserializeObject<HasEvent>(json, readAhead)!.Event);
    }

    public class HasEvent
    {
        public PaymentEvent? Event { get; set; }
    }

    public class HasNode
    {
        public Node? Node { get; set; }
    }

    public class HasRoot
    {
        public Root? Root { get; set; }
    }

    public class HasClosedShape
    {
        public ClosedShape? Value { get; set; }
    }

    public class HasAmbiguous
    {
        public AmbiguousBase? Value { get; set; }
    }

    public class HasDecoy
    {
        public DecoyBase? Value { get; set; }
    }

    public class HasPlain
    {
        public PlainBase? Value { get; set; }
    }

    public class HasContainer
    {
        public Container<int>? Value { get; set; }
    }
}

namespace ClosedTypeFixtures
{
    // every closed type here is implicitly abstract; only the sealed leaves can be instantiated
    public closed record class PaymentEvent(string PaymentId);

    public sealed record class PaymentAuthorized(string PaymentId, decimal Amount) : PaymentEvent(PaymentId);

    public sealed record class PaymentCaptured(string PaymentId, string Reference) : PaymentEvent(PaymentId);

    public closed record class ClosedShape;

    public sealed record class ClosedCircle(double Radius) : ClosedShape;

    public closed record class Node;

    // an intermediate closed type, expanded through rather than given a discriminator of its own
    public closed record class Branch : Node;

    public sealed record class NamedLeaf(string Name) : Branch;

    public closed record class Root;

    // not closed, so this is where inference stops
    public record class OpenBranch : Root;

    public sealed record class OpenLeaf : OpenBranch;

    public abstract record class PlainBase(string Id);

    public sealed record class PlainDerived(string Id) : PlainBase(Id);

    public closed record class Container<T>(T Value);

    public sealed record class Wrapper<T>(T Value) : Container<T>(Value);

    public closed record class AmbiguousBase;

    [MyCompany.IsClosedType(DerivedTypes = [typeof(DecoyDerived)])]
    public record class DecoyBase;

    public sealed record class DecoyDerived : DecoyBase;
}

namespace ClosedTypeFixtures.Discriminators.First
{
    public sealed record class Circle : AmbiguousBase;
}

namespace ClosedTypeFixtures.Discriminators.Second
{
    public sealed record class Circle : AmbiguousBase;
}

namespace MyCompany
{
    // the same simple name as the compiler marker but a different namespace, so it must not be
    // treated as a closed type
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class IsClosedTypeAttribute : Attribute
    {
        public Type[] DerivedTypes { get; set; } = [];
    }
}
