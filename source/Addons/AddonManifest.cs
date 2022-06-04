using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace AutoUpdate.Addons
{
    public enum AddonType
    {
        GameLibrary,
        MetadataProvider,
        Generic,
        ThemeDesktop,
        ThemeFullscreen
    }

    public class AddonManifest : ObservableObject
    {
        private static Version sdkVersion = null;
        private static Version desktopVersion = null;
        private static Version fullscreenVersion = null;

        public static Version GetApiVersion(AddonType type)
        {
            switch (type)
            {
                case AddonType.GameLibrary:
                case AddonType.MetadataProvider:
                case AddonType.Generic:
                    if (sdkVersion == null)
                    {
                        sdkVersion = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => a.GetName().Name == "Playnite.SDK")?.GetName().Version;
                    }
                    return sdkVersion;
                case AddonType.ThemeDesktop:
                    return desktopVersion;
                case AddonType.ThemeFullscreen:
                    return fullscreenVersion;
            }

            return new Version(999, 0);
        }

        public class AddonUserAgreement
        {
            public DateTime Updated { get; set; }
            public string AgreementUrl { get; set; }
        }

        public class AddonScreenshot
        {
            public string Thumbnail { get; set; }
            public string Image { get; set; }
        }

        public string IconUrl { get; set; }
        public List<AddonScreenshot> Screenshots { get; set; }
        public AddonType Type { get; set; }
        public string InstallerManifestUrl { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
        public string AddonId { get; set; }
        public string Author { get; set; }
        public Dictionary<string, string> Links { get; set; }
        public List<string> Tags { get; set; }
        public AddonUserAgreement UserAgreement { get; set; }
        public string SourceUrl { get; set; }
        [YamlDotNet.Serialization.YamlIgnore]
        private AddonInstallerManifest installerManifest = null;
        [YamlDotNet.Serialization.YamlIgnore]
        public AddonInstallerManifest InstallerManifest
        {
            get
            {
                if (installerManifest == null)
                {
                    try
                    {
                        using (var client = new System.Net.Http.HttpClient())
                        {
                            using (var response = client.GetStringAsync(InstallerManifestUrl))
                            {
                                response.Wait();
                                if (!response.IsFaulted)
                                {
                                    var yaml = response.Result;
                                    var deserializer = new YamlDotNet.Serialization.Deserializer();
                                    var manifest = deserializer.Deserialize<AddonInstallerManifest>(yaml);
                                    installerManifest = manifest;
                                    installerManifest.Packages = installerManifest.Packages.OrderByDescending(p => p.Version).ToList();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AutoUpdate.logger.Error(ex, $"Failed to download InstallerManifest for \"{Name}\"");
                    }
                }
                return installerManifest;
            }
        }
    }

    public class AddonInstallerManifest
    {
        public string AddonId { get; set; }
        public List<AddonInstallerPackage> Packages { get; set; }
        public AddonType AddonType { get; set; }

        public AddonInstallerPackage GetLatestCompatiblePackage()
        {
            var apiVersion = AddonManifest.GetApiVersion(AddonType);

            if (apiVersion == null)
                return null;
            if (!Packages.HasItems())
                return null;

            return Packages.
                Where(a => a.RequiredApiVersion.Major == apiVersion.Major && a.RequiredApiVersion <= apiVersion).
                OrderByDescending(a => a.Version).FirstOrDefault();
        }
    }

    public class AddonInstallerPackage
    {
        public System.Version Version { get; set; }
        public string PackageUrl { get; set; }
        public System.Version RequiredApiVersion { get; set; }
        public DateTime ReleaseDate { get; set; }
        public List<string> Changelog { get; set; }
    }

    public enum ExtensionType
    {
        GenericPlugin,
        GameLibrary,
        Script,
        MetadataProvider
    }

    public class BaseExtensionManifest
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Author { get; set; }

        public string Version { get; set; }

        public List<Link> Links { get; set; }

        [YamlIgnore]
        public string DirectoryPath { get; set; }

        [YamlIgnore]
        public string DirectoryName { get; set; }

        [YamlIgnore]
        public string DescriptionPath { get; set; }

        public void VerifyManifest()
        {
            if (!System.Version.TryParse(Version, out var extver))
            {
                throw new Exception("Extension version string must be a real version!");
            }
        }
    }
}
