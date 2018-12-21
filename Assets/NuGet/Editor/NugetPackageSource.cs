namespace NugetForUnity
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Xml;
    using System.Xml.Linq;
    using Debug = UnityEngine.Debug;

    /// <summary>
    /// Represents a NuGet Package Source (a "server").
    /// </summary>
    [Serializable]
    public class NugetPackageSource
    {
        /// <summary>
        /// Gets or sets the name of the package source.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the path of the package source.
        /// </summary>
        public string SavedPath { get; set; }

        /// <summary>
        /// Gets path, with the values of environment variables expanded.
        /// </summary>
        public string ExpandedPath
        {
            get
            {
                return Environment.ExpandEnvironmentVariables(SavedPath);
            }
        }

        /// <summary>
        /// Gets or sets the password used to access the feed. Null indicates that no password is used.
        /// </summary>
        public string SavedPassword { get; set; }

        /// <summary>
        /// Gets password, with the values of environment variables expanded.
        /// </summary>
        public string ExpandedPassword
        {
            get
            {
                return SavedPassword != null ? Environment.ExpandEnvironmentVariables(SavedPassword) : null;
            }
        }

        public bool HasPassword
        {
            get { return SavedPassword != null; }

            set
            {
                if (value)
                {
                    if (SavedPassword == null)
                    {
                        SavedPassword = string.Empty; // Initialize newly-enabled password to empty string.
                    }
                }
                else
                {
                    SavedPassword = null; // Clear password to null when disabled.
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicated whether the path is a local path or a remote path.
        /// </summary>
        public bool IsLocalPath { get; private set; }

        /// <summary>
        /// Gets or sets a value indicated whether this source is enabled or not.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NugetPackageSource"/> class.
        /// </summary>
        /// <param name="name">The name of the package source.</param>
        /// <param name="path">The path to the package source.</param>
        public NugetPackageSource(string name, string path)
        {
            Name = name;
            SavedPath = path;
            IsLocalPath = !ExpandedPath.StartsWith("http");
            IsEnabled = true;
        }

        public NugetPackage FindPackageById(NugetPackageIdentifier package)
        {
            if (IsLocalPath)
            {
                string localPackagePath = System.IO.Path.Combine(ExpandedPath, string.Format("./{0}.{1}.nupkg", package.Id, package.Version));
                if (File.Exists(localPackagePath))
                {
                    return NugetPackage.FromNupkgFile(localPackagePath);
                }

                return null;
            }
            else
            {
                // See here: http://www.odata.org/documentation/odata-version-2-0/uri-conventions/
                string url = string.Empty;

                url = string.Format("{0}FindPackagesById()?$id='{1}'&$filter=Version eq {2}", ExpandedPath, package.Id, package.Version);

                try
                {
                    var foundPackages = GetPackagesFromUrl(url, ExpandedPassword);
                    return foundPackages.FirstOrDefault();
                }
                catch (System.Exception e)
                {
                    Debug.LogErrorFormat("Unable to retrieve packages from {0}\n{1}", url, e.ToString());
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets a NugetPackage from the NuGet server that is in the range given.
        /// </summary>
        /// <param name="package">The <see cref="NugetPackageIdentifier"/> containing the ID and Version of the package to get.</param>
        /// <returns>The retrieved package, if there is one.  Null if no matching package was found.</returns>
        public IEnumerable<NugetPackage> FindPackagesById(string id, string versionRange, bool includePrerelease)
        {
            List<NugetPackage> foundPackages = null;

            if (IsLocalPath)
            {
                // TODO: Sort the local packages?  Currently assuming they are in alphabetical order due to the filesystem.
                // TODO: Optimize to no longer use GetLocalPackages, since that loads the .nupkg itself

                // Try to find later versions of the same package
                var packages = FindLocalPackages(id, true, includePrerelease);
                foundPackages = new List<NugetPackage>(packages.SkipWhile(x => !package.InRange(x)));
            }
            else
            {
                // See here: http://www.odata.org/documentation/odata-version-2-0/uri-conventions/
                string url = string.Empty;

                // We used to rely on expressions such as &$filter=Version ge '9.0.1' to find versions in a range, but the results were sorted alphabetically. This
                // caused version 10.0.0 to be less than version 9.0.0. In order to work around this issue, we'll request all versions and perform filtering ourselves.

                url = string.Format("{0}FindPackagesById()?$orderby=Version asc&id='{1}'", ExpandedPath, package.Id);

                try
                {
                    foundPackages = GetPackagesFromUrl(url, ExpandedPassword);
                }
                catch (System.Exception e)
                {
                    foundPackages = new List<NugetPackage>();
                    Debug.LogErrorFormat("Unable to retrieve package list from {0}\n{1}", url, e.ToString());
                }

                foundPackages.Sort();
                if (foundPackages.Exists(p => package.InRange(p)))
                {
                    // Return all the packages in the range of versions specified by 'package'.
                    foundPackages.RemoveAll(p => !package.InRange(p));
                }
                else
                {
                    // There are no packages in the range of versions specified by 'package'.
                    // Return the most recent version after the version specified by 'package'.
                    foundPackages.RemoveAll(p => package.CompareVersion(p.Version) < 0);
                    if (foundPackages.Count > 0)
                    {
                        foundPackages.RemoveRange(1, foundPackages.Count - 1);
                    }
                }
            }

            if (foundPackages != null)
            {
                foreach (NugetPackage foundPackage in foundPackages)
                {
                    foundPackage.PackageSource = this;
                }
            }

            return foundPackages;
        }

        /// <summary>
        /// Gets a list of NuGetPackages from this package source.
        /// This allows searching for partial IDs or even the empty string (the default) to list ALL packages.
        /// 
        /// NOTE: See the functions and parameters defined here: https://www.nuget.org/api/v2/$metadata
        /// </summary>
        /// <param name="searchTerm">The search term to use to filter packages. Defaults to the empty string.</param>
        /// <param name="includeAllVersions">True to include older versions that are not the latest version.</param>
        /// <param name="includePrerelease">True to include prerelease packages (alpha, beta, etc).</param>
        /// <param name="numberToGet">The number of packages to fetch.</param>
        /// <param name="numberToSkip">The number of packages to skip before fetching.</param>
        /// <returns>The list of available packages.</returns>
        public List<NugetPackage> Search(string searchTerm = "", bool includeAllVersions = false, bool includePrerelease = false, int numberToGet = 15, int numberToSkip = 0)
        {
            if (IsLocalPath)
            {
                return GetLocalPackages(searchTerm, includeAllVersions, includePrerelease, numberToGet, numberToSkip);
            }

            //Example URL: "http://www.nuget.org/api/v2/Search()?$filter=IsLatestVersion&$orderby=Id&$skip=0&$top=30&searchTerm='newtonsoft'&targetFramework=''&includePrerelease=false";

            string url = ExpandedPath;

            // call the search method
            url += "Search()?";

            // filter results
            if (!includeAllVersions)
            {
                if (!includePrerelease)
                {
                    url += "$filter=IsLatestVersion&";
                }
                else
                {
                    url += "$filter=IsAbsoluteLatestVersion&";
                }
            }

            // order results
            //url += "$orderby=Id&";
            //url += "$orderby=LastUpdated&";
            url += "$orderby=DownloadCount desc&";

            // skip a certain number of entries
            url += string.Format("$skip={0}&", numberToSkip);

            // show a certain number of entries
            url += string.Format("$top={0}&", numberToGet);

            // apply the search term
            url += string.Format("searchTerm='{0}'&", searchTerm);

            // apply the target framework filters
            url += "targetFramework=''&";

            // should we include prerelease packages?
            url += string.Format("includePrerelease={0}", includePrerelease.ToString().ToLower());

            try
            {
                return GetPackagesFromUrl(url, ExpandedPassword);
            }
            catch (System.Exception e)
            {
                Debug.LogErrorFormat("Unable to retrieve package list from {0}\n{1}", url, e.ToString());
                return new List<NugetPackage>();
            }
        }

        /// <summary>
        /// Gets a list of all available packages from a local source (not a web server) that match the given filters.
        /// </summary>
        /// <param name="searchTerm">The search term to use to filter packages. Defaults to the empty string.</param>
        /// <param name="includeAllVersions">True to include older versions that are not the latest version.</param>
        /// <param name="includePrerelease">True to include prerelease packages (alpha, beta, etc).</param>
        /// <param name="numberToGet">The number of packages to fetch.</param>
        /// <param name="numberToSkip">The number of packages to skip before fetching.</param>
        /// <returns>The list of available packages.</returns>
        private IEnumerable<NugetPackage> FindLocalPackages(string searchTerm = "", bool includeAllVersions = false, bool includePrerelease = false)
        {
            string path = ExpandedPath;

            if (!Directory.Exists(path))
            {
                Debug.LogErrorFormat("Local folder not found: {0}", path);
                return Enumerable.Empty<NugetPackage>();
            }

            string[] packagePaths = Directory.GetFiles(path, string.Format("{0}.*.nupkg", searchTerm));

            IEnumerable<NugetPackage> packages = packagePaths.Select(p =>
            {
                var package = NugetPackage.FromNupkgFile(p);
                package.PackageSource = this;
                return package;

            }).ToList();

            if (!includePrerelease)
            {
                packages = packages.Where(p => !p.IsPrerelease);
            }

            if (!includeAllVersions)
            {
                packages = packages.GroupBy(p => p.Id).Select(g => g.OrderByDescending(p => p).First());
            }

            packages = packages.OrderByDescending(p => p);

            return packages;
        }

        /// <summary>
        /// Builds a list of NugetPackages from the XML returned from the HTTP GET request issued at the given URL.
        /// Note that NuGet uses an Atom-feed (XML Syndicaton) superset called OData.
        /// See here http://www.odata.org/documentation/odata-version-2-0/uri-conventions/
        /// </summary>
        /// <param name="url"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private IEnumerable<NugetPackage> GetPackagesFromUrl(string url, string password)
        {
            NugetHelper.LogVerbose("Getting packages from: {0}", url);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Mono doesn't have a Certificate Authority, so we have to provide all validation manually.  Currently just accept anything.
            // See here: http://stackoverflow.com/questions/4926676/mono-webrequest-fails-with-https

            // remove all handlers
            ServicePointManager.ServerCertificateValidationCallback = null;

            // add anonymous handler
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, policyErrors) => true;

            Stream responseStream = NugetHelper.RequestUrl(url, password, timeOut: 5000);
            StreamReader streamReader = new StreamReader(responseStream);
            var document = XDocument.Load(streamReader);

            stopwatch.Stop();
            NugetHelper.LogVerbose("Retreived packages in {1} ms", stopwatch.ElapsedMilliseconds);

            foreach (var package in NugetODataResponse.Parse(document))
            {
                package.PackageSource = this;
                yield return package;
            }
        }

        private IEnumerable<NugetPackage> GetLocalUpdates(IEnumerable<PackagesConfigFile.Package> toUpdate, bool includePrerelease = false, bool includeAllVersions = false)
        {
            foreach (var package in toUpdate)
            {
                var availablePackages = FindLocalPackages(package.Id, includeAllVersions, includePrerelease)
                    .Where(p => Version.Compare(p.Version, package.Version) > 0);

                foreach (var newPackage in availablePackages)
                {
                    yield return newPackage;
                }
            }
        }


        private IEnumerable<NugetPackage> GetOnlineUpdates(IEnumerable<PackagesConfigFile.Package> toUpdate, bool includePrerelease = false, bool includeAllVersions = false)
        {
            IEnumerable<NugetPackage> packages = Enumerable.Empty<NugetPackage>();

            // check for updates in groups of 10 instead of all of them, since that causes servers to throw errors for queries that are too long
            while (toUpdate.Any())
            {
                var packageGroup = toUpdate.Take(10);
                toUpdate = toUpdate.Skip(10);

                string packageIds = string.Join("|", packageGroup.Select(p => p.Id));
                string versions = string.Join("|", packageGroup.Select(p => p.Version));
                string targetFrameworks = string.Join("|", packageGroup.Select(p => p.TargetFramework));
                string allowedVersions = string.Join("|", packageGroup.Select(p => p.AllowedVersions));

                string url = string.Format("{0}GetUpdates()?packageIds='{1}'&versions='{2}'&includePrerelease={3}&includeAllVersions={4}&targetFrameworks='{5}'&versionConstraints='{6}'", ExpandedPath, packageIds, versions, includePrerelease.ToString().ToLower(), includeAllVersions.ToString().ToLower(), targetFrameworks, allowedVersions);


                try
                {
                    packages = packages.Concat(GetPackagesFromUrl(url, ExpandedPassword));
                }
                catch (System.Exception e)
                {
                    WebException webException = e as WebException;
                    HttpWebResponse webResponse = webException != null ? webException.Response as HttpWebResponse : null;
                    if (webResponse != null && webResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        // Some web services, such as VSTS don't support the GetUpdates API. Attempt to retrieve updates via FindPackagesById.
                        NugetHelper.LogVerbose("{0} not found. Falling back to FindPackagesById.", url);
                        packages.Concat(GetOnlineUpdatesFallback(packageGroup, includePrerelease, includeAllVersions));
                    }

                    Debug.LogErrorFormat("Unable to retrieve package list from {0}\n{1}", url, e.ToString());
                }
            }


            return packages.OrderByDescending(p => p);
        }


        private IEnumerable<NugetPackage> GetOnlineUpdatesFallback(IEnumerable<PackagesConfigFile.Package> toUpdate, bool includePrerelease = false, bool includeAllVersions = false)
        {
            foreach (var package in toUpdate)
            {
                string versionRange = string.Format("({0},{1}", package.Version); // Minimum of Current ID (exclusive) with no maximum (exclusive).
                //NugetPackageIdentifier id = new NugetPackageIdentifier(installedPackage.Id, versionRange); 
                packageUpdates = FindPackagesById(id, versionRange, includePrerelease);

                NugetPackage mostRecentPrerelease = includePrerelease ? packageUpdates.FindLast(p => p.IsPrerelease) : default(NugetPackage);
                packageUpdates.RemoveAll(p => p.IsPrerelease && p != mostRecentPrerelease);

                if (!includeAllVersions && packageUpdates.Count > 0)
                {
                    packageUpdates.RemoveRange(0, packageUpdates.Count - 1);
                }

                updates.AddRange(packageUpdates);
            }

            return updates;
        }

        /// <summary>
        /// Queries the source with the given list of installed packages to get any updates that are available.
        /// </summary>
        /// <param name="toUpdate">The list of currently installed packages.</param>
        /// <param name="includePrerelease">True to include prerelease packages (alpha, beta, etc).</param>
        /// <param name="includeAllVersions">True to include older versions that are not the latest version.</param>
        /// <param name="targetFrameworks">The specific frameworks to target?</param>
        /// <param name="versionRange">The version constraints?</param>
        /// <returns>A list of all updates available.</returns>
        public IEnumerable<NugetPackage> GetUpdates(IEnumerable<PackagesConfigFile.Package> toUpdate, bool includePrerelease = false, bool includeAllVersions = false)
        {
            if (IsLocalPath)
            {
                return GetLocalUpdates(toUpdate, includePrerelease, includeAllVersions);
            }

            // check for updates in groups of 10 instead of all of them, since that causes servers to throw errors for queries that are too long
            for (int i = 0; i < toUpdate.Count(); i += 10)
            {
                var packageGroup = toUpdate.Skip(i).Take(10);

                string packageIds = string.Join("|", packageGroup.Select(p => p.Id));
                string versions = string.Join("|", packageGroup.Select(p => p.Version));
                string targetFrameworks = string.Join("|", packageGroup.Select(p => p.TargetFramework));
                string allowedVersions = string.Join("|", packageGroup.Select(p => p.AllowedVersions));

                string url = string.Format("{0}GetUpdates()?packageIds='{1}'&versions='{2}'&includePrerelease={3}&includeAllVersions={4}&targetFrameworks='{5}'&versionConstraints='{6}'", ExpandedPath, packageIds, versions, includePrerelease.ToString().ToLower(), includeAllVersions.ToString().ToLower(), targetFrameworks, allowedVersions);

                try
                {
                    foreach (var package in GetPackagesFromUrl(url, ExpandedPassword))
                    {
                        yield return pac
                    }
                }
                catch (System.Exception e)
                {
                    WebException webException = e as WebException;
                    HttpWebResponse webResponse = webException != null ? webException.Response as HttpWebResponse : null;
                    if (webResponse != null && webResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        // Some web services, such as VSTS don't support the GetUpdates API. Attempt to retrieve updates via FindPackagesById.
                        NugetHelper.LogVerbose("{0} not found. Falling back to FindPackagesById.", url);
                        return GetUpdatesFallback(toUpdate, includePrerelease, includeAllVersions, targetFrameworks, versionRange);
                    }

                    Debug.LogErrorFormat("Unable to retrieve package list from {0}\n{1}", url, e.ToString());
                }
            }

            // sort alphabetically
            updates.Sort(delegate (NugetPackage x, NugetPackage y)
            {
                if (x.Id == null && y.Id == null)
                    return 0;
                else if (x.Id == null)
                    return -1;
                else if (y.Id == null)
                    return 1;
                else if (x.Id == y.Id)
                    return x.Version.CompareTo(y.Version);
                else
                    return x.Id.CompareTo(y.Id);
            });

#if TEST_GET_UPDATES_FALLBACK
            // Enable this define in order to test that GetUpdatesFallback is working as intended. This tests that it returns the same set of packages
            // that are returned by the GetUpdates API. Since GetUpdates isn't available when using a Visual Studio Team Services feed, the intention
            // is that this test would be conducted by using nuget.org's feed where both paths can be compared.
            List<NugetPackage> updatesReplacement = GetUpdatesFallback(installedPackages, includePrerelease, includeAllVersions, targetFrameworks, versionContraints);
            ComparePackageLists(updates, updatesReplacement, "GetUpdatesFallback doesn't match GetUpdates API");
#endif

            return updates;
        }

        private static void ComparePackageLists(List<NugetPackage> updates, List<NugetPackage> updatesReplacement, string errorMessageToDisplayIfListsDoNotMatch)
        {
            System.Text.StringBuilder matchingComparison = new System.Text.StringBuilder();
            System.Text.StringBuilder missingComparison = new System.Text.StringBuilder();
            foreach (NugetPackage package in updates)
            {
                if (updatesReplacement.Contains(package))
                {
                    matchingComparison.Append(matchingComparison.Length == 0 ? "Matching: " : ", ");
                    matchingComparison.Append(package.ToString());
                }
                else
                {
                    missingComparison.Append(missingComparison.Length == 0 ? "Missing: " : ", ");
                    missingComparison.Append(package.ToString());
                }
            }
            System.Text.StringBuilder extraComparison = new System.Text.StringBuilder();
            foreach (NugetPackage package in updatesReplacement)
            {
                if (!updates.Contains(package))
                {
                    extraComparison.Append(extraComparison.Length == 0 ? "Extra: " : ", ");
                    extraComparison.Append(package.ToString());
                }
            }
            if (missingComparison.Length > 0 || extraComparison.Length > 0)
            {
                Debug.LogWarningFormat("{0}\n{1}\n{2}\n{3}", errorMessageToDisplayIfListsDoNotMatch, matchingComparison, missingComparison, extraComparison);
            }
        }

    }
}