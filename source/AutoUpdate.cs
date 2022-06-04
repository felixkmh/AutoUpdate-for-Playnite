using LibGit2Sharp;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using YamlDotNet.Serialization;
using AutoUpdate.Addons;
using System.Net.Http;
using System.Net;
using System.Text.RegularExpressions;
using AutoUpdate.ViewModels;
using AutoUpdate.Views;
using System.Windows;
using System.Windows.Input;

namespace AutoUpdate
{
    public class AutoUpdate : GenericPlugin
    {
        internal static readonly ILogger logger = LogManager.GetLogger();
        public static AutoUpdate Instance { get; private set; }

        private AutoUpdateSettingsViewModel settings { get; set; }
        private AutoUpdateSettings Settings => settings.Settings;

        private List<ExtensionInstallQueueItem> updates = new List<ExtensionInstallQueueItem>();

        public override Guid Id { get; } = Guid.Parse("e998a914-7644-4d1b-b6ff-57e3d8c39a6b");

        private Task backgroundTask = Task.CompletedTask;

        public AutoUpdate(IPlayniteAPI api) : base(api)
        {
            settings = new AutoUpdateSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
            Instance = this;
        }

        internal string ExtensionQueueFilePath => Path.Combine(PlayniteApi.Paths.ConfigurationPath, "extinstalls.json");
        internal string AddonsRepoPath => Path.Combine(GetPluginUserDataPath(), "PlayniteAddons");

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
            // Add code to be executed when game is finished installing.
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            // Add code to be executed when game is started running.
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            // Add code to be executed when Playnite is initialized.
            PlayniteApi.Notifications.Messages.CollectionChanged += Messages_CollectionChanged;

            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                if (Settings.ShowSummaryBuild || Settings.ShowSummaryMinor || Settings.ShowSummaryMajor)
                {
                    if (Settings.LastChanglogs.Count > 0 && PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop)
                    {
                            var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions { ShowCloseButton = true, ShowMaximizeButton = true });
                            window.Content = new SummaryView { DataContext = new SummaryViewModel { LastChanglogs = Settings.LastChanglogs } };
                            window.Owner = Application.Current.Windows.Cast<Window>().FirstOrDefault(w => w.Name == "WindowMain");
                            window.Width = 600;
                            window.Height = 400;
                            window.Title = ResourceProvider.GetString("LOC_AU_UpdateSummaryTitle");
                            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            window.PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) window.Close(); };
                            window.Show();
                    }
                }
                Settings.LastChanglogs.Clear();
                SavePluginSettings(Settings);
            }), DispatcherPriority.ApplicationIdle);
        }

        private void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            PlayniteApi.Notifications.Messages.CollectionChanged -= Messages_CollectionChanged;
            if (Settings.SuppressNotificationMajor ||
                Settings.SuppressNotificationMinor ||
                Settings.SuppressNotificationBuild ||
                Settings.AutoUpdateMajor ||
                Settings.AutoUpdateMinor ||
                Settings.AutoUpdateBuild)
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    var toRemove = e.NewItems.OfType<NotificationMessage>().FirstOrDefault(m => m.Id == "AddonUpdateAvailable");
                    if (toRemove is NotificationMessage message)
                    {
                        Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                        {
                            PlayniteApi.Notifications.Messages.Remove(message);
                        }));
                        if (!Settings.SuppressNotificationMajor ||
                            !Settings.SuppressNotificationMinor ||
                            !Settings.SuppressNotificationBuild ||
                            Settings.AutoUpdateMajor ||
                            Settings.AutoUpdateMinor ||
                            Settings.AutoUpdateBuild)
                        {
                            QueueUpdateInstallation(message);
                        }
                    }
                }
            }
            PlayniteApi.Notifications.Messages.CollectionChanged += Messages_CollectionChanged;
        }

        private class UpdateInfo
        {
            public AddonManifest Manifest { get; set; }
            public AddonInstallerPackage Package { get; set; }
            public System.Version InstalledVersion { get; set; }
        }

        private void QueueUpdateInstallation(NotificationMessage updateNotification)
        {
            backgroundTask = backgroundTask.ContinueWith(t =>
            {
                var queuedUpdates = new List<UpdateInfo>();
                updates.Clear();
                bool suppressNotification = false;

                if (!Directory.Exists(AddonsRepoPath))
                {
                    LibGit2Sharp.Repository.Clone("https://github.com/JosefNemec/PlayniteAddonDatabase.git", AddonsRepoPath);
                }
                if (Directory.Exists(AddonsRepoPath))
                {
                    var repo = new LibGit2Sharp.Repository(AddonsRepoPath);
                    if (repo is LibGit2Sharp.Repository)
                    {
                        Commands.Pull(
                            repo,
                            new LibGit2Sharp.Signature(new Identity("felixkmh", "24227002+felixkmh@users.noreply.github.com"), DateTimeOffset.Now),
                            new PullOptions() { MergeOptions = new MergeOptions { MergeFileFavor = MergeFileFavor.Theirs } }
                        );
                    }

                    var desktopThemeIds = Directory.GetDirectories(Path.Combine(PlayniteApi.Paths.ConfigurationPath, "Themes", "Desktop"))
                        .Select(Path.GetFileName);
                    var fullscreeThemeIds = Directory.GetDirectories(Path.Combine(PlayniteApi.Paths.ConfigurationPath, "Themes", "Fullscreen"))
                        .Select(Path.GetFileName);

                    var addonDir = Path.Combine(AddonsRepoPath, "addons");
                    var manifestFiles = System.IO.Directory.GetFiles(Path.Combine(addonDir, "generic"), "*.yaml")
                        .Concat(System.IO.Directory.GetFiles(Path.Combine(addonDir, "library"), "*.yaml"))
                        .Concat(System.IO.Directory.GetFiles(Path.Combine(addonDir, "metadata"), "*.yaml"))
                        .Where(p => PlayniteApi.Addons.Addons.Contains(Path.GetFileNameWithoutExtension(p)))
                        .Concat(System.IO.Directory.GetFiles(Path.Combine(addonDir, "themes_desktop"), "*.yaml").Where(p => desktopThemeIds.Contains(Path.GetFileNameWithoutExtension(p))))
                        .Concat(System.IO.Directory.GetFiles(Path.Combine(addonDir, "themes_fullscreen"), "*.yaml").Where(p => fullscreeThemeIds.Contains(Path.GetFileNameWithoutExtension(p))));

                    var addonManifests = manifestFiles.AsParallel().Select(file =>
                    {
                        using (var yaml = File.OpenText(file))
                        {
                            IDeserializer d = new DeserializerBuilder()
                                .IgnoreUnmatchedProperties()
                                .Build();
                            var manifest = d.Deserialize<AddonManifest>(yaml);
                            return manifest;
                        }
                    }).OfType<AddonManifest>().ToList();

                    var fileNameRegex = new Regex(@"(?<=filename=).*(?=\s?)");

                    IDeserializer deserializer = new DeserializerBuilder()
                                .IgnoreUnmatchedProperties()
                                .Build();


                    foreach (var manifest in addonManifests)
                    {
                        if (PlayniteApi.Addons.DisabledAddons.Contains(manifest.AddonId))
                        {
                            continue;
                        }

                        string extensionManifestPath = null;
                        bool isInstalled = PlayniteApi.Addons.Addons.Contains(manifest.AddonId);

                        if (manifest.Type == AddonType.ThemeDesktop || manifest.Type == AddonType.ThemeFullscreen)
                        {
                            var typeFolder = manifest.Type == AddonType.ThemeDesktop ? "Desktop" : "Fullscreen";
                            var themePath = Path.Combine(PlayniteApi.Paths.ConfigurationPath, "Themes", typeFolder, manifest.AddonId);
                            if (Directory.Exists(themePath))
                            {
                                extensionManifestPath = Path.Combine(themePath, "theme.yaml");
                                isInstalled = File.Exists(extensionManifestPath);
                            }
                        }
                        else
                        {
                            extensionManifestPath = Path.Combine(PlayniteApi.Paths.ConfigurationPath, "Extensions", manifest.AddonId, "extension.yaml");
                        }

                        if (!isInstalled || !File.Exists(extensionManifestPath))
                        {
                            continue;
                        }

                        System.Version installedVersion = null;
                        using (var file = File.OpenText(extensionManifestPath))
                        {
                            var extensionManifest = deserializer.Deserialize<BaseExtensionManifest>(file);
                            if (extensionManifest != null && System.Version.TryParse(extensionManifest.Version, out var v))
                            {
                                installedVersion = v;
                            }
                        }

                        var latest = manifest.InstallerManifest.GetLatestCompatiblePackage();

                        bool update = false;

                        if (installedVersion != null && latest?.Version != null && installedVersion < latest.Version)
                        {
                            AutoUpdateSettings.VersionField versionField = GetUpdateKind(installedVersion, latest.Version);

                            update |= versionField == AutoUpdateSettings.VersionField.Build && Settings.AutoUpdateBuild;
                            update |= versionField == AutoUpdateSettings.VersionField.Minor && Settings.AutoUpdateMinor;
                            update |= versionField == AutoUpdateSettings.VersionField.Major && Settings.AutoUpdateMajor;

                            suppressNotification |= versionField == AutoUpdateSettings.VersionField.Build && Settings.SuppressNotificationBuild;
                            suppressNotification |= versionField == AutoUpdateSettings.VersionField.Minor && Settings.SuppressNotificationMinor;
                            suppressNotification |= versionField == AutoUpdateSettings.VersionField.Major && Settings.SuppressNotificationMajor;
                        }

                        if (update)
                        {
                            queuedUpdates.Add(new UpdateInfo { Package = latest, Manifest = manifest, InstalledVersion = installedVersion });
                        }
                    }

                    if (!suppressNotification)
                    {
                        var notification = new NotificationMessage(
                            "AutoUpdateAvailable",
                            updateNotification.Text,
                            updateNotification.Type,
                            updateNotification.ActivationAction);
                        Dispatcher.CurrentDispatcher.Invoke(() => PlayniteApi.Notifications.Add(notification));
                    }

                    using (var client = new WebClient())
                    {

                        foreach (var latest in queuedUpdates)
                        {
                            try
                            {
                                var tempPath = Path.Combine(Path.GetTempPath(), "Playnite");
                                var data = client.DownloadData(latest.Package.PackageUrl);
                                var header = client.ResponseHeaders["Content-Disposition"];
                                if (!string.IsNullOrEmpty(header))
                                {
                                    var fileName = fileNameRegex.Match(header)?.Value;
                                    if (fileName.StartsWith("\"") && fileName.EndsWith("\""))
                                    {
                                        fileName = fileName.Substring(1, fileName.Length - 2);
                                    }
                                    if (!string.IsNullOrEmpty(fileName) && data.Length > 512 &&
                                    (fileName.EndsWith(".pext", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".pthm", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        string filePath = Path.Combine(tempPath, fileName);
                                        using (var file = File.Create(filePath))
                                        {
                                            file.Write(data, 0, data.Length);
                                        }
                                        updates.Add(new ExtensionInstallQueueItem(filePath, ExtInstallType.Install));

                                        bool addToSummary = false;

                                        AutoUpdateSettings.VersionField versionField = GetUpdateKind(latest.InstalledVersion, latest.Package.Version);

                                        addToSummary |= versionField == AutoUpdateSettings.VersionField.Build && Settings.ShowSummaryBuild;
                                        addToSummary |= versionField == AutoUpdateSettings.VersionField.Minor && Settings.ShowSummaryMinor;
                                        addToSummary |= versionField == AutoUpdateSettings.VersionField.Major && Settings.ShowSummaryMajor;

                                        if (addToSummary)
                                        {
                                            Settings.LastChanglogs[latest.Manifest.Name] = latest.Manifest.InstallerManifest.Packages
                                                .Where(p => p.Version > latest.InstalledVersion)
                                                .Where(p => p.Version <= latest.Package.Version)
                                                .OrderByDescending(p => p.Version)
                                                .ToList();
                                        } else
                                        {
                                            Settings.LastChanglogs.Remove(latest.Manifest.Name);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex, $"Failed to download {latest.Package.PackageUrl}");
                            }
                        }
                    }
                }
                GC.Collect();
                t?.Dispose();
            });
        }

        private static AutoUpdateSettings.VersionField GetUpdateKind(System.Version installedVersion, System.Version latest)
        {
            AutoUpdateSettings.VersionField versionField = AutoUpdateSettings.VersionField.None;
            if (latest.Major == installedVersion.Major && latest.Minor == installedVersion.Minor)
            {
                versionField = AutoUpdateSettings.VersionField.Build;
            }
            else if (latest.Major == installedVersion.Major)
            {
                versionField = AutoUpdateSettings.VersionField.Minor;
            }
            else
            {
                versionField = AutoUpdateSettings.VersionField.Major;
            }

            return versionField;
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            // Add code to be executed when Playnite is shutting down.
            PlayniteApi.Notifications.Messages.CollectionChanged -= Messages_CollectionChanged;

            try
            {
                if (updates.Count > 0)
                {
                    if (File.Exists(ExtensionQueueFilePath))
                    {
                        var file = File.ReadAllText(ExtensionQueueFilePath);
                        var queued = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ExtensionInstallQueueItem>>(file);
                        if (queued != null)
                        {
                            foreach(var item in queued)
                            {
                                updates.Add(item);
                            }
                        }
                    }
                    using (var file = File.CreateText(ExtensionQueueFilePath))
                    {
                        file.Write(Newtonsoft.Json.JsonConvert.SerializeObject(updates));
                    }
                    SavePluginSettings(Settings);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to write update queue to {ExtensionQueueFilePath}");
            }
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            // Add code to be executed when library is updated.
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new AutoUpdateSettingsView();
        }
    }
}