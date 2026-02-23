using System.Windows.Controls;
using KibleYonu.Models;
using KibleYonu.ViewModels;

namespace KibleYonu.Views
{
    public partial class KiblePanelControl : UserControl
    {
        public KiblePanelControl()
        {
            InitializeComponent();
        }

        private void DetayCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is KiblePanelViewModel vm && sender is ComboBox combo)
            {
                switch (combo.SelectedIndex)
                {
                    case 0: vm.DetaySeviyesi = DetaySeviyesi.Basit; break;
                    case 1: vm.DetaySeviyesi = DetaySeviyesi.Normal; break;
                    case 2: vm.DetaySeviyesi = DetaySeviyesi.Detayli; break;
                }
            }
        }
    }
}
