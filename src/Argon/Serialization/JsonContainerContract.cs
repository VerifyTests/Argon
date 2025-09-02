// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Contract details for a <see cref="System.Type" /> used by the <see cref="JsonSerializer" />.
/// </summary>
public class JsonContainerContract : JsonContract
{
    // will be null for containers that don't have an item type (e.g. IList) or for complex objects
    internal JsonContract? ItemContract { get; set; }

    /// <summary>
    /// Gets or sets the default collection items <see cref="JsonConverter" />.
    /// </summary>
    public JsonConverter? ItemConverter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the collection items preserve object references.
    /// </summary>
    public bool? ItemIsReference { get; set; }

    /// <summary>
    /// Gets or sets the collection item reference loop handling.
    /// </summary>
    public ReferenceLoopHandling? ItemReferenceLoopHandling { get; set; }

    /// <summary>
    /// Gets or sets the collection item type name handling.
    /// </summary>
    public TypeNameHandling? ItemTypeNameHandling { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonContainerContract" /> class.
    /// </summary>
    [RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
    [RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
    internal JsonContainerContract(Type underlyingType)
        : base(underlyingType)
    {
        var containerAttribute = AttributeCache<JsonContainerAttribute>.GetAttribute(underlyingType);

        if (containerAttribute != null)
        {
            if (containerAttribute.ItemConverterType != null)
            {
                ItemConverter = JsonTypeReflector.CreateJsonConverterInstance(containerAttribute.ItemConverterType);
            }

            ItemIsReference = containerAttribute.itemIsReference;
            ItemReferenceLoopHandling = containerAttribute.itemReferenceLoopHandling;
            ItemTypeNameHandling = containerAttribute.itemTypeNameHandling;
        }
    }
}