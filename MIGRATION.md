# API scope and adoption

This is a new independent library with no predecessor package or migrated public API. It adds
`SemanticVersion` under `BenScr.SemanticVersions`; no existing package is replaced, deprecated or removed.

Use this package for strict SemVer 2.0 values. `System.Version`, NuGet versions, npm ranges and
Git tag names have different acceptance rules. Convert those inputs explicitly when needed; this
library does not silently strip prefixes, add missing components or relax numeric leading zeros.

Core number properties are strings because valid SemVer numbers need not fit in Int32 or UInt64.
Applications that need arithmetic can choose their own numeric conversion and limits. Versions
longer than 4096 characters are outside this implementation's documented resource contract.

Default equality and ordering ignore build metadata. Choose `SemanticVersion.ExactComparer` for
collections that need to distinguish complete build identities. A sorted set using the default
comparer may retain only one of several builds at the same precedence; that is intentional.

Version 1.0.0 targets .NET 10. No claims are made for .NET Framework, .NET Standard or older runtimes.
