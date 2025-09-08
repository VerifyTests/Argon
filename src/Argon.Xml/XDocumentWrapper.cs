class XDocumentWrapper(XDocument document) :
    XContainerWrapper(document),
    IXmlDocument
{
    XDocument Document => (XDocument) WrappedNode!;

    public override List<IXmlNode> ChildNodes
    {
        get
        {
            var childNodes = base.ChildNodes;
            if (Document.Declaration != null && (childNodes.Count == 0 || childNodes[0].NodeType != XmlNodeType.XmlDeclaration))
            {
                childNodes.Insert(0, new XDeclarationWrapper(Document.Declaration));
            }

            return childNodes;
        }
    }

    protected override bool HasChildNodes
    {
        get
        {
            if (base.HasChildNodes)
            {
                return true;
            }

            return Document.Declaration != null;
        }
    }

    public IXmlNode CreateComment(string text) =>
        new XObjectWrapper(new XComment(text));

    public IXmlNode CreateTextNode(string text) =>
        new XObjectWrapper(new XText(text));

    public IXmlNode CreateCDataSection(string data) =>
        new XObjectWrapper(new XCData(data));

    public IXmlNode CreateWhitespace(string text) =>
        new XObjectWrapper(new XText(text));

    public IXmlNode CreateSignificantWhitespace(string text) =>
        new XObjectWrapper(new XText(text));

    public IXmlNode CreateXmlDeclaration(string? version, string? encoding, string? standalone) =>
        new XDeclarationWrapper(new(version, encoding, standalone));

    public IXmlNode CreateXmlDocumentType(string name, string? publicId, string? systemId, string? internalSubset) =>
        new XDocumentTypeWrapper(new(name, publicId, systemId, internalSubset!));

    public IXmlNode CreateProcessingInstruction(string target, string data) =>
        new XProcessingInstructionWrapper(new(target, data));

    public IXmlElement CreateElement(string elementName) =>
        new XElementWrapper(new(elementName));

    public IXmlElement CreateElement(string qualifiedName, string namespaceUri)
    {
        var localName = XmlUtils.GetLocalName(qualifiedName);
        return new XElementWrapper(new(XName.Get(localName, namespaceUri)));
    }

    public IXmlNode CreateAttribute(string name, string value) =>
        new XAttributeWrapper(new(name, value));

    public IXmlNode CreateAttribute(string qualifiedName, string namespaceUri, string value)
    {
        var localName = XmlUtils.GetLocalName(qualifiedName);
        return new XAttributeWrapper(new(XName.Get(localName, namespaceUri), value));
    }

    public IXmlElement? DocumentElement
    {
        get
        {
            if (Document.Root == null)
            {
                return null;
            }

            return new XElementWrapper(Document.Root);
        }
    }

    public override IXmlNode AppendChild(IXmlNode newChild)
    {
        if (newChild is XDeclarationWrapper declarationWrapper)
        {
            Document.Declaration = declarationWrapper.Declaration;
            return declarationWrapper;
        }

        return base.AppendChild(newChild);
    }
}