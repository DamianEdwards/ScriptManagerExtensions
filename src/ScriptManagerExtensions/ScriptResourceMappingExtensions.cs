using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web.Hosting;
using System.Web.UI;

namespace ScriptManagerExtensions
{
    /// <summary>
    /// Contains extension
    /// </summary>
    public static class ScriptResourceMappingExtensions
    {
        public const string VerRegexPattern = @"(?<ver>[1-9](?:\d*)(?:\.\d*){0,2})";

        /// <summary>
        /// Gets or sets the virtual path provider used by the extensions.
        /// Defaults to System.Web.Hosting.HostingEnvironment.VirtualPathProvider.
        /// </summary>
        public static VirtualPathProvider VirtualPathProvider
        {
            get;
            set;
        }

        static ScriptResourceMappingExtensions()
        {
            VirtualPathProvider = HostingEnvironment.VirtualPathProvider;
        }

        /// <summary>
        ///    Adds a System.Web.UI.ScriptResourceDefinition object to the System.Web.UI.ScriptResourceMapping
        ///    object, but only if a file matching the supplied regex pattern is found. If the pattern includes
        ///    a capture group called "ver", it will be used to determine the highest version found.
        /// </summary>
        /// <param name="map">The ScriptResourceMapping object.</param>
        /// <param name="name">The name of the script resource.</param>
        /// <param name="definition">
        ///     A System.Web.UI.ScriptResourceDefinition object that specifies location support
        ///     for script resources. The Path and optionally DebugPath properties should contain
        ///     the path to the virtual folder containing the script to find, e.g. ~/Scripts/
        /// </param>
        /// <param name="fileNamePattern">
        ///     The regex pattern to match files against. If the pattern includes
        ///     a capture group called "ver", it will be used to determine the highest version found.
        /// </param>
        public static void AddDefinitionIfFound(this ScriptResourceMapping map, string name, ScriptResourceDefinition definition, string fileNamePattern)
        {
            AddDefinitionIfFound(map, name, typeof(ScriptManager).Assembly, definition, fileNamePattern);
        }

        /// <summary>
        ///    Adds a System.Web.UI.ScriptResourceDefinition object to the System.Web.UI.ScriptResourceMapping
        ///    object, but only if a file matching the supplied regex pattern is found. If the pattern includes
        ///    a capture group called "ver", it will be used to determine the highest version found.
        /// </summary>
        /// <param name="map">The ScriptResourceMapping object.</param>
        /// <param name="name">The name of the script resource.</param>
        /// <param name="assembly">A System.Reflection.Assembly object that is used along with the name as the script resource key.</param>
        /// <param name="definition">
        ///     A System.Web.UI.ScriptResourceDefinition object that specifies location support
        ///     for script resources. The Path and optionally DebugPath properties should contain
        ///     the path to the virtual folder containing the script to find, e.g. ~/Scripts/
        /// </param>
        /// <param name="fileNamePattern">
        ///     The regex pattern to match files against. If the pattern includes
        ///     a capture group called "ver", it will be used to determine the highest version found.
        ///     Use ScriptResourceMappingExtensions.VerRegexPattern for a pattern matching standard
        ///     versioning strings, e.g. 1, 1.0, 1.1.2, etc.
        /// </param>
        public static void AddDefinitionIfFound(this ScriptResourceMapping map, string name, Assembly assembly, ScriptResourceDefinition definition, string fileNamePattern)
        {
            var scriptsFolder = VirtualPathProvider.GetDirectory(definition.Path);

            if (scriptsFolder == null)
            {
                return;
            }

            var scripts = scriptsFolder.Files.Cast<VirtualFile>();

            var fileNamePatternRegex = new Regex(fileNamePattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var supportVer = fileNamePatternRegex.GroupNumberFromName("ver") >= 0;

            var foundFile = (from file in scripts
                             let match = fileNamePatternRegex.Match(file.Name)
                             where match.Success
                             let fileVer = supportVer ? GetVersion(match) : null
                             orderby fileVer descending
                             select new
                             {
                                 file.Name,
                                 Version = fileVer
                             }).FirstOrDefault();

            if (foundFile != null)
            {
                var ver = foundFile.Version != null
                    ? Regex.Match(foundFile.Name, VerRegexPattern, RegexOptions.IgnoreCase).Groups["ver"].Value
                    : "";
                var isMin = foundFile.Name.EndsWith(".min.js", StringComparison.OrdinalIgnoreCase);

                var fileNamePrefix = foundFile.Name.Substring(0,
                    foundFile.Name.IndexOf(ver + (isMin ? ".min.js" : ".js")));

                var altFile = scripts.SingleOrDefault(f => f.Name.Equals(
                    String.Format(isMin ? "{0}{1}.js" : "{0}{1}.min.js", fileNamePrefix, ver),
                    StringComparison.OrdinalIgnoreCase));

                var hasBoth = altFile != null;

                var minFileName = isMin
                    ? foundFile.Name
                    : hasBoth
                        ? altFile.Name
                        : null;

                var nonMinFileName = !isMin
                    ? foundFile.Name
                    : hasBoth
                        ? altFile.Name
                        : null;

                var minOnly = isMin && !hasBoth;

                var path = minOnly || hasBoth
                    ? minFileName
                    : nonMinFileName;

                var debugPath = hasBoth
                    ? nonMinFileName
                    : null;

                path = path == null ? null : definition.Path + path;

                debugPath = debugPath == null ? null : (definition.DebugPath ?? definition.Path) + debugPath;

                var cdnPath = definition.CdnPath != null
                    ? ver != null
                        ? String.Format(definition.CdnPath, ver)
                        : definition.CdnPath
                    : null;

                var cdnDebugPath = definition.CdnDebugPath != null
                    ? ver != null
                        ? String.Format(definition.CdnDebugPath, ver)
                        : definition.CdnDebugPath
                    : null;

                map.AddDefinition(name, assembly, new ScriptResourceDefinition
                {
                    Path = path,
                    DebugPath = debugPath,
                    CdnPath = cdnPath,
                    CdnDebugPath = cdnDebugPath,
                    CdnSupportsSecureConnection = definition.CdnSupportsSecureConnection
                });
            }
        }

        private static Version GetVersion(Match match)
        {
            var verGroup = match.Groups["ver"];
            if (verGroup == null)
            {
                return null;
            }

            var versionString = verGroup.Value;
            if (String.IsNullOrEmpty(versionString))
            {
                return null;
            }

            int versionNumber;
            if (Int32.TryParse(versionString, out versionNumber))
            {
                return new Version(versionNumber, 0);
            }

            return new Version(versionString);
        }
    }
}