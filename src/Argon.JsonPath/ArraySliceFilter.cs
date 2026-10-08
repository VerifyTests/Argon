namespace Argon;

class ArraySliceFilter :
    PathFilter
{
    public int? Start { get; set; }
    public int? End { get; set; }
    public int? Step { get; set; }

    public override IEnumerable<JToken> ExecuteFilter(JToken root, IEnumerable<JToken> current, JsonSelectSettings settings)
    {
        if (Step == 0)
        {
            throw new JsonException("Step cannot be zero.");
        }

        foreach (var token in current)
        {
            if (token is JArray a)
            {
                // set defaults for null arguments
                var stepCount = Step ?? 1;
                var startIndex = Start ?? (stepCount > 0 ? 0 : a.Count - 1);
                var stopIndex = End ?? (stepCount > 0 ? a.Count : -1);

                // start from the end of the list if start is negative
                if (Start < 0)
                {
                    startIndex = a.Count + startIndex;
                }

                // end from the start of the list if stop is negative
                if (End < 0)
                {
                    stopIndex = a.Count + stopIndex;
                }

                // ensure indexes keep within collection bounds
                startIndex = Math.Max(startIndex, stepCount > 0 ? 0 : int.MinValue);
                startIndex = Math.Min(startIndex, stepCount > 0 ? a.Count : a.Count - 1);
                stopIndex = Math.Max(stopIndex, -1);
                stopIndex = Math.Min(stopIndex, a.Count);

                var positiveStep = stepCount > 0;

                if (IsValid(startIndex, stopIndex, positiveStep))
                {
                    for (var i = startIndex; IsValid(i, stopIndex, positiveStep); i += stepCount)
                    {
                        yield return a[i];
                    }
                }
                else
                {
                    if (settings?.ErrorWhenNoMatch ?? false)
                    {
                        throw new JsonException(string.Format("Array slice of {0} to {1} returned no results.",
                            Start == null ? "*" : Start.GetValueOrDefault().ToString(InvariantCulture),
                            End == null ? "*" : End.GetValueOrDefault().ToString(InvariantCulture)));
                    }
                }
            }
            else
            {
                if (settings?.ErrorWhenNoMatch ?? false)
                {
                    throw new JsonException($"Array slice is not valid on {token.GetType().Name}.");
                }
            }
        }
    }

    static bool IsValid(int index, int stopIndex, bool positiveStep)
    {
        if (positiveStep)
        {
            return index < stopIndex;
        }

        return index > stopIndex;
    }
}