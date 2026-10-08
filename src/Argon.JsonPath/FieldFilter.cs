namespace Argon;

class FieldFilter(string? name) :
    PathFilter
{
    internal string? Name = name;

    public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, JsonSelectSettings settings)
    {
        foreach (var token in current)
        {
            if (token is JObject o)
            {
                if (Name == null)
                {
                    foreach (var p in o)
                    {
                        yield return p.Value!;
                    }
                }
                else
                {
                    var v = o[Name];

                    if (v != null)
                    {
                        yield return v;
                    }
                    else if (settings?.ErrorWhenNoMatch ?? false)
                    {
                        throw new JsonException($"Property '{Name}' does not exist on JObject.");
                    }
                }
            }
            else
            {
                if (settings?.ErrorWhenNoMatch ?? false)
                {
                    throw new JsonException($"Property '{Name ?? "*"}' not valid on {token.GetType().Name}.");
                }
            }
        }
    }
}