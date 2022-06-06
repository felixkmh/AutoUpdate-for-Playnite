using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoUpdate.ViewModels
{
    public class AvailableUpdatesViewModel : ObservableObject
    {
        ObservableCollection<Models.UpdateSummary> updates = new ObservableCollection<Models.UpdateSummary>();
        public ObservableCollection<Models.UpdateSummary> Updates { get => updates; set => SetValue(ref updates, value); }
    }
}
