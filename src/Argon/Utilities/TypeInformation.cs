namespace Argon;

class TypeInformation(Type type, PrimitiveTypeCode typeCode)
{
    public Type Type { get; } = type;
    public PrimitiveTypeCode TypeCode { get; } = typeCode;
}