using AutoUpdate.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AutoUpdate.ViewModels
{
    public class AvailableUpdatesViewModel : ObservableObject
    {
        AutoUpdate plugin;
        
        ObservableCollection<Models.UpdateSummary> updates = new ObservableCollection<Models.UpdateSummary>();
        public ObservableCollection<Models.UpdateSummary> Updates { get => updates; set => SetValue(ref updates, value); }

        public ICommand CheckForUpdatesCommand { get; }

        private bool isChecking = false;
        public bool IsChecking { get => isChecking; set => SetValue(ref isChecking, value); }

        public AutoUpdateSettingsViewModel SettingsViewModel { get; }

        public AvailableUpdatesViewModel(AutoUpdate autoUpdate)
        {
            plugin = autoUpdate;
            SettingsViewModel = plugin.settings;
            CheckForUpdatesCommand = new RelayCommand(() =>
            {
                if (!plugin.IsChecking)
                {
                    IsChecking = true;
                    var task = plugin.QueueUpdateInstallation(null);
                    task.ContinueWith(t =>
                    {
                        IsChecking = false;
                        t?.Dispose();
                    });
                }
            });
        }
    }
}
