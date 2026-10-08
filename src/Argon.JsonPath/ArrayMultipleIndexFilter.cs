namespace Argon;

class ArrayMultipleIndexFilter(List<int> indexes) :
    PathFilter
{
    internal List<int> Indexes = indexes;

    public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, JsonSelectSettings settings)
    {
        foreach (var token in current)
        {
            foreach (var index in Indexes)
            {
                var v = GetTokenIndex(token, settings, index);

                if (v != null)
                {
                    yield return v;
                }
            }
        }
    }
}