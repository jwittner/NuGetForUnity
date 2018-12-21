using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NugetForUnity
{
    public static class VersionRange
    {

        /// <summary>
        /// Gets a value indicating whether the minimum version number (only valid when HasVersionRange is true) is inclusive (true) or exclusive (false).
        /// </summary>
        public static bool IsMinInclusive(string range) { return range.StartsWith("["); }

        /// <summary>
        /// Gets a value indicating whether the maximum version number (only valid when HasVersionRange is true) is inclusive (true) or exclusive (false).
        /// </summary>
        public static bool IsMaxInclusive(string range) { return range.EndsWith("]"); }

        /// <summary>
        /// Gets the minimum version number of the NuGet package. Only valid when HasVersionRange is true.
        /// </summary>
        public static string MinimumVersion(string range) { return range.TrimStart(new[] { '[', '(' }).TrimEnd(new[] { ']', ')' }).Split(new[] { ',' })[0].Trim(); }

        /// <summary>
        /// Gets the maximum version number of the NuGet package. Only valid when HasVersionRange is true.
        /// </summary>
        public static string MaximumVersion(string range)
        {
            // if there is no MaxVersion specified, but the Max is Inclusive, then it is an EXACT version match with the stored MINIMUM
            string[] minMax = range.TrimStart(new[] { '[', '(' }).TrimEnd(new[] { ']', ')' }).Split(new[] { ',' });
            return minMax.Length == 2 ? minMax[1].Trim() : null;
        }

        /// <summary>
        /// Determines if the given version is in the version range.
        /// See here: https://docs.nuget.org/ndocs/create-packages/dependency-versions
        /// </summary>
        /// <param name="version">The version to check if is in the range.</param>
        /// <returns>True if the given version is in the range, otherwise false.</returns>
        public static int CompareVersion(string version, string range)
        {
            if (!string.IsNullOrEmpty(MinimumVersion(range)))
            {
                int compare = Version.Compare(MinimumVersion(range), version);
                // -1 = Min < other <-- Inclusive & Exclusive
                //  0 = Min = other <-- Inclusive Only
                // +1 = Min > other <-- OUT OF RANGE

                if (IsMinInclusive(range))
                {
                    if (compare > 0)
                    {
                        return -1;
                    }
                }
                else
                {
                    if (compare >= 0)
                    {
                        return -1;
                    }
                }
            }

            if (!string.IsNullOrEmpty(MaximumVersion(range)))
            {
                int compare = Version.Compare(MaximumVersion(range), version);
                // -1 = Max < other <-- OUT OF RANGE
                //  0 = Max = other <-- Inclusive Only
                // +1 = Max > other <-- Inclusive & Exclusive

                if (IsMaxInclusive(range))
                {
                    if (compare < 0)
                    {
                        return 1;
                    }
                }
                else
                {
                    if (compare <= 0)
                    {
                        return 1;
                    }
                }
            }
            else
            {
                if (IsMaxInclusive(range))
                {
                    // if there is no MaxVersion specified, but the Max is Inclusive, then it is an EXACT version match with the stored MINIMUM
                    return Version.Compare(MinimumVersion(range), version);
                }
            }

            return 0;
        }
    }

}