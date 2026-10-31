// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Specifies that when a method returns <see cref="ReturnValue"/>, the parameter will not be null
/// even if the corresponding type allows it.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class NotNullWhenAttribute : Attribute
{
    public NotNullWhenAttribute(bool returnValue)
    {
        this.ReturnValue = returnValue;
    }

    /// <summary>
    /// Gets the return value condition.
    /// </summary>
    public bool ReturnValue { get; }
}

/// <summary>
/// Specifies that the method or property will ensure that the listed field and property members have not-null values.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
internal sealed class SetsRequiredMembersAttribute : Attribute;
#endif
