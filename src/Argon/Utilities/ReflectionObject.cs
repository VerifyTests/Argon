// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

class ReflectionObject
{
    public ObjectConstructor? Creator { get; }
    public IReadOnlyDictionary<string, ReflectionMember> Members { get; }

    ReflectionObject(ObjectConstructor? creator, IReadOnlyDictionary<string, ReflectionMember> members)
    {
        Members = members;
        Creator = creator;
    }

    public object? GetValue(object target, string member)
    {
        var getter = Members[member].Getter!;
        return getter(target);
    }

    public Type GetType(string member) =>
        Members[member].MemberType!;

    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    public static ReflectionObject Create(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.NonPublicConstructors |
            DynamicallyAccessedMemberTypes.PublicConstructors |
            DynamicallyAccessedMemberTypes.PublicEvents |
            DynamicallyAccessedMemberTypes.PublicFields |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.PublicNestedTypes |
            DynamicallyAccessedMemberTypes.PublicProperties)]
        Type type, MethodBase? creator, params string[] memberNames)
    {
        var creatorConstructor = CreatorConstructor(type, creator);

        var memberLookup = new Dictionary<string, ReflectionMember>();

        foreach (var memberName in memberNames)
        {
            var members = type.GetMember(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.SetField);
            if (members.Length != 1)
            {
                throw new ArgumentException($"Expected a single member with the name '{memberName}'.");
            }

            var member = members[0];

            var reflectionMember = new ReflectionMember();

            switch (member.MemberType)
            {
                case MemberTypes.Field:
                case MemberTypes.Property:
                    if (member.CanReadMemberValue(false))
                    {
                        reflectionMember.Getter = DelegateFactory.CreateGet<object>(member);
                    }

                    break;
                case MemberTypes.Method:
                    var method = (MethodInfo) member;
                    var parameters = method.GetParameters();
                    if (parameters.Length == 0 &&
                        method.ReturnType != typeof(void))
                    {
                        var call = DelegateFactory.CreateMethodCall<object>(method);
                        reflectionMember.Getter = target => call(target);
                    }

                    break;
                default:
                    throw new ArgumentException($"Unexpected member type '{member.MemberType}' for member '{member.Name}'.");
            }

            reflectionMember.MemberType = member.GetMemberUnderlyingType();

            memberLookup[memberName] = reflectionMember;
        }

        return new(creatorConstructor, memberLookup.ToFrozenDictionary());
    }

    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    static ObjectConstructor? CreatorConstructor(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        Type type,
        MethodBase? creator)
    {
        if (creator == null)
        {
            if (type.HasDefaultConstructor())
            {
                var ctor = DelegateFactory.CreateDefaultConstructor<object>(type);

                return  _ => ctor();
            }
        }
        else
        {
            return DelegateFactory.CreateParameterizedConstructor(creator);
        }

        return null;
    }
}