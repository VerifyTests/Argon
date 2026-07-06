// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Converts XML to and from JSON.
/// </summary>
public class XmlNodeConverter :
    JsonConverter
{
    internal static readonly List<IXmlNode> EmptyChildNodes = [];

    const string textName = "#text";
    const string commentName = "#comment";
    const string cDataName = "#cdata-section";
    const string whitespaceName = "#whitespace";
    const string significantWhitespaceName = "#significant-whitespace";
    const string declarationName = "?xml";
    const string jsonNamespaceUri = "http://james.newtonking.com/projects/json";

    /// <summary>
    /// Gets or sets the name of the root element to insert when deserializing to XML if the JSON structure has produced multiple root elements.
    /// </summary>
    public string? DeserializeRootElementName { get; set; }

    /// <summary>
    /// Gets or sets a value to indicate whether to write the Json.NET array attribute.
    /// This attribute helps preserve arrays when converting the written XML back to JSON.
    /// </summary>
    public bool WriteArrayAttribute { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to write the root JSON object.
    /// </summary>
    public bool OmitRootObject { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to encode special characters when converting JSON to XML.
    /// If <c>true</c>, special characters like ':', '@', '?', '#' and '$' in JSON property names aren't used to specify
    /// XML namespaces, attributes or processing directives. Instead special characters are encoded and written
    /// as part of the XML element name.
    /// </summary>
    public bool EncodeSpecialCharacters { get; set; }

    /// <summary>
    /// Writes the JSON representation of the object.
    /// </summary>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var node = WrapXml(value);

        var manager = new XmlNamespaceManager(new NameTable());
        PushParentNamespaces(node, manager);

        if (!OmitRootObject)
        {
            writer.WriteStartObject();
        }

        SerializeNode(writer, node, manager, !OmitRootObject);

        if (!OmitRootObject)
        {
            writer.WriteEndObject();
        }
    }

    static IXmlNode WrapXml(object value)
    {
        if (value is XObject xObject)
        {
            return XContainerWrapper.WrapNode(xObject);
        }

        if (value is XmlNode node)
        {
            return XmlNodeWrapper.WrapNode(node);
        }

        throw new ArgumentException("Value must be an XML object.", nameof(value));
    }

    static void PushParentNamespaces(IXmlNode node, XmlNamespaceManager manager)
    {
        List<IXmlNode>? parentElements = null;

        var parent = node;
        while ((parent = parent.ParentNode) != null)
        {
            if (parent.NodeType == XmlNodeType.Element)
            {
                parentElements ??= [];

                parentElements.Add(parent);
            }
        }

        if (parentElements != null)
        {
            parentElements.Reverse();

            foreach (var parentElement in parentElements)
            {
                manager.PushScope();
                foreach (var attribute in parentElement.Attributes)
                {
                    if (attribute.NamespaceUri == "http://www.w3.org/2000/xmlns/" && attribute.LocalName != "xmlns")
                    {
                        manager.AddNamespace(attribute.LocalName, attribute.Value);
                    }
                }
            }
        }
    }

    static string ResolveFullName(IXmlNode node, XmlNamespaceManager manager)
    {
        var prefix = node.NamespaceUri == null || node is {LocalName: "xmlns", NamespaceUri: "http://www.w3.org/2000/xmlns/"}
            ? null
            : manager.LookupPrefix(node.NamespaceUri);

        if (prefix.IsNullOrEmpty())
        {
            return XmlConvert.DecodeName(node.LocalName);
        }

        return $"{prefix}:{XmlConvert.DecodeName(node.LocalName)}";
    }

    static string GetPropertyName(IXmlNode node, XmlNamespaceManager manager)
    {
        switch (node.NodeType)
        {
            case XmlNodeType.Attribute:
                if (node.NamespaceUri == jsonNamespaceUri)
                {
                    return $"${node.LocalName}";
                }

                return $"@{ResolveFullName(node, manager)}";
            case XmlNodeType.CDATA:
                return cDataName;
            case XmlNodeType.Comment:
                return commentName;
            case XmlNodeType.Element:
                if (node.NamespaceUri == jsonNamespaceUri)
                {
                    return $"${node.LocalName}";
                }

                return ResolveFullName(node, manager);
            case XmlNodeType.ProcessingInstruction:
                return $"?{ResolveFullName(node, manager)}";
            case XmlNodeType.DocumentType:
                return $"!{ResolveFullName(node, manager)}";
            case XmlNodeType.XmlDeclaration:
                return declarationName;
            case XmlNodeType.SignificantWhitespace:
                return significantWhitespaceName;
            case XmlNodeType.Text:
                return textName;
            case XmlNodeType.Whitespace:
                return whitespaceName;
            default:
                throw new JsonSerializationException($"Unexpected XmlNodeType when getting node name: {node.NodeType}");
        }
    }

    static bool IsArray(IXmlNode node)
    {
        foreach (var attribute in node.Attributes)
        {
            if (attribute is {LocalName: "Array", NamespaceUri: jsonNamespaceUri})
            {
                return XmlConvert.ToBoolean(attribute.Value);
            }
        }

        return false;
    }

    void SerializeGroupedNodes(JsonWriter writer, IXmlNode node, XmlNamespaceManager manager, bool writePropertyName)
    {
        switch (node.ChildNodes.Count)
        {
            case 0:
            {
                // nothing to serialize
                break;
            }
            case 1:
            {
                // avoid grouping when there is only one node
                var nodeName = GetPropertyName(node.ChildNodes[0], manager);
                WriteGroupedNodes(writer, manager, writePropertyName, node.ChildNodes, nodeName);
                break;
            }
            default:
            {
                // check whether nodes have the same name
                // if they don't then group into dictionary together by name

                // value of dictionary will be a single IXmlNode when there is one for a name,
                // or a List<IXmlNode> when there are multiple
                Dictionary<string, object>? nodesGroupedByName = null;

                string? nodeName = null;

                for (var i = 0; i < node.ChildNodes.Count; i++)
                {
                    var childNode = node.ChildNodes[i];
                    var currentNodeName = GetPropertyName(childNode, manager);

                    if (nodesGroupedByName == null)
                    {
                        if (nodeName == null)
                        {
                            nodeName = currentNodeName;
                        }
                        else if (currentNodeName == nodeName)
                        {
                            // current node name matches others
                        }
                        else
                        {
                            nodesGroupedByName = [];
                            if (i > 1)
                            {
                                var nodes = new List<IXmlNode>(i);
                                for (var j = 0; j < i; j++)
                                {
                                    nodes.Add(node.ChildNodes[j]);
                                }

                                nodesGroupedByName.Add(nodeName, nodes);
                            }
                            else
                            {
                                nodesGroupedByName.Add(nodeName, node.ChildNodes[0]);
                            }

                            nodesGroupedByName.Add(currentNodeName, childNode);
                        }
                    }
                    else
                    {
                        if (nodesGroupedByName.TryGetValue(currentNodeName, out var value))
                        {
                            if (value is not List<IXmlNode> nodes)
                            {
                                nodes = [(IXmlNode) value];
                                nodesGroupedByName[currentNodeName] = nodes;
                            }

                            nodes.Add(childNode);
                        }
                        else
                        {
                            nodesGroupedByName.Add(currentNodeName, childNode);
                        }
                    }
                }

                if (nodesGroupedByName == null)
                {
                    WriteGroupedNodes(writer, manager, writePropertyName, node.ChildNodes, nodeName!);
                }
                else
                {
                    // loop through grouped nodes. write single name instances as normal,
                    // write multiple names together in an array
                    foreach (var nodeNameGroup in nodesGroupedByName)
                    {
                        if (nodeNameGroup.Value is List<IXmlNode> nodes)
                        {
                            WriteGroupedNodes(writer, manager, writePropertyName, nodes, nodeNameGroup.Key);
                        }
                        else
                        {
                            WriteGroupedNodes(writer, manager, writePropertyName, (IXmlNode) nodeNameGroup.Value, nodeNameGroup.Key);
                        }
                    }
                }

                break;
            }
        }
    }

    void WriteGroupedNodes(JsonWriter writer, XmlNamespaceManager manager, bool writePropertyName, List<IXmlNode> groupedNodes, string elementNames)
    {
        var writeArray = groupedNodes.Count != 1 || IsArray(groupedNodes[0]);

        if (writeArray)
        {
            // Comments share the name "#comment" and cannot be array values. Write each as an
            // inline JSON comment (which round-trips back to XML comments) instead of an empty
            // array that silently drops the comment text.
            if (groupedNodes[0].NodeType == XmlNodeType.Comment)
            {
                foreach (var node in groupedNodes)
                {
                    writer.WriteComment(node.Value);
                }

                return;
            }

            if (writePropertyName)
            {
                writer.WritePropertyName(elementNames);
            }

            writer.WriteStartArray();

            foreach (var node in groupedNodes)
            {
                SerializeNode(writer, node, manager, false);
            }

            writer.WriteEndArray();
        }
        else
        {
            SerializeNode(writer, groupedNodes[0], manager, writePropertyName);
        }
    }

    void WriteGroupedNodes(JsonWriter writer, XmlNamespaceManager manager, bool writePropertyName, IXmlNode node, string elementNames)
    {
        var writeArray = IsArray(node);

        if (writeArray)
        {
            if (writePropertyName)
            {
                writer.WritePropertyName(elementNames);
            }

            writer.WriteStartArray();

            SerializeNode(writer, node, manager, false);

            writer.WriteEndArray();
        }
        else
        {
            SerializeNode(writer, node, manager, writePropertyName);
        }
    }

    void SerializeNode(JsonWriter writer, IXmlNode node, XmlNamespaceManager manager, bool writePropertyName)
    {
        switch (node.NodeType)
        {
            case XmlNodeType.Document:
            case XmlNodeType.DocumentFragment:
                SerializeGroupedNodes(writer, node, manager, writePropertyName);
                break;
            case XmlNodeType.Element:
                if (IsArray(node) &&
                    AllSameName(node) &&
                    node.ChildNodes.Count > 0)
                {
                    SerializeGroupedNodes(writer, node, manager, false);
                }
                else
                {
                    manager.PushScope();

                    foreach (var attribute in node.Attributes)
                    {
                        if (attribute.NamespaceUri == "http://www.w3.org/2000/xmlns/")
                        {
                            var namespacePrefix = attribute.LocalName == "xmlns"
                                ? string.Empty
                                : XmlConvert.DecodeName(attribute.LocalName);
                            var namespaceUri = attribute.Value;
                            if (namespaceUri == null)
                            {
                                throw new JsonSerializationException("Namespace attribute must have a value.");
                            }

                            manager.AddNamespace(namespacePrefix, namespaceUri);
                        }
                    }

                    if (writePropertyName)
                    {
                        writer.WritePropertyName(GetPropertyName(node, manager));
                    }

                    if (!ValueAttributes(node.Attributes) &&
                        node.ChildNodes is [{NodeType: XmlNodeType.Text}])
                    {
                        // write elements with a single text child as a name value pair
                        writer.WriteValue(node.ChildNodes[0].Value);
                    }
                    else if (node.ChildNodes.Count == 0 &&
                             node.Attributes.Count == 0)
                    {
                        var element = (IXmlElement) node;

                        // empty element
                        if (element.IsEmpty)
                        {
                            writer.WriteNull();
                        }
                        else
                        {
                            writer.WriteValue(string.Empty);
                        }
                    }
                    else
                    {
                        writer.WriteStartObject();

                        foreach (var attribute in node.Attributes)
                        {
                            SerializeNode(writer, attribute, manager, true);
                        }

                        SerializeGroupedNodes(writer, node, manager, true);

                        writer.WriteEndObject();
                    }

                    manager.PopScope();
                }

                break;
            case XmlNodeType.Comment:
                if (writePropertyName)
                {
                    writer.WriteComment(node.Value);
                }

                break;
            case XmlNodeType.Attribute:
            case XmlNodeType.Text:
            case XmlNodeType.CDATA:
            case XmlNodeType.ProcessingInstruction:
            case XmlNodeType.Whitespace:
            case XmlNodeType.SignificantWhitespace:
                if (node is {NamespaceUri: "http://www.w3.org/2000/xmlns/", Value: jsonNamespaceUri})
                {
                    return;
                }

                if (node is {NamespaceUri: jsonNamespaceUri, LocalName: "Array"})
                {
                    return;
                }

                if (writePropertyName)
                {
                    writer.WritePropertyName(GetPropertyName(node, manager));
                }

                writer.WriteValue(node.Value);
                break;
            case XmlNodeType.XmlDeclaration:
                var declaration = (IXmlDeclaration) node;
                writer.WritePropertyName(GetPropertyName(node, manager));
                writer.WriteStartObject();

                if (!declaration.Version.IsNullOrEmpty())
                {
                    writer.WritePropertyName("@version");
                    writer.WriteValue(declaration.Version);
                }

                if (!declaration.Encoding.IsNullOrEmpty())
                {
                    writer.WritePropertyName("@encoding");
                    writer.WriteValue(declaration.Encoding);
                }

                if (!declaration.Standalone.IsNullOrEmpty())
                {
                    writer.WritePropertyName("@standalone");
                    writer.WriteValue(declaration.Standalone);
                }

                writer.WriteEndObject();
                break;
            case XmlNodeType.DocumentType:
                var documentType = (IXmlDocumentType) node;
                writer.WritePropertyName(GetPropertyName(node, manager));
                writer.WriteStartObject();

                if (!documentType.Name.IsNullOrEmpty())
                {
                    writer.WritePropertyName("@name");
                    writer.WriteValue(documentType.Name);
                }

                if (!documentType.Public.IsNullOrEmpty())
                {
                    writer.WritePropertyName("@public");
                    writer.WriteValue(documentType.Public);
                }

                if (!documentType.System.IsNullOrEmpty())
                {
                    writer.WritePropertyName("@system");
                    writer.WriteValue(documentType.System);
                }

                if (!documentType.InternalSubset.IsNullOrEmpty())
                {
                    writer.WritePropertyName("@internalSubset");
                    writer.WriteValue(documentType.InternalSubset);
                }

                writer.WriteEndObject();
                break;
            default:
                throw new JsonSerializationException($"Unexpected XmlNodeType when serializing nodes: {node.NodeType}");
        }
    }

    static bool AllSameName(IXmlNode node)
    {
        foreach (var childNode in node.ChildNodes)
        {
            if (childNode.LocalName != node.LocalName)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Reads the JSON representation of the object.
    /// </summary>
    public override object? ReadJson(JsonReader reader, Type type, object? existingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.Null:
                return null;
            case JsonToken.StartObject:
                break;
            default:
                throw JsonSerializationException.Create(reader, "XmlNodeConverter can only convert JSON that begins with an object.");
        }

        var manager = new XmlNamespaceManager(new NameTable());
        IXmlDocument? document = null;
        IXmlNode? rootNode = null;

        if (type.IsAssignableTo<XObject>())
        {
            if (type != typeof(XContainer)
                && type != typeof(XDocument)
                && type != typeof(XElement)
                && type != typeof(XNode)
                && type != typeof(XObject))
            {
                throw JsonSerializationException.Create(reader, "XmlNodeConverter only supports deserializing XDocument, XElement, XContainer, XNode or XObject.");
            }

            var d = new XDocument();
            document = new XDocumentWrapper(d);
            rootNode = document;
        }

        if (type.IsAssignableTo<XmlNode>())
        {
            if (type != typeof(XmlDocument)
                && type != typeof(XmlElement)
                && type != typeof(XmlNode))
            {
                throw JsonSerializationException.Create(reader, "XmlNodeConverter only supports deserializing XmlDocument, XmlElement or XmlNode.");
            }

            var d = new XmlDocument
            {
                // prevent http request when resolving any DTD references
                XmlResolver = null!
            };

            document = new XmlDocumentWrapper(d);
            rootNode = document;
        }

        if (document == null || rootNode == null)
        {
            throw JsonSerializationException.Create(reader, $"Unexpected type when converting XML: {type}");
        }

        if (DeserializeRootElementName.IsNullOrEmpty())
        {
            reader.ReadAndAssert();
            DeserializeNode(reader, document, manager, rootNode);
        }
        else
        {
            ReadElement(reader, document, rootNode, DeserializeRootElementName, manager);
        }

        if (type == typeof(XElement))
        {
            var element = (XElement) document.DocumentElement!.WrappedNode!;
            element.Remove();

            return element;
        }

        if (type == typeof(XmlElement))
        {
            return document.DocumentElement!.WrappedNode;
        }

        return document.WrappedNode;
    }

    void DeserializeValue(JsonReader reader, IXmlDocument document, XmlNamespaceManager manager, string propertyName, IXmlNode currentNode)
    {
        if (!EncodeSpecialCharacters)
        {
            switch (propertyName)
            {
                case textName:
                    currentNode.AppendChild(document.CreateTextNode(ConvertTokenToXmlValue(reader) ?? ""));
                    return;
                case cDataName:
                    currentNode.AppendChild(document.CreateCDataSection(ConvertTokenToXmlValue(reader) ?? ""));
                    return;
                case whitespaceName:
                    currentNode.AppendChild(document.CreateWhitespace(ConvertTokenToXmlValue(reader) ?? ""));
                    return;
                case significantWhitespaceName:
                    currentNode.AppendChild(document.CreateSignificantWhitespace(ConvertTokenToXmlValue(reader) ?? ""));
                    return;
                default:
                    // processing instructions and the xml declaration start with ?
                    if (!propertyName.IsNullOrEmpty() && propertyName[0] == '?')
                    {
                        CreateInstruction(reader, document, currentNode, propertyName);
                        return;
                    }

                    if (string.Equals(propertyName, "!DOCTYPE", StringComparison.OrdinalIgnoreCase))
                    {
                        CreateDocumentType(reader, document, currentNode);
                        return;
                    }

                    break;
            }
        }

        if (reader.TokenType == JsonToken.StartArray)
        {
            // handle nested arrays
            ReadArrayElements(reader, document, propertyName, currentNode, manager);
            return;
        }

        // have to wait until attributes have been parsed before creating element
        // attributes may contain namespace info used by the element
        ReadElement(reader, document, currentNode, propertyName, manager);
    }

    void ReadElement(JsonReader reader, IXmlDocument document, IXmlNode currentNode, string propertyName, XmlNamespaceManager manager)
    {
        if (propertyName.IsNullOrEmpty())
        {
            throw JsonSerializationException.Create(reader, "XmlNodeConverter cannot convert JSON with an empty property name to XML.");
        }

        Dictionary<string, string?>? attributeNameValues = null;
        string? elementPrefix = null;

        if (EncodeSpecialCharacters)
        {
            if (ShouldReadInto(reader))
            {
                reader.ReadAndAssert();
            }
        }
        else
        {
            attributeNameValues = ShouldReadInto(reader)
                ? ReadAttributeElements(reader, manager)
                : null;
            elementPrefix = XmlUtils.GetPrefix(propertyName);

            if (propertyName.StartsWith('@'))
            {
                var attributeName = propertyName[1..];
                var attributePrefix = XmlUtils.GetPrefix(attributeName);

                AddAttribute(reader, document, currentNode, propertyName, attributeName, manager, attributePrefix);
                return;
            }

            if (propertyName.StartsWith('$'))
            {
                switch (propertyName)
                {
                    case JsonTypeReflector.ArrayValuesPropertyName:
                        propertyName = propertyName[1..];
                        elementPrefix = manager.LookupPrefix(jsonNamespaceUri);
                        break;
                    case JsonTypeReflector.IdPropertyName:
                    case JsonTypeReflector.RefPropertyName:
                    case JsonTypeReflector.TypePropertyName:
                    case JsonTypeReflector.ValuePropertyName:
                        var attributeName = propertyName[1..];
                        var attributePrefix = manager.LookupPrefix(jsonNamespaceUri);
                        AddAttribute(reader, document, currentNode, propertyName, attributeName, manager, attributePrefix);
                        return;
                }
            }
        }

        CreateElement(reader, document, currentNode, propertyName, manager, elementPrefix, attributeNameValues);
    }

    void CreateElement(JsonReader reader, IXmlDocument document, IXmlNode currentNode, string elementName, XmlNamespaceManager manager, string? elementPrefix, Dictionary<string, string?>? attributeNameValues)
    {
        var element = CreateElement(elementName, document, elementPrefix, manager);

        currentNode.AppendChild(element);

        if (attributeNameValues != null)
        {
            // add attributes to newly created element
            foreach (var nameValue in attributeNameValues)
            {
                var encoded = XmlConvert.EncodeName(nameValue.Key);
                var attributePrefix = XmlUtils.GetPrefix(nameValue.Key);

                IXmlNode attribute;
                if (attributePrefix.IsNullOrEmpty())
                {
                    attribute = document.CreateAttribute(encoded, nameValue.Value!);
                }
                else
                {
                    attribute = document.CreateAttribute(encoded, manager.LookupNamespace(attributePrefix) ?? string.Empty, nameValue.Value!);
                }

                element.SetAttributeNode(attribute);
            }
        }

        switch (reader.TokenType)
        {
            case JsonToken.String:
            case JsonToken.Integer:
            case JsonToken.Float:
            case JsonToken.Boolean:
            case JsonToken.Date:
            case JsonToken.Bytes:
                var text = ConvertTokenToXmlValue(reader);
                if (text != null)
                {
                    element.AppendChild(document.CreateTextNode(text));
                }

                break;
            case JsonToken.Null:

                // empty element. do nothing
                break;
            case JsonToken.EndObject:

                // finished element will have no children to deserialize
                manager.RemoveNamespace(string.Empty, manager.DefaultNamespace);
                break;
            default:
                manager.PushScope();
                DeserializeNode(reader, document, manager, element);
                manager.PopScope();
                manager.RemoveNamespace(string.Empty, manager.DefaultNamespace);
                break;
        }
    }

    static void AddAttribute(JsonReader reader, IXmlDocument document, IXmlNode currentNode, string propertyName, string attributeName, XmlNamespaceManager manager, string? attributePrefix)
    {
        if (currentNode.NodeType == XmlNodeType.Document)
        {
            throw JsonSerializationException.Create(reader, $"JSON root object has property '{propertyName}' that will be converted to an attribute. A root object cannot have any attribute properties. Consider specifying a DeserializeRootElementName.");
        }

        var encodedName = XmlConvert.EncodeName(attributeName);
        var attributeValue = ConvertTokenToXmlValue(reader)!;

        var attribute = attributePrefix.IsNullOrEmpty()
            ? document.CreateAttribute(encodedName, attributeValue)
            : document.CreateAttribute(encodedName, manager.LookupNamespace(attributePrefix)!, attributeValue);

        ((IXmlElement) currentNode).SetAttributeNode(attribute);
    }

    static string? ConvertTokenToXmlValue(JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonToken.String:
                return reader.Value?.ToString();
            case JsonToken.Integer:
                if (reader.Value is BigInteger i)
                {
                    return i.ToString(InvariantCulture);
                }

                return XmlConvert.ToString(Convert.ToInt64(reader.Value, InvariantCulture));
            case JsonToken.Float:
            {
                if (reader.Value is decimal d)
                {
                    return XmlConvert.ToString(d);
                }

                if (reader.Value is float f)
                {
                    return XmlConvert.ToString(f);
                }

                return XmlConvert.ToString(Convert.ToDouble(reader.Value, InvariantCulture));
            }
            case JsonToken.Boolean:
                return XmlConvert.ToString(Convert.ToBoolean(reader.Value, InvariantCulture));
            case JsonToken.Date:
            {
                if (reader.Value is DateTimeOffset offset)
                {
                    return XmlConvert.ToString(offset);
                }

                var d = Convert.ToDateTime(reader.Value, InvariantCulture);
                return XmlConvert.ToString(d, ToSerializationMode(d.Kind));
            }
            case JsonToken.Bytes:
                return Convert.ToBase64String((byte[]) reader.GetValue());
            case JsonToken.Null:
                return null;
            default:
                throw JsonSerializationException.Create(reader, $"Cannot get an XML string value from token type '{reader.TokenType}'.");
        }
    }

    static XmlDateTimeSerializationMode ToSerializationMode(DateTimeKind kind)
    {
        switch (kind)
        {
            case DateTimeKind.Local:
                return XmlDateTimeSerializationMode.Local;
            case DateTimeKind.Unspecified:
                return XmlDateTimeSerializationMode.Unspecified;
            case DateTimeKind.Utc:
                return XmlDateTimeSerializationMode.Utc;
            default:
                throw MiscellaneousUtils.CreateArgumentOutOfRangeException(nameof(kind), kind, "Unexpected DateTimeKind value.");
        }
    }

    void ReadArrayElements(JsonReader reader, IXmlDocument document, string propertyName, IXmlNode currentNode, XmlNamespaceManager manager)
    {
        var elementPrefix = XmlUtils.GetPrefix(propertyName);

        var nestedArrayElement = CreateElement(propertyName, document, elementPrefix, manager);

        currentNode.AppendChild(nestedArrayElement);

        var count = 0;
        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
        {
            DeserializeValue(reader, document, manager, propertyName, nestedArrayElement);
            count++;
        }

        if (WriteArrayAttribute)
        {
            AddJsonArrayAttribute(nestedArrayElement, document);
        }

        if (count == 1 && WriteArrayAttribute)
        {
            // Match on the decoded local name and namespace (not the raw JSON property name),
            // otherwise prefixed or XML-encoded names never match and a single-element nested
            // array loses its json:Array marker on round-trip.
            XmlUtils.GetQualifiedNameParts(propertyName, out var qualifiedPrefix, out var localName);
            var ns = qualifiedPrefix.IsNullOrEmpty() ? manager.DefaultNamespace : manager.LookupNamespace(qualifiedPrefix);

            foreach (var childNode in nestedArrayElement.ChildNodes)
            {
                if (childNode is IXmlElement element && element.LocalName == localName && element.NamespaceUri == ns)
                {
                    AddJsonArrayAttribute(element, document);
                    break;
                }
            }
        }
    }

    static void AddJsonArrayAttribute(IXmlElement element, IXmlDocument document)
    {
        element.SetAttributeNode(document.CreateAttribute("json:Array", jsonNamespaceUri, "true"));

        // linq to xml doesn't automatically include prefixes via the namespace manager
        if (element is XElementWrapper)
        {
            if (element.GetPrefixOfNamespace(jsonNamespaceUri) == null)
            {
                element.SetAttributeNode(document.CreateAttribute("xmlns:json", "http://www.w3.org/2000/xmlns/", jsonNamespaceUri));
            }
        }
    }

    static bool ShouldReadInto(JsonReader reader)
    {
        // a string token means the element only has a single text child
        switch (reader.TokenType)
        {
            case JsonToken.String:
            case JsonToken.Null:
            case JsonToken.Boolean:
            case JsonToken.Integer:
            case JsonToken.Float:
            case JsonToken.Date:
            case JsonToken.Bytes:
                return false;
        }

        return true;
    }

    static Dictionary<string, string?>? ReadAttributeElements(JsonReader reader, XmlNamespaceManager manager)
    {
        Dictionary<string, string?>? attributeNameValues = null;
        var finished = false;

        // read properties until first non-attribute is encountered
        while (!finished && reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonToken.PropertyName:
                    var attributeName = (string) reader.GetValue();

                    if (attributeName.IsNullOrEmpty())
                    {
                        finished = true;
                        continue;
                    }

                    var firstChar = attributeName[0];
                    string? attributeValue;

                    switch (firstChar)
                    {
                        case '@':
                            attributeNameValues ??= [];

                            attributeName = attributeName[1..];
                            reader.ReadAndAssert();
                            attributeValue = ConvertTokenToXmlValue(reader)!;
                            attributeNameValues.Add(attributeName, attributeValue);

                            if (IsNamespaceAttribute(attributeName, out var namespacePrefix))
                            {
                                manager.AddNamespace(namespacePrefix, attributeValue);
                            }

                            break;
                        case '$':
                            switch (attributeName)
                            {
                                case JsonTypeReflector.ArrayValuesPropertyName:
                                case JsonTypeReflector.IdPropertyName:
                                case JsonTypeReflector.RefPropertyName:
                                case JsonTypeReflector.TypePropertyName:
                                case JsonTypeReflector.ValuePropertyName:
                                    // check that JsonNamespaceUri is in scope
                                    // if it isn't then add it to document and namespace manager
                                    var jsonPrefix = manager.LookupPrefix(jsonNamespaceUri);
                                    if (jsonPrefix == null)
                                    {
                                        attributeNameValues ??= [];

                                        // ensure that the prefix used is free
                                        int? i = null;
                                        while (manager.LookupNamespace($"json{i}") != null)
                                        {
                                            i = i.GetValueOrDefault() + 1;
                                        }

                                        jsonPrefix = $"json{i}";

                                        attributeNameValues.Add($"xmlns:{jsonPrefix}", jsonNamespaceUri);
                                        manager.AddNamespace(jsonPrefix, jsonNamespaceUri);
                                    }

                                    // special case $values, it will have a non-primitive value
                                    if (attributeName == JsonTypeReflector.ArrayValuesPropertyName)
                                    {
                                        finished = true;
                                        break;
                                    }

                                    attributeName = attributeName[1..];
                                    reader.ReadAndAssert();

                                    if (!JsonTokenUtils.IsPrimitiveToken(reader.TokenType))
                                    {
                                        throw JsonSerializationException.Create(reader, $"Unexpected JsonToken: {reader.TokenType}");
                                    }

                                    attributeNameValues ??= [];

                                    attributeValue = reader.Value?.ToString();
                                    attributeNameValues.Add($"{jsonPrefix}:{attributeName}", attributeValue);
                                    break;
                                default:
                                    finished = true;
                                    break;
                            }

                            break;
                        default:
                            finished = true;
                            break;
                    }

                    break;
                case JsonToken.EndObject:
                case JsonToken.Comment:
                    finished = true;
                    break;
                default:
                    throw JsonSerializationException.Create(reader, $"Unexpected JsonToken: {reader.TokenType}");
            }
        }

        return attributeNameValues;
    }

    static void CreateInstruction(JsonReader reader, IXmlDocument document, IXmlNode currentNode, string propertyName)
    {
        if (propertyName == declarationName)
        {
            string? version = null;
            string? encoding = null;
            string? standalone = null;
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                switch (reader.Value?.ToString())
                {
                    case "@version":
                        reader.ReadAndAssert();
                        version = ConvertTokenToXmlValue(reader);
                        break;
                    case "@encoding":
                        reader.ReadAndAssert();
                        encoding = ConvertTokenToXmlValue(reader);
                        break;
                    case "@standalone":
                        reader.ReadAndAssert();
                        standalone = ConvertTokenToXmlValue(reader);
                        break;
                    default:
                        throw JsonSerializationException.Create(reader, $"Unexpected property name encountered while deserializing XmlDeclaration: {reader.Value}");
                }
            }

            var declaration = document.CreateXmlDeclaration(version!, encoding!, standalone!);
            currentNode.AppendChild(declaration);
        }
        else
        {
            var instruction = document.CreateProcessingInstruction(propertyName[1..], ConvertTokenToXmlValue(reader)!);
            currentNode.AppendChild(instruction);
        }
    }

    static void CreateDocumentType(JsonReader reader, IXmlDocument document, IXmlNode currentNode)
    {
        string? name = null;
        string? publicId = null;
        string? systemId = null;
        string? internalSubset = null;
        while (reader.Read() && reader.TokenType != JsonToken.EndObject)
        {
            switch (reader.Value?.ToString())
            {
                case "@name":
                    reader.ReadAndAssert();
                    name = ConvertTokenToXmlValue(reader);
                    break;
                case "@public":
                    reader.ReadAndAssert();
                    publicId = ConvertTokenToXmlValue(reader);
                    break;
                case "@system":
                    reader.ReadAndAssert();
                    systemId = ConvertTokenToXmlValue(reader);
                    break;
                case "@internalSubset":
                    reader.ReadAndAssert();
                    internalSubset = ConvertTokenToXmlValue(reader);
                    break;
                default:
                    throw JsonSerializationException.Create(reader, $"Unexpected property name encountered while deserializing XmlDeclaration: {reader.Value}");
            }
        }

        var documentType = document.CreateXmlDocumentType(name!, publicId!, systemId!, internalSubset!);
        currentNode.AppendChild(documentType);
    }

    IXmlElement CreateElement(string elementName, IXmlDocument document, string? elementPrefix, XmlNamespaceManager manager)
    {
        var encodeName = EncodeSpecialCharacters ? XmlConvert.EncodeLocalName(elementName) : XmlConvert.EncodeName(elementName);
        var ns = elementPrefix.IsNullOrEmpty() ? manager.DefaultNamespace : manager.LookupNamespace(elementPrefix);

        if (ns.IsNullOrEmpty())
        {
            return document.CreateElement(encodeName);
        }

        return document.CreateElement(encodeName, ns);
    }

    void DeserializeNode(JsonReader reader, IXmlDocument document, XmlNamespaceManager manager, IXmlNode currentNode)
    {
        do
        {
            switch (reader.TokenType)
            {
                case JsonToken.PropertyName:
                    if (currentNode.NodeType == XmlNodeType.Document && document.DocumentElement != null)
                    {
                        throw JsonSerializationException.Create(reader, "JSON root object has multiple properties. The root object must have a single property in order to create a valid XML document. Consider specifying a DeserializeRootElementName.");
                    }

                    var propertyName = (string) reader.GetValue();
                    reader.ReadAndAssert();

                    if (reader.TokenType == JsonToken.StartArray)
                    {
                        var count = 0;
                        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                        {
                            DeserializeValue(reader, document, manager, propertyName, currentNode);
                            count++;
                        }

                        if (count == 1 && WriteArrayAttribute)
                        {
                            XmlUtils.GetQualifiedNameParts(propertyName, out var elementPrefix, out var localName);
                            var ns = elementPrefix.IsNullOrEmpty() ? manager.DefaultNamespace : manager.LookupNamespace(elementPrefix);

                            foreach (var childNode in currentNode.ChildNodes)
                            {
                                if (childNode is IXmlElement element && element.LocalName == localName && element.NamespaceUri == ns)
                                {
                                    AddJsonArrayAttribute(element, document);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        DeserializeValue(reader, document, manager, propertyName, currentNode);
                    }

                    continue;
                case JsonToken.Comment:
                    currentNode.AppendChild(document.CreateComment(reader.StringValue));
                    break;
                case JsonToken.EndObject:
                case JsonToken.EndArray:
                    return;
                default:
                    throw JsonSerializationException.Create(reader, $"Unexpected JsonToken when deserializing node: {reader.TokenType}");
            }
        } while (reader.Read());
        // don't read if current token is a property. token was already read when parsing element attributes
    }

    /// <summary>
    /// Checks if the <paramref name="attributeName" /> is a namespace attribute.
    /// </summary>
    /// <param name="attributeName">Attribute name to test.</param>
    /// <param name="prefix">The attribute name prefix if it has one, otherwise an empty string.</param>
    /// <returns><c>true</c> if attribute name is for a namespace attribute, otherwise <c>false</c>.</returns>
    static bool IsNamespaceAttribute(string attributeName, [NotNullWhen(true)] out string? prefix)
    {
        if (attributeName.StartsWith("xmlns", StringComparison.Ordinal))
        {
            if (attributeName.Length == 5)
            {
                prefix = string.Empty;
                return true;
            }

            if (attributeName[5] == ':')
            {
                prefix = attributeName.Substring(6, attributeName.Length - 6);
                return true;
            }
        }

        prefix = null;
        return false;
    }

    static bool ValueAttributes(List<IXmlNode> c)
    {
        foreach (var xmlNode in c)
        {
            if (xmlNode.NamespaceUri == jsonNamespaceUri)
            {
                continue;
            }

            if (xmlNode is {NamespaceUri: "http://www.w3.org/2000/xmlns/", Value: jsonNamespaceUri})
            {
                continue;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether this instance can convert the specified value type.
    /// </summary>
    /// <param name="valueType">Type of the value.</param>
    /// <returns>
    /// <c>true</c> if this instance can convert the specified value type; otherwise, <c>false</c>.
    /// </returns>
    public override bool CanConvert(Type valueType) =>
        valueType.IsAssignableTo<XObject>() ||
        valueType.IsAssignableTo<XmlNode>();
}