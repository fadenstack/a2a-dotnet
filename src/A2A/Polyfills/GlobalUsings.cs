// Names the read-only set type exposed by extension negotiation results.
// IReadOnlySet<T> exists from .NET 5; netstandard2.0 exposes the read-only collection view instead.
#if NETSTANDARD2_0
global using ExtensionSet = System.Collections.Generic.IReadOnlyCollection<string>;
#else
global using ExtensionSet = System.Collections.Generic.IReadOnlySet<string>;
#endif
