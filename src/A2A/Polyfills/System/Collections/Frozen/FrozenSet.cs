// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETSTANDARD2_0
#pragma warning disable CA2016, RCS1141, VSTHRD003
using System.Collections;

namespace System.Collections.Frozen;

/// <summary>
/// Minimal read-only set used where the framework has no <c>FrozenSet&lt;T&gt;</c>.
/// </summary>
internal sealed class FrozenSet<T> : IReadOnlyCollection<T>, ICollection<T>
{
    private readonly HashSet<T> _items;

    internal FrozenSet(IEnumerable<T> items, IEqualityComparer<T>? comparer) =>
        _items = new HashSet<T>(items, comparer);

    /// <summary>Gets an empty set.</summary>
    public static FrozenSet<T> Empty { get; } = new([], null);

    /// <inheritdoc />
    public int Count => _items.Count;

    /// <inheritdoc />
    bool ICollection<T>.IsReadOnly => true;

    /// <inheritdoc />
    public bool Contains(T item) => _items.Contains(item);

    /// <inheritdoc />
    public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void ICollection<T>.Add(T item) => throw new NotSupportedException();

    void ICollection<T>.Clear() => throw new NotSupportedException();

    bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
}

/// <summary>Creates <see cref="FrozenSet{T}"/> instances.</summary>
internal static class FrozenSetPolyfill
{
    /// <summary>Creates a read-only set from the specified items.</summary>
    public static FrozenSet<T> ToFrozenSet<T>(this IEnumerable<T> source, IEqualityComparer<T>? comparer = null) =>
        new(source, comparer);
}
#endif
