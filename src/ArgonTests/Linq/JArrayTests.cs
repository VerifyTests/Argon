// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

// ReSharper disable UseObjectOrCollectionInitializer

// ReSharper disable UnusedVariable
public class JArrayTests : TestFixtureBase
{
    [Fact]
    public void RemoveSpecificAndRemoveSelf()
    {
        var o = new JObject
        {
            {"results", new JArray(1, 2, 3, 4)}
        };

        var a = (JArray) o["results"];

        var last = a.Last();

        Assert.True(a.Remove(last));

        last = a.Last();
        last.Remove();

        Assert.Equal(2, a.Count);
    }

    [Fact]
    public void Clear()
    {
        var a = new JArray {1};
        Assert.Single(a);

        a.Clear();
        Assert.Empty(a);
    }

    [Fact]
    public void AddToSelf()
    {
        var a = new JArray();
        a.Add(a);

        Assert.False(ReferenceEquals(a[0], a));
    }

    [Fact]
    public void Contains()
    {
        var v = new JValue(1);

        var a = new JArray {v};

#pragma warning disable xUnit2017
        Assert.False(a.Contains(new JValue(2)));
        Assert.False(a.Contains(new JValue(1)));
        Assert.False(a.Contains(null));
#pragma warning restore xUnit2017
        Assert.Contains(v, a);
    }

    [Fact]
    public void GenericCollectionCopyTo()
    {
        var j = new JArray
        {
            new JValue(1),
            new JValue(2),
            new JValue(3)
        };
        Assert.Equal(3, j.Count);

        var a = new JToken[5];

        ((ICollection<JToken>) j).CopyTo(a, 1);

        Assert.Null(a[0]);

        Assert.Equal(1, (int) a[1]);

        Assert.Equal(2, (int) a[2]);

        Assert.Equal(3, (int) a[3]);

        Assert.Null(a[4]);
    }

    [Fact]
    public void GenericCollectionCopyToNegativeArrayIndexShouldThrow()
    {
        var j = new JArray();

        XUnitAssert.Throws<ArgumentOutOfRangeException>(
            () => ((ICollection<JToken>) j).CopyTo(new JToken[1], -1),
            """
            arrayIndex is less than 0.
            Parameter name: arrayIndex
            """,
            "arrayIndex is less than 0. (Parameter 'arrayIndex')");
    }

    [Fact]
    public void GenericCollectionCopyToArrayIndexEqualGreaterToArrayLengthShouldThrow()
    {
        var j = new JArray();

        var exception = Assert.Throws<ArgumentException>(() => ((ICollection<JToken>) j).CopyTo(new JToken[1], 1));
        Assert.Equal("arrayIndex is equal to or greater than the length of array.", exception.Message);
    }

    [Fact]
    public void GenericCollectionCopyToInsufficientArrayCapacity()
    {
        var j = new JArray
        {
            new JValue(1),
            new JValue(2),
            new JValue(3)
        };

        var exception = Assert.Throws<ArgumentException>(() => ((ICollection<JToken>) j).CopyTo(new JToken[3], 1));
        Assert.Equal("The number of elements in the source JObject is greater than the available space from arrayIndex to the end of the destination array.", exception.Message);
    }

    [Fact]
    public void Remove()
    {
        var v = new JValue(1);
        var j = new JArray {v};

        Assert.Single(j);

        Assert.False( j.Remove(new JValue(1)));
        Assert.False( j.Remove(null));
        Assert.True(j.Remove(v));
        Assert.False( j.Remove(v));

        Assert.Empty(j);
    }

    [Fact]
    public void IndexOf()
    {
        var v1 = new JValue(1);
        var v2 = new JValue(1);
        var v3 = new JValue(1);

        var j = new JArray {v1};

        Assert.Equal(0, j.IndexOf(v1));

        j.Add(v2);
        Assert.Equal(0, j.IndexOf(v1));
        Assert.Equal(1, j.IndexOf(v2));

        j.AddFirst(v3);
        Assert.Equal(1, j.IndexOf(v1));
        Assert.Equal(2, j.IndexOf(v2));
        Assert.Equal(0, j.IndexOf(v3));

        v3.Remove();
        Assert.Equal(0, j.IndexOf(v1));
        Assert.Equal(1, j.IndexOf(v2));
        Assert.Equal(-1, j.IndexOf(v3));
    }

    [Fact]
    public void RemoveAt()
    {
        var v1 = new JValue(1);
        var v2 = new JValue(1);
        var v3 = new JValue(1);

        var j = new JArray
        {
            v1,
            v2,
            v3
        };

        Assert.Contains(v1, j);
        j.RemoveAt(0);
#pragma warning disable xUnit2017
        Assert.False(j.Contains(new JValue(v1)));

        Assert.Contains(v3, j);
        j.RemoveAt(1);
        Assert.False(j.Contains(new JValue(v3)));
#pragma warning restore xUnit2017

        Assert.Single(j);
    }

    [Fact]
    public void RemoveAtOutOfRangeIndexShouldError()
    {
        var j = new JArray();

        XUnitAssert.Throws<ArgumentOutOfRangeException>(
            () => j.RemoveAt(0),
            """
            Index is equal to or greater than Count.
            Parameter name: index
            """,
            "Index is equal to or greater than Count. (Parameter 'index')");
    }

    [Fact]
    public void RemoveAtNegativeIndexShouldError()
    {
        var j = new JArray();

        XUnitAssert.Throws<ArgumentOutOfRangeException>(
            () => j.RemoveAt(-1),
            """
            Index is less than 0.
            Parameter name: index
            """,
            "Index is less than 0. (Parameter 'index')");
    }

    [Fact]
    public void Insert()
    {
        var v1 = new JValue(1);
        var v2 = new JValue(2);
        var v3 = new JValue(3);
        var v4 = new JValue(4);

        var j = new JArray
        {
            v1,
            v2,
            v3
        };

        j.Insert(1, v4);

        Assert.Equal(0, j.IndexOf(v1));
        Assert.Equal(1, j.IndexOf(v4));
        Assert.Equal(2, j.IndexOf(v2));
        Assert.Equal(3, j.IndexOf(v3));
    }

    [Fact]
    public void AddFirstAddedTokenShouldBeFirst()
    {
        var v1 = new JValue(1);
        var v2 = new JValue(2);
        var v3 = new JValue(3);

        var j = new JArray();
        Assert.Null(j.First);
        Assert.Null(j.Last);

        j.AddFirst(v1);
        Assert.Equal(v1, j.First);
        Assert.Equal(v1, j.Last);

        j.AddFirst(v2);
        Assert.Equal(v2, j.First);
        Assert.Equal(v1, j.Last);

        j.AddFirst(v3);
        Assert.Equal(v3, j.First);
        Assert.Equal(v1, j.Last);
    }

    [Fact]
    public void InsertShouldInsertAtZeroIndex()
    {
        var v1 = new JValue(1);
        var v2 = new JValue(2);

        var j = new JArray();

        j.Insert(0, v1);
        Assert.Equal(0, j.IndexOf(v1));

        j.Insert(0, v2);
        Assert.Equal(1, j.IndexOf(v1));
        Assert.Equal(0, j.IndexOf(v2));
    }

    [Fact]
    public void InsertNull()
    {
        var j = new JArray();
        j.Insert(0, null);

        Assert.Null(((JValue) j[0]).Value);
    }

    [Fact]
    public void InsertNegativeIndexShouldThrow()
    {
        var j = new JArray();

        XUnitAssert.Throws<ArgumentOutOfRangeException>(
            () => j.Insert(-1, new JValue(1)),
            """
            Index was out of range. Must be non-negative and less than the size of the collection.
            Parameter name: index
            """,
            "Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index')");
    }

    [Fact]
    public void InsertOutOfRangeIndexShouldThrow()
    {
        var j = new JArray();

        XUnitAssert.Throws<ArgumentOutOfRangeException>(
            () => j.Insert(2, new JValue(1)),
            """
            Index must be within the bounds of the List.
            Parameter name: index
            """,
            "Index must be within the bounds of the List. (Parameter 'index')");
    }

    [Fact]
    public void Item()
    {
        var v1 = new JValue(1);
        var v2 = new JValue(2);
        var v3 = new JValue(3);
        var v4 = new JValue(4);

        var j = new JArray
        {
            v1,
            v2,
            v3
        };

        j[1] = v4;

        Assert.Null(v2.Parent);
        Assert.Equal(-1, j.IndexOf(v2));
        Assert.Equal(j, v4.Parent);
        Assert.Equal(1, j.IndexOf(v4));
    }

    [Fact]
    public void Parse_ShouldThrowOnUnexpectedToken()
    {
        var json = """{"prop":"value"}""";

        var exception = Assert.Throws<JsonReaderException>(() => JArray.Parse(json));
        Assert.Equal("Error reading JArray from JsonReader. Current JsonReader item is not an array: StartObject. Path '', line 1, position 1.", exception.Message);
    }

    public class ListItemFields
    {
        public string ListItemText { get; set; }
        public object ListItemValue { get; set; }
    }

    [Fact]
    public void ArrayOrder()
    {
        var itemZeroText = "Zero text";

        IEnumerable<ListItemFields> t =
        [
            new() {ListItemText = "First", ListItemValue = 1},
            new() {ListItemText = "Second", ListItemValue = 2},
            new() {ListItemText = "Third", ListItemValue = 3}
        ];

        var optionValues =
            new JObject(
                new JProperty("options",
                    new JArray(
                        new JObject(
                            new JProperty("text", itemZeroText),
                            new JProperty("value", "0")),
                        from r in t
                        orderby r.ListItemValue
                        select new JObject(
                            new JProperty("text", r.ListItemText),
                            new JProperty("value", r.ListItemValue.ToString())))));

        var result = $"myOptions = {optionValues}";

        XUnitAssert.AreEqualNormalized(
            """
            myOptions = {
              "options": [
                {
                  "text": "Zero text",
                  "value": "0"
                },
                {
                  "text": "First",
                  "value": "1"
                },
                {
                  "text": "Second",
                  "value": "2"
                },
                {
                  "text": "Third",
                  "value": "3"
                }
              ]
            }
            """,
            result);
    }

    [Fact]
    public void Iterate()
    {
        var a = new JArray(1, 2, 3, 4, 5);

        var i = 1;
        foreach (var token in a)
        {
            Assert.Equal(i, (int) token);
            i++;
        }
    }

    [Fact]
    public void AddArrayToSelf()
    {
        var a = new JArray(1, 2);
        a.Add(a);

        Assert.Equal(3, a.Count);
        Assert.Equal(1, (int) a[0]);
        Assert.Equal(2, (int) a[1]);
        Assert.NotSame(a, a[2]);
    }

    [Fact]
    public void SetValueWithInvalidIndex()
    {
        var exception = Assert.Throws<Exception>(() =>
        {
            var a = new JArray
            {
                ["badvalue"] = new JValue(3)
            };
        });
        Assert.Equal("""Set JArray values with invalid key value: "badvalue". Int32 array index expected.""", exception.Message);
    }

    [Fact]
    public void SetValue()
    {
        object key = 0;

        var a = new JArray((object) null)
        {
            [key] = new JValue(3)
        };

        Assert.Equal(3, (int) a[key]);
    }

    [Fact]
    public void ReplaceAll()
    {
        var a = new JArray(new[] {1, 2, 3});
        Assert.Equal(3, a.Count);
        Assert.Equal(1, (int) a[0]);
        Assert.Equal(2, (int) a[1]);
        Assert.Equal(3, (int) a[2]);

        a.ReplaceAll(1);
        Assert.Single(a);
        Assert.Equal(1, (int) a[0]);
    }

    [Fact]
    public void ParseIncomplete()
    {
        var exception = Assert.Throws<JsonReaderException>(() => JArray.Parse("[1"));
        Assert.Equal("Unexpected end of content while loading JArray. Path '[0]', line 1, position 2.", exception.Message);
    }

    [Fact]
    public void InsertAddEnd()
    {
        var array = new JArray();
        array.Insert(0, 123);
        array.Insert(1, 456);

        Assert.Equal(2, array.Count);
        Assert.Equal(123, (int) array[0]);
        Assert.Equal(456, (int) array[1]);
    }

    [Fact]
    public void ParseAdditionalContent()
    {
        var json = """
                   [
                   "Small",
                   "Medium",
                   "Large"
                   ], 987987
                   """;

        var exception = Assert.Throws<JsonReaderException>(() => JArray.Parse(json));
        Assert.Equal("Additional text encountered after finished reading JSON content: ,. Path '', line 5, position 1.", exception.Message);
    }

    [Fact]
    public void ToListOnEmptyArray()
    {
        var json = """{"decks":[]}""";

        var decks = (JArray) JObject.Parse(json)["decks"];
        var l = decks.ToList();
        Assert.Empty(l);

        json = """{"decks":[1]}""";

        decks = (JArray) JObject.Parse(json)["decks"];
        l = decks.ToList();
        Assert.Single(l);
    }

    [Fact]
    public void Parse_NoComments()
    {
        var json = "[1,2/*comment*/,3]";

        var a = JArray.Parse(json, new());

        Assert.Equal(3, a.Count);
        Assert.Equal(1, (int) a[0]);
        Assert.Equal(2, (int) a[1]);
        Assert.Equal(3, (int) a[2]);

        a = JArray.Parse(json, new()
        {
            CommentHandling = CommentHandling.Ignore
        });

        Assert.Equal(3, a.Count);
        Assert.Equal(1, (int) a[0]);
        Assert.Equal(2, (int) a[1]);
        Assert.Equal(3, (int) a[2]);

        a = JArray.Parse(json, new()
        {
            CommentHandling = CommentHandling.Load
        });

        Assert.Equal(4, a.Count);
        Assert.Equal(1, (int) a[0]);
        Assert.Equal(2, (int) a[1]);
        Assert.Equal(JTokenType.Comment, a[2].Type);
        Assert.Equal(3, (int) a[3]);
    }

    [Fact]
    public void Parse_ExcessiveContentJustComments()
    {
        var json = """
                   [1,2,3]/*comment*/
                   //Another comment.
                   """;

        var a = JArray.Parse(json);

        Assert.Equal(3, a.Count);
        Assert.Equal(1, (int) a[0]);
        Assert.Equal(2, (int) a[1]);
        Assert.Equal(3, (int) a[2]);
    }

    [Fact]
    public void Parse_ExcessiveContent()
    {
        var json = """
                   [1,2,3]/*comment*/
                   //Another comment.
                   []
                   """;

        var exception = Assert.Throws<JsonReaderException>(() => JArray.Parse(json));
        Assert.Equal("Additional text encountered after finished reading JSON content: [. Path '', line 3, position 0.", exception.Message);
    }

    [Fact]
    public void Parse_LineInfo()
    {
        var json = "[1,2,3]";

        var a = JArray.Parse(json, new());

        Assert.True(((IJsonLineInfo) a).HasLineInfo());
        Assert.True(((IJsonLineInfo) a[0]).HasLineInfo());
        Assert.True(((IJsonLineInfo) a[1]).HasLineInfo());
        Assert.True(((IJsonLineInfo) a[2]).HasLineInfo());

        a = JArray.Parse(json, new()
        {
            LineInfoHandling = LineInfoHandling.Ignore
        });

        Assert.False( ((IJsonLineInfo) a).HasLineInfo());
        Assert.False( ((IJsonLineInfo) a[0]).HasLineInfo());
        Assert.False( ((IJsonLineInfo) a[1]).HasLineInfo());
        Assert.False( ((IJsonLineInfo) a[2]).HasLineInfo());

        a = JArray.Parse(json, new()
        {
            LineInfoHandling = LineInfoHandling.Load
        });

        Assert.True(((IJsonLineInfo) a).HasLineInfo());
        Assert.True(((IJsonLineInfo) a[0]).HasLineInfo());
        Assert.True(((IJsonLineInfo) a[1]).HasLineInfo());
        Assert.True(((IJsonLineInfo) a[2]).HasLineInfo());
    }
}