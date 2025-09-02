// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Contract details for a <see cref="Type" /> used by the <see cref="JsonSerializer" />.
/// </summary>
[RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
[RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
public class JsonDynamicContract : JsonContainerContract
{
    /// <summary>
    /// Gets the object's properties.
    /// </summary>
    public JsonPropertyCollection Properties { get; }

    /// <summary>
    /// Gets or sets the property name resolver.
    /// </summary>
    public Func<JsonWriter, string, string>? PropertyNameResolver { get; set; }

    static ThreadSafeStore<string, CallSite<Func<CallSite, object, object>>> callSiteGetters =
        new(CreateCallSiteGetter);

    static ThreadSafeStore<string, CallSite<Func<CallSite, object, object?, object>>> callSiteSetters =
        new(CreateCallSiteSetter);

    static CallSite<Func<CallSite, object, object>> CreateCallSiteGetter(string name)
    {
        var getMemberBinder = (GetMemberBinder) DynamicUtils.BinderWrapper.GetMember(name, typeof(DynamicUtils));

        return CallSite<Func<CallSite, object, object>>.Create(new NoThrowGetBinderMember(getMemberBinder));
    }

    static CallSite<Func<CallSite, object, object?, object>> CreateCallSiteSetter(string name)
    {
        var binder = (SetMemberBinder) DynamicUtils.BinderWrapper.SetMember(name, typeof(DynamicUtils));

        return CallSite<Func<CallSite, object, object?, object>>.Create(new NoThrowSetBinderMember(binder));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDynamicContract" /> class.
    /// </summary>
    public JsonDynamicContract(Type underlyingType)
        : base(underlyingType)
    {
        ContractType = JsonContractType.Dynamic;

        Properties = new(UnderlyingType);
    }

    internal static bool TryGetMember(IDynamicMetaObjectProvider dynamicProvider, string name, out object? value)
    {
        var callSite = callSiteGetters.Get(name);

        var result = callSite.Target(callSite, dynamicProvider);

        if (ReferenceEquals(result, NoThrowExpressionVisitor.ErrorResult))
        {
            value = null;
            return false;
        }

        value = result;
        return true;
    }

    internal static bool TrySetMember(IDynamicMetaObjectProvider dynamicProvider, string name, object? value)
    {
        var callSite = callSiteSetters.Get(name);

        var result = callSite.Target(callSite, dynamicProvider, value);

        return !ReferenceEquals(result, NoThrowExpressionVisitor.ErrorResult);
    }
}