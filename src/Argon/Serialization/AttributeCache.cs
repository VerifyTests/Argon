// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

[RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
[RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
static class AttributeCache<T> where T : Attribute
{
    static ThreadSafeStore<ICustomAttributeProvider, T?> TypeAttributeCache = new(JsonTypeReflector.GetAttribute<T>);

    public static T? GetAttribute(ICustomAttributeProvider provider) =>
        TypeAttributeCache.Get(provider);
}