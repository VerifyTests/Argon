namespace Argon;

class QueryScanFilter(QueryExpression expression) :
    PathFilter
{
    internal QueryExpression Expression = expression;

    public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, JsonSelectSettings settings)
    {
        foreach (var token in current)
        {
            if (token is JContainer c)
            {
                foreach (var d in c.DescendantsAndSelf())
                {
                    if (Expression.IsMatch(root, d, settings))
                    {
                        yield return d;
                    }
                }
            }
            else
            {
                if (Expression.IsMatch(root, token, settings))
                {
                    yield return token;
                }
            }
        }
    }
}