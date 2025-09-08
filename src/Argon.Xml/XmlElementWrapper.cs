class XmlElementWrapper(XmlElement element) :
    XmlNodeWrapper(element),
    IXmlElement
{
    public void SetAttributeNode(IXmlNode attribute)
    {
        var xmlAttributeWrapper = (XmlNodeWrapper) attribute;

        element.SetAttributeNode((XmlAttribute) xmlAttributeWrapper.WrappedNode);
    }

    public string GetPrefixOfNamespace(string namespaceUri) =>
        element.GetPrefixOfNamespace(namespaceUri);

    public bool IsEmpty => element.IsEmpty;
}