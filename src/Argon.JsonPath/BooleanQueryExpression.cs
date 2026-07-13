using System.Text.RegularExpressions;

class BooleanQueryExpression(QueryOperator @operator, object left, object? right) :
    QueryExpression(@operator)
{
    public readonly object Left = left;
    public readonly object? Right = right;

    // constant operands never change, so the single-element wrappers IsMatch needs
    // are built once instead of per candidate token
    readonly JToken[]? leftConstant = left is JToken leftToken ? [leftToken] : null;
    readonly JToken[]? rightConstant = right is JToken rightToken ? [rightToken] : null;

    // the regex operand is parsed once here instead of re-sliced and re-parsed per candidate token;
    // a malformed pattern is surfaced as a JsonException at parse time rather than an
    // ArgumentOutOfRangeException during evaluation
    readonly (string Pattern, RegexOptions Options)? regex =
        @operator == QueryOperator.RegexEquals ? ParseRegex(right) : null;

    static (string Pattern, RegexOptions Options) ParseRegex(object? right)
    {
        if (right is not JValue {Value: string regexText})
        {
            throw new JsonException("A regex query operator '=~' requires a regex operand.");
        }

        var patternOptionDelimiterIndex = regexText.LastIndexOf('/');

        // a valid pattern is enclosed in slashes: /pattern/ or /pattern/options
        if (regexText.Length < 2 || regexText[0] != '/' || patternOptionDelimiterIndex < 1)
        {
            throw new JsonException($"Path regex must be enclosed in slashes, for example /pattern/: {regexText}");
        }

        var pattern = regexText.Substring(1, patternOptionDelimiterIndex - 1);
        var options = MiscellaneousUtils.GetRegexOptions(regexText.AsSpan(patternOptionDelimiterIndex + 1));
        return (pattern, options);
    }

    static IEnumerable<JToken> GetFilterResult(JToken root, JToken t, object? o)
    {
        if (o is List<PathFilter> pathFilters)
        {
            return JPath.Evaluate(pathFilters, root, t, JTokenExtensions.DefaultSettings);
        }

        return [];
    }

    public override bool IsMatch(JToken root, JToken t, JsonSelectSettings settings)
    {
        if (Operator == QueryOperator.Exists)
        {
            return leftConstant != null ||
                   GetFilterResult(root, t, Left).Any();
        }

        // single constant right operand is the dominant filter shape (e.g. @.a == 1):
        // compare directly without materializing result collections
        if (rightConstant != null)
        {
            var rightResult = rightConstant[0];
            if (leftConstant != null)
            {
                return MatchTokens(leftConstant[0], rightResult, settings);
            }

            foreach (var leftResult in GetFilterResult(root, t, Left))
            {
                if (MatchTokens(leftResult, rightResult, settings))
                {
                    return true;
                }
            }

            return false;
        }

        using var leftResults = (leftConstant ?? GetFilterResult(root, t, Left)).GetEnumerator();
        if (leftResults.MoveNext())
        {
            var rightResultsEn = GetFilterResult(root, t, Right);
            var rightResults = rightResultsEn as ICollection<JToken> ?? rightResultsEn.ToList();

            do
            {
                var leftResult = leftResults.Current;
                foreach (var rightResult in rightResults)
                {
                    if (MatchTokens(leftResult, rightResult, settings))
                    {
                        return true;
                    }
                }
            } while (leftResults.MoveNext());
        }

        return false;
    }

    bool MatchTokens(JToken? leftResult, JToken? rightResult, JsonSelectSettings settings)
    {
        if (leftResult is JValue leftValue &&
            rightResult is JValue rightValue)
        {
            switch (Operator)
            {
                case QueryOperator.RegexEquals:
                    if (RegexEquals(leftValue, settings))
                    {
                        return true;
                    }

                    break;
                case QueryOperator.Equals:
                    if (EqualsWithStringCoercion(leftValue, rightValue))
                    {
                        return true;
                    }

                    break;
                case QueryOperator.StrictEquals:
                    if (EqualsWithStrictMatch(leftValue, rightValue))
                    {
                        return true;
                    }

                    break;
                case QueryOperator.NotEquals:
                    if (!EqualsWithStringCoercion(leftValue, rightValue))
                    {
                        return true;
                    }

                    break;
                case QueryOperator.StrictNotEquals:
                    if (!EqualsWithStrictMatch(leftValue, rightValue))
                    {
                        return true;
                    }

                    break;
                case QueryOperator.GreaterThan:
                    if (leftValue.CompareTo(rightValue) > 0)
                    {
                        return true;
                    }

                    break;
                case QueryOperator.GreaterThanOrEquals:
                    if (leftValue.CompareTo(rightValue) >= 0)
                    {
                        return true;
                    }

                    break;
                case QueryOperator.LessThan:
                    if (leftValue.CompareTo(rightValue) < 0)
                    {
                        return true;
                    }

                    break;
                case QueryOperator.LessThanOrEquals:
                    if (leftValue.CompareTo(rightValue) <= 0)
                    {
                        return true;
                    }

                    break;
                case QueryOperator.Exists:
                    return true;
            }
        }
        else
        {
            // can only specify primitive types in a comparison
            // notequals will always be true
            if (Operator is
                QueryOperator.Exists or
                QueryOperator.NotEquals)
            {
                return true;
            }
        }

        return false;
    }

    bool RegexEquals(JValue input, JsonSelectSettings settings)
    {
        if (input.Type != JTokenType.String)
        {
            return false;
        }

        var (pattern, options) = regex!.Value;
        var timeout = settings.RegexMatchTimeout ?? Regex.InfiniteMatchTimeout;
        return Regex.IsMatch((string) input.GetValue(), pattern, options, timeout);
    }

    static bool EqualsWithStringCoercion(JValue value, JValue queryValue)
    {
        if (value.Equals(queryValue))
        {
            return true;
        }

        // Handle comparing an integer with a float
        // e.g. Comparing 1 and 1.0
        if ((value.Type == JTokenType.Integer && queryValue.Type == JTokenType.Float) ||
            (value.Type == JTokenType.Float && queryValue.Type == JTokenType.Integer))
        {
            return JValue.Compare(value.Type, value.Value, queryValue.Value) == 0;
        }

        if (queryValue.Type != JTokenType.String)
        {
            return false;
        }

        var queryValueString = (string) queryValue.GetValue();

        string currentValueString;

        // potential performance issue with converting every value to string?
        switch (value.Type)
        {
            case JTokenType.Date:
                using (var writer = StringUtils.CreateStringWriter(64))
                {
                    if (value.Value is DateTimeOffset offset)
                    {
                        DateTimeUtils.WriteDateTimeOffsetString(writer, offset);
                    }
                    else
                    {
                        DateTimeUtils.WriteDateTimeString(writer, (DateTime) value.GetValue());
                    }

                    currentValueString = writer.ToString();
                }

                break;
            case JTokenType.Bytes:
                currentValueString = Convert.ToBase64String((byte[]) value.GetValue());
                break;
            case JTokenType.Guid:
                currentValueString = ((Guid) value.GetValue()).ToString();
                break;
            case JTokenType.TimeSpan:
                currentValueString = ((TimeSpan) value.GetValue()).ToString();
                break;
            case JTokenType.Uri:
                currentValueString = ((Uri) value.GetValue()).OriginalString;
                break;
            default:
                return false;
        }

        return string.Equals(currentValueString, queryValueString, StringComparison.Ordinal);
    }

    internal static bool EqualsWithStrictMatch(JValue value, JValue queryValue)
    {
        // Handle comparing an integer with a float
        // e.g. Comparing 1 and 1.0
        if ((value.Type == JTokenType.Integer && queryValue.Type == JTokenType.Float)
            || (value.Type == JTokenType.Float && queryValue.Type == JTokenType.Integer))
        {
            return JValue.Compare(value.Type, value.Value, queryValue.Value) == 0;
        }

        // we handle floats and integers the exact same way, so they are pseudo equivalent
        return value.Type == queryValue.Type &&
               value.Equals(queryValue);
    }
}