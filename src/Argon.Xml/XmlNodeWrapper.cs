class XmlNodeWrapper(XmlNode node) :
    IXmlNode
{
    XmlNode node = node;
    List<IXmlNode>? childNodes;
    List<IXmlNode>? attributes;

    public object WrappedNode => node;

    public XmlNodeType NodeType => node.NodeType;

    public virtual string LocalName => node.LocalName;

    public List<IXmlNode> ChildNodes
    {
        get
        {
            // childnodes is read multiple times
            // cache results to prevent multiple reads which kills perf in large documents
            if (childNodes == null)
            {
                if (node.HasChildNodes)
                {
                    childNodes = new(node.ChildNodes.Count);
                    foreach (XmlNode childNode in node.ChildNodes)
                    {
                        childNodes.Add(WrapNode(childNode));
                    }
                }
                else
                {
                    childNodes = XmlNodeConverter.EmptyChildNodes;
                }
            }

            return childNodes;
        }
    }

    internal static IXmlNode WrapNode(XmlNode node)
    {
        switch (node.NodeType)
        {
            case XmlNodeType.Element:
                return new XmlElementWrapper((XmlElement) node);
            case XmlNodeType.XmlDeclaration:
                return new XmlDeclarationWrapper((XmlDeclaration) node);
            case XmlNodeType.DocumentType:
                return new XmlDocumentTypeWrapper((XmlDocumentType) node);
            default:
                return new XmlNodeWrapper(node);
        }
    }

    public List<IXmlNode> Attributes
    {
        get
        {
            // attributes is read multiple times
            // cache results to prevent multiple reads which kills perf in large documents
            if (attributes == null)
            {
                if (HasAttributes)
                {
                    attributes = new(node.Attributes!.Count);
                    foreach (XmlAttribute attribute in node.Attributes)
                    {
                        attributes.Add(WrapNode(attribute));
                    }
                }
                else
                {
                    attributes = XmlNodeConverter.EmptyChildNodes;
                }
            }

            return attributes;
        }
    }

    bool HasAttributes
    {
        get
        {
            if (node is XmlElement element)
            {
                return element.HasAttributes;
            }

            return node.Attributes?.Count > 0;
        }
    }

    public IXmlNode? ParentNode
    {
        get
        {
            var node = this.node is XmlAttribute attribute ? attribute.OwnerElement : this.node.ParentNode;

            if (node == null)
            {
                return null;
            }

            return WrapNode(node);
        }
    }

    public string Value
    {
        get => node.Value!;
        set => node.Value = value;
    }

    public IXmlNode AppendChild(IXmlNode newChild)
    {
        var xmlNodeWrapper = (XmlNodeWrapper) newChild;
        node.AppendChild(xmlNodeWrapper.node);
        childNodes = null;
        attributes = null;

        return newChild;
    }

    public string NamespaceUri => node.NamespaceURI;
}