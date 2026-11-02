using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BenScr.SemanticVersions;

/// <summary>An immutable SemVer 2.0 version with culture-independent precedence and equality.</summary>
/// <remarks>Equality, hashing and ordering ignore build metadata. Use <see cref="ExactComparer"/> or
/// <see cref="ExactEquals"/> when metadata is part of identity. Instances and comparers are thread-safe.
/// Numeric components are retained as decimal strings without fixed-width integer limits.</remarks>
public sealed class SemanticVersion : IComparable<SemanticVersion>, IComparable, IEquatable<SemanticVersion>
{
    private readonly string text;
    private readonly string precedenceText;
    private readonly string[] prereleaseIdentifiers;

    /// <summary>Maximum accepted input length, inclusive, in UTF-16 code units: 4096.</summary>
    /// <remarks>This implementation resource limit is additional to SemVer grammar. All accepted characters are ASCII.</remarks>
    public const int MaximumTextLength = 4096;

    private SemanticVersion(string text, string major, string minor, string patch, string? prerelease, string? buildMetadata)
    {
        this.text = text;
        Major = major;
        Minor = minor;
        Patch = patch;
        Prerelease = prerelease;
        BuildMetadata = buildMetadata;
        precedenceText = buildMetadata is null ? text : text[..(text.Length - buildMetadata.Length - 1)];
        prereleaseIdentifiers = prerelease?.Split('.') ?? Array.Empty<string>();
    }

    /// <summary>Gets the nonnegative major component as canonical decimal digits.</summary>
    public string Major { get; }

    /// <summary>Gets the nonnegative minor component as canonical decimal digits.</summary>
    public string Minor { get; }

    /// <summary>Gets the nonnegative patch component as canonical decimal digits.</summary>
    public string Patch { get; }

    /// <summary>Gets the case-sensitive dot-separated prerelease text, or null for a normal release.</summary>
    public string? Prerelease { get; }

    /// <summary>Gets the case-sensitive dot-separated build metadata, or null when absent.</summary>
    public string? BuildMetadata { get; }

    /// <summary>Gets whether prerelease identifiers are present.</summary>
    public bool IsPrerelease => Prerelease is not null;

    /// <summary>Gets an immutable equality comparer that includes build metadata using ordinal text identity.</summary>
    /// <remarks>Two null references are equal; null differs from a version. Hashing null throws ArgumentNullException.
    /// Hash codes are for in-process collections and are not stable serialized identifiers.</remarks>
    public static IEqualityComparer<SemanticVersion> ExactComparer { get; } = new ExactIdentityComparer();

    /// <summary>Parses a complete, strict SemVer 2.0 version of at most <see cref="MaximumTextLength"/> characters.</summary>
    /// <param name="text">ASCII version text; whitespace and prefixes such as v or = are not accepted.</param>
    /// <returns>An immutable version retaining the exact accepted text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="FormatException">The text violates the grammar or exceeds the input limit.</exception>
    public static SemanticVersion Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!TryParse(text, out SemanticVersion? version))
        {
            throw new FormatException($"Expected a complete SemVer 2.0 version of at most {MaximumTextLength} ASCII characters.");
        }
        return version;
    }

    /// <summary>Parses strict SemVer text without throwing for null, malformed or over-limit input.</summary>
    /// <param name="text">The complete input; no trimming, case conversion or Unicode normalization occurs.</param>
    /// <param name="version">The immutable result on success; null on every parsing failure.</param>
    /// <returns>True only for grammar-valid input within <see cref="MaximumTextLength"/>.</returns>
    /// <remarks>Allocation failures are not converted to false. Parsing takes linear time in the bounded input length.</remarks>
    public static bool TryParse(string? text, [NotNullWhen(true)] out SemanticVersion? version)
    {
        version = null;
        if (text is null || text.Length < 5 || text.Length > MaximumTextLength)
        {
            return false;
        }
        int position = 0;
        if (!ReadNumber(text, ref position, out string major) || !Consume(text, ref position, '.') ||
            !ReadNumber(text, ref position, out string minor) || !Consume(text, ref position, '.') ||
            !ReadNumber(text, ref position, out string patch))
        {
            return false;
        }
        string? prerelease = null;
        string? buildMetadata = null;
        if (Consume(text, ref position, '-'))
        {
            if (!ReadIdentifiers(text, ref position, true, out prerelease))
            {
                return false;
            }
        }
        if (Consume(text, ref position, '+'))
        {
            if (!ReadIdentifiers(text, ref position, false, out buildMetadata))
            {
                return false;
            }
        }
        if (position != text.Length)
        {
            return false;
        }
        version = new SemanticVersion(text, major, minor, patch, prerelease, buildMetadata);
        return true;
    }

    /// <summary>Compares SemVer precedence, ignoring build metadata; every version is greater than null.</summary>
    /// <param name="other">The version to compare, or null.</param>
    /// <returns>A negative, zero or positive value for lower, equal or higher precedence.</returns>
    public int CompareTo(SemanticVersion? other)
    {
        if (other is null)
        {
            return 1;
        }
        int comparison = CompareNumber(Major, other.Major);
        if (comparison == 0) { comparison = CompareNumber(Minor, other.Minor); }
        if (comparison == 0) { comparison = CompareNumber(Patch, other.Patch); }
        if (comparison != 0)
        {
            return comparison;
        }
        if (prereleaseIdentifiers.Length == 0 || other.prereleaseIdentifiers.Length == 0)
        {
            return (other.prereleaseIdentifiers.Length > 0 ? 1 : 0) - (prereleaseIdentifiers.Length > 0 ? 1 : 0);
        }
        int count = Math.Min(prereleaseIdentifiers.Length, other.prereleaseIdentifiers.Length);
        for (int index = 0; index < count; index++)
        {
            string left = prereleaseIdentifiers[index];
            string right = other.prereleaseIdentifiers[index];
            bool leftNumeric = IsNumeric(left);
            bool rightNumeric = IsNumeric(right);
            comparison = leftNumeric && rightNumeric ? CompareNumber(left, right)
                : leftNumeric != rightNumeric ? (leftNumeric ? -1 : 1)
                : string.CompareOrdinal(left, right);
            if (comparison != 0)
            {
                return comparison;
            }
        }
        return prereleaseIdentifiers.Length.CompareTo(other.prereleaseIdentifiers.Length);
    }

    int IComparable.CompareTo(object? obj) => obj is null ? 1 : obj is SemanticVersion version
        ? CompareTo(version) : throw new ArgumentException("Expected a SemanticVersion.", nameof(obj));

    /// <summary>Compares precedence with null ordered before every version; two nulls compare equally.</summary>
    public static int Compare(SemanticVersion? left, SemanticVersion? right) =>
        left is null ? (right is null ? 0 : -1) : left.CompareTo(right);

    /// <summary>Tests equal precedence, ignoring build metadata. Null is never equal to this instance.</summary>
    public bool Equals(SemanticVersion? other) => other is not null &&
        string.Equals(precedenceText, other.precedenceText, StringComparison.Ordinal);

    /// <summary>Tests equal precedence when the object is a SemanticVersion; otherwise returns false.</summary>
    public override bool Equals(object? obj) => obj is SemanticVersion version && Equals(version);

    /// <summary>Returns an in-process ordinal hash of the version excluding build metadata.</summary>
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(precedenceText);

    /// <summary>Tests exact ordinal identity, including case and build metadata. Null returns false.</summary>
    public bool ExactEquals(SemanticVersion? other) => other is not null && string.Equals(text, other.text, StringComparison.Ordinal);

    /// <summary>Returns the exact accepted version text, including build metadata.</summary>
    public override string ToString() => text;

    /// <summary>Tests equal precedence; two null references are equal.</summary>
    public static bool operator ==(SemanticVersion? left, SemanticVersion? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Tests unequal precedence, including null handling.</summary>
    public static bool operator !=(SemanticVersion? left, SemanticVersion? right) => !(left == right);

    /// <summary>Tests lower precedence with null ordered before every version.</summary>
    public static bool operator <(SemanticVersion? left, SemanticVersion? right) => Compare(left, right) < 0;

    /// <summary>Tests higher precedence with null ordered before every version.</summary>
    public static bool operator >(SemanticVersion? left, SemanticVersion? right) => Compare(left, right) > 0;

    /// <summary>Tests lower or equal precedence with null ordered before every version.</summary>
    public static bool operator <=(SemanticVersion? left, SemanticVersion? right) => Compare(left, right) <= 0;

    /// <summary>Tests higher or equal precedence with null ordered before every version.</summary>
    public static bool operator >=(SemanticVersion? left, SemanticVersion? right) => Compare(left, right) >= 0;

    private static bool ReadNumber(string text, ref int position, out string number)
    {
        int start = position;
        while (position < text.Length && char.IsAsciiDigit(text[position])) { position++; }
        number = text[start..position];
        return number.Length > 0 && (number.Length == 1 || number[0] != '0');
    }

    private static bool Consume(string text, ref int position, char expected)
    {
        if (position == text.Length || text[position] != expected) { return false; }
        position++;
        return true;
    }

    private static bool ReadIdentifiers(string text, ref int position, bool prerelease, out string? identifiers)
    {
        identifiers = null;
        int start = position;
        while (true)
        {
            int identifierStart = position;
            bool numeric = true;
            while (position < text.Length && (char.IsAsciiLetterOrDigit(text[position]) || text[position] == '-'))
            {
                numeric &= char.IsAsciiDigit(text[position]);
                position++;
            }
            int length = position - identifierStart;
            if (length == 0 || (prerelease && numeric && length > 1 && text[identifierStart] == '0'))
            {
                return false;
            }
            if (!Consume(text, ref position, '.')) { break; }
        }
        identifiers = text[start..position];
        return true;
    }

    private static bool IsNumeric(string text)
    {
        foreach (char character in text)
        {
            if (!char.IsAsciiDigit(character)) { return false; }
        }
        return true;
    }

    private static int CompareNumber(string left, string right) => left.Length != right.Length
        ? left.Length.CompareTo(right.Length) : string.CompareOrdinal(left, right);

    private sealed class ExactIdentityComparer : IEqualityComparer<SemanticVersion>
    {
        public bool Equals(SemanticVersion? left, SemanticVersion? right) =>
            left is null ? right is null : left.ExactEquals(right);

        public int GetHashCode(SemanticVersion value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return StringComparer.Ordinal.GetHashCode(value.text);
        }
    }
}
