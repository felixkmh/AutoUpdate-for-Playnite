using AutoUpdate.Addons;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AutoUpdate.Models
{
    public class UpdateSummary : ObservableObject
    {
        string name;
        public string Name { get => name; set => SetValue(ref name, value); }

        string currentVersion;
        public string CurrentVersion { get => currentVersion; set => SetValue(ref currentVersion, value); }

        string newVersion;
        public string NewVersion { get => newVersion; set => SetValue(ref newVersion, value); }

        ObservableCollection<AddonInstallerPackage> newPackages = new ObservableCollection<AddonInstallerPackage>();
        public ObservableCollection<AddonInstallerPackage> NewPackages { get => newPackages; set => SetValue(ref newPackages, value); }

        ICommand showChangelogCommand;
        public ICommand ShowChangelogCommand { get => showChangelogCommand; set => SetValue(ref showChangelogCommand, value); }

        ICommand removeFromQueueCommand;
        public ICommand RemoveFromQueueCommand { get => removeFromQueueCommand; set => SetValue(ref removeFromQueueCommand, value); }
    }
}
