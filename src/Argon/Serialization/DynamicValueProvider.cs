// Copyright (c) 2007 James Newton-King. All rights reserved.
// Use of this source code is governed by The MIT License,
// as found in the license.md file.

namespace Argon;

/// <summary>
/// Get and set values for a <see cref="MemberInfo" /> using dynamic methods.
/// </summary>
[RequiresDynamicCode(MiscellaneousUtils.AotWarning)]
public class DynamicValueProvider : IValueProvider
{
    readonly MemberInfo member;
    Func<object, object?>? getter;
    Action<object, object?>? setter;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicValueProvider" /> class.
    /// </summary>
    /// <param name="member">The member info.</param>
    public DynamicValueProvider(MemberInfo member) =>
        this.member = member;

    /// <summary>
    /// Sets the value.
    /// </summary>
    /// <param name="target">The target to set the value on.</param>
    /// <param name="value">The value to set on the target.</param>
    public void SetValue(object target, object? value)
    {
        try
        {
            setter ??= DelegateFactory.CreateSet<object>(member);

#if !RELEASE
            // dynamic method doesn't check whether the type is 'legal' to set
            // add this check for unit tests
            var underlyingType = member.GetMemberUnderlyingType();
            if (value == null)
            {
                if (!underlyingType.IsNullable())
                {
                    throw new JsonSerializationException($"Incompatible value. Cannot set {member} to null.");
                }
            }
            else if (!underlyingType.IsInstanceOfType(value))
            {
                throw new JsonSerializationException($"Incompatible value. Cannot set {member} to type {value.GetType()}.");
            }
#endif

            setter(target, value);
        }
        catch (Exception exception)
        {
            throw new JsonSerializationException($"Error setting value to '{member.Name}' on '{target.GetType()}'.", exception);
        }
    }

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <param name="target">The target to get the value from.</param>
    /// <returns>The value.</returns>
    public object? GetValue(object target)
    {
        try
        {
            getter ??= DelegateFactory.CreateGet<object>(member);

            return getter(target);
        }
        catch (Exception exception)
        {
            throw new JsonSerializationException($"Error getting value from '{member.Name}' on '{target.GetType()}'.", exception);
        }
    }
}