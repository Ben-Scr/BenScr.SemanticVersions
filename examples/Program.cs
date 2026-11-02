using System;
using System.Collections.Generic;
using System.Linq;
using BenScr.SemanticVersions;

var candidates = new[] { "2.0.0", "1.0.0-rc.1", "1.0.0", "1.0.0-alpha.2", "1.0.0-alpha.11" }
    .Select(SemanticVersion.Parse).ToArray();
Array.Sort(candidates);
Require(candidates[0].ToString() == "1.0.0-alpha.2" && candidates[^1].ToString() == "2.0.0", "Release sorting");
var firstBuild = SemanticVersion.Parse("1.2.3+sha.abc");
var secondBuild = SemanticVersion.Parse("1.2.3+sha.def");
Require(firstBuild == secondBuild && firstBuild.CompareTo(secondBuild) == 0, "Metadata ignored for precedence");
Require(!firstBuild.ExactEquals(secondBuild), "Exact identity includes metadata");
Require(new HashSet<SemanticVersion> { firstBuild, secondBuild }.Count == 1, "Precedence hashing");
Require(new HashSet<SemanticVersion>(new[] { firstBuild, secondBuild }, SemanticVersion.ExactComparer).Count == 2, "Exact hashing");
Require(SemanticVersion.Parse("18446744073709551616.0.0") > SemanticVersion.Parse("18446744073709551615.0.0"), "Large numeric components");
Require(!SemanticVersion.TryParse("1.0.0-01", out _), "Strict prerelease validation");
Require(SemanticVersion.Parse("1.0.0+001").BuildMetadata == "001", "Build zeros preserved");
Require(!SemanticVersion.TryParse("1.0.0+" + new string('x', SemanticVersion.MaximumTextLength), out _), "Input resource bound");
Console.WriteLine(string.Join(" < ", candidates.Select(version => version.ToString())));
Console.WriteLine("SemVer parsing, ordering, exact identity and collection contracts passed.");

static void Require(bool condition, string message)
{
    if (!condition) { throw new InvalidOperationException(message); }
}
