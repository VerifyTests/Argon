class XDeclarationWrapper(XDeclaration declaration) :
    XObjectWrapper(null),
    IXmlDeclaration
{
    internal XDeclaration Declaration { get; } = declaration;

    public override XmlNodeType NodeType => XmlNodeType.XmlDeclaration;

    public string? Version => Declaration.Version;

    public string? Encoding
    {
        get => Declaration.Encoding;
        set => Declaration.Encoding = value;
    }

    public string? Standalone
    {
        get => Declaration.Standalone;
        set => Declaration.Standalone = value;
    }
}