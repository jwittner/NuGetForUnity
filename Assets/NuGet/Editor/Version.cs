namespace NugetForUnity
{
    public static class Version
    {
        /// <summary>
        /// Compares two version numbers in the form "1.2". Also supports an optional 3rd and 4th number as well as a prerelease tag, such as "1.3.0.1-alpha2".
        /// Returns:
        /// -1 if versionA is less than versionB
        ///  0 if versionA is equal to versionB
        /// +1 if versionA is greater than versionB
        /// </summary>
        /// <param name="versionA">The first version number to compare.</param>
        /// <param name="versionB">The second version number to compare.</param>
        /// <returns>-1 if versionA is less than versionB. 0 if versionA is equal to versionB. +1 if versionA is greater than versionB</returns>
        public static int Compare(string versionA, string versionB)
        {
            try
            {
                string[] splitStringsA = versionA.Split('-');
                versionA = splitStringsA[0];
                string prereleaseA = string.Empty;

                if (splitStringsA.Length > 1)
                {
                    prereleaseA = splitStringsA[1];
                    for (int i = 2; i < splitStringsA.Length; i++)
                    {
                        prereleaseA += "-" + splitStringsA[i];
                    }
                }

                string[] splitA = versionA.Split('.');
                int majorA = int.Parse(splitA[0]);
                int minorA = int.Parse(splitA[1]);
                int patchA = 0;
                if (splitA.Length >= 3)
                {
                    patchA = int.Parse(splitA[2]);
                }
                int buildA = 0;
                if (splitA.Length >= 4)
                {
                    buildA = int.Parse(splitA[3]);
                }

                string[] splitStringsB = versionB.Split('-');
                versionB = splitStringsB[0];
                string prereleaseB = string.Empty;

                if (splitStringsB.Length > 1)
                {
                    prereleaseB = splitStringsB[1];
                    for (int i = 2; i < splitStringsB.Length; i++)
                    {
                        prereleaseB += "-" + splitStringsB[i];
                    }
                }

                string[] splitB = versionB.Split('.');
                int majorB = int.Parse(splitB[0]);
                int minorB = int.Parse(splitB[1]);
                int patchB = 0;
                if (splitB.Length >= 3)
                {
                    patchB = int.Parse(splitB[2]);
                }
                int buildB = 0;
                if (splitB.Length >= 4)
                {
                    buildB = int.Parse(splitB[3]);
                }

                int major = majorA < majorB ? -1 : majorA > majorB ? 1 : 0;
                int minor = minorA < minorB ? -1 : minorA > minorB ? 1 : 0;
                int patch = patchA < patchB ? -1 : patchA > patchB ? 1 : 0;
                int build = buildA < buildB ? -1 : buildA > buildB ? 1 : 0;
                int prerelease = string.Compare(prereleaseA, prereleaseB);

                if (major == 0)
                {
                    // if major versions are equal, compare minor versions
                    if (minor == 0)
                    {
                        if (patch == 0)
                        {
                            // if patch versions are equal, compare build versions
                            if (build == 0)
                            {
                                // if the build versions are equal, just return the prerelease version comparison
                                return prerelease;
                            }

                            // the build versions are different, so use them
                            return build;
                        }

                        // the patch versions are different, so use them
                        return patch;
                    }

                    // the minor versions are different, so use them
                    return minor;
                }

                // the major versions are different, so use them
                return major;
            }
            catch (System.Exception)
            {
                UnityEngine.Debug.LogErrorFormat("Compare Error: {0} {1}", versionA, versionB);
                return -1;
            }
        }
    }

}