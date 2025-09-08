// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace TestObjects;

class ParticipantEntity
{
    [Argon.JsonConstructor]
    public ParticipantEntity()
    {
    }

    /// <summary>
    /// Gets or sets the date and time that the participant was created in the CU.
    /// </summary>
    [JsonProperty(PropertyName = "pa_created", DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset CreationDate { get; internal set; }

    /// <summary>
    /// Gets the properties of the participant.
    /// </summary>
    [JsonProperty(PropertyName = "pa_info")]
    public Dictionary<string, string> Properties
    {
        get => field ??= [];
        set;
    }
}