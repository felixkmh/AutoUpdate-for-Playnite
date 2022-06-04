using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoUpdate
{
    public class AutoUpdateSettings : ObservableObject
    {
        [Flags]
        public enum VersionField
        {
            None  = 0,
            Build = 1,
            Minor = 2,
            Major = 4,
        }

        private bool suppressNotificationBuild = false;
        public bool SuppressNotificationBuild { get => suppressNotificationBuild; set => SetValue(ref suppressNotificationBuild, value); }

        private bool suppressNotificationMinor = false;
        public bool SuppressNotificationMinor { get => suppressNotificationMinor; set => SetValue(ref suppressNotificationMinor, value); }

        private bool suppressNotificationMajor = false;
        public bool SuppressNotificationMajor { get => suppressNotificationMajor; set => SetValue(ref suppressNotificationMajor, value); }

        private bool autoUpdateBuild = false;
        public bool AutoUpdateBuild { get => autoUpdateBuild; set => SetValue(ref autoUpdateBuild, value); }

        private bool autoUpdateMinor = false;
        public bool AutoUpdateMinor { get => autoUpdateMinor; set => SetValue(ref autoUpdateMinor, value); }

        private bool autoUpdateMajor = false;
        public bool AutoUpdateMajor { get => autoUpdateMajor; set => SetValue(ref autoUpdateMajor, value); }
    }

    public class AutoUpdateSettingsViewModel : ObservableObject, ISettings
    {
        private readonly AutoUpdate plugin;
        private AutoUpdateSettings editingClone { get; set; }

        private AutoUpdateSettings settings;
        public AutoUpdateSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public AutoUpdateSettingsViewModel(AutoUpdate plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<AutoUpdateSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new AutoUpdateSettings();
            }
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = editingClone;
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }
    }
}