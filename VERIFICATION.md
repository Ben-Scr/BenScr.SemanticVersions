# Verification

The release workflow is `pwsh -File tools/Verify.ps1` from a clean committed checkout. It uses
.NET SDK 10.0.303, xUnit v3 4.0.2 and Microsoft Testing Platform. Dependency versions are locked.

## Behavior coverage

- Published SemVer grammar examples, explicit malformed inputs, complete-string parsing and exact round trips.
- Core and prerelease numeric ordering beyond UInt64, ASCII ordering and official precedence examples.
- Equality, hash and ordering agreement; metadata-sensitive exact identity in real hash collections.
- Nulls, nongeneric comparison, culture independence and concurrent reads of immutable values.
- Inclusive 4096-character input bounds, maximum-length core and metadata, and thousands of identifiers.
- 4,000 fixed-seed comparisons against an independent BigInteger-based reference, antisymmetry and transitivity.
- 6,000 fixed-seed mutations checked against an independent nonbacktracking grammar expression.

The initial local Windows run passed 81 tests with none skipped. Exact release evidence is recorded
by each verifier invocation in `artifacts/verification-<id>/tests.trx` and `release-manifest.json`.
The configured CI matrix runs Windows and Ubuntu; a configuration file alone does not establish
a successful remote run. CI logs and artifacts supply that evidence.

## Artifact and consumer checks

Verification builds source, tests, the example and package auditor; checks whitespace and style;
compares exported API members with `docs/PublicApi.txt`; executes the complete example; and packs
both nupkg and snupkg. The archive auditor checks the exact inventory, package ID/version,
repository commit, dependency absence, original icon/license bytes, README, XML documentation,
matching Portable PDB identities and source checksums/Source Link mappings.

An isolated temporary consumer restores the exact package from the local verification feed into
an empty cache. It has no project reference. It exercises ordering, strict syntax, metadata identity,
collections, large numeric components and input limits. The same script accepts the public NuGet feed
after indexing; that separate run is required before publication is reported as verified.

No timing benchmarks, Native AOT, trimming, interactive debugger stepping, other runtime targets or
architectures are claimed. Final NuGet indexing, public symbols, repository signatures and the
rendered package icon are recorded during the release procedure.
