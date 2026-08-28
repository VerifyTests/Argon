class QueryFilter(QueryExpression expression) :
    PathFilter
{
    internal QueryExpression Expression = expression;

    public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, JsonSelectSettings settings)
    {
        foreach (var token in current)
        {
            if (token is not JContainer container)
            {
                continue;
            }

            // walk the children through the sibling links: foreach over a JToken goes through
            // Children(), which boxes an enumerator per input token
            var v = container.First;
            while (v != null)
            {
                if (Expression.IsMatch(root, v, settings))
                {
                    yield return v;
                }

                v = v.Next;
            }
        }
    }
}