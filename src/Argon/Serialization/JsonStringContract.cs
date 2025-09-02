// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Contract details for a <see cref="Type" /> used by the <see cref="JsonSerializer" />.
/// </summary>
[RequiresUnreferencedCode(MiscellaneousUtils.TrimWarning)]
public class JsonStringContract : JsonPrimitiveContract
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonStringContract" /> class.
    /// </summary>
    public JsonStringContract(Type underlyingType)
        : base(underlyingType) =>
        ContractType = JsonContractType.String;
}