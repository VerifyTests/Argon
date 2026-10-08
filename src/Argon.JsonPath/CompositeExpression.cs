namespace Argon;

class CompositeExpression(QueryOperator @operator) :
    QueryExpression(@operator)
{
    public List<QueryExpression> Expressions { get; set; } = [];

    public override bool IsMatch(JToken root, JToken t, JsonSelectSettings settings)
    {
        switch (Operator)
        {
            case QueryOperator.And:
                foreach (var e in Expressions)
                {
                    if (!e.IsMatch(root, t, settings))
                    {
                        return false;
                    }
                }

                return true;
            case QueryOperator.Or:
                foreach (var e in Expressions)
                {
                    if (e.IsMatch(root, t, settings))
                    {
                        return true;
                    }
                }

                return false;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}