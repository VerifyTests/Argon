namespace Argon;

abstract class QueryExpression
{
    internal QueryOperator Operator;

    public QueryExpression(QueryOperator @operator) =>
        Operator = @operator;

    // For unit tests
    public bool IsMatch(JToken root, JToken t) =>
        IsMatch(root, t, JTokenExtensions.DefaultSettings);

    public abstract bool IsMatch(JToken root, JToken t, JsonSelectSettings settings);
}