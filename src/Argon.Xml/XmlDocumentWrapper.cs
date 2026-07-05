class XmlDocumentWrapper(XmlDocument document) :
    XmlNodeWrapper(document),
    IXmlDocument
{
    public IXmlNode CreateComment(string? data) =>
        new XmlNodeWrapper(document.CreateComment(data));

    public IXmlNode CreateTextNode(string? text) =>
        new XmlNodeWrapper(document.CreateTextNode(text));

    public IXmlNode CreateCDataSection(string? data) =>
        new XmlNodeWrapper(document.CreateCDataSection(data));

    public IXmlNode CreateWhitespace(string? text) =>
        new XmlNodeWrapper(document.CreateWhitespace(text));

    public IXmlNode CreateSignificantWhitespace(string? text) =>
        new XmlNodeWrapper(document.CreateSignificantWhitespace(text));

    public IXmlNode CreateXmlDeclaration(string version, string? encoding, string? standalone) =>
        new XmlDeclarationWrapper(document.CreateXmlDeclaration(version, encoding, standalone));

    public IXmlNode CreateXmlDocumentType(string name, string? publicId, string? systemId, string? internalSubset) =>
        new XmlDocumentTypeWrapper(document.CreateDocumentType(name, publicId, systemId, internalSubset));

    public IXmlNode CreateProcessingInstruction(string target, string data) =>
        new XmlNodeWrapper(document.CreateProcessingInstruction(target, data));

    public IXmlElement CreateElement(string elementName) =>
        new XmlElementWrapper(document.CreateElement(elementName));

    public IXmlElement CreateElement(string qualifiedName, string namespaceUri) =>
        new XmlElementWrapper(document.CreateElement(qualifiedName, namespaceUri));

    public IXmlNode CreateAttribute(string name, string value)
    {
        var attribute = new XmlNodeWrapper(document.CreateAttribute(name))
        {
            Value = value
        };

        return attribute;
    }

    public IXmlNode CreateAttribute(string qualifiedName, string namespaceUri, string value)
    {
        var attribute = new XmlNodeWrapper(document.CreateAttribute(qualifiedName, namespaceUri))
        {
            Value = value
        };

        return attribute;
    }

    public IXmlElement? DocumentElement
    {
        get
        {
            if (document.DocumentElement == null)
            {
                return null;
            }

            return new XmlElementWrapper(document.DocumentElement);
        }
    }
}