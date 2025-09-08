
// ReSharper disable ReturnTypeCanBeNotNullable

class XmlDocumentTypeWrapper(XmlDocumentType type) :
    XmlNodeWrapper(type),
    IXmlDocumentType
{
    public string Name => type.Name;

    public string? System => type.SystemId;

    public string? Public => type.PublicId;

    public string? InternalSubset => type.InternalSubset;

    public override string LocalName => "DOCTYPE";
}