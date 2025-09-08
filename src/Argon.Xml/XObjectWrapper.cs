class XObjectWrapper(XObject? o) :
    IXmlNode
{
    public object? WrappedNode => o;

    public virtual XmlNodeType NodeType => o?.NodeType ?? XmlNodeType.None;

    public virtual string LocalName => null!;

    public virtual List<IXmlNode> ChildNodes => XmlNodeConverter.EmptyChildNodes;

    public virtual List<IXmlNode> Attributes => XmlNodeConverter.EmptyChildNodes;

    public virtual IXmlNode? ParentNode => null;

    public virtual string Value
    {
        get => throw new InvalidOperationException();
        set => throw new InvalidOperationException();
    }

    public virtual IXmlNode AppendChild(IXmlNode newChild) =>
        throw new InvalidOperationException();

    public virtual string? NamespaceUri => null;
}