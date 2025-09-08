class XmlDeclarationWrapper(XmlDeclaration declaration) :
    XmlNodeWrapper(declaration),
    IXmlDeclaration
{
    public string Version => declaration.Version;

    public string? Encoding
    {
        get => declaration.Encoding;
        set => declaration.Encoding = value;
    }

    public string? Standalone
    {
        get => declaration.Standalone;
        set => declaration.Standalone = value;
    }
}