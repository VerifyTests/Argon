// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

using System.Text.RegularExpressions;
using TestCaseSource = Xunit.MemberDataAttribute;
// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable UnusedVariable
// ReSharper disable RedundantAssignment

public class JPathExecuteTests : TestFixtureBase
{
    // FieldMultipleFilter threw for ErrorWhenNoMatch unconditionally (missing else), so
    // $['a','b'] errored even when the properties existed.
    [Fact]
    public void SelectTokensMultipleExistingFieldsDoesNotError()
    {
        var o = JObject.Parse("""{"a":1,"b":2}""");

        var values = o.SelectTokens("$['a','b']", errorWhenNoMatch: true)
            .Select(_ => (int) _)
            .ToList();

        Assert.Equal(new[] {1, 2}, values);
    }

    [Fact]
    public void SelectTokensMultipleWithMissingFieldStillErrors()
    {
        var o = JObject.Parse("""{"a":1}""");

        Assert.Throws<JsonException>(() =>
            o.SelectTokens("$['a','missing']", errorWhenNoMatch: true).ToList());
    }

    // filter numeric literals were only terminated by space or ')', so && / || directly
    // after a number failed to parse.
    [Fact]
    public void FilterWithNoSpacesAroundLogicalAnd()
    {
        var a = JArray.Parse("""[{"a":1,"b":2},{"a":1,"b":9}]""");

        var matches = a.SelectTokens("$[?(@.a==1&&@.b==2)]").ToList();

        Assert.Single(matches);
        Assert.Equal(2, (int) matches[0]["b"]);
    }

    [Fact]
    public void FilterWithNoSpacesAroundLogicalOr()
    {
        var a = JArray.Parse("""[{"a":1},{"a":5},{"a":9}]""");

        var matches = a.SelectTokens("$[?(@.a==1||@.a==5)]").ToList();

        Assert.Equal(2, matches.Count);
    }

    // GetTokenIndex had no negative-index guard, so $[-1] crashed with
    // ArgumentOutOfRangeException instead of returning null / throwing JsonException.
    [Fact]
    public void NegativeArrayIndexReturnsNull()
    {
        var a = new JArray(1, 2, 3);

        Assert.Null(a.SelectToken("$[-1]"));
    }

    [Fact]
    public void NegativeArrayIndexWithErrorThrowsJsonException()
    {
        var a = new JArray(1, 2, 3);

        Assert.Throws<JsonException>(() => a.SelectToken("$[-1]", errorWhenNoMatch: true));
    }

    [Fact]
    public void AndBindsTighterThanOr()
    {
        var array = JArray.Parse("""[{"b":1,"c":1},{"a":1,"b":1},{"c":1}]""");

        // '&&' binds tighter than '||': "(a && b) || c" matches all three items
        var result = array.SelectTokens("$[?(@.a && @.b || @.c)]").ToList();
        Assert.Equal(3, result.Count);

        // the logically identical "c || (a && b)" must select the same set regardless of order
        var reordered = array.SelectTokens("$[?(@.c || @.a && @.b)]").ToList();
        Assert.Equal(3, reordered.Count);
    }

    [Fact]
    public void GreaterThanIssue1518()
    {
        var statusJson = """{"usingmem": "214376"}"""; //214,376
        var jObj = JObject.Parse(statusJson);

        var aa = jObj.SelectToken("$..[?(@.usingmem>10)]"); //found,10
        Assert.Equal(jObj, aa);

        var bb = jObj.SelectToken("$..[?(@.usingmem>27000)]"); //null, 27,000
        Assert.Equal(jObj, bb);

        var cc = jObj.SelectToken("$..[?(@.usingmem>21437)]"); //found, 21,437
        Assert.Equal(jObj, cc);

        var dd = jObj.SelectToken("$..[?(@.usingmem>21438)]"); //null,21,438
        Assert.Equal(jObj, dd);
    }

    [Fact]
    public void BacktrackingRegex_SingleMatch_TimeoutRespected()
    {
        const string RegexBacktrackingPattern = "(?<a>(.*?))[|].*(?<b>(.*?))[|].*(?<c>(.*?))[|].*(?<d>[1-3])[|].*(?<e>(.*?))[|].*[|].*[|].*(?<f>(.*?))[|].*[|].*(?<g>(.*?))[|].*(?<h>(.*))";

        var regexBacktrackingData = new JArray
        {
            new JObject(
                new JProperty("b", @"15/04/2020 8:18:03 PM|1|System.String[]|3|Libero eligendi magnam ut inventore.. Quaerat et sit voluptatibus repellendus blanditiis aliquam ut.. Quidem qui ut sint in ex et tempore.|||.\iste.cpp||46018|-1"))
        };

        var settings = new JsonSelectSettings
        {
            RegexMatchTimeout = TimeSpan.FromSeconds(0.01)
        };
        Assert.Throws<RegexMatchTimeoutException>(() => regexBacktrackingData
            .SelectTokens(
                $"[?(@.b =~ /{RegexBacktrackingPattern}/)]",
                settings)
            .ToArray());
    }

    [Fact]
    public void GreaterThanWithIntegerParameterAndStringValue()
    {
        var json = """
            {
              "persons": [
                {
                  "name"  : "John",
                  "age": "26"
                },
                {
                  "name"  : "Jane",
                  "age": "2"
                }
              ]
            }
            """;

        var models = JObject.Parse(json);

        var results = models.SelectTokens("$.persons[?(@.age > 3)]").ToList();

        Assert.Single(results);
    }

    [Fact]
    public void GreaterThanWithStringParameterAndIntegerValue()
    {
        var json = """
            {
              "persons": [
                {
                  "name"  : "John",
                  "age": 26
                },
                {
                  "name"  : "Jane",
                  "age": 2
                }
              ]
            }
            """;

        var models = JObject.Parse(json);

        var results = models.SelectTokens("$.persons[?(@.age > '3')]").ToList();

        Assert.Single(results);
    }

    [Fact]
    public void RecursiveWildcard()
    {
        var json = """
            {
                "a": [
                    {
                        "id": 1
                    }
                ],
                "b": [
                    {
                        "id": 2
                    },
                    {
                        "id": 3,
                        "c": {
                            "id": 4
                        }
                    }
                ],
                "d": [
                    {
                        "id": 5
                    }
                ]
            }
            """;

        var models = JObject.Parse(json);

        var results = models.SelectTokens("$.b..*.id").ToList();

        Assert.Equal(3, results.Count);
        Assert.Equal(2, (int) results[0]);
        Assert.Equal(3, (int) results[1]);
        Assert.Equal(4, (int) results[2]);
    }

    [Fact]
    public void ScanFilter()
    {
        var json = """
            {
              "elements": [
                {
                  "id": "A",
                  "children": [
                    {
                      "id": "AA",
                      "children": [
                        {
                          "id": "AAA"
                        },
                        {
                          "id": "AAB"
                        }
                      ]
                    },
                    {
                      "id": "AB"
                    }
                  ]
                },
                {
                  "id": "B",
                  "children": []
                }
              ]
            }
            """;

        var models = JObject.Parse(json);

        var results = models.SelectTokens("$.elements..[?(@.id=='AAA')]").ToList();

        Assert.Single(results);
        Assert.Equal(models["elements"][0]["children"][0]["children"][0], results[0]);
    }

    [Fact]
    public void FilterTrue()
    {
        var json = """
            {
              "elements": [
                {
                  "id": "A",
                  "children": [
                    {
                      "id": "AA",
                      "children": [
                        {
                          "id": "AAA"
                        },
                        {
                          "id": "AAB"
                        }
                      ]
                    },
                    {
                      "id": "AB"
                    }
                  ]
                },
                {
                  "id": "B",
                  "children": []
                }
              ]
            }
            """;

        var models = JObject.Parse(json);

        var results = models.SelectTokens("$.elements[?(true)]").ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal(results[0], models["elements"][0]);
        Assert.Equal(results[1], models["elements"][1]);
    }

    [Fact]
    public void ScanFilterTrue()
    {
        var json = """
            {
              "elements": [
                {
                  "id": "A",
                  "children": [
                    {
                      "id": "AA",
                      "children": [
                        {
                          "id": "AAA"
                        },
                        {
                          "id": "AAB"
                        }
                      ]
                    },
                    {
                      "id": "AB"
                    }
                  ]
                },
                {
                  "id": "B",
                  "children": []
                }
              ]
            }
            """;

        var models = JObject.Parse(json);

        var results = models.SelectTokens("$.elements..[?(true)]").ToList();

        Assert.Equal(25, results.Count);
    }

    [Fact]
    public void ScanQuoted()
    {
        var json = """
            {
                "Node1": {
                    "Child1": {
                        "Name": "IsMe",
                        "TargetNode": {
                            "Prop1": "Val1",
                            "Prop2": "Val2"
                        }
                    },
                    "My.Child.Node": {
                        "TargetNode": {
                            "Prop1": "Val1",
                            "Prop2": "Val2"
                        }
                    }
                },
                "Node2": {
                    "TargetNode": {
                        "Prop1": "Val1",
                        "Prop2": "Val2"
                    }
                }
            }
            """;

        var models = JObject.Parse(json);

        var result = models.SelectTokens("$..['My.Child.Node']").Count();
        Assert.Equal(1, result);

        result = models.SelectTokens("..['My.Child.Node']").Count();
        Assert.Equal(1, result);
    }

    [Fact]
    public void ScanMultipleQuoted()
    {
        var json = """
            {
                "Node1": {
                    "Child1": {
                        "Name": "IsMe",
                        "TargetNode": {
                            "Prop1": "Val1",
                            "Prop2": "Val2"
                        }
                    },
                    "My.Child.Node": {
                        "TargetNode": {
                            "Prop1": "Val3",
                            "Prop2": "Val4"
                        }
                    }
                },
                "Node2": {
                    "TargetNode": {
                        "Prop1": "Val5",
                        "Prop2": "Val6"
                    }
                }
            }
            """;

        var models = JObject.Parse(json);

        var results = models.SelectTokens("$..['My.Child.Node','Prop1','Prop2']").ToList();
        Assert.Equal("Val1", (string) results[0]);
        Assert.Equal("Val2", (string) results[1]);
        Assert.Equal(JTokenType.Object, results[2].Type);
        Assert.Equal("Val3", (string) results[3]);
        Assert.Equal("Val4", (string) results[4]);
        Assert.Equal("Val5", (string) results[5]);
        Assert.Equal("Val6", (string) results[6]);
    }

    [Fact]
    public void ParseWithEmptyArrayContent()
    {
        var json = """
            {
                'controls': [
                    {
                        'messages': {
                            'addSuggestion': {
                                'en-US': 'Add'
                            }
                        }
                    },
                    {
                        'header': {
                            'controls': []
                        },
                        'controls': [
                            {
                                'controls': [
                                    {
                                        'defaultCaption': {
                                            'en-US': 'Sort by'
                                        },
                                        'sortOptions': [
                                            {
                                                'label': {
                                                    'en-US': 'Name'
                                                }
                                            }
                                        ]
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
            """;
        var jToken = JObject.Parse(json);
        var tokens = jToken.SelectTokens("$..en-US").ToList();

        Assert.Equal(3, tokens.Count);
        Assert.Equal("Add", (string) tokens[0]);
        Assert.Equal("Sort by", (string) tokens[1]);
        Assert.Equal("Name", (string) tokens[2]);
    }

    [Fact]
    public void SelectTokenAfterEmptyContainer()
    {
        var json = """
            {
                'cont': [],
                'test': 'no one will find me'
            }
            """;

        var o = JObject.Parse(json);

        var results = o.SelectTokens("$..test").ToList();

        Assert.Single(results);
        Assert.Equal("no one will find me", (string) results[0]);
    }

    [Fact]
    public void EvaluatePropertyWithRequired()
    {
        var json = "{\"bookId\":\"1000\"}";
        var o = JObject.Parse(json);

        var bookId = (string) o.SelectToken("bookId", true);

        Assert.Equal("1000", bookId);
    }

    [Fact]
    public void EvaluateEmptyPropertyIndexer()
    {
        var o = new JObject(
            new JProperty("", 1));

        var token = o.SelectToken("['']");
        Assert.Equal(1, (int) token);
    }

    [Fact]
    public void EvaluateEmptyString()
    {
        var o = new JObject(
            new JProperty("Blah", 1));

        var token = o.SelectToken("");
        Assert.Equal(o, token);

        token = o.SelectToken("['']");
        Assert.Null(token);
    }

    [Fact]
    public void EvaluateEmptyStringWithMatchingEmptyProperty()
    {
        var o = new JObject(
            new JProperty(" ", 1));

        var token = o.SelectToken("[' ']");
        Assert.Equal(1, (int) token);
    }

    [Fact]
    public void EvaluateWhitespaceString()
    {
        var o = new JObject(
            new JProperty("Blah", 1));

        var token = o.SelectToken(" ");
        Assert.Equal(o, token);
    }

    [Fact]
    public void EvaluateDollarString()
    {
        var o = new JObject(
            new JProperty("Blah", 1));

        var token = o.SelectToken("$");
        Assert.Equal(o, token);
    }

    [Fact]
    public void EvaluateDollarTypeString()
    {
        var o = new JObject(
            new JProperty("$values", new JArray(1, 2, 3)));

        var token = o.SelectToken("$values[1]");
        Assert.Equal(2, (int) token);
    }

    [Fact]
    public void EvaluateSingleProperty()
    {
        var o = new JObject(
            new JProperty("Blah", 1));

        var token = o.SelectToken("Blah");
        Assert.NotNull(token);
        Assert.Equal(JTokenType.Integer, token.Type);
        Assert.Equal(1, (int) token);
    }

    [Fact]
    public void EvaluateWildcardProperty()
    {
        var o = new JObject(
            new JProperty("Blah", 1),
            new JProperty("Blah2", 2));

        var tokens = o.SelectTokens("$.*").ToList();
        Assert.NotNull(tokens);
        Assert.Equal(2, tokens.Count);
        Assert.Equal(1, (int) tokens[0]);
        Assert.Equal(2, (int) tokens[1]);
    }

    [Fact]
    public void QuoteName()
    {
        var o = new JObject(
            new JProperty("Blah", 1));

        var token = o.SelectToken("['Blah']");
        Assert.NotNull(token);
        Assert.Equal(JTokenType.Integer, token.Type);
        Assert.Equal(1, (int) token);
    }

    [Fact]
    public void EvaluateMissingProperty()
    {
        var o = new JObject(
            new JProperty("Blah", 1));

        var token = o.SelectToken("Missing[1]");
        Assert.Null(token);
    }

    [Fact]
    public void EvaluateIndexerOnObject()
    {
        var o = new JObject(
            new JProperty("Blah", 1));

        var token = o.SelectToken("[1]");
        Assert.Null(token);
    }

    [Fact]
    public void EvaluateIndexerOnObjectWithError()
    {
        var o = new JObject(
            new JProperty("Blah", 1));

        var exception = Assert.Throws<JsonException>(() => o.SelectToken("[1]", true));
        Assert.Equal("Index 1 not valid on JObject.", exception.Message);
    }

    [Fact]
    public void EvaluateWildcardIndexOnObjectWithError()
    {
        var o = new JObject(
            new JProperty("Blah", 1));

        var exception = Assert.Throws<JsonException>(() => o.SelectToken("[*]", true));
        Assert.Equal("Index * not valid on JObject.", exception.Message);
    }

    [Fact]
    public void EvaluateSliceOnObjectWithError()
    {
        var o = new JObject(
            new JProperty("Blah", 1));

        var exception = Assert.Throws<JsonException>(() => o.SelectToken("[:]", true));
        Assert.Equal("Array slice is not valid on JObject.", exception.Message);
    }

    [Fact]
    public void EvaluatePropertyOnArray()
    {
        var a = new JArray(1, 2, 3, 4, 5);

        var token = a.SelectToken("BlahBlah");
        Assert.Null(token);
    }

    [Fact]
    public void EvaluateMultipleResultsError()
    {
        var a = new JArray(1, 2, 3, 4, 5);

        var exception = Assert.Throws<JsonException>(() => a.SelectToken("[0, 1]"));
        Assert.Equal("Path returned multiple tokens.", exception.Message);
    }

    [Fact]
    public void EvaluatePropertyOnArrayWithError()
    {
        var a = new JArray(1, 2, 3, 4, 5);

        var exception = Assert.Throws<JsonException>(() => a.SelectToken("BlahBlah", true));
        Assert.Equal("Property 'BlahBlah' not valid on JArray.", exception.Message);
    }

    [Fact]
    public void EvaluateNoResultsWithMultipleArrayIndexes()
    {
        var a = new JArray(1, 2, 3, 4, 5);

        var exception = Assert.Throws<JsonException>(() => a.SelectToken("[9,10]", true));
        Assert.Equal("Index 9 outside the bounds of JArray.", exception.Message);
    }

    [Fact]
    public void EvaluateMissingPropertyWithError()
    {
        var o = new JObject(
            new JProperty("Blah", 1));

        var exception = Assert.Throws<JsonException>(() => o.SelectToken("Missing", true));
        Assert.Equal("Property 'Missing' does not exist on JObject.", exception.Message);
    }

    [Fact]
    public void EvaluatePropertyWithoutError()
    {
        var o = new JObject(
            new JProperty("Blah", 1));

        var v = (JValue) o.SelectToken("Blah", true);
        Assert.Equal(1, v.Value);
    }

    [Fact]
    public void EvaluateMissingPropertyIndexWithError()
    {
        var o = new JObject(
            new JProperty("Blah", 1));

        var exception = Assert.Throws<JsonException>(() => o.SelectToken("['Missing','Missing2']", true));
        Assert.Equal("Property 'Missing' does not exist on JObject.", exception.Message);
    }

    [Fact]
    public void EvaluateMultiPropertyIndexOnArrayWithError()
    {
        var a = new JArray(1, 2, 3, 4, 5);

        var exception = Assert.Throws<JsonException>(() => a.SelectToken("['Missing','Missing2']", true));
        Assert.Equal("Properties 'Missing', 'Missing2' not valid on JArray.", exception.Message);
    }

    [Fact]
    public void EvaluateArraySliceWithError()
    {
        var a = new JArray(1, 2, 3, 4, 5);

        var exception = Assert.Throws<JsonException>(() => a.SelectToken("[99:]", true));
        Assert.Equal("Array slice of 99 to * returned no results.", exception.Message);

        var exception1 = Assert.Throws<JsonException>(() => a.SelectToken("[1:-19]", true));
        Assert.Equal("Array slice of 1 to -19 returned no results.", exception1.Message);

        var exception2 = Assert.Throws<JsonException>(() => a.SelectToken("[:-19]", true));
        Assert.Equal("Array slice of * to -19 returned no results.", exception2.Message);

        a = [];

        var exception3 = Assert.Throws<JsonException>(() => a.SelectToken("[:]", true));
        Assert.Equal("Array slice of * to * returned no results.", exception3.Message);
    }

    [Fact]
    public void EvaluateOutOfBoundsIndex()
    {
        var a = new JArray(1, 2, 3, 4, 5);

        var token = a.SelectToken("[1000].Ha");
        Assert.Null(token);
    }

    [Fact]
    public void EvaluateArrayOutOfBoundsIndexWithError()
    {
        var a = new JArray(1, 2, 3, 4, 5);

        var exception = Assert.Throws<JsonException>(() => a.SelectToken("[1000].Ha", true));
        Assert.Equal("Index 1000 outside the bounds of JArray.", exception.Message);
    }

    [Fact]
    public void EvaluateArray()
    {
        var a = new JArray(1, 2, 3, 4);

        var token = a.SelectToken("[1]");
        Assert.NotNull(token);
        Assert.Equal(JTokenType.Integer, token.Type);
        Assert.Equal(2, (int) token);
    }

    [Fact]
    public void EvaluateArraySlice()
    {
        var a = new JArray(1, 2, 3, 4, 5, 6, 7, 8, 9);

        var tokens = a.SelectTokens("[-3:]").ToList();
        Assert.Equal(3, tokens.Count);
        Assert.Equal(7, (int) tokens[0]);
        Assert.Equal(8, (int) tokens[1]);
        Assert.Equal(9, (int) tokens[2]);

        tokens = a.SelectTokens("[-1:-2:-1]").ToList();
        Assert.Single(tokens);
        Assert.Equal(9, (int) tokens[0]);

        tokens = a.SelectTokens("[-2:-1]").ToList();
        Assert.Single(tokens);
        Assert.Equal(8, (int) tokens[0]);

        tokens = a.SelectTokens("[1:1]").ToList();
        Assert.Empty(tokens);

        tokens = a.SelectTokens("[1:2]").ToList();
        Assert.Single(tokens);
        Assert.Equal(2, (int) tokens[0]);

        tokens = a.SelectTokens("[::-1]").ToList();
        Assert.Equal(9, tokens.Count);
        Assert.Equal(9, (int) tokens[0]);
        Assert.Equal(8, (int) tokens[1]);
        Assert.Equal(7, (int) tokens[2]);
        Assert.Equal(6, (int) tokens[3]);
        Assert.Equal(5, (int) tokens[4]);
        Assert.Equal(4, (int) tokens[5]);
        Assert.Equal(3, (int) tokens[6]);
        Assert.Equal(2, (int) tokens[7]);
        Assert.Equal(1, (int) tokens[8]);

        tokens = a.SelectTokens("[::-2]").ToList();
        Assert.Equal(5, tokens.Count);
        Assert.Equal(9, (int) tokens[0]);
        Assert.Equal(7, (int) tokens[1]);
        Assert.Equal(5, (int) tokens[2]);
        Assert.Equal(3, (int) tokens[3]);
        Assert.Equal(1, (int) tokens[4]);
    }

    [Fact]
    public void EvaluateWildcardArray()
    {
        var a = new JArray(1, 2, 3, 4);

        var tokens = a.SelectTokens("[*]").ToList();
        Assert.NotNull(tokens);
        Assert.Equal(4, tokens.Count);
        Assert.Equal(1, (int) tokens[0]);
        Assert.Equal(2, (int) tokens[1]);
        Assert.Equal(3, (int) tokens[2]);
        Assert.Equal(4, (int) tokens[3]);
    }

    [Fact]
    public void EvaluateArrayMultipleIndexes()
    {
        var a = new JArray(1, 2, 3, 4);

        var tokens = a.SelectTokens("[1,2,0]").ToList();
        Assert.Equal(3, tokens.Count);
        Assert.Equal(2, (int) tokens.ElementAt(0));
        Assert.Equal(3, (int) tokens.ElementAt(1));
        Assert.Equal(1, (int) tokens.ElementAt(2));
    }

    [Fact]
    public void EvaluateScan()
    {
        var o1 = new JObject {{"Name", 1}};
        var o2 = new JObject {{"Name", 2}};
        var a = new JArray(o1, o2);

        var tokens = a.SelectTokens("$..Name").ToList();
        Assert.NotNull(tokens);
        Assert.Equal(2, tokens.Count);
        Assert.Equal(1, (int) tokens[0]);
        Assert.Equal(2, (int) tokens[1]);
    }

    [Fact]
    public void EvaluateWildcardScan()
    {
        var o1 = new JObject {{"Name", 1}};
        var o2 = new JObject {{"Name", 2}};
        var a = new JArray(o1, o2);

        var tokens = a.SelectTokens("$..*").ToList();
        Assert.NotNull(tokens);
        Assert.Equal(5, tokens.Count);
        Assert.True(JToken.DeepEquals(a, tokens[0]));
        Assert.True(JToken.DeepEquals(o1, tokens[1]));
        Assert.Equal(1, (int) tokens[2]);
        Assert.True(JToken.DeepEquals(o2, tokens[3]));
        Assert.Equal(2, (int) tokens[4]);
    }

    [Fact]
    public void EvaluateScanNestResults()
    {
        var o1 = new JObject {{"Name", 1}};
        var o2 = new JObject {{"Name", 2}};
        var o3 = new JObject {{"Name", new JObject {{"Name", new JArray(3)}}}};
        var a = new JArray(o1, o2, o3);

        var tokens = a.SelectTokens("$..Name").ToList();
        Assert.NotNull(tokens);
        Assert.Equal(4, tokens.Count);
        Assert.Equal(1, (int) tokens[0]);
        Assert.Equal(2, (int) tokens[1]);
        Assert.True(JToken.DeepEquals(new JObject {{"Name", new JArray(3)}}, tokens[2]));
        Assert.True(JToken.DeepEquals(new JArray(3), tokens[3]));
    }

    [Fact]
    public void EvaluateWildcardScanNestResults()
    {
        var o1 = new JObject {{"Name", 1}};
        var o2 = new JObject {{"Name", 2}};
        var o3 = new JObject {{"Name", new JObject {{"Name", new JArray(3)}}}};
        var a = new JArray(o1, o2, o3);

        var tokens = a.SelectTokens("$..*").ToList();
        Assert.NotNull(tokens);
        Assert.Equal(9, tokens.Count);

        Assert.True(JToken.DeepEquals(a, tokens[0]));
        Assert.True(JToken.DeepEquals(o1, tokens[1]));
        Assert.Equal(1, (int) tokens[2]);
        Assert.True(JToken.DeepEquals(o2, tokens[3]));
        Assert.Equal(2, (int) tokens[4]);
        Assert.True(JToken.DeepEquals(o3, tokens[5]));
        Assert.True(JToken.DeepEquals(new JObject {{"Name", new JArray(3)}}, tokens[6]));
        Assert.True(JToken.DeepEquals(new JArray(3), tokens[7]));
        Assert.Equal(3, (int) tokens[8]);
    }

    [Fact]
    public void EvaluateSinglePropertyReturningArray()
    {
        var o = new JObject(
            new JProperty("Blah", new[] {1, 2, 3}));

        var token = o.SelectToken("Blah");
        Assert.NotNull(token);
        Assert.Equal(JTokenType.Array, token.Type);

        token = o.SelectToken("Blah[2]");
        Assert.Equal(JTokenType.Integer, token.Type);
        Assert.Equal(3, (int) token);
    }

    [Fact]
    public void EvaluateLastSingleCharacterProperty()
    {
        var o2 = JObject.Parse("{'People':[{'N':'Jeff'}]}");
        var a2 = (string) o2.SelectToken("People[0].N");

        Assert.Equal("Jeff", a2);
    }

    [Fact]
    public void ExistsQuery()
    {
        var a = new JArray(new JObject(new JProperty("hi", "ho")), new JObject(new JProperty("hi2", "ha")));

        var tokens = a.SelectTokens("[ ?( @.hi ) ]").ToList();
        Assert.NotNull(tokens);
        Assert.Single(tokens);
        Assert.True(JToken.DeepEquals(new JObject(new JProperty("hi", "ho")), tokens[0]));
    }

    [Fact]
    public void EqualsQuery()
    {
        var a = new JArray(
            new JObject(new JProperty("hi", "ho")),
            new JObject(new JProperty("hi", "ha")));

        var tokens = a.SelectTokens("[ ?( @.['hi'] == 'ha' ) ]").ToList();
        Assert.NotNull(tokens);
        Assert.Single(tokens);
        Assert.True(JToken.DeepEquals(new JObject(new JProperty("hi", "ha")), tokens[0]));
    }

    [Fact]
    public void NotEqualsQuery()
    {
        var a = new JArray(
            new JArray(new JObject(new JProperty("hi", "ho"))),
            new JArray(new JObject(new JProperty("hi", "ha"))));

        var tokens = a.SelectTokens("[ ?( @..hi <> 'ha' ) ]").ToList();
        Assert.NotNull(tokens);
        Assert.Single(tokens);
        Assert.True(JToken.DeepEquals(new JArray(new JObject(new JProperty("hi", "ho"))), tokens[0]));
    }

    [Fact]
    public void NoPathQuery()
    {
        var a = new JArray(1, 2, 3);

        var tokens = a.SelectTokens("[ ?( @ > 1 ) ]").ToList();
        Assert.NotNull(tokens);
        Assert.Equal(2, tokens.Count);
        Assert.Equal(2, (int) tokens[0]);
        Assert.Equal(3, (int) tokens[1]);
    }

    [Fact]
    public void MultipleQueries()
    {
        var a = new JArray(1, 2, 3, 4, 5, 6, 7, 8, 9);

        // json path does item based evaluation - http://www.sitepen.com/blog/2008/03/17/jsonpath-support/
        // first query resolves array to ints
        // int has no children to query
        var tokens = a.SelectTokens("[?(@ <> 1)][?(@ <> 4)][?(@ < 7)]").ToList();
        Assert.NotNull(tokens);
        Assert.Empty(tokens);
    }

    [Fact]
    public void GreaterQuery()
    {
        var a = new JArray(
            new JObject(new JProperty("hi", 1)),
            new JObject(new JProperty("hi", 2)),
            new JObject(new JProperty("hi", 3)));

        var tokens = a.SelectTokens("[ ?( @.hi > 1 ) ]").ToList();
        Assert.NotNull(tokens);
        Assert.Equal(2, tokens.Count);
        Assert.True(JToken.DeepEquals(new JObject(new JProperty("hi", 2)), tokens[0]));
        Assert.True(JToken.DeepEquals(new JObject(new JProperty("hi", 3)), tokens[1]));
    }

    [Fact]
    public void LesserQuery_ValueFirst()
    {
        var a = new JArray(
            new JObject(new JProperty("hi", 1)),
            new JObject(new JProperty("hi", 2)),
            new JObject(new JProperty("hi", 3)));

        var tokens = a.SelectTokens("[ ?( 1 < @.hi ) ]").ToList();
        Assert.NotNull(tokens);
        Assert.Equal(2, tokens.Count);
        Assert.True(JToken.DeepEquals(new JObject(new JProperty("hi", 2)), tokens[0]));
        Assert.True(JToken.DeepEquals(new JObject(new JProperty("hi", 3)), tokens[1]));
    }

    [Fact]
    public void GreaterQueryBigInteger()
    {
        var a = new JArray(
            new JObject(new JProperty("hi", new BigInteger(1))),
            new JObject(new JProperty("hi", new BigInteger(2))),
            new JObject(new JProperty("hi", new BigInteger(3))));

        var tokens = a.SelectTokens("[ ?( @.hi > 1 ) ]").ToList();
        Assert.NotNull(tokens);
        Assert.Equal(2, tokens.Count);
        Assert.True(JToken.DeepEquals(new JObject(new JProperty("hi", 2)), tokens[0]));
        Assert.True(JToken.DeepEquals(new JObject(new JProperty("hi", 3)), tokens[1]));
    }

    [Fact]
    public void GreaterOrEqualQuery()
    {
        var a = new JArray(
            new JObject(new JProperty("hi", 1)),
            new JObject(new JProperty("hi", 2)),
            new JObject(new JProperty("hi", 2.0)),
            new JObject(new JProperty("hi", 3)));

        var tokens = a.SelectTokens("[ ?( @.hi >= 1 ) ]").ToList();
        Assert.NotNull(tokens);
        Assert.Equal(4, tokens.Count);
        Assert.True(JToken.DeepEquals(new JObject(new JProperty("hi", 1)), tokens[0]));
        Assert.True(JToken.DeepEquals(new JObject(new JProperty("hi", 2)), tokens[1]));
        Assert.True(JToken.DeepEquals(new JObject(new JProperty("hi", 2.0)), tokens[2]));
        Assert.True(JToken.DeepEquals(new JObject(new JProperty("hi", 3)), tokens[3]));
    }

    [Fact]
    public void NestedQuery()
    {
        var a = new JArray(
            new JObject(
                new JProperty("name", "Bad Boys"),
                new JProperty("cast", new JArray(
                    new JObject(new JProperty("name", "Will Smith"))))),
            new JObject(
                new JProperty("name", "Independence Day"),
                new JProperty("cast", new JArray(
                    new JObject(new JProperty("name", "Will Smith"))))),
            new JObject(
                new JProperty("name", "The Rock"),
                new JProperty("cast", new JArray(
                    new JObject(new JProperty("name", "Nick Cage")))))
        );

        var tokens = a.SelectTokens("[?(@.cast[?(@.name=='Will Smith')])].name").ToList();
        Assert.NotNull(tokens);
        Assert.Equal(2, tokens.Count);
        Assert.Equal("Bad Boys", (string) tokens[0]);
        Assert.Equal("Independence Day", (string) tokens[1]);
    }

    [Fact]
    public void MultiplePaths()
    {
        var a = JArray.Parse(
            """
            [
              {
                "price": 199,
                "max_price": 200
              },
              {
                "price": 200,
                "max_price": 200
              },
              {
                "price": 201,
                "max_price": 200
              }
            ]
            """);

        var results = a.SelectTokens("[?(@.price > @.max_price)]").ToList();
        Assert.Single(results);
        Assert.Equal(a[2], results[0]);
    }

    [Fact]
    public void Exists_True()
    {
        var a = JArray.Parse(
            """
            [
              {
                "price": 199,
                "max_price": 200
              },
              {
                "price": 200,
                "max_price": 200
              },
              {
                "price": 201,
                "max_price": 200
              }
            ]
            """);

        var results = a.SelectTokens("[?(true)]").ToList();
        Assert.Equal(3, results.Count);
        Assert.Equal(a[0], results[0]);
        Assert.Equal(a[1], results[1]);
        Assert.Equal(a[2], results[2]);
    }

    [Fact]
    public void Exists_Null()
    {
        var a = JArray.Parse(
            """
            [
              {
                "price": 199,
                "max_price": 200
              },
              {
                "price": 200,
                "max_price": 200
              },
              {
                "price": 201,
                "max_price": 200
              }
            ]
            """);

        var results = a.SelectTokens("[?(true)]").ToList();
        Assert.Equal(3, results.Count);
        Assert.Equal(a[0], results[0]);
        Assert.Equal(a[1], results[1]);
        Assert.Equal(a[2], results[2]);
    }

    [Fact]
    public void WildcardWithProperty()
    {
        var o = JObject.Parse(
            """
            {
                "station": 92000041000001,
                "containers": [
                    {
                        "id": 1,
                        "text": "Sort system",
                        "containers": [
                            {
                                "id": "2",
                                "text": "Yard 11"
                            },
                            {
                                "id": "92000020100006",
                                "text": "Sort yard 12"
                            },
                            {
                                "id": "92000020100005",
                                "text": "Yard 13"
                            }
                        ]
                    },
                    {
                        "id": "92000020100011",
                        "text": "TSP-1"
                    },
                    {
                        "id":"92000020100007",
                        "text": "Passenger 15"
                    }
                ]
            }
            """);

        var tokens = o.SelectTokens("$..*[?(@.text)]").ToList();
        var i = 0;
        Assert.Equal("Sort system", (string) tokens[i++]["text"]);
        Assert.Equal("TSP-1", (string) tokens[i++]["text"]);
        Assert.Equal("Passenger 15", (string) tokens[i++]["text"]);
        Assert.Equal("Yard 11", (string) tokens[i++]["text"]);
        Assert.Equal("Sort yard 12", (string) tokens[i++]["text"]);
        Assert.Equal("Yard 13", (string) tokens[i++]["text"]);
        Assert.Equal(6, tokens.Count);
    }

    [Fact]
    public void QueryAgainstNonStringValues()
    {
        var values = new List<object>
        {
            "ff2dc672-6e15-4aa2-afb0-18f4f69596ad",
            new Guid("ff2dc672-6e15-4aa2-afb0-18f4f69596ad"),
            "http://localhost",
            new Uri("http://localhost"),
            "2000-12-05T05:07:59Z",
            new DateTime(2000, 12, 5, 5, 7, 59, DateTimeKind.Utc),
            "2000-12-05T05:07:59-10:00",
            new DateTimeOffset(2000, 12, 5, 5, 7, 59, -TimeSpan.FromHours(10)),
            "SGVsbG8gd29ybGQ=",
            "Hello world"u8.ToArray(),
            "365.23:59:59",
            new TimeSpan(365, 23, 59, 59)
        };

        var o = new JObject(
            new JProperty("prop",
                new JArray(
                    values.Select(v => new JObject(new JProperty("childProp", v)))
                )
            )
        );

        var tokens = o.SelectTokens("$.prop[?(@.childProp =='ff2dc672-6e15-4aa2-afb0-18f4f69596ad')]").ToList();
        Assert.Equal(2, tokens.Count);

        tokens = o.SelectTokens("$.prop[?(@.childProp =='http://localhost')]").ToList();
        Assert.Equal(2, tokens.Count);

        tokens = o.SelectTokens("$.prop[?(@.childProp =='2000-12-05T05:07:59Z')]").ToList();
        Assert.Equal(2, tokens.Count);

        tokens = o.SelectTokens("$.prop[?(@.childProp =='2000-12-05T05:07:59-10:00')]").ToList();
        Assert.Equal(2, tokens.Count);

        tokens = o.SelectTokens("$.prop[?(@.childProp =='SGVsbG8gd29ybGQ=')]").ToList();
        Assert.Equal(2, tokens.Count);

        tokens = o.SelectTokens("$.prop[?(@.childProp =='365.23:59:59')]").ToList();
        Assert.Equal(2, tokens.Count);
    }

    [Fact]
    public void Example()
    {
        var o = JObject.Parse(
            """
            {
                "Stores": [
                  "Lambton Quay",
                  "Willis Street"
                ],
                "Manufacturers": [
                  {
                    "Name": "Acme Co",
                    "Products": [
                      {
                        "Name": "Anvil",
                        "Price": 50
                      }
                    ]
                  },
                  {
                    "Name": "Contoso",
                    "Products": [
                      {
                        "Name": "Elbow Grease",
                        "Price": 99.95
                      },
                      {
                        "Name": "Headlight Fluid",
                        "Price": 4
                      }
                    ]
                  }
                ]
              }
            """);

        var name = (string) o.SelectToken("Manufacturers[0].Name");
        // Acme Co

        var productPrice = (decimal) o.SelectToken("Manufacturers[0].Products[0].Price");
        // 50

        var productName = (string) o.SelectToken("Manufacturers[1].Products[0].Name");
        // Elbow Grease

        Assert.Equal("Acme Co", name);
        Assert.Equal(50m, productPrice);
        Assert.Equal("Elbow Grease", productName);

        var storeNames = o.SelectToken("Stores").Select(s => (string) s).ToList();
        // Lambton Quay
        // Willis Street

        var firstProductNames = o["Manufacturers"].Select(m => (string) m.SelectToken("Products[1].Name")).ToList();
        // null
        // Headlight Fluid

        var totalPrice = o["Manufacturers"].Sum(m => (decimal) m.SelectToken("Products[0].Price"));
        // 149.95

        Assert.Equal(2, storeNames.Count);
        Assert.Equal("Lambton Quay", storeNames[0]);
        Assert.Equal("Willis Street", storeNames[1]);
        Assert.Equal(2, firstProductNames.Count);
        Assert.Null(firstProductNames[0]);
        Assert.Equal("Headlight Fluid", firstProductNames[1]);
        Assert.Equal(149.95m, totalPrice);
    }

    [Fact]
    public void NotEqualsAndNonPrimitiveValues()
    {
        var json = """
            [
              {
                "name": "string",
                "value": "aString"
              },
              {
                "name": "number",
                "value": 123
              },
              {
                "name": "array",
                "value": [
                  1,
                  2,
                  3,
                  4
                ]
              },
              {
                "name": "object",
                "value": {
                  "1": 1
                }
              }
            ]
            """;

        var a = JArray.Parse(json);

        var result = a.SelectTokens("$.[?(@.value!=1)]").ToList();
        Assert.Equal(4, result.Count);

        result = a.SelectTokens("$.[?(@.value!='2000-12-05T05:07:59-10:00')]").ToList();
        Assert.Equal(4, result.Count);

        result = a.SelectTokens("$.[?(@.value!=null)]").ToList();
        Assert.Equal(4, result.Count);

        result = a.SelectTokens("$.[?(@.value!=123)]").ToList();
        Assert.Equal(3, result.Count);

        result = a.SelectTokens("$.[?(@.value)]").ToList();
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void RootInFilter()
    {
        var json = """
                   [
                      {
                         "store" : {
                            "book" : [
                               {
                                  "category" : "reference",
                                  "author" : "Nigel Rees",
                                  "title" : "Sayings of the Century",
                                  "price" : 8.95
                               },
                               {
                                  "category" : "fiction",
                                  "author" : "Evelyn Waugh",
                                  "title" : "Sword of Honour",
                                  "price" : 12.99
                               },
                               {
                                  "category" : "fiction",
                                  "author" : "Herman Melville",
                                  "title" : "Moby Dick",
                                  "isbn" : "0-553-21311-3",
                                  "price" : 8.99
                               },
                               {
                                  "category" : "fiction",
                                  "author" : "J. R. R. Tolkien",
                                  "title" : "The Lord of the Rings",
                                  "isbn" : "0-395-19395-8",
                                  "price" : 22.99
                               }
                            ],
                            "bicycle" : {
                               "color" : "red",
                               "price" : 19.95
                            }
                         },
                         "expensive" : 10
                      }
                   ]
                   """;

        var a = JArray.Parse(json);

        var result = a.SelectTokens("$.[?($.[0].store.bicycle.price < 20)]").ToList();
        Assert.Single(result);

        result = a.SelectTokens("$.[?($.[0].store.bicycle.price < 10)]").ToList();
        Assert.Empty(result);
    }

    [Fact]
    public void RootInFilterWithRootObject()
    {
        var json = """
            {
                "store" : {
                    "book" : [
                        {
                            "category" : "reference",
                            "author" : "Nigel Rees",
                            "title" : "Sayings of the Century",
                            "price" : 8.95
                        },
                        {
                            "category" : "fiction",
                            "author" : "Evelyn Waugh",
                            "title" : "Sword of Honour",
                            "price" : 12.99
                        },
                        {
                            "category" : "fiction",
                            "author" : "Herman Melville",
                            "title" : "Moby Dick",
                            "isbn" : "0-553-21311-3",
                            "price" : 8.99
                        },
                        {
                            "category" : "fiction",
                            "author" : "J. R. R. Tolkien",
                            "title" : "The Lord of the Rings",
                            "isbn" : "0-395-19395-8",
                            "price" : 22.99
                        }
                    ],
                    "bicycle" : [
                        {
                            "color" : "red",
                            "price" : 19.95
                        }
                    ]
                },
                "expensive" : 10
            }
            """;

        var a = JObject.Parse(json);

        var result = a.SelectTokens("$..book[?(@.price <= $['expensive'])]").ToList();
        Assert.Equal(2, result.Count);

        result = a.SelectTokens("$.store..[?(@.price > $.expensive)]").ToList();
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void RootInFilterWithInitializers()
    {
        var rootObject = new JObject
        {
            {"referenceDate", new JValue(DateTime.MinValue)},
            {
                "dateObjectsArray",
                new JArray
                {
                    new JObject {{"date", new JValue(DateTime.MinValue)}},
                    new JObject {{"date", new JValue(DateTime.MaxValue)}},
                    new JObject {{"date", new JValue(DateTime.Now)}},
                    new JObject {{"date", new JValue(DateTime.MinValue)}}
                }
            }
        };

        var result = rootObject.SelectTokens("$.dateObjectsArray[?(@.date == $.referenceDate)]").ToList();
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void IdentityOperator()
    {
        var o = JObject.Parse(
            """
            {
                'Values': [
                    {
                        'Coercible': 1,
                        'Name': 'Number'
                    },
                    {
                        'Coercible': '1',
                        'Name': 'String'
                    }]
            }
            """);

        // just to verify expected behavior hasn't changed
        var sanity1 = o.SelectTokens("Values[?(@.Coercible == '1')].Name").Select(_ => (string) _);
        var sanity2 = o.SelectTokens("Values[?(@.Coercible != '1')].Name").Select(_ => (string) _);
        // new behavior
        var mustBeNumber1 = o.SelectTokens("Values[?(@.Coercible === 1)].Name").Select(_ => (string) _);
        var mustBeString1 = o.SelectTokens("Values[?(@.Coercible !== 1)].Name").Select(_ => (string) _);
        var mustBeString2 = o.SelectTokens("Values[?(@.Coercible === '1')].Name").Select(_ => (string) _);
        var mustBeNumber2 = o.SelectTokens("Values[?(@.Coercible !== '1')].Name").Select(_ => (string) _);

        // FAILS-- JPath returns { "String" }
        //Xunit.Assert.Equal(new[] { "Number", "String" }, sanity1);
        // FAILS-- JPath returns { "Number" }
        //Assert.IsTrue(!sanity2.Any());
        Assert.Equal("Number", mustBeNumber1.Single());
        Assert.Equal("String", mustBeString1.Single());
        Assert.Equal("Number", mustBeNumber2.Single());
        Assert.Equal("String", mustBeString2.Single());
    }

    [Fact]
    public void QueryWithEscapedPath()
    {
        var token = JToken.Parse(
            """
            {
            "Property": [
                  {
                    "@Name": "x",
                    "@Value": "y",
                    "@Type": "FindMe"
                  }
               ]
            }
            """);

        var tokens = token.SelectTokens("$..[?(@.['@Type'] == 'FindMe')]").ToList();
        Assert.Single(tokens);
    }

    [Fact]
    public void Equals_FloatWithInt()
    {
        var token = JToken.Parse(
            """
            {
              "Values": [
                {
                  "Property": 1
                }
              ]
            }
            """);

        Assert.NotNull(token.SelectToken("Values[?(@.Property == 1.0)]"));
    }

    [Theory]
    [TestCaseSource(nameof(StrictMatchWithInverseTestData))]
    public static void EqualsStrict(string value1, string value2, bool matchStrict)
    {
        var completeJson = $$"""
                             {
                               "Values": [
                                 {
                                   "Property": {{value1}}
                                 }
                               ]
                             }
                             """;
        var completeEqualsStrictPath = $"$.Values[?(@.Property === {value2})]";
        var completeNotEqualsStrictPath = $"$.Values[?(@.Property !== {value2})]";

        var token = JToken.Parse(completeJson);

        var hasEqualsStrict = token.SelectTokens(completeEqualsStrictPath).Any();
        Assert.Equal(matchStrict, hasEqualsStrict);

        var hasNotEqualsStrict = token.SelectTokens(completeNotEqualsStrictPath).Any();
        Assert.NotEqual(matchStrict, hasNotEqualsStrict);
    }

    public static IEnumerable<object[]> StrictMatchWithInverseTestData()
    {
        foreach (var item in StrictMatchTestData())
        {
            yield return [item[0], item[1], item[2]];

            if (!item[0].Equals(item[1]))
            {
                // Test the inverse
                yield return [item[1], item[0], item[2]];
            }
        }
    }

    static IEnumerable<object[]> StrictMatchTestData()
    {
        yield return ["1", "1", true];
        yield return ["1", "1.0", true];
        yield return ["1", "true", false];
        yield return ["1", "'1'", false];
        yield return ["'1'", "'1'", true];
        yield return ["false", "false", true];
        yield return ["true", "false", false];
        yield return ["1", "1.1", false];
        yield return ["1", "null", false];
        yield return ["null", "null", true];
        yield return ["null", "'null'", false];
    }
}