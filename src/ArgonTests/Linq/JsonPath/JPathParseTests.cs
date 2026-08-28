// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

public class JPathParseTests : TestFixtureBase
{
    [Fact]
    public void BooleanQuery_TwoValues()
    {
        var path = new JPath("[?(1 > 2)]");
        Assert.Single(path.Filters);
        var booleanExpression = (BooleanQueryExpression) ((QueryFilter) path.Filters[0]).Expression;
        Assert.Equal(1, (int) (JValue) booleanExpression.Left);
        Assert.Equal(2, (int) (JValue) booleanExpression.Right);
        Assert.Equal(QueryOperator.GreaterThan, booleanExpression.Operator);
    }

    [Fact]
    public void BooleanQuery_TwoPaths()
    {
        var path = new JPath("[?(@.price > @.max_price)]");
        Assert.Single(path.Filters);
        var booleanExpression = (BooleanQueryExpression) ((QueryFilter) path.Filters[0]).Expression;
        var leftPaths = (List<PathFilter>) booleanExpression.Left;
        var rightPaths = (List<PathFilter>) booleanExpression.Right;

        Assert.Equal("price", ((FieldFilter) leftPaths[0]).Name);
        Assert.Equal("max_price", ((FieldFilter) rightPaths[0]).Name);
        Assert.Equal(QueryOperator.GreaterThan, booleanExpression.Operator);
    }

    [Fact]
    public void SingleProperty()
    {
        var path = new JPath("Blah");
        Assert.Single(path.Filters);
        Assert.Equal("Blah", ((FieldFilter) path.Filters[0]).Name);
    }

    [Fact]
    public void SingleQuotedProperty()
    {
        var path = new JPath("['Blah']");
        Assert.Single(path.Filters);
        Assert.Equal("Blah", ((FieldFilter) path.Filters[0]).Name);
    }

    [Fact]
    public void SingleQuotedPropertyWithWhitespace()
    {
        var path = new JPath("[  'Blah'  ]");
        Assert.Single(path.Filters);
        Assert.Equal("Blah", ((FieldFilter) path.Filters[0]).Name);
    }

    [Fact]
    public void SingleQuotedPropertyWithDots()
    {
        var path = new JPath("['Blah.Ha']");
        Assert.Single(path.Filters);
        Assert.Equal("Blah.Ha", ((FieldFilter) path.Filters[0]).Name);
    }

    [Fact]
    public void SingleQuotedPropertyWithBrackets()
    {
        var path = new JPath("['[*]']");
        Assert.Single(path.Filters);
        Assert.Equal("[*]", ((FieldFilter) path.Filters[0]).Name);
    }

    [Fact]
    public void SinglePropertyWithRoot()
    {
        var path = new JPath("$.Blah");
        Assert.Single(path.Filters);
        Assert.Equal("Blah", ((FieldFilter) path.Filters[0]).Name);
    }

    [Fact]
    public void SinglePropertyWithRootWithStartAndEndWhitespace()
    {
        var path = new JPath(" $.Blah ");
        Assert.Single(path.Filters);
        Assert.Equal("Blah", ((FieldFilter) path.Filters[0]).Name);
    }

    [Fact]
    public void RootWithBadWhitespace()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath("$ .Blah"));
        Assert.Equal("Unexpected character while parsing path:  ", exception.Message);
    }

    [Fact]
    public void NoFieldNameAfterDot()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath("$.Blah."));
        Assert.Equal("Unexpected end while parsing path.", exception.Message);
    }

    [Fact]
    public void RootWithBadWhitespace2()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath("$. Blah"));
        Assert.Equal("Unexpected character while parsing path:  ", exception.Message);
    }

    [Fact]
    public void WildcardPropertyWithRoot()
    {
        var path = new JPath("$.*");
        Assert.Single(path.Filters);
        Assert.Null(((FieldFilter) path.Filters[0]).Name);
    }

    [Fact]
    public void WildcardArrayWithRoot()
    {
        var path = new JPath("$.[*]");
        Assert.Single(path.Filters);
        Assert.Null(((ArrayIndexFilter) path.Filters[0]).Index);
    }

    [Fact]
    public void RootArrayNoDot()
    {
        var path = new JPath("$[1]");
        Assert.Single(path.Filters);
        Assert.Equal(1, ((ArrayIndexFilter) path.Filters[0]).Index);
    }

    [Fact]
    public void WildcardArray()
    {
        var path = new JPath("[*]");
        Assert.Single(path.Filters);
        Assert.Null(((ArrayIndexFilter) path.Filters[0]).Index);
    }

    [Fact]
    public void WildcardArrayWithProperty()
    {
        var path = new JPath("[ * ].derp");
        Assert.Equal(2, path.Filters.Count);
        Assert.Null(((ArrayIndexFilter) path.Filters[0]).Index);
        Assert.Equal("derp", ((FieldFilter) path.Filters[1]).Name);
    }

    [Fact]
    public void QuotedWildcardPropertyWithRoot()
    {
        var path = new JPath("$.['*']");
        Assert.Single(path.Filters);
        Assert.Equal("*", ((FieldFilter) path.Filters[0]).Name);
    }

    [Fact]
    public void SingleScanWithRoot()
    {
        var path = new JPath("$..Blah");
        Assert.Single(path.Filters);
        Assert.Equal("Blah", ((ScanFilter) path.Filters[0]).Name);
    }

    [Fact]
    public void QueryTrue()
    {
        var path = new JPath("$.elements[?(true)]");
        Assert.Equal(2, path.Filters.Count);
        Assert.Equal("elements", ((FieldFilter) path.Filters[0]).Name);
        Assert.Equal(QueryOperator.Exists, ((QueryFilter) path.Filters[1]).Expression.Operator);
    }

    [Fact]
    public void ScanQuery()
    {
        var path = new JPath("$.elements..[?(@.id=='AAA')]");
        Assert.Equal(2, path.Filters.Count);
        Assert.Equal("elements", ((FieldFilter) path.Filters[0]).Name);

        var expression = (BooleanQueryExpression) ((QueryScanFilter) path.Filters[1]).Expression;

        var paths = (List<PathFilter>) expression.Left;

        object o = paths[0];
        Assert.IsType<FieldFilter>(o);
    }

    [Fact]
    public void WildcardScanWithRoot()
    {
        var path = new JPath("$..*");
        Assert.Single(path.Filters);
        Assert.Null(((ScanFilter) path.Filters[0]).Name);
    }

    [Fact]
    public void WildcardScanWithRootWithWhitespace()
    {
        var path = new JPath("$..* ");
        Assert.Single(path.Filters);
        Assert.Null(((ScanFilter) path.Filters[0]).Name);
    }

    [Fact]
    public void TwoProperties()
    {
        var path = new JPath("Blah.Two");
        Assert.Equal(2, path.Filters.Count);
        Assert.Equal("Blah", ((FieldFilter) path.Filters[0]).Name);
        Assert.Equal("Two", ((FieldFilter) path.Filters[1]).Name);
    }

    [Fact]
    public void OnePropertyOneScan()
    {
        var path = new JPath("Blah..Two");
        Assert.Equal(2, path.Filters.Count);
        Assert.Equal("Blah", ((FieldFilter) path.Filters[0]).Name);
        Assert.Equal("Two", ((ScanFilter) path.Filters[1]).Name);
    }

    [Fact]
    public void SinglePropertyAndIndexer()
    {
        var path = new JPath("Blah[0]");
        Assert.Equal(2, path.Filters.Count);
        Assert.Equal("Blah", ((FieldFilter) path.Filters[0]).Name);
        Assert.Equal(0, ((ArrayIndexFilter) path.Filters[1]).Index);
    }

    [Fact]
    public void SinglePropertyAndExistsQuery()
    {
        var path = new JPath("Blah[ ?( @..name ) ]");
        Assert.Equal(2, path.Filters.Count);
        Assert.Equal("Blah", ((FieldFilter) path.Filters[0]).Name);
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[1]).Expression;
        Assert.Equal(QueryOperator.Exists, expressions.Operator);
        var paths = (List<PathFilter>) expressions.Left;
        Assert.Single(paths);
        Assert.Equal("name", ((ScanFilter) paths[0]).Name);
    }

    [Fact]
    public void SinglePropertyAndFilterWithWhitespace()
    {
        var path = new JPath("Blah[ ?( @.name=='hi' ) ]");
        Assert.Equal(2, path.Filters.Count);
        Assert.Equal("Blah", ((FieldFilter) path.Filters[0]).Name);
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[1]).Expression;
        Assert.Equal(QueryOperator.Equals, expressions.Operator);
        Assert.Equal("hi", (string) (JToken) expressions.Right);
    }

    [Fact]
    public void SinglePropertyAndFilterWithEscapeQuote()
    {
        var path = new JPath(@"Blah[ ?( @.name=='h\'i' ) ]");
        Assert.Equal(2, path.Filters.Count);
        Assert.Equal("Blah", ((FieldFilter) path.Filters[0]).Name);
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[1]).Expression;
        Assert.Equal(QueryOperator.Equals, expressions.Operator);
        Assert.Equal("h'i", (string) (JToken) expressions.Right);
    }

    [Fact]
    public void SinglePropertyAndFilterWithDoubleEscape()
    {
        var path = new JPath(@"Blah[ ?( @.name=='h\\i' ) ]");
        Assert.Equal(2, path.Filters.Count);
        Assert.Equal("Blah", ((FieldFilter) path.Filters[0]).Name);
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[1]).Expression;
        Assert.Equal(QueryOperator.Equals, expressions.Operator);
        Assert.Equal("h\\i", (string) (JToken) expressions.Right);
    }

    [Fact]
    public void SinglePropertyAndFilterWithEscapesAroundText()
    {
        // the quoted string is sliced out of the expression until the first escape, and the
        // text between escapes is copied in as each one is resolved
        var path = new JPath(@"Blah[ ?( @.name=='one\ttwo\\three\nfour' ) ]");
        Assert.Equal(2, path.Filters.Count);
        Assert.Equal("Blah", ((FieldFilter) path.Filters[0]).Name);
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[1]).Expression;
        Assert.Equal(QueryOperator.Equals, expressions.Operator);
        Assert.Equal("one\ttwo\\three\nfour", (string) (JToken) expressions.Right);
    }

    [Fact]
    public void SinglePropertyAndFilterWithEscapeAtEnd()
    {
        var path = new JPath(@"Blah[ ?( @.name=='hi\n' ) ]");
        Assert.Equal(2, path.Filters.Count);
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[1]).Expression;
        Assert.Equal("hi\n", (string) (JToken) expressions.Right);
    }

    [Fact]
    public void SinglePropertyAndFilterWithEmptyString()
    {
        var path = new JPath("Blah[ ?( @.name=='' ) ]");
        Assert.Equal(2, path.Filters.Count);
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[1]).Expression;
        Assert.Equal("", (string) (JToken) expressions.Right);
    }

    [Fact]
    public void SinglePropertyAndFilterWithRegexAndOptions()
    {
        var path = new JPath("Blah[ ?( @.name=~/hi/i ) ]");
        Assert.Equal(2, path.Filters.Count);
        Assert.Equal("Blah", ((FieldFilter) path.Filters[0]).Name);
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[1]).Expression;
        Assert.Equal(QueryOperator.RegexEquals, expressions.Operator);
        Assert.Equal("/hi/i", (string) (JToken) expressions.Right);
    }

    [Fact]
    public void SinglePropertyAndFilterWithRegex()
    {
        var path = new JPath("Blah[?(@.title =~ /^.*Sword.*$/)]");
        Assert.Equal(2, path.Filters.Count);
        Assert.Equal("Blah", ((FieldFilter) path.Filters[0]).Name);
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[1]).Expression;
        Assert.Equal(QueryOperator.RegexEquals, expressions.Operator);
        Assert.Equal("/^.*Sword.*$/", (string) (JToken) expressions.Right);
    }

    [Fact]
    public void SinglePropertyAndFilterWithEscapedRegex()
    {
        var path = new JPath(@"Blah[?(@.title =~ /[\-\[\]\/\{\}\(\)\*\+\?\.\\\^\$\|]/g)]");
        Assert.Equal(2, path.Filters.Count);
        Assert.Equal("Blah", ((FieldFilter) path.Filters[0]).Name);
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[1]).Expression;
        Assert.Equal(QueryOperator.RegexEquals, expressions.Operator);
        Assert.Equal(@"/[\-\[\]\/\{\}\(\)\*\+\?\.\\\^\$\|]/g", (string) (JToken) expressions.Right);
    }

    [Fact]
    public void SinglePropertyAndFilterWithOpenRegex()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath(@"Blah[?(@.title =~ /[\"));
        Assert.Equal("Path ended with an open regex.", exception.Message);
    }

    [Fact]
    public void SinglePropertyAndFilterWithUnknownEscape()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath(@"Blah[ ?( @.name=='h\i' ) ]"));
        Assert.Equal(@"Unknown escape character: \i", exception.Message);
    }

    [Fact]
    public void SinglePropertyAndFilterWithFalse()
    {
        var path = new JPath("Blah[ ?( @.name==false ) ]");
        Assert.Equal(2, path.Filters.Count);
        Assert.Equal("Blah", ((FieldFilter) path.Filters[0]).Name);
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[1]).Expression;
        Assert.Equal(QueryOperator.Equals, expressions.Operator);
        Assert.False( (bool) (JToken) expressions.Right);
    }

    [Fact]
    public void SinglePropertyAndFilterWithTrue()
    {
        var path = new JPath("Blah[ ?( @.name==true ) ]");
        Assert.Equal(2, path.Filters.Count);
        Assert.Equal("Blah", ((FieldFilter) path.Filters[0]).Name);
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[1]).Expression;
        Assert.Equal(QueryOperator.Equals, expressions.Operator);
        Assert.True((bool) (JToken) expressions.Right);
    }

    [Fact]
    public void SinglePropertyAndFilterWithNull()
    {
        var path = new JPath("Blah[ ?( @.name==null ) ]");
        Assert.Equal(2, path.Filters.Count);
        Assert.Equal("Blah", ((FieldFilter) path.Filters[0]).Name);
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[1]).Expression;
        Assert.Equal(QueryOperator.Equals, expressions.Operator);
        Assert.Null(((JValue) expressions.Right).Value);
    }

    [Fact]
    public void FilterWithScan()
    {
        var path = new JPath("[?(@..name<>null)]");
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[0]).Expression;
        var paths = (List<PathFilter>) expressions.Left;
        Assert.Equal("name", ((ScanFilter) paths[0]).Name);
    }

    [Fact]
    public void FilterWithNotEquals()
    {
        var path = new JPath("[?(@.name<>null)]");
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[0]).Expression;
        Assert.Equal(QueryOperator.NotEquals, expressions.Operator);
    }

    [Fact]
    public void FilterWithNotEquals2()
    {
        var path = new JPath("[?(@.name!=null)]");
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[0]).Expression;
        Assert.Equal(QueryOperator.NotEquals, expressions.Operator);
    }

    [Fact]
    public void FilterWithLessThan()
    {
        var path = new JPath("[?(@.name<null)]");
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[0]).Expression;
        Assert.Equal(QueryOperator.LessThan, expressions.Operator);
    }

    [Fact]
    public void FilterWithLessThanOrEquals()
    {
        var path = new JPath("[?(@.name<=null)]");
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[0]).Expression;
        Assert.Equal(QueryOperator.LessThanOrEquals, expressions.Operator);
    }

    [Fact]
    public void FilterWithGreaterThan()
    {
        var path = new JPath("[?(@.name>null)]");
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[0]).Expression;
        Assert.Equal(QueryOperator.GreaterThan, expressions.Operator);
    }

    [Fact]
    public void FilterWithGreaterThanOrEquals()
    {
        var path = new JPath("[?(@.name>=null)]");
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[0]).Expression;
        Assert.Equal(QueryOperator.GreaterThanOrEquals, expressions.Operator);
    }

    [Fact]
    public void FilterWithInteger()
    {
        var path = new JPath("[?(@.name>=12)]");
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[0]).Expression;
        Assert.Equal(12, (int) (JToken) expressions.Right);
    }

    [Fact]
    public void FilterWithNegativeInteger()
    {
        var path = new JPath("[?(@.name>=-12)]");
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[0]).Expression;
        Assert.Equal(-12, (int) (JToken) expressions.Right);
    }

    [Fact]
    public void FilterWithFloat()
    {
        var path = new JPath("[?(@.name>=12.1)]");
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[0]).Expression;
        Assert.Equal(12.1d, (double) (JToken) expressions.Right);
    }

    [Fact]
    public void FilterExistWithAnd()
    {
        var path = new JPath("[?(@.name&&@.title)]");
        var expressions = (CompositeExpression) ((QueryFilter) path.Filters[0]).Expression;
        Assert.Equal(QueryOperator.And, expressions.Operator);
        Assert.Equal(2, expressions.Expressions.Count);

        var first = (BooleanQueryExpression) expressions.Expressions[0];
        var firstPaths = (List<PathFilter>) first.Left;
        Assert.Equal("name", ((FieldFilter) firstPaths[0]).Name);
        Assert.Equal(QueryOperator.Exists, first.Operator);

        var second = (BooleanQueryExpression) expressions.Expressions[1];
        var secondPaths = (List<PathFilter>) second.Left;
        Assert.Equal("title", ((FieldFilter) secondPaths[0]).Name);
        Assert.Equal(QueryOperator.Exists, second.Operator);
    }

    [Fact]
    public void FilterExistWithAndOr()
    {
        // '&&' binds tighter than '||', so "name && title || pie" parses as "(name && title) || pie":
        // the root is an Or of [ (name && title), pie ].
        var path = new JPath("[?(@.name&&@.title||@.pie)]");
        var orExpression = (CompositeExpression) ((QueryFilter) path.Filters[0]).Expression;
        Assert.Equal(QueryOperator.Or, orExpression.Operator);
        Assert.Equal(2, orExpression.Expressions.Count);

        var andExpression = (CompositeExpression) orExpression.Expressions[0];
        Assert.Equal(QueryOperator.And, andExpression.Operator);
        Assert.Equal(2, andExpression.Expressions.Count);

        var andFirst = (BooleanQueryExpression) andExpression.Expressions[0];
        var andFirstPaths = (List<PathFilter>) andFirst.Left;
        Assert.Equal("name", ((FieldFilter) andFirstPaths[0]).Name);
        Assert.Equal(QueryOperator.Exists, andFirst.Operator);

        var andSecond = (BooleanQueryExpression) andExpression.Expressions[1];
        var andSecondPaths = (List<PathFilter>) andSecond.Left;
        Assert.Equal("title", ((FieldFilter) andSecondPaths[0]).Name);
        Assert.Equal(QueryOperator.Exists, andSecond.Operator);

        var orSecond = (BooleanQueryExpression) orExpression.Expressions[1];
        var orSecondPaths = (List<PathFilter>) orSecond.Left;
        Assert.Equal("pie", ((FieldFilter) orSecondPaths[0]).Name);
        Assert.Equal(QueryOperator.Exists, orSecond.Operator);
    }

    [Fact]
    public void FilterWithRoot()
    {
        var path = new JPath("[?($.name>=12.1)]");
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[0]).Expression;
        var paths = (List<PathFilter>) expressions.Left;
        Assert.Equal(2, paths.Count);
        object o = paths[0];
        Assert.IsType<RootFilter>(o);
        object o1 = paths[1];
        Assert.IsType<FieldFilter>(o1);
    }

    [Fact]
    public void BadOr1()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath("[?(@.name||)]"));
        Assert.Equal("Unexpected character while parsing path query: )", exception.Message);
    }

    [Fact]
    public void BaddOr2()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath("[?(@.name|)]"));
        Assert.Equal("Unexpected character while parsing path query: |", exception.Message);
    }

    [Fact]
    public void BaddOr3()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath("[?(@.name|"));
        Assert.Equal("Unexpected character while parsing path query: |", exception.Message);
    }

    [Fact]
    public void BaddOr4()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath("[?(@.name||"));
        Assert.Equal("Path ended with open query.", exception.Message);
    }

    [Fact]
    public void NoAtAfterOr()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath("[?(@.name||s"));
        Assert.Equal("Unexpected character while parsing path query: s", exception.Message);
    }

    [Fact]
    public void NoPathAfterAt()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath("[?(@.name||@"));
        Assert.Equal("Path ended with open query.", exception.Message);
    }

    [Fact]
    public void NoPathAfterDot()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath("[?(@.name||@."));
        Assert.Equal("Unexpected end while parsing path.", exception.Message);
    }

    [Fact]
    public void NoPathAfterDot2()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath("[?(@.name||@.)]"));
        Assert.Equal("Unexpected end while parsing path.", exception.Message);
    }

    [Fact]
    public void FilterWithFloatExp()
    {
        var path = new JPath("[?(@.name>=5.56789e+0)]");
        var expressions = (BooleanQueryExpression) ((QueryFilter) path.Filters[0]).Expression;
        Assert.Equal(5.56789e+0, (double) (JToken) expressions.Right);
    }

    [Fact]
    public void MultiplePropertiesAndIndexers()
    {
        var path = new JPath("Blah[0]..Two.Three[1].Four");
        Assert.Equal(6, path.Filters.Count);
        Assert.Equal("Blah", ((FieldFilter) path.Filters[0]).Name);
        Assert.Equal(0, ((ArrayIndexFilter) path.Filters[1]).Index);
        Assert.Equal("Two", ((ScanFilter) path.Filters[2]).Name);
        Assert.Equal("Three", ((FieldFilter) path.Filters[3]).Name);
        Assert.Equal(1, ((ArrayIndexFilter) path.Filters[4]).Index);
        Assert.Equal("Four", ((FieldFilter) path.Filters[5]).Name);
    }

    [Fact]
    public void BadCharactersInIndexer()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath("Blah[[0]].Two.Three[1].Four"));
        Assert.Equal("Unexpected character while parsing path indexer: [", exception.Message);
    }

    [Fact]
    public void UnclosedIndexer()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath("Blah[0"));
        Assert.Equal("Path ended with open indexer.", exception.Message);
    }

    [Fact]
    public void IndexerOnly()
    {
        var path = new JPath("[111119990]");
        Assert.Single(path.Filters);
        Assert.Equal(111119990, ((ArrayIndexFilter) path.Filters[0]).Index);
    }

    [Fact]
    public void IndexerOnlyWithWhitespace()
    {
        var path = new JPath("[  10  ]");
        Assert.Single(path.Filters);
        Assert.Equal(10, ((ArrayIndexFilter) path.Filters[0]).Index);
    }

    [Fact]
    public void MultipleIndexes()
    {
        var path = new JPath("[111119990,3]");
        Assert.Single(path.Filters);
        Assert.Equal(2, ((ArrayMultipleIndexFilter) path.Filters[0]).Indexes.Count);
        Assert.Equal(111119990, ((ArrayMultipleIndexFilter) path.Filters[0]).Indexes[0]);
        Assert.Equal(3, ((ArrayMultipleIndexFilter) path.Filters[0]).Indexes[1]);
    }

    [Fact]
    public void MultipleIndexesWithWhitespace()
    {
        var path = new JPath("[   111119990  ,   3   ]");
        Assert.Single(path.Filters);
        Assert.Equal(2, ((ArrayMultipleIndexFilter) path.Filters[0]).Indexes.Count);
        Assert.Equal(111119990, ((ArrayMultipleIndexFilter) path.Filters[0]).Indexes[0]);
        Assert.Equal(3, ((ArrayMultipleIndexFilter) path.Filters[0]).Indexes[1]);
    }

    [Fact]
    public void MultipleQuotedIndexes()
    {
        var path = new JPath("['111119990','3']");
        Assert.Single(path.Filters);
        Assert.Equal(2, ((FieldMultipleFilter) path.Filters[0]).Names.Count);
        Assert.Equal("111119990", ((FieldMultipleFilter) path.Filters[0]).Names[0]);
        Assert.Equal("3", ((FieldMultipleFilter) path.Filters[0]).Names[1]);
    }

    [Fact]
    public void MultipleQuotedIndexesWithWhitespace()
    {
        var path = new JPath("[ '111119990' , '3' ]");
        Assert.Single(path.Filters);
        Assert.Equal(2, ((FieldMultipleFilter) path.Filters[0]).Names.Count);
        Assert.Equal("111119990", ((FieldMultipleFilter) path.Filters[0]).Names[0]);
        Assert.Equal("3", ((FieldMultipleFilter) path.Filters[0]).Names[1]);
    }

    [Fact]
    public void SlicingIndexAll()
    {
        var path = new JPath("[111119990:3:2]");
        Assert.Single(path.Filters);
        Assert.Equal(111119990, ((ArraySliceFilter) path.Filters[0]).Start);
        Assert.Equal(3, ((ArraySliceFilter) path.Filters[0]).End);
        Assert.Equal(2, ((ArraySliceFilter) path.Filters[0]).Step);
    }

    [Fact]
    public void SlicingIndex()
    {
        var path = new JPath("[111119990:3]");
        Assert.Single(path.Filters);
        Assert.Equal(111119990, ((ArraySliceFilter) path.Filters[0]).Start);
        Assert.Equal(3, ((ArraySliceFilter) path.Filters[0]).End);
        Assert.Null(((ArraySliceFilter) path.Filters[0]).Step);
    }

    [Fact]
    public void SlicingIndexNegative()
    {
        var path = new JPath("[-111119990:-3:-2]");
        Assert.Single(path.Filters);
        Assert.Equal(-111119990, ((ArraySliceFilter) path.Filters[0]).Start);
        Assert.Equal(-3, ((ArraySliceFilter) path.Filters[0]).End);
        Assert.Equal(-2, ((ArraySliceFilter) path.Filters[0]).Step);
    }

    [Fact]
    public void SlicingIndexEmptyStop()
    {
        var path = new JPath("[  -3  :  ]");
        Assert.Single(path.Filters);
        Assert.Equal(-3, ((ArraySliceFilter) path.Filters[0]).Start);
        Assert.Null(((ArraySliceFilter) path.Filters[0]).End);
        Assert.Null(((ArraySliceFilter) path.Filters[0]).Step);
    }

    [Fact]
    public void SlicingIndexEmptyStart()
    {
        var path = new JPath("[ : 1 : ]");
        Assert.Single(path.Filters);
        Assert.Null(((ArraySliceFilter) path.Filters[0]).Start);
        Assert.Equal(1, ((ArraySliceFilter) path.Filters[0]).End);
        Assert.Null(((ArraySliceFilter) path.Filters[0]).Step);
    }

    [Fact]
    public void SlicingIndexWhitespace()
    {
        var path = new JPath("[  -111119990  :  -3  :  -2  ]");
        Assert.Single(path.Filters);
        Assert.Equal(-111119990, ((ArraySliceFilter) path.Filters[0]).Start);
        Assert.Equal(-3, ((ArraySliceFilter) path.Filters[0]).End);
        Assert.Equal(-2, ((ArraySliceFilter) path.Filters[0]).Step);
    }

    [Fact]
    public void EmptyIndexer()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath("[]"));
        Assert.Equal("Array index expected.", exception.Message);
    }

    [Fact]
    public void IndexerCloseInProperty()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath("]"));
        Assert.Equal("Unexpected character while parsing path: ]", exception.Message);
    }

    [Fact]
    public void AdjacentIndexers()
    {
        var path = new JPath($"[1][0][0][{int.MaxValue}]");
        Assert.Equal(4, path.Filters.Count);
        Assert.Equal(1, ((ArrayIndexFilter) path.Filters[0]).Index);
        Assert.Equal(0, ((ArrayIndexFilter) path.Filters[1]).Index);
        Assert.Equal(0, ((ArrayIndexFilter) path.Filters[2]).Index);
        Assert.Equal(int.MaxValue, ((ArrayIndexFilter) path.Filters[3]).Index);
    }

    [Fact]
    public void MissingDotAfterIndexer()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath("[1]Blah"));
        Assert.Equal("Unexpected character following indexer: B", exception.Message);
    }

    [Fact]
    public void PropertyFollowingEscapedPropertyName()
    {
        var path = new JPath("frameworks.NET5_0_OR_GREATER.dependencies.['System.Xml.ReaderWriter'].source");
        Assert.Equal(5, path.Filters.Count);

        Assert.Equal("frameworks", ((FieldFilter) path.Filters[0]).Name);
        Assert.Equal("NET5_0_OR_GREATER", ((FieldFilter) path.Filters[1]).Name);
        Assert.Equal("dependencies", ((FieldFilter) path.Filters[2]).Name);
        Assert.Equal("System.Xml.ReaderWriter", ((FieldFilter) path.Filters[3]).Name);
        Assert.Equal("source", ((FieldFilter) path.Filters[4]).Name);
    }

    // The following malformed paths previously threw IndexOutOfRangeException (indexing past the end
    // of the expression) instead of the documented JsonException.

    [Fact]
    public void RootDollarAtEndAfterWhitespace()
    {
        var path = new JPath(" $");
        Assert.Empty(path.Filters);
    }

    [Fact]
    public void OpenIndexerEndingWithWhitespace()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath("$[ "));
        Assert.Equal("Path ended with open indexer.", exception.Message);
    }

    [Fact]
    public void QueryEndingWithUnterminatedNumber()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath("$[?(1"));
        Assert.Equal("Path ended with open query.", exception.Message);
    }

    [Fact]
    public void QueryEndingWithUnterminatedComparisonValue()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath("$[?(@.a == 12"));
        Assert.Equal("Path ended with open query.", exception.Message);
    }

    // '&&' immediately followed by '||' was silently accepted (and evaluated the left operand twice).
    [Fact]
    public void AndImmediatelyFollowedByOr()
    {
        var exception = Assert.Throws<JsonException>(() => new JPath("$[?(@.a &&|| @.b)]"));
        Assert.Equal("Unexpected character while parsing path query: |", exception.Message);
    }

    // '=~' with a non-slash pattern threw ArgumentOutOfRangeException during evaluation; it is now
    // reported as a JsonException at parse time.
    [Fact]
    public void RegexOperatorWithoutSlashesThrowsAtParseTime() =>
        Assert.Throws<JsonException>(() => new JPath("$[?(@.name =~ 'abc')]"));
}
