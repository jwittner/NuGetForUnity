using System;

namespace NugetForUnity
{
    /// <summary>
    /// Represents an identifier for a NuGet package.  It contains only an ID and a Version number.
    /// </summary>
    [Serializable]
    public class NugetPackageIdentifier : IEquatable<NugetPackageIdentifier>, IComparable<NugetPackage>
    {
        /// <summary>
        /// Gets or sets the ID of the NuGet package.
        /// </summary>
        public string Id;

        /// <summary>
        /// Gets or sets the version number of the NuGet package.
        /// </summary>
        public string Version;

        /// <summary>
        /// Gets a value indicating whether this is a prerelease package or an official release package.
        /// </summary>
        public bool IsPrerelease { get { return Version.Contains("-"); } }
       

        /// <summary>
        /// Initializes a new instance of a <see cref="NugetPackageIdentifider"/> with empty ID and Version.
        /// </summary>
        public NugetPackageIdentifier()
        {
            Id = string.Empty;
            Version = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="NugetPackageIdentifider"/> with the given ID and Version.
        /// </summary>
        /// <param name="id">The ID of the package.</param>
        /// <param name="version">The version number of the package.</param>
        public NugetPackageIdentifier(string id, string version)
        {
            Id = id;
            Version = version;
        }

        /// <summary>
        /// Checks to see if this <see cref="NugetPackageIdentifier"/> is equal to the given one.
        /// </summary>
        /// <param name="other">The other <see cref="NugetPackageIdentifier"/> to check equality with.</param>
        /// <returns>True if the package identifiers are equal, otherwise false.</returns>
        public bool Equals(NugetPackageIdentifier other)
        {
            return other != null && other.Id == Id && other.Version == Version;
        }

        /// <summary>
        /// Checks to see if the first <see cref="NugetPackageIdentifier"/> is less than the second.
        /// </summary>
        /// <param name="first">The first to compare.</param>
        /// <param name="second">The second to compare.</param>
        /// <returns>True if the first is less than the second.</returns>
        public static bool operator <(NugetPackageIdentifier first, NugetPackageIdentifier second)
        {
            if (first.Id != second.Id)
            {
                return string.Compare(first.Id, second.Id) < 0;
            }

            return NugetForUnity.Version.Compare(first.Version, second.Version) < 0;
        }

        /// <summary>
        /// Checks to see if the first <see cref="NugetPackageIdentifier"/> is greater than the second.
        /// </summary>
        /// <param name="first">The first to compare.</param>
        /// <param name="second">The second to compare.</param>
        /// <returns>True if the first is greater than the second.</returns>
        public static bool operator >(NugetPackageIdentifier first, NugetPackageIdentifier second)
        {
            if (first.Id != second.Id)
            {
                return string.Compare(first.Id, second.Id) > 0;
            }

            return NugetForUnity.Version.Compare(first.Version, second.Version) > 0;
        }

        /// <summary>
        /// Checks to see if the first <see cref="NugetPackageIdentifier"/> is less than or equal to the second.
        /// </summary>
        /// <param name="first">The first to compare.</param>
        /// <param name="second">The second to compare.</param>
        /// <returns>True if the first is less than or equal to the second.</returns>
        public static bool operator <=(NugetPackageIdentifier first, NugetPackageIdentifier second)
        {
            if (first.Id != second.Id)
            {
                return string.Compare(first.Id, second.Id) <= 0;
            }

            return NugetForUnity.Version.Compare(first.Version, second.Version) <= 0;
        }

        /// <summary>
        /// Checks to see if the first <see cref="NugetPackageIdentifier"/> is greater than or equal to the second.
        /// </summary>
        /// <param name="first">The first to compare.</param>
        /// <param name="second">The second to compare.</param>
        /// <returns>True if the first is greater than or equal to the second.</returns>
        public static bool operator >=(NugetPackageIdentifier first, NugetPackageIdentifier second)
        {
            if (first.Id != second.Id)
            {
                return string.Compare(first.Id, second.Id) >= 0;
            }

            return NugetForUnity.Version.Compare(first.Version, second.Version) >= 0;
        }

        /// <summary>
        /// Checks to see if the first <see cref="NugetPackageIdentifier"/> is equal to the second.
        /// They are equal if the Id and the Version match.
        /// </summary>
        /// <param name="first">The first to compare.</param>
        /// <param name="second">The second to compare.</param>
        /// <returns>True if the first is equal to the second.</returns>
        public static bool operator ==(NugetPackageIdentifier first, NugetPackageIdentifier second)
        {
            if (ReferenceEquals(first, null))
            {
                return ReferenceEquals(second, null);
            }

            return first.Equals(second);
        }

        /// <summary>
        /// Checks to see if the first <see cref="NugetPackageIdentifier"/> is not equal to the second.
        /// They are not equal if the Id or the Version differ.
        /// </summary>
        /// <param name="first">The first to compare.</param>
        /// <param name="second">The second to compare.</param>
        /// <returns>True if the first is not equal to the second.</returns>
        public static bool operator !=(NugetPackageIdentifier first, NugetPackageIdentifier second)
        {
            if (ReferenceEquals(first, null))
            {
                return !ReferenceEquals(second, null);
            }

            return !first.Equals(second);
        }

        /// <summary>
        /// Determines if a given object is equal to this <see cref="NugetPackageIdentifier"/>.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if the given object is equal to this <see cref="NugetPackageIdentifier"/>, otherwise false.</returns>
        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to NugetPackageIdentifier return false.
            NugetPackageIdentifier p = obj as NugetPackageIdentifier;
            if ((object)p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (Id == p.Id) && (Version == p.Version);
        }

        /// <summary>
        /// Gets the hashcode for this <see cref="NugetPackageIdentifier"/>.
        /// </summary>
        /// <returns>The hashcode for this instance.</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ Version.GetHashCode();
        }

        /// <summary>
        /// Returns the string representation of this <see cref="NugetPackageIdentifer"/> in the form "{ID}.{Version}".
        /// </summary>
        /// <returns>A string in the form "{ID}.{Version}".</returns>
        public override string ToString()
        {
            return string.Format("{0}.{1}", Id, Version);
        }

        /// <summary>
        /// Compares the given version string with the version of this <see cref="NugetPackageIdentifier"/>.
        /// See here: https://docs.nuget.org/ndocs/create-packages/dependency-versions
        /// </summary>
        /// <param name="otherVersion">The version to check if is in the range.</param>
        /// <returns>-1 if otherVersion is less than the version range. 0 if otherVersion is inside the version range. +1 if otherVersion is greater than the version range.</returns>
        public int CompareVersion(string otherVersion)
        {
            // if it has no version range specified (ie only a single version number) NuGet's specs state that that is the minimum version number, inclusive
            int compare = NugetForUnity.Version.Compare(Version, otherVersion);
            return compare <= 0 ? 0 : compare;
        }

        public int CompareTo(NugetPackage other)
        {
            if (this.Id != other.Id)
            {
                return string.Compare(this.Id, other.Id);
            }

            return NugetForUnity.Version.Compare(this.Version, other.Version);
        }
    }
}