﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutoUpdate.Views
{
    /// <summary>
    /// Interaktionslogik für AvailableUpdatesView.xaml
    /// </summary>
    public partial class AvailableUpdatesView : UserControl
    {
        public AvailableUpdatesView()
        {
            InitializeComponent();
        }

        public AvailableUpdatesView(ViewModels.AvailableUpdatesViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
