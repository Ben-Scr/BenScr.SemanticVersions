using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace BenScr.SemanticVersions.Tests;

public sealed class VersionTests
{
    [Theory]
    [InlineData("0.0.0")]
    [InlineData("1.2.3")]
    [InlineData("1.0.0-alpha")]
    [InlineData("1.0.0-alpha.1")]
    [InlineData("1.0.0-0.3.7")]
    [InlineData("1.0.0-x.7.z.92")]
    [InlineData("1.0.0-x-y-z.--")]
    [InlineData("1.0.0-alpha+001")]
    [InlineData("1.0.0+20130313144700")]
    [InlineData("1.0.0-beta+exp.sha.5114f85")]
    [InlineData("1.0.0+21AF26D3----117B344092BD")]
    [InlineData("1.2.3--")]
    [InlineData("1.2.3+00.000")]
    [InlineData("1.2.3-00a.01-.0A.1-0")]
    [InlineData("99999999999999999999999999999.18446744073709551616.1-184467440737095516160")]
    public void ValidVersionsRoundTripExactly(string text)
    {
        Assert.True(SemanticVersion.TryParse(text, out SemanticVersion? parsed));
        Assert.NotNull(parsed);
        Assert.Equal(text, parsed.ToString());
        Assert.True(parsed.ExactEquals(SemanticVersion.Parse(text)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("1.2")]
    [InlineData("1.2.3.4")]
    [InlineData("01.2.3")]
    [InlineData("1.02.3")]
    [InlineData("1.2.03")]
    [InlineData("1.2.3-01")]
    [InlineData("1.2.3-a.00")]
    [InlineData("1.2.3-")]
    [InlineData("1.2.3+")]
    [InlineData("1.2.3-+")]
    [InlineData("1.2.3-+a")]
    [InlineData("1.2.3-a+")]
    [InlineData("1.2.3-a..b")]
    [InlineData("1.2.3+a..b")]
    [InlineData("1.2.3-.a")]
    [InlineData("1.2.3+.a")]
    [InlineData("1.2.3-a.")]
    [InlineData("1.2.3+a.")]
    [InlineData("1.2.3+a+b")]
    [InlineData("1.2.3_alpha")]
    [InlineData("1.2.3-a_b")]
    [InlineData("1.2.3+a_b")]
    [InlineData("1.2.3 a")]
    [InlineData(" 1.2.3")]
    [InlineData("1.2.3 ")]
    [InlineData("1.2.3\n")]
    [InlineData("1.2.3\r\n")]
    [InlineData("1.2.3\0")]
    [InlineData("v1.2.3")]
    [InlineData("=1.2.3")]
    [InlineData("+1.2.3")]
    [InlineData("-1.2.3")]
    [InlineData("1.-2.3")]
    [InlineData("1.2.-3")]
    [InlineData("1.2.3-ä")]
    [InlineData("1.2.3+é")]
    [InlineData("١.2.3")]
    [InlineData("1.2.3-١")]
    [InlineData("1.2.3+😀")]
    [InlineData("1.2.3+\ud800")]
    public void MalformedVersionsRejectAndClearOutput(string? text)
    {
        SemanticVersion? parsed = SemanticVersion.Parse("1.0.0");
        Assert.False(SemanticVersion.TryParse(text, out parsed));
        Assert.Null(parsed);
        if (text is null)
        {
            Assert.Throws<ArgumentNullException>(() => SemanticVersion.Parse(text!));
        }
        else
        {
            Assert.Throws<FormatException>(() => SemanticVersion.Parse(text));
        }
    }

    [Fact]
    public void ComponentPropertiesPreserveMetadataAndCase()
    {
        var version = SemanticVersion.Parse("999999999999999999999.20.3-RC.1+Build.000");
        Assert.Equal("999999999999999999999", version.Major);
        Assert.Equal("20", version.Minor);
        Assert.Equal("3", version.Patch);
        Assert.Equal("RC.1", version.Prerelease);
        Assert.Equal("Build.000", version.BuildMetadata);
        Assert.True(version.IsPrerelease);
        var release = SemanticVersion.Parse("0.0.0");
        Assert.Null(release.Prerelease);
        Assert.Null(release.BuildMetadata);
        Assert.False(release.IsPrerelease);
    }

    [Fact]
    public void OfficialPrecedenceSequenceSorts()
    {
        string[] ordered = { "1.0.0-alpha", "1.0.0-alpha.1", "1.0.0-alpha.beta", "1.0.0-beta",
            "1.0.0-beta.2", "1.0.0-beta.11", "1.0.0-rc.1", "1.0.0", "2.0.0", "2.1.0", "2.1.1" };
        SemanticVersion[] sorted = ordered.Reverse().Select(SemanticVersion.Parse).ToArray();
        Array.Sort(sorted);
        Assert.Equal(ordered, sorted.Select(value => value.ToString()));
        for (int index = 1; index < sorted.Length; index++)
        {
            Assert.True(sorted[index - 1] < sorted[index]);
        }
    }

    [Theory]
    [InlineData("1.2.9", "1.2.10")]
    [InlineData("1.9.999", "1.10.0")]
    [InlineData("9.999.999", "10.0.0")]
    [InlineData("1.0.0-9", "1.0.0-10")]
    [InlineData("1.0.0-99999", "1.0.0--")]
    [InlineData("1.0.0-Z", "1.0.0-a")]
    [InlineData("1.0.0-A", "1.0.0-AA")]
    [InlineData("1.0.0-a", "1.0.0-a.0")]
    [InlineData("1.0.0-0", "1.0.0")]
    [InlineData("1.0.0-18446744073709551615", "1.0.0-18446744073709551616")]
    [InlineData("18446744073709551615.0.0", "18446744073709551616.0.0")]
    [InlineData("1.18446744073709551615.0", "1.18446744073709551616.0")]
    [InlineData("1.0.18446744073709551615", "1.0.18446744073709551616")]
    public void NumericAndAsciiOrdering(string lower, string higher)
    {
        var left = SemanticVersion.Parse(lower);
        var right = SemanticVersion.Parse(higher);
        Assert.True(left < right);
        Assert.True(left <= right);
        Assert.True(right > left);
        Assert.True(right >= left);
        Assert.NotEqual(left, right);
        Assert.True(left != right);
        Assert.True(left.CompareTo(right) < 0);
        Assert.True(SemanticVersion.Compare(right, left) > 0);
    }

    [Fact]
    public void PrecedenceEqualityAndCollectionsIgnoreOnlyBuildMetadata()
    {
        var left = SemanticVersion.Parse("1.2.3-alpha+one");
        var right = SemanticVersion.Parse("1.2.3-alpha+two");
        var bare = SemanticVersion.Parse("1.2.3-alpha");
        Assert.True(left == right);
        Assert.Equal(left, bare);
        Assert.True(left.Equals((object)right));
        Assert.Equal(0, left.CompareTo(right));
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
        Assert.Single(new HashSet<SemanticVersion> { left, right, bare });
        Assert.Single(new SortedSet<SemanticVersion> { left, right, bare });
        var dictionary = new Dictionary<SemanticVersion, int> { [left] = 7 };
        Assert.Equal(7, dictionary[right]);
        Assert.NotEqual(left, SemanticVersion.Parse("1.2.3-Alpha"));
        Assert.NotEqual(left, SemanticVersion.Parse("1.2.3"));
        Assert.False(left.Equals("1.2.3-alpha+one"));
    }

    [Fact]
    public void ExactComparerIncludesMetadataAndIsConsistentWithIdentity()
    {
        var first = SemanticVersion.Parse("1.0.0+ABC");
        var duplicate = SemanticVersion.Parse("1.0.0+ABC");
        var lower = SemanticVersion.Parse("1.0.0+abc");
        var bare = SemanticVersion.Parse("1.0.0");
        IEqualityComparer<SemanticVersion> comparer = SemanticVersion.ExactComparer;
        Assert.True(first.ExactEquals(duplicate));
        Assert.True(comparer.Equals(first, duplicate));
        Assert.Equal(comparer.GetHashCode(first), comparer.GetHashCode(duplicate));
        Assert.False(first.ExactEquals(lower));
        Assert.False(first.ExactEquals(bare));
        Assert.Equal(3, new HashSet<SemanticVersion>(new[] { first, duplicate, lower, bare }, comparer).Count);
        Assert.True(comparer.Equals(null, null));
        Assert.False(comparer.Equals(first, null));
        Assert.False(comparer.Equals(null, first));
        Assert.Throws<ArgumentNullException>(() => comparer.GetHashCode(null!));
    }

    [Fact]
    public void NullOrderingIsConsistentAcrossInterfacesAndOperators()
    {
        var version = SemanticVersion.Parse("0.0.0");
        SemanticVersion? absent = null;
        Assert.True(absent == null);
        Assert.False(version == absent);
        Assert.True(version != absent);
        Assert.False(version.Equals((SemanticVersion?)null));
        Assert.False(version.Equals((object?)null));
        Assert.False(version.ExactEquals(null));
        Assert.True(absent < version);
        Assert.True(version > absent);
        Assert.True(absent <= null);
        Assert.True(absent >= null);
        Assert.Equal(0, SemanticVersion.Compare(null, null));
        Assert.True(version.CompareTo(null) > 0);
        Assert.True(((IComparable)version).CompareTo(null) > 0);
        Assert.Equal(0, ((IComparable)version).CompareTo(SemanticVersion.Parse("0.0.0+meta")));
        Assert.Throws<ArgumentException>(() => ((IComparable)version).CompareTo("0.0.0"));
        var array = new ArrayList { version, null, SemanticVersion.Parse("1.0.0") };
        array.Sort();
        Assert.Null(array[0]);
        Assert.Same(version, array[1]);
    }

    [Fact]
    public void InputLimitIsInclusiveAndDoesNotImposeIntegerWidth()
    {
        string largestCore = new string('9', SemanticVersion.MaximumTextLength - 4) + ".0.0";
        Assert.Equal(SemanticVersion.MaximumTextLength, largestCore.Length);
        Assert.True(SemanticVersion.TryParse(largestCore, out SemanticVersion? parsed));
        Assert.True(parsed > SemanticVersion.Parse("999999999999999999999999.0.0"));
        string largestMetadata = "0.0.0+" + new string('a', SemanticVersion.MaximumTextLength - 6);
        Assert.True(SemanticVersion.TryParse(largestMetadata, out _));
        Assert.False(SemanticVersion.TryParse(largestMetadata + "a", out _));
        Assert.Throws<FormatException>(() => SemanticVersion.Parse(largestMetadata + "a"));
        Assert.False(SemanticVersion.TryParse(largestCore + "0", out _));
        string manyIdentifiers = "0.0.0-" + string.Join('.', Enumerable.Repeat("0", 2045));
        Assert.True(SemanticVersion.TryParse(manyIdentifiers, out _));
    }

    [Fact]
    public void CultureDoesNotAffectParsingOrPrecedence()
    {
        CultureInfo before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            Assert.True(SemanticVersion.Parse("1.0.0-I") < SemanticVersion.Parse("1.0.0-i"));
            Assert.False(SemanticVersion.TryParse("1,2,3", out _));
            Assert.Equal("1.2.3+I", SemanticVersion.Parse("1.2.3+I").ToString());
        }
        finally { CultureInfo.CurrentCulture = before; }
    }

    [Fact]
    public void FixedSeedOrderingMatchesBigIntegerReferenceAndTransitivity()
    {
        var random = new Random(713009);
        for (int iteration = 0; iteration < 4000; iteration++)
        {
            string first = RandomVersion(random);
            string second = RandomVersion(random);
            string third = RandomVersion(random);
            var a = SemanticVersion.Parse(first);
            var b = SemanticVersion.Parse(second);
            var c = SemanticVersion.Parse(third);
            Assert.Equal(Math.Sign(ReferenceCompare(first, second)), Math.Sign(a.CompareTo(b)));
            Assert.Equal(-Math.Sign(a.CompareTo(b)), Math.Sign(b.CompareTo(a)));
            Assert.Equal(a.CompareTo(b) == 0, a.Equals(b));
            if (a <= b && b <= c) { Assert.True(a <= c); }
            Assert.Equal(0, a.CompareTo(SemanticVersion.Parse(first.Split('+')[0] + "+different")));
        }
    }

    [Fact]
    public void FixedSeedParserMatchesIndependentGrammar()
    {
        const string number = "(?:0|[1-9][0-9]*)";
        const string pre = "(?:0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*)";
        const string build = "[0-9A-Za-z-]+";
        var grammar = new Regex($"\\A{number}\\.{number}\\.{number}(?:-{pre}(?:\\.{pre})*)?(?:\\+{build}(?:\\.{build})*)?\\z",
            RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, TimeSpan.FromSeconds(2));
        var random = new Random(70912);
        const string alphabet = "0123aAzZ.-+_ /\n";
        for (int iteration = 0; iteration < 6000; iteration++)
        {
            string valid = RandomVersion(random);
            int offset = random.Next(valid.Length + 1);
            string mutated = valid.Insert(offset, alphabet[random.Next(alphabet.Length)].ToString());
            Assert.Equal(grammar.IsMatch(mutated), SemanticVersion.TryParse(mutated, out _));
        }
    }

    [Fact]
    public void ImmutableVersionsAndComparersSupportConcurrentReads()
    {
        var value = SemanticVersion.Parse("123456789012345678901234567890.1.2-rc.9+immutable");
        var samePrecedence = SemanticVersion.Parse("123456789012345678901234567890.1.2-rc.9+other");
        int hash = value.GetHashCode();
        Parallel.For(0, 1000, _ =>
        {
            Assert.Equal(0, value.CompareTo(samePrecedence));
            Assert.Equal(hash, value.GetHashCode());
            Assert.True(value.ExactEquals(SemanticVersion.Parse(value.ToString())));
            Assert.False(SemanticVersion.ExactComparer.Equals(value, samePrecedence));
        });
    }

    private static string RandomVersion(Random random)
    {
        string Number()
        {
            int length = random.Next(1, 80);
            return random.Next(1, 10).ToString(CultureInfo.InvariantCulture) +
                new string(Enumerable.Range(1, length - 1).Select(_ => (char)('0' + random.Next(10))).ToArray());
        }
        string core = random.Next(3) == 0 ? "1.0.0" : Number() + "." + Number() + "." + Number();
        string[] identifiers = { "0", "alpha", "Alpha", "-", "rc", "9", "10", "00x", Number() };
        string prerelease = random.Next(3) == 0 ? "" : "-" + string.Join('.',
            Enumerable.Range(0, random.Next(1, 6)).Select(_ => identifiers[random.Next(identifiers.Length)]));
        return core + prerelease + (random.Next(2) == 0 ? "+build.000" : "");
    }

    private static int ReferenceCompare(string left, string right)
    {
        string[] a = left.Split('+')[0].Split('-', 2);
        string[] b = right.Split('+')[0].Split('-', 2);
        BigInteger[] coreA = a[0].Split('.').Select(x => BigInteger.Parse(x, CultureInfo.InvariantCulture)).ToArray();
        BigInteger[] coreB = b[0].Split('.').Select(x => BigInteger.Parse(x, CultureInfo.InvariantCulture)).ToArray();
        for (int index = 0; index < 3; index++)
        {
            int difference = coreA[index].CompareTo(coreB[index]);
            if (difference != 0) { return difference; }
        }
        if (a.Length != b.Length) { return b.Length.CompareTo(a.Length); }
        if (a.Length == 1) { return 0; }
        string[] idsA = a[1].Split('.');
        string[] idsB = b[1].Split('.');
        for (int index = 0; index < Math.Min(idsA.Length, idsB.Length); index++)
        {
            bool numericA = BigInteger.TryParse(idsA[index], NumberStyles.None, CultureInfo.InvariantCulture, out BigInteger intA);
            bool numericB = BigInteger.TryParse(idsB[index], NumberStyles.None, CultureInfo.InvariantCulture, out BigInteger intB);
            int difference = numericA && numericB ? intA.CompareTo(intB) : numericA != numericB
                ? (numericA ? -1 : 1) : StringComparer.Ordinal.Compare(idsA[index], idsB[index]);
            if (difference != 0) { return difference; }
        }
        return idsA.Length.CompareTo(idsB.Length);
    }
}
