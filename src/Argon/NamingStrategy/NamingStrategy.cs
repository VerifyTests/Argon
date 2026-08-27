// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// A base class for resolving how property names and dictionary keys are serialized.
/// </summary>
public abstract class NamingStrategy
{
    // dictionary keys are user data, so the cache is capped; once it is full later keys are
    // resolved without being added
    const int dictionaryKeyCacheMax = 512;

    ConcurrentDictionary<string, string>? dictionaryKeyCache;

    /// <summary>
    /// A flag indicating whether dictionary keys should be processed.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool ProcessDictionaryKeys { get; set; }

    /// <summary>
    /// A flag indicating whether explicitly specified property names,
    /// e.g. a property name customized with a <see cref="JsonPropertyAttribute" />, should be processed.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool OverrideSpecifiedNames { get; set; }

    /// <summary>
    /// Gets the serialized name for a given property name.
    /// </summary>
    /// <param name="name">The initial property name.</param>
    /// <param name="hasSpecifiedName">A flag indicating whether the property has had a name explicitly specified.</param>
    /// <returns>The serialized property name.</returns>
    public virtual string GetPropertyName(string name, bool hasSpecifiedName)
    {
        if (hasSpecifiedName && !OverrideSpecifiedNames)
        {
            return name;
        }

        return ResolvePropertyName(name);
    }

    /// <summary>
    /// Gets the serialized key for a given dictionary key.
    /// </summary>
    /// <param name="name">The initial dictionary key.</param>
    /// <returns>The serialized dictionary key.</returns>
    public virtual string GetDictionaryKey(string name, object original)
    {
        if (!ProcessDictionaryKeys)
        {
            return name;
        }

        if (!CacheDictionaryKeys)
        {
            return ResolvePropertyName(name);
        }

        // the same keys usually repeat across the entries of a dictionary, and across calls
        var cache = dictionaryKeyCache ??= new();
        if (cache.TryGetValue(name, out var resolved))
        {
            return resolved;
        }

        resolved = ResolvePropertyName(name);
        if (cache.Count < dictionaryKeyCacheMax)
        {
            cache.TryAdd(name, resolved);
        }

        return resolved;
    }

    /// <summary>
    /// A flag indicating whether resolved dictionary keys can be cached, which requires
    /// <see cref="ResolvePropertyName" /> to return the same result every time it is passed the same
    /// name. True for all the naming strategies included in Argon. Override to <c>false</c> in a
    /// strategy that resolves a name differently depending on state outside that name.
    /// </summary>
    protected virtual bool CacheDictionaryKeys => true;

    /// <summary>
    /// Resolves the specified property name.
    /// </summary>
    protected abstract string ResolvePropertyName(string name);

    /// <summary>
    /// Hash code calculation
    /// </summary>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = GetType().GetHashCode(); // make sure different types do not result in equal values
            hashCode = (hashCode * 397) ^ ProcessDictionaryKeys.GetHashCode();
            hashCode = (hashCode * 397) ^ OverrideSpecifiedNames.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// Object equality implementation
    /// </summary>
    public override bool Equals(object? obj) =>
        Equals(obj as NamingStrategy);

    /// <summary>
    /// Compare to another NamingStrategy
    /// </summary>
    protected bool Equals(NamingStrategy? other)
    {
        if (other == null)
        {
            return false;
        }

        return GetType() == other.GetType() &&
               ProcessDictionaryKeys == other.ProcessDictionaryKeys &&
               OverrideSpecifiedNames == other.OverrideSpecifiedNames;
    }
}