# BenScr.SemanticVersions

Strict SemVer parsing, release sorting and explicit identity comparison for .NET 10, without runtime package dependencies.

Created by Ben Schneider — https://github.com/Ben-Scr

```shell
dotnet add package BenScr.SemanticVersions --version 1.0.0
```

```csharp
using System;
using System.Collections.Generic;
using BenScr.SemanticVersions;

var release = SemanticVersion.Parse("2.0.0");
var candidate = SemanticVersion.Parse("2.0.0-rc.1+build.004");
Console.WriteLine(candidate < release); // True

var first = SemanticVersion.Parse("1.2.3+abc");
var second = SemanticVersion.Parse("1.2.3+def");
Console.WriteLine(first == second);          // True: same precedence
Console.WriteLine(first.ExactEquals(second)); // False: different metadata

var distinctBuilds = new HashSet<SemanticVersion>(SemanticVersion.ExactComparer)
{
    first, second
};
Console.WriteLine(distinctBuilds.Count); // 2
```

## Parsing contract

The grammar and precedence rules follow [Semantic Versioning 2.0.0](https://semver.org/spec/v2.0.0.html). Input requires three nonnegative decimal components. Core numbers and numeric prerelease identifiers reject leading zeros. Identifiers accept only ASCII letters, digits and hyphens; dot-separated identifiers cannot be empty. Build metadata may contain numeric identifiers with leading zeros.

`Parse` accepts a complete string without trimming or normalization. Prefixes such as `v`, missing components, Unicode identifiers, trailing whitespace and package-manager range syntax are rejected. Null throws `ArgumentNullException`; malformed or over-limit text throws `FormatException`. `TryParse` returns false and a null output for each of those failures, including null input. Allocation failures still propagate.

`MaximumTextLength` is **4096 UTF-16 code units, inclusive**. Because valid input is ASCII, accepted characters occupy one code unit each. This explicit resource limit is an implementation constraint beyond the SemVer grammar. Numeric components have no separate fixed-width limit. For example, `18446744073709551616.0.0` is valid and compares numerically beyond `UInt64.MaxValue`.

The immutable `Major`, `Minor` and `Patch` properties return decimal strings, preserving arbitrarily large values within the input bound. `Prerelease` and `BuildMetadata` return their exact dot-separated text or null when absent. `IsPrerelease` reports whether prerelease text exists. `ToString()` returns the exact accepted input, including casing and build metadata. No increment, dependency resolution, NuGet range or npm range API is included.

## Ordering and identity

Core components compare numerically. A prerelease sorts before its corresponding normal release. Prerelease numeric identifiers compare numerically and sort before nonnumeric identifiers; other identifiers compare in ASCII order. When common identifiers are equal, the longer identifier list sorts later. Build metadata does not affect precedence.

`CompareTo`, `Compare`, the relational operators, `Equals`, `GetHashCode` and equality operators all use that same precedence identity. Consequently, both `HashSet<SemanticVersion>` and `SortedSet<SemanticVersion>` combine versions that differ only in build metadata. Ordinary string sorting is not version sorting. Use `Array.Sort`, `List.Sort` or the default generic comparer on parsed instances.

Use `ExactEquals` for a pair of exact version identities or `ExactComparer` for a dictionary or set that retains separate builds. Exact identity uses ordinal comparison of the complete accepted text, including metadata and case. It does not provide an alternative version precedence. Two exact-equal versions always have equal hashes from `ExactComparer`; its hash codes, and ordinary version hash codes, are for in-process collections rather than persistent IDs.

Every version orders after null. Two null references compare equally, both equality modes consider them equal, and an instance is never equal to null. `ExactComparer.GetHashCode(null)` throws `ArgumentNullException`. The nongeneric `IComparable` implementation rejects objects of other types with `ArgumentException`.

## Resources and concurrency

Parsing and comparisons take linear time in the bounded text lengths. Parsing owns its component strings and identifier storage; it exposes no mutable arrays. There is no recursion, numeric overflow, ambient-culture dependency or shared mutable state. Instances and the exact comparer support concurrent reads. The implementation allocates strings and arrays during parsing and does not claim allocation-free operation.

## Build and verification

Install .NET SDK 10.0.303 and PowerShell 7. From a clean committed checkout:

```powershell
pwsh -File tools/Verify.ps1
```

The verifier builds all projects, checks formatting and the public API baseline, runs the tests and example, packs the library and symbols, audits the archives and runs a consumer with an empty package cache. CI executes the same verification on Windows and Ubuntu. No solution file is required.

See [VERIFICATION.md](VERIFICATION.md), [RELEASE.md](RELEASE.md), [MIGRATION.md](MIGRATION.md) and [provenance](docs/PROVENANCE.md). The package embeds the unchanged `BenScr_Icon_Master.png` and the original custom [LICENSE.txt](LICENSE.txt).
