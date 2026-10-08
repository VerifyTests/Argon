namespace Argon;

class ReflectionMember
{
    public Type? MemberType { get; set; }
    public Func<object, object?>? Getter { get; set; }
}