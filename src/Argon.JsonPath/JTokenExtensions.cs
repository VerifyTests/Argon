/// <summary>
/// Extensions to <see cref="JToken" />.
/// </summary>
public static class JTokenExtensions
{
    // A parsed JPath is immutable and evaluation holds no per-query state, so parsing
    // (per-segment substrings + filter tree) can be paid once per distinct expression.
    // The wholesale clear keeps the cache bounded for pathological dynamic paths.
    const int pathCacheLimit = 512;
    static readonly ConcurrentDictionary<string, JPath> pathCache = new(StringComparer.Ordinal);

    static JPath ParsePath(string path)
    {
        if (pathCache.TryGetValue(path, out var jPath))
        {
            return jPath;
        }

        jPath = new(path);

        if (pathCache.Count >= pathCacheLimit)
        {
            pathCache.Clear();
        }

        pathCache[path] = jPath;
        return jPath;
    }

    /// <summary>
    /// Selects a <see cref="JToken" /> using a JSONPath expression. Selects the token that matches the object path.
    /// </summary>
    /// <param name="token">The <see cref="JToken" /> to search.</param>
    /// <param name="path">A <see cref="String" /> that contains a JSONPath expression.</param>
    /// <returns>A <see cref="JToken" />, or <c>null</c>.</returns>
    public static JToken? SelectToken(this JToken token, string path) =>
        SelectToken(token, path, settings: null);

    /// <summary>
    /// Selects a <see cref="JToken" /> using a JSONPath expression. Selects the token that matches the object path.
    /// </summary>
    /// <param name="token">The <see cref="JToken" /> to search.</param>
    /// <param name="path">A <see cref="String" /> that contains a JSONPath expression. </param>
    /// <param name="errorWhenNoMatch">A flag to indicate whether an error should be thrown if no tokens are found when evaluating part of the expression.</param>
    /// <returns>A <see cref="JToken" />.</returns>
    public static JToken? SelectToken(this JToken token, string path, bool errorWhenNoMatch)
    {
        var settings = errorWhenNoMatch
            ? new JsonSelectSettings {ErrorWhenNoMatch = true}
            : null;

        return SelectToken(token, path, settings);
    }

    /// <summary>
    /// Selects a <see cref="JToken" /> using a JSONPath expression. Selects the token that matches the object path.
    /// </summary>
    /// <param name="token">The <see cref="JToken" /> to search.</param>
    /// <param name="path">A <see cref="String" /> that contains a JSONPath expression.</param>
    /// <param name="settings">The <see cref="JsonSelectSettings" /> used to select tokens.</param>
    /// <returns>A <see cref="JToken" />.</returns>
    public static JToken? SelectToken(this JToken token, string path, JsonSelectSettings? settings)
    {
        var jPath = ParsePath(path);

        settings ??= DefaultSettings;
        JToken? result = null;
        foreach (var t in jPath.Evaluate(token, token, settings))
        {
            if (result != null)
            {
                throw new JsonException("Path returned multiple tokens.");
            }

            result = t;
        }

        return result;
    }

    /// <summary>
    /// Selects a collection of elements using a JSONPath expression.
    /// </summary>
    /// <param name="token">The <see cref="JToken" /> to search.</param>
    /// <param name="path">A <see cref="String" /> that contains a JSONPath expression.</param>
    /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains the selected elements.</returns>
    public static IEnumerable<JToken> SelectTokens(this JToken token, string path) =>
        SelectTokens(token, path, settings: null);

    /// <summary>
    /// Selects a collection of elements using a JSONPath expression.
    /// </summary>
    /// <param name="token">The <see cref="JToken" /> to search.</param>
    /// <param name="path">A <see cref="String" /> that contains a JSONPath expression.</param>
    /// <param name="errorWhenNoMatch">A flag to indicate whether an error should be thrown if no tokens are found when evaluating part of the expression.</param>
    /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains the selected elements.</returns>
    public static IEnumerable<JToken> SelectTokens(this JToken token, string path, bool errorWhenNoMatch)
    {
        var settings = errorWhenNoMatch
            ? new JsonSelectSettings {ErrorWhenNoMatch = true}
            : null;

        return SelectTokens(token, path, settings);
    }

    /// <summary>
    /// Default <see cref="JsonSelectSettings" /> to sue when none are passed in.
    /// </summary>
    public static JsonSelectSettings DefaultSettings { get; } = new();

    /// <summary>
    /// Selects a collection of elements using a JSONPath expression.
    /// </summary>
    /// <param name="token">The <see cref="JToken" /> to search.</param>
    /// <param name="path">A <see cref="String" /> that contains a JSONPath expression.</param>
    /// <param name="settings">The <see cref="JsonSelectSettings" /> used to select tokens.</param>
    /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains the selected elements.</returns>
    public static IEnumerable<JToken> SelectTokens(this JToken token, string path, JsonSelectSettings? settings)
    {
        settings ??= DefaultSettings;

        var p = ParsePath(path);
        return p.Evaluate(token, token, settings);
    }
}