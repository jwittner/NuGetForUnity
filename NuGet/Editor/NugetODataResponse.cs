﻿using System.Collections.Generic;
using System.Xml.Linq;

namespace NugetForUnity
{
    /// <summary>
    /// Provides helper methods for parsing a NuGet server OData response.
    /// OData is a superset of the Atom API.
    /// </summary>
    public static class NugetODataResponse
    {
        private static string AtomNamespace = "http://www.w3.org/2005/Atom";

        private static string DataServicesNamespace = "http://schemas.microsoft.com/ado/2007/08/dataservices";

        private static string MetaDataNamespace = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";

        /// <summary>
        /// Gets the string value of a NuGet metadata property from the given properties element and property name.
        /// </summary>
        /// <param name="properties">The properties element.</param>
        /// <param name="name">The name of the property to get.</param>
        /// <returns>The string value of the property.</returns>
        private static string GetProperty(this XElement properties, string name)
        {
            return (string)properties.Element(XName.Get(name, DataServicesNamespace)) ?? string.Empty;
        }

        /// <summary>
        /// Gets the <see cref="XElement"/> within the Atom namespace with the given name.
        /// </summary>
        /// <param name="element">The element containing the Atom element.</param>
        /// <param name="name">The name of the Atom element</param>
        /// <returns>The Atom element.</returns>
        private static XElement GetAtomElement(this XElement element, string name)
        {
            return element.Element(XName.Get(name, AtomNamespace));
        }

        /// <summary>
        /// Parses the given <see cref="XDocument"/> and returns the list of <see cref="NugetPackage"/>s contained within.
        /// </summary>
        /// <param name="document">The <see cref="XDocument"/> that is the OData XML response from the NuGet server.</param>
        /// <returns>The list of <see cref="NugetPackage"/>s read from the given XML.</returns>
        public static List<NugetPackage> Parse(XDocument document)
        {
            List<NugetPackage> packages = new List<NugetPackage>();

            var packageEntries = document.Root.Elements(XName.Get("entry", AtomNamespace));
            foreach (var entry in packageEntries)
            {
                NugetPackage package = new NugetPackage();
                package.Id = entry.GetAtomElement("title").Value;
                package.DownloadUrl = entry.GetAtomElement("content").Attribute("src").Value;

                var entryProperties = entry.Element(XName.Get("properties", MetaDataNamespace));
                package.Title = entryProperties.GetProperty("Title");
                package.Version = entryProperties.GetProperty("Version");
                package.Description = entryProperties.GetProperty("Description");
                package.ReleaseNotes = entryProperties.GetProperty("ReleaseNotes");
                package.LicenseUrl = entryProperties.GetProperty("LicenseUrl");

                string iconUrl = entryProperties.GetProperty("IconUrl");
                if (!string.IsNullOrEmpty(iconUrl))
                {
                    package.Icon = NugetHelper.DownloadImage(iconUrl);
                }

                // if there is no title, just use the ID as the title
                if (string.IsNullOrEmpty(package.Title))
                {
                    package.Title = package.Id;
                }

                // Get dependencies
                package.Dependencies = new List<NugetPackageIdentifier>();
                string rawDependencies = entryProperties.GetProperty("Dependencies");
                if (!string.IsNullOrEmpty(rawDependencies))
                {
                    string[] dependencies = rawDependencies.Split('|');
                    foreach (var dependencyString in dependencies)
                    {
                        string[] details = dependencyString.Split(':');
                        string id = details[0];
                        string version = details[1];
                        string framework = string.Empty;

                        if (details.Length > 2)
                        {
                            framework = details[2];
                        }

                        // some packages (ex: FSharp.Data - 2.1.0) have inproper "semi-empty" dependencies such as:
                        // "Zlib.Portable:1.10.0:portable-net40+sl50+wp80+win80|::net40"
                        // so we need to only add valid dependencies and skip invalid ones
                        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(version))
                        {
                            // TODO: Fix this for Unity's .NET 4.6 support
                            // only use the dependency if there is no framework specified, or it is explicitly .NET 3.0
                            if (string.IsNullOrEmpty(framework) || framework == "net30")
                            {
                                NugetPackageIdentifier dependency = new NugetPackageIdentifier(id, version);
                                package.Dependencies.Add(dependency);
                            }
                        }
                    }
                }

                packages.Add(package);
            }

            return packages;
        }
    }
}
