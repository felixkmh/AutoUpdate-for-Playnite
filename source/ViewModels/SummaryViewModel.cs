using AutoUpdate.Addons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoUpdate.ViewModels
{
    public class SummaryViewModel : ObservableObject
    {
        private Dictionary<string, List<AddonInstallerPackage>> lastChanglogs = new Dictionary<string, List<AddonInstallerPackage>>();
        public Dictionary<string, List<AddonInstallerPackage>> LastChanglogs { get => lastChanglogs; set => SetValue(ref lastChanglogs, value); }
    }
}
