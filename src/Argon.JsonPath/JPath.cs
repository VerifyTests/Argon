// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

class JPath
{
    static readonly char[] floatCharacters = ['.', 'E', 'e'];

    readonly string expression;
    public List<PathFilter> Filters { get; }

    int currentIndex;

    public JPath(string expression)
    {
        this.expression = expression;
        Filters = [];

        ParseMain();
    }

    void ParseMain()
    {
        var currentPartStartIndex = currentIndex;

        EatWhitespace();

        if (expression.Length == currentIndex)
        {
            return;
        }

        if (expression[currentIndex] == '$')
        {
            // '$' is the last character (guard against leading whitespace, where currentIndex > 0)
            if (currentIndex == expression.Length - 1)
            {
                return;
            }

            // only increment position for "$." or "$["
            // otherwise assume property that starts with $
            var c = expression[currentIndex + 1];
            if (c is '.' or '[')
            {
                currentIndex++;
                currentPartStartIndex = currentIndex;
            }
        }

        if (!ParsePath(Filters, currentPartStartIndex, false))
        {
            var lastCharacterIndex = currentIndex;

            EatWhitespace();

            if (currentIndex < expression.Length)
            {
                throw new JsonException($"Unexpected character while parsing path: {expression[lastCharacterIndex]}");
            }
        }
    }

    bool ParsePath(List<PathFilter> filters, int currentPartStartIndex, bool query)
    {
        var scan = false;
        var followingIndexer = false;
        var followingDot = false;

        var ended = false;
        while (currentIndex < expression.Length && !ended)
        {
            var currentChar = expression[currentIndex];

            switch (currentChar)
            {
                case '[':
                case '(':
                    if (currentIndex > currentPartStartIndex)
                    {
                        var member = expression.Substring(currentPartStartIndex, currentIndex - currentPartStartIndex);
                        if (member == "*")
                        {
                            member = null;
                        }

                        filters.Add(CreatePathFilter(member, scan));
                        scan = false;
                    }

                    filters.Add(ParseIndexer(currentChar, scan));
                    scan = false;

                    currentIndex++;
                    currentPartStartIndex = currentIndex;
                    followingIndexer = true;
                    followingDot = false;
                    break;
                case ']':
                case ')':
                    ended = true;
                    break;
                case ' ':
                    if (currentIndex < expression.Length)
                    {
                        ended = true;
                    }

                    break;
                case '.':
                    if (currentIndex > currentPartStartIndex)
                    {
                        var member = expression.Substring(currentPartStartIndex, currentIndex - currentPartStartIndex);
                        if (member == "*")
                        {
                            member = null;
                        }

                        filters.Add(CreatePathFilter(member, scan));
                        scan = false;
                    }

                    if (currentIndex + 1 < expression.Length && expression[currentIndex + 1] == '.')
                    {
                        scan = true;
                        currentIndex++;
                    }

                    currentIndex++;
                    currentPartStartIndex = currentIndex;
                    followingIndexer = false;
                    followingDot = true;
                    break;
                default:
                    if (query && currentChar is '=' or '<' or '!' or '>' or '|' or '&')
                    {
                        ended = true;
                    }
                    else
                    {
                        if (followingIndexer)
                        {
                            throw new JsonException($"Unexpected character following indexer: {currentChar}");
                        }

                        currentIndex++;
                    }

                    break;
            }
        }

        var atPathEnd = currentIndex == expression.Length;

        if (currentIndex > currentPartStartIndex)
        {
            var member = expression.Substring(currentPartStartIndex, currentIndex - currentPartStartIndex).TrimEnd();
            if (member == "*")
            {
                member = null;
            }

            filters.Add(CreatePathFilter(member, scan));
        }
        else
        {
            // no field name following dot in path and at end of base path/query
            if (followingDot && (atPathEnd || query))
            {
                throw new JsonException("Unexpected end while parsing path.");
            }
        }

        return atPathEnd;
    }

    static PathFilter CreatePathFilter(string? member, bool scan) =>
        scan ? new ScanFilter(member) : new FieldFilter(member);

    PathFilter ParseIndexer(char indexerOpenChar, bool scan)
    {
        currentIndex++;

        var indexerCloseChar = indexerOpenChar == '[' ? ']' : ')';

        EnsureLength("Path ended with open indexer.");

        EatWhitespace();

        // EatWhitespace can consume the remaining characters (e.g. "$[ ")
        EnsureLength("Path ended with open indexer.");

        if (expression[currentIndex] == '\'')
        {
            return ParseQuotedField(indexerCloseChar, scan);
        }

        if (expression[currentIndex] == '?')
        {
            return ParseQuery(indexerCloseChar, scan);
        }

        return ParseArrayIndexer(indexerCloseChar);
    }

    PathFilter ParseArrayIndexer(char indexerCloseChar)
    {
        var start = currentIndex;
        int? end = null;
        List<int>? indexes = null;
        var colonCount = 0;
        int? startIndex = null;
        int? endIndex = null;
        int? step = null;

        while (currentIndex < expression.Length)
        {
            var currentCharacter = expression[currentIndex];

            if (currentCharacter == ' ')
            {
                end = currentIndex;
                EatWhitespace();
                continue;
            }

            if (currentCharacter == indexerCloseChar)
            {
                var length = (end ?? currentIndex) - start;

                if (indexes != null)
                {
                    if (length == 0)
                    {
                        throw new JsonException("Array index expected.");
                    }

                    var indexer = expression.Substring(start, length);
                    var index = Convert.ToInt32(indexer, InvariantCulture);

                    indexes.Add(index);
                    return new ArrayMultipleIndexFilter(indexes);
                }

                if (colonCount > 0)
                {
                    if (length > 0)
                    {
                        var indexer = expression.Substring(start, length);
                        var index = Convert.ToInt32(indexer, InvariantCulture);

                        if (colonCount == 1)
                        {
                            endIndex = index;
                        }
                        else
                        {
                            step = index;
                        }
                    }

                    return new ArraySliceFilter {Start = startIndex, End = endIndex, Step = step};
                }
                // ReSharper disable once RedundantIfElseBlock
                else
                {
                    if (length == 0)
                    {
                        throw new JsonException("Array index expected.");
                    }

                    var indexer = expression.Substring(start, length);
                    var index = Convert.ToInt32(indexer, InvariantCulture);

                    return new ArrayIndexFilter {Index = index};
                }
            }

            if (currentCharacter == ',')
            {
                var length = (end ?? currentIndex) - start;

                if (length == 0)
                {
                    throw new JsonException("Array index expected.");
                }

                indexes ??= [];

                var indexer = expression.Substring(start, length);
                indexes.Add(Convert.ToInt32(indexer, InvariantCulture));

                currentIndex++;

                EatWhitespace();

                start = currentIndex;
                end = null;
            }
            else if (currentCharacter == '*')
            {
                currentIndex++;
                EnsureLength("Path ended with open indexer.");
                EatWhitespace();

                if (expression[currentIndex] != indexerCloseChar)
                {
                    throw new JsonException($"Unexpected character while parsing path indexer: {currentCharacter}");
                }

                return new ArrayIndexFilter();
            }
            else if (currentCharacter == ':')
            {
                var length = (end ?? currentIndex) - start;

                if (length > 0)
                {
                    var indexer = expression.Substring(start, length);
                    var index = Convert.ToInt32(indexer, InvariantCulture);

                    if (colonCount == 0)
                    {
                        startIndex = index;
                    }
                    else if (colonCount == 1)
                    {
                        endIndex = index;
                    }
                    else
                    {
                        step = index;
                    }
                }

                colonCount++;

                currentIndex++;

                EatWhitespace();

                start = currentIndex;
                end = null;
            }
            else if (!char.IsDigit(currentCharacter) && currentCharacter != '-')
            {
                throw new JsonException($"Unexpected character while parsing path indexer: {currentCharacter}");
            }
            else
            {
                if (end != null)
                {
                    throw new JsonException($"Unexpected character while parsing path indexer: {currentCharacter}");
                }

                currentIndex++;
            }
        }

        throw new JsonException("Path ended with open indexer.");
    }

    void EatWhitespace()
    {
        while (currentIndex < expression.Length)
        {
            if (expression[currentIndex] != ' ')
            {
                break;
            }

            currentIndex++;
        }
    }

    PathFilter ParseQuery(char indexerCloseChar, bool scan)
    {
        currentIndex++;
        EnsureLength("Path ended with open indexer.");

        if (this.expression[currentIndex] != '(')
        {
            throw new JsonException($"Unexpected character while parsing path indexer: {this.expression[currentIndex]}");
        }

        currentIndex++;

        var expression = ParseExpression();

        currentIndex++;
        EnsureLength("Path ended with open indexer.");
        EatWhitespace();

        // EatWhitespace can consume the remaining characters
        EnsureLength("Path ended with open indexer.");

        if (this.expression[currentIndex] != indexerCloseChar)
        {
            throw new JsonException($"Unexpected character while parsing path indexer: {this.expression[currentIndex]}");
        }

        if (scan)
        {
            return new QueryScanFilter(expression);
        }

        return new QueryFilter(expression);
    }

    bool TryParseExpression(out List<PathFilter>? expressionPath)
    {
        if (expression[currentIndex] == '$')
        {
            expressionPath = [RootFilter.Instance];
        }
        else if (expression[currentIndex] == '@')
        {
            expressionPath = [];
        }
        else
        {
            expressionPath = null;
            return false;
        }

        currentIndex++;

        if (ParsePath(expressionPath, currentIndex, true))
        {
            throw new JsonException("Path ended with open query.");
        }

        return true;
    }

    JsonException CreateUnexpectedCharacterException() =>
        new($"Unexpected character while parsing path query: {expression[currentIndex]}");

    object ParseSide()
    {
        EatWhitespace();

        if (TryParseExpression(out var expressionPath))
        {
            EatWhitespace();
            EnsureLength("Path ended with open query.");

            return expressionPath!;
        }

        if (TryParseValue(out var value))
        {
            EatWhitespace();
            EnsureLength("Path ended with open query.");

            return new JValue(value);
        }

        // a value that ran to the end of the expression (e.g. "$[?(1") leaves currentIndex at the end;
        // report it as an open query rather than indexing past the end
        EnsureLength("Path ended with open query.");

        throw CreateUnexpectedCharacterException();
    }

    QueryExpression ParseExpression()
    {
        // '&&' binds tighter than '||': collect '&&'-joined terms into an And group, then
        // join the And groups with '||' into an Or. So "a && b || c" parses as "(a && b) || c".
        var orGroups = new List<QueryExpression>();
        var andGroup = new List<QueryExpression>();

        while (true)
        {
            // guards re-entry after consuming a '&&'/'||' that was the last thing in the expression
            EnsureLength("Path ended with open query.");

            andGroup.Add(ParseBooleanExpression());

            // ParseBooleanExpression always leaves currentIndex on an in-bounds terminator
            var currentChar = expression[currentIndex];

            if (currentChar == '&')
            {
                if (!Match("&&"))
                {
                    throw CreateUnexpectedCharacterException();
                }

                continue;
            }

            // the '&&' group is complete: fold it into a single expression
            orGroups.Add(Combine(andGroup, QueryOperator.And));
            andGroup = [];

            if (currentChar == '|')
            {
                if (!Match("||"))
                {
                    throw CreateUnexpectedCharacterException();
                }

                continue;
            }

            if (currentChar == ')')
            {
                return Combine(orGroups, QueryOperator.Or);
            }

            throw CreateUnexpectedCharacterException();
        }
    }

    // wraps the expressions in a composite of the given operator, or returns the single expression as-is
    static QueryExpression Combine(List<QueryExpression> expressions, QueryOperator @operator)
    {
        if (expressions.Count == 1)
        {
            return expressions[0];
        }

        return new CompositeExpression(@operator)
        {
            Expressions = expressions
        };
    }

    BooleanQueryExpression ParseBooleanExpression()
    {
        var left = ParseSide();
        object? right = null;

        QueryOperator op;
        if (expression[currentIndex] == ')'
            || expression[currentIndex] == '|'
            || expression[currentIndex] == '&')
        {
            op = QueryOperator.Exists;
        }
        else
        {
            op = ParseOperator();

            right = ParseSide();
        }

        return new(op, left, right);
    }

    bool TryParseValue(out object? value)
    {
        var currentChar = expression[currentIndex];
        if (currentChar == '\'')
        {
            value = ReadQuotedString();
            return true;
        }

        if (char.IsDigit(currentChar) || currentChar == '-')
        {
            // parse the slice of the expression the number occupies: accumulating it into a
            // StringBuilder first allocated the builder and a string for every number in a path
            var start = currentIndex;

            currentIndex++;
            while (currentIndex < expression.Length)
            {
                currentChar = expression[currentIndex];
                if (currentChar is ' ' or ')' or '&' or '|')
                {
                    var numberText = expression.AsSpan(start, currentIndex - start);

                    if (numberText.IndexOfAny(floatCharacters) == -1)
                    {
                        var result = long.TryParse(numberText, NumberStyles.Integer, InvariantCulture, out var l);
                        value = l;
                        return result;
                    }
                    else
                    {
                        var result = double.TryParse(numberText, NumberStyles.Float | NumberStyles.AllowThousands, InvariantCulture, out var d);
                        value = d;
                        return result;
                    }
                }

                currentIndex++;
            }
        }
        else if (currentChar == 't')
        {
            if (Match("true"))
            {
                value = true;
                return true;
            }
        }
        else if (currentChar == 'f')
        {
            if (Match("false"))
            {
                value = false;
                return true;
            }
        }
        else if (currentChar == 'n')
        {
            if (Match("null"))
            {
                value = null;
                return true;
            }
        }
        else if (currentChar == '/')
        {
            value = ReadRegexString();
            return true;
        }

        value = null;
        return false;
    }

    string ReadQuotedString()
    {
        // the builder is only created once an escape is found: a string with no escapes in it
        // is a slice of the expression
        StringBuilder? stringBuilder = null;
        var start = currentIndex + 1;
        var copiedTo = start;

        currentIndex++;
        while (currentIndex < expression.Length)
        {
            var currentChar = expression[currentIndex];
            if (currentChar == '\\' && currentIndex + 1 < expression.Length)
            {
                stringBuilder ??= new();
                stringBuilder.Append(expression, copiedTo, currentIndex - copiedTo);

                currentIndex++;
                currentChar = expression[currentIndex];

                char resolvedChar;
                switch (currentChar)
                {
                    case 'b':
                        resolvedChar = '\b';
                        break;
                    case 't':
                        resolvedChar = '\t';
                        break;
                    case 'n':
                        resolvedChar = '\n';
                        break;
                    case 'f':
                        resolvedChar = '\f';
                        break;
                    case 'r':
                        resolvedChar = '\r';
                        break;
                    case '\\':
                    case '"':
                    case '\'':
                    case '/':
                        resolvedChar = currentChar;
                        break;
                    default:
                        throw new JsonException($@"Unknown escape character: \{currentChar}");
                }

                stringBuilder.Append(resolvedChar);

                currentIndex++;
                copiedTo = currentIndex;
            }
            else if (currentChar == '\'')
            {
                if (stringBuilder == null)
                {
                    var text = expression.Substring(start, currentIndex - start);
                    currentIndex++;
                    return text;
                }

                stringBuilder.Append(expression, copiedTo, currentIndex - copiedTo);
                currentIndex++;
                return stringBuilder.ToString();
            }
            else
            {
                currentIndex++;
            }
        }

        throw new JsonException("Path ended with an open string.");
    }

    string ReadRegexString()
    {
        var startIndex = currentIndex;

        currentIndex++;
        while (currentIndex < expression.Length)
        {
            var currentChar = expression[currentIndex];

            // handle escaped / character
            if (currentChar == '\\' && currentIndex + 1 < expression.Length)
            {
                currentIndex += 2;
            }
            else if (currentChar == '/')
            {
                currentIndex++;

                while (currentIndex < expression.Length)
                {
                    currentChar = expression[currentIndex];

                    if (char.IsLetter(currentChar))
                    {
                        currentIndex++;
                    }
                    else
                    {
                        break;
                    }
                }

                return expression.Substring(startIndex, currentIndex - startIndex);
            }
            else
            {
                currentIndex++;
            }
        }

        throw new JsonException("Path ended with an open regex.");
    }

    bool Match(string s)
    {
        var currentPosition = currentIndex;
        foreach (var ch in s)
        {
            if (currentPosition < expression.Length &&
                expression[currentPosition] == ch)
            {
                currentPosition++;
            }
            else
            {
                return false;
            }
        }

        currentIndex = currentPosition;
        return true;
    }

    QueryOperator ParseOperator()
    {
        if (currentIndex + 1 >= expression.Length)
        {
            throw new JsonException("Path ended with open query.");
        }

        if (Match("==="))
        {
            return QueryOperator.StrictEquals;
        }

        if (Match("=="))
        {
            return QueryOperator.Equals;
        }

        if (Match("=~"))
        {
            return QueryOperator.RegexEquals;
        }

        if (Match("!=="))
        {
            return QueryOperator.StrictNotEquals;
        }

        if (Match("!=") || Match("<>"))
        {
            return QueryOperator.NotEquals;
        }

        if (Match("<="))
        {
            return QueryOperator.LessThanOrEquals;
        }

        if (Match("<"))
        {
            return QueryOperator.LessThan;
        }

        if (Match(">="))
        {
            return QueryOperator.GreaterThanOrEquals;
        }

        if (Match(">"))
        {
            return QueryOperator.GreaterThan;
        }

        throw new JsonException("Could not read query operator.");
    }

    PathFilter ParseQuotedField(char indexerCloseChar, bool scan)
    {
        List<string>? fields = null;

        while (currentIndex < expression.Length)
        {
            var field = ReadQuotedString();

            EatWhitespace();
            EnsureLength("Path ended with open indexer.");

            if (expression[currentIndex] == indexerCloseChar)
            {
                if (fields != null)
                {
                    fields.Add(field);
                    return scan
                        ? new ScanMultipleFilter(fields)
                        : new FieldMultipleFilter(fields);
                }

                return CreatePathFilter(field, scan);
            }

            if (expression[currentIndex] == ',')
            {
                currentIndex++;
                EatWhitespace();

                fields ??= [];

                fields.Add(field);
            }
            else
            {
                throw new JsonException($"Unexpected character while parsing path indexer: {expression[currentIndex]}");
            }
        }

        throw new JsonException("Path ended with open indexer.");
    }

    void EnsureLength(string message)
    {
        if (currentIndex >= expression.Length)
        {
            throw new JsonException(message);
        }
    }

    internal IEnumerable<JToken> Evaluate(JToken root, JToken t, JsonSelectSettings settings) =>
        Evaluate(Filters, root, t, settings);

    internal static IEnumerable<JToken> Evaluate(List<PathFilter> filters, JToken root, JToken t, JsonSelectSettings settings)
    {
        IEnumerable<JToken> current = [t];
        foreach (var filter in filters)
        {
            current = filter.ExecuteFilter(root, current, settings);
        }

        return current;
    }
}