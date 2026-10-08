namespace Argon;

class RootFilter :
    PathFilter
{
    public static readonly RootFilter Instance = new();

    RootFilter()
    {
    }

    public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, JsonSelectSettings settings) =>
        [root];
}