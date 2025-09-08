// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Represents a reader that provides fast, non-cached, forward-only access to serialized JSON data.
/// </summary>
public class JTokenReader :
    JsonReader,
    IJsonLineInfo
{
    readonly JToken root;
    JToken? parent;

    /// <summary>
    /// Gets the <see cref="JToken" /> at the reader's current position.
    /// </summary>
    public JToken? CurrentToken { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="JTokenReader" /> class.
    /// </summary>
    public JTokenReader(JToken token) =>
        root = token;

    /// <summary>
    /// Initializes a new instance of the <see cref="JTokenReader" /> class.
    /// </summary>
    /// <param name="initialPath">The initial path of the token. It is prepended to the returned <see cref="Path" />.</param>
    public JTokenReader(JToken token, string initialPath) :
        this(token) =>
        Path = initialPath;

    /// <summary>
    /// Reads the next JSON token from the underlying <see cref="JToken" />.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the next token was read successfully; <c>false</c> if there are no more tokens to read.
    /// </returns>
    public override bool Read()
    {
        if (CurrentState != State.Start)
        {
            if (CurrentToken == null)
            {
                return false;
            }

            if (CurrentToken is JContainer container && parent != container)
            {
                return ReadInto(container);
            }

            return ReadOver(CurrentToken);
        }

        // The current value could already be the root value if it is a comment
        if (CurrentToken == root)
        {
            return false;
        }

        CurrentToken = root;
        SetToken(CurrentToken);
        return true;
    }

    bool ReadOver(JToken t)
    {
        if (t == root)
        {
            return ReadToEnd();
        }

        var next = t.Next;
        if (next == null || next == t || t == t.Parent!.Last)
        {
            if (t.Parent == null)
            {
                return ReadToEnd();
            }

            return SetEnd(t.Parent);
        }

        CurrentToken = next;
        SetToken(CurrentToken);
        return true;
    }

    bool ReadToEnd()
    {
        CurrentToken = null;
        SetToken(JsonToken.None);
        return false;
    }

    static JsonToken? GetEndToken(JContainer c)
    {
        switch (c.Type)
        {
            case JTokenType.Object:
                return JsonToken.EndObject;
            case JTokenType.Array:
                return JsonToken.EndArray;
            case JTokenType.Property:
                return null;
            default:
                throw MiscellaneousUtils.CreateArgumentOutOfRangeException(nameof(c.Type), c.Type, "Unexpected JContainer type.");
        }
    }

    bool ReadInto(JContainer c)
    {
        var firstChild = c.First;
        if (firstChild == null)
        {
            return SetEnd(c);
        }

        SetToken(firstChild);
        CurrentToken = firstChild;
        parent = c;
        return true;
    }

    bool SetEnd(JContainer c)
    {
        var endToken = GetEndToken(c);
        if (endToken == null)
        {
            return ReadOver(c);
        }

        SetToken(endToken.GetValueOrDefault());
        CurrentToken = c;
        parent = c;
        return true;
    }

    void SetToken(JToken token)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                SetToken(JsonToken.StartObject);
                break;
            case JTokenType.Array:
                SetToken(JsonToken.StartArray);
                break;
            case JTokenType.Property:
                SetToken(JsonToken.PropertyName, ((JProperty) token).Name);
                break;
            case JTokenType.Comment:
                SetToken(JsonToken.Comment, ((JValue) token).Value);
                break;
            case JTokenType.Integer:
                SetToken(JsonToken.Integer, ((JValue) token).Value);
                break;
            case JTokenType.Float:
                SetToken(JsonToken.Float, ((JValue) token).Value);
                break;
            case JTokenType.String:
                SetToken(JsonToken.String, ((JValue) token).Value);
                break;
            case JTokenType.Boolean:
                SetToken(JsonToken.Boolean, ((JValue) token).Value);
                break;
            case JTokenType.Null:
                SetToken(JsonToken.Null, ((JValue) token).Value);
                break;
            case JTokenType.Undefined:
                SetToken(JsonToken.Undefined, ((JValue) token).Value);
                break;
            case JTokenType.Date:
            {
                var v = ((JValue) token).Value;
                SetToken(JsonToken.Date, v);
                break;
            }
            case JTokenType.Raw:
                SetToken(JsonToken.Raw, ((JValue) token).Value);
                break;
            case JTokenType.Bytes:
                SetToken(JsonToken.Bytes, ((JValue) token).Value);
                break;
            case JTokenType.Guid:
                SetToken(JsonToken.String, SafeToString(((JValue) token).Value));
                break;
            case JTokenType.Uri:
            {
                var v = ((JValue) token).Value;
                SetToken(JsonToken.String, v is Uri uri ? uri.OriginalString : SafeToString(v));
                break;
            }
            case JTokenType.TimeSpan:
                SetToken(JsonToken.String, SafeToString(((JValue) token).Value));
                break;
            default:
                throw MiscellaneousUtils.CreateArgumentOutOfRangeException(nameof(token.Type), token.Type, "Unexpected JTokenType.");
        }
    }

    static string? SafeToString(object? value) =>
        value?.ToString();

    bool IJsonLineInfo.HasLineInfo()
    {
        if (CurrentState == State.Start)
        {
            return false;
        }

        IJsonLineInfo? info = CurrentToken;
        return info != null && info.HasLineInfo();
    }

    int IJsonLineInfo.LineNumber
    {
        get
        {
            if (CurrentState == State.Start)
            {
                return 0;
            }

            IJsonLineInfo? info = CurrentToken;
            if (info == null)
            {
                return 0;
            }

            return info.LineNumber;
        }
    }

    int IJsonLineInfo.LinePosition
    {
        get
        {
            if (CurrentState == State.Start)
            {
                return 0;
            }

            IJsonLineInfo? info = CurrentToken;
            if (info == null)
            {
                return 0;
            }

            return info.LinePosition;
        }
    }

    /// <summary>
    /// Gets the path of the current JSON token.
    /// </summary>
    [field: AllowNull, MaybeNull]
    public override string Path
    {
        get
        {
            var path = base.Path;

            field ??= root.Path;

            if (!field.IsNullOrEmpty())
            {
                if (path.IsNullOrEmpty())
                {
                    return field;
                }

                if (path.StartsWith('['))
                {
                    path = field + path;
                }
                else
                {
                    path = $"{field}.{path}";
                }
            }

            return path;
        }
    }
}