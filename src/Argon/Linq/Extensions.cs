// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Contains the LINQ to JSON extension methods.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Returns a collection of tokens that contains the ancestors of every token in the source collection.
    /// </summary>
    /// <typeparam name="T">The type of the objects in source, constrained to <see cref="JToken" />.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains the source collection.</param>
    /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains the ancestors of every token in the source collection.</returns>
    public static IJEnumerable<JToken> Ancestors<T>(this IEnumerable<T> source)
        where T : JToken =>
        source.SelectMany(j => j.Ancestors()).AsJEnumerable();

    /// <summary>
    /// Returns a collection of tokens that contains every token in the source collection, and the ancestors of every token in the source collection.
    /// </summary>
    /// <typeparam name="T">The type of the objects in source, constrained to <see cref="JToken" />.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains the source collection.</param>
    /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains every token in the source collection, the ancestors of every token in the source collection.</returns>
    public static IJEnumerable<JToken> AncestorsAndSelf<T>(this IEnumerable<T> source)
        where T : JToken =>
        source.SelectMany(j => j.AncestorsAndSelf()).AsJEnumerable();

    /// <summary>
    /// Returns a collection of tokens that contains the descendants of every token in the source collection.
    /// </summary>
    /// <typeparam name="T">The type of the objects in source, constrained to <see cref="JContainer" />.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains the source collection.</param>
    /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains the descendants of every token in the source collection.</returns>
    public static IJEnumerable<JToken> Descendants<T>(this IEnumerable<T> source)
        where T : JContainer =>
        source.SelectMany(j => j.Descendants()).AsJEnumerable();

    /// <summary>
    /// Returns a collection of tokens that contains every token in the source collection, and the descendants of every token in the source collection.
    /// </summary>
    /// <typeparam name="T">The type of the objects in source, constrained to <see cref="JContainer" />.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains the source collection.</param>
    /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains every token in the source collection, and the descendants of every token in the source collection.</returns>
    public static IJEnumerable<JToken> DescendantsAndSelf<T>(this IEnumerable<T> source)
        where T : JContainer =>
        source.SelectMany(j => j.DescendantsAndSelf()).AsJEnumerable();

    /// <summary>
    /// Returns a collection of child properties of every object in the source collection.
    /// </summary>
    /// <param name="source">An <see cref="IEnumerable{T}" /> of <see cref="JObject" /> that contains the source collection.</param>
    /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="JProperty" /> that contains the properties of every object in the source collection.</returns>
    public static IJEnumerable<JProperty> Properties(this IEnumerable<JObject> source) =>
        source.SelectMany(d => d.Properties()).AsJEnumerable();

    /// <summary>
    /// Returns a collection of child values of every object in the source collection with the given key.
    /// </summary>
    /// <param name="source">An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains the source collection.</param>
    /// <param name="key">The token key.</param>
    /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains the values of every token in the source collection with the given key.</returns>
    public static IJEnumerable<JToken> Values(this IEnumerable<JToken> source, object? key) =>
        Values<JToken, JToken>(source, key)!.AsJEnumerable();

    /// <summary>
    /// Returns a collection of child values of every object in the source collection.
    /// </summary>
    /// <param name="source">An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains the source collection.</param>
    /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains the values of every token in the source collection.</returns>
    public static IJEnumerable<JToken> Values(this IEnumerable<JToken> source) =>
        source.Values(null);

    /// <summary>
    /// Returns a collection of converted child values of every object in the source collection with the given key.
    /// </summary>
    /// <typeparam name="U">The type to convert the values to.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains the source collection.</param>
    /// <param name="key">The token key.</param>
    /// <returns>An <see cref="IEnumerable{T}" /> that contains the converted values of every token in the source collection with the given key.</returns>
    public static IEnumerable<U?> Values<U>(this IEnumerable<JToken> source, object key) =>
        Values<JToken, U>(source, key);

    /// <summary>
    /// Returns a collection of converted child values of every object in the source collection.
    /// </summary>
    /// <typeparam name="U">The type to convert the values to.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains the source collection.</param>
    /// <returns>An <see cref="IEnumerable{T}" /> that contains the converted values of every token in the source collection.</returns>
    public static IEnumerable<U?> Values<U>(this IEnumerable<JToken> source) =>
        Values<JToken, U>(source, null);

    /// <summary>
    /// Converts the value.
    /// </summary>
    /// <typeparam name="U">The type to convert the value to.</typeparam>
    /// <param name="value">A <see cref="JToken" /> cast as a <see cref="IEnumerable{T}" /> of <see cref="JToken" />.</param>
    /// <returns>A converted value.</returns>
    public static U? Value<U>(this IEnumerable<JToken> value) =>
        value.Value<JToken, U>();

    /// <summary>
    /// Converts the value.
    /// </summary>
    /// <typeparam name="T">The source collection type.</typeparam>
    /// <typeparam name="U">The type to convert the value to.</typeparam>
    /// <param name="value">A <see cref="JToken" /> cast as a <see cref="IEnumerable{T}" /> of <see cref="JToken" />.</param>
    /// <returns>A converted value.</returns>
    public static U? Value<T, U>(this IEnumerable<T> value)
        where T : JToken
    {
        if (value is not JToken token)
        {
            throw new ArgumentException("Source value must be a JToken.");
        }

        return token.Convert<JToken, U>();
    }

    internal static IEnumerable<U?> Values<T, U>(this IEnumerable<T> source, object? key)
        where T : JToken
    {
        if (key == null)
        {
            foreach (var token in source)
            {
                if (token is JValue value)
                {
                    yield return Convert<JValue, U>(value);
                }
                else
                {
                    foreach (var t in token.Children())
                    {
                        yield return t.Convert<JToken, U>();
                    }
                }
            }

            yield break;
        }

        foreach (var token in source)
        {
            var value = token[key];
            if (value != null)
            {
                yield return value.Convert<JToken, U>();
            }
        }
    }

    /// <summary>
    /// Returns a collection of child tokens of every array in the source collection.
    /// </summary>
    /// <typeparam name="T">The source collection type.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains the source collection.</param>
    /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains the values of every token in the source collection.</returns>
    public static IJEnumerable<JToken> Children<T>(this IEnumerable<T> source)
        where T : JToken =>
        Children<T, JToken>(source)!.AsJEnumerable();

    /// <summary>
    /// Returns a collection of converted child tokens of every array in the source collection.
    /// </summary>
    /// <param name="source">An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains the source collection.</param>
    /// <typeparam name="U">The type to convert the values to.</typeparam>
    /// <typeparam name="T">The source collection type.</typeparam>
    /// <returns>An <see cref="IEnumerable{T}" /> that contains the converted values of every token in the source collection.</returns>
    public static IEnumerable<U?> Children<T, U>(this IEnumerable<T> source)
        where T : JToken
    {
        foreach (var token in source)
        {
            foreach (var child in token.Children())
            {
                yield return child.Convert<JToken, U>();
            }
        }
    }

    internal static IEnumerable<U?> Convert<T, U>(this IEnumerable<T> source)
        where T : JToken
    {
        foreach (var token in source)
        {
            yield return Convert<JToken, U>(token);
        }
    }

    internal static U? Convert<T, U>(this T token)
        where T : JToken?
    {
        if (token == null)
        {
#pragma warning disable CS8653
            // A default expression introduces a null value for a type parameter.
            return default;
#pragma warning restore CS8653
        }

        if (token is U castValue
            // don't want to cast JValue to its interfaces, want to get the internal value
            && typeof(U) != typeof(IComparable) && typeof(U) != typeof(IFormattable))
        {
            return castValue;
        }

        if (token is not JValue value)
        {
            throw new InvalidCastException($"Cannot cast {token.GetType()} to {typeof(T)}.");
        }

        if (value.Value is U u)
        {
            return u;
        }

        var targetType = typeof(U);

        if (targetType.IsNullableType())
        {
            if (value.Value == null)
            {
#pragma warning disable CS8653
                // A default expression introduces a null value for a type parameter.
                return default;
#pragma warning restore CS8653
            }

            targetType = Nullable.GetUnderlyingType(targetType)!;
        }

#if NET6_0_OR_GREATER
        if (value.Value != null &&
            ConvertUtils.TryConvertModernNumber(value.Value, targetType, out var converted))
        {
            return (U) converted!;
        }
#endif

        return (U) System.Convert.ChangeType(value.Value, targetType, InvariantCulture)!;
    }

    /// <summary>
    /// Returns the input typed as <see cref="IJEnumerable{T}" />.
    /// </summary>
    /// <param name="source">An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains the source collection.</param>
    /// <returns>The input typed as <see cref="IJEnumerable{T}" />.</returns>
    public static IJEnumerable<JToken> AsJEnumerable(this IEnumerable<JToken> source) =>
        source.AsJEnumerable<JToken>();

    /// <summary>
    /// Returns the input typed as <see cref="IJEnumerable{T}" />.
    /// </summary>
    /// <typeparam name="T">The source collection type.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> that contains the source collection.</param>
    /// <returns>The input typed as <see cref="IJEnumerable{T}" />.</returns>
    public static IJEnumerable<T> AsJEnumerable<T>(this IEnumerable<T> source)
        where T : JToken
    {
        if (source is IJEnumerable<T> customEnumerable)
        {
            return customEnumerable;
        }

        return new JEnumerable<T>(source);
    }
}