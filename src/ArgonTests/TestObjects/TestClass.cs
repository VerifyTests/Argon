// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace TestObjects;

[DataContract]
public class TestClass
{
    [DataMember]
    public string Name { get; set; } = "Rick";

    [DataMember]
    public DateTime Now { get; set; } = DateTime.Now;

    [DataMember]
    public decimal BigNumber { get; set; } = 1212121.22M;

    [DataMember]
    public Address Address1 { get; set; } = new();

    [DataMember]
    public List<Address> Addresses { get; set; } = [];

    [DataMember]
    public List<string> strings = [];

    [DataMember]
    public Dictionary<string, int> dictionary = [];
}