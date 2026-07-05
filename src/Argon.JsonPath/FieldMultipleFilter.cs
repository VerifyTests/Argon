class FieldMultipleFilter(List<string> names) :
    PathFilter
{
    internal List<string> Names = names;

    public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, JsonSelectSettings settings)
    {
        foreach (var token in current)
        {
            if (token is JObject o)
            {
                foreach (var name in Names)
                {
                    var v = o[name];

                    if (v != null)
                    {
                        yield return v;
                    }
                    else if (settings?.ErrorWhenNoMatch ?? false)
                    {
                        throw new JsonException($"Property '{name}' does not exist on JObject.");
                    }
                }
            }
            else
            {
                if (settings?.ErrorWhenNoMatch ?? false)
                {
                    throw new JsonException($"Properties {string.Join(", ", Names.Select(n => $"'{n}'"))} not valid on {token.GetType().Name}.");
                }
            }
        }
    }
}