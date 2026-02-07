using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace HeliosProfilePatcher
{
    public partial class PreviewWindow : Window
    {
        public ObservableCollection<InterfaceItem> Interfaces { get; }
        public ObservableCollection<BindingItem> Bindings { get; }
        private string[] _vars = new string[5] { "", "", "", "", ""};


        public PreviewWindow(
        IEnumerable<InterfaceItem> interfaces,
        IEnumerable<BindingItem> bindings,
        string[] vars)
        {
            InitializeComponent();
            _vars = vars;
            Interfaces = new ObservableCollection<InterfaceItem>(interfaces);
            Bindings = new ObservableCollection<BindingItem>(
            bindings.Select(SubstituteVariables));
            DataContext = this;
        }

        private BindingItem SubstituteVariables(BindingItem item)
        {
            item.Xml = item.Xml.Replace("$(VehicleName)", _vars[1], StringComparison.CurrentCultureIgnoreCase)
                .Replace("$(HeliosPath)", _vars[0], StringComparison.CurrentCultureIgnoreCase)
                .Replace("$(var1)", _vars[2], StringComparison.CurrentCultureIgnoreCase)
                .Replace("$(var2)", _vars[3], StringComparison.CurrentCultureIgnoreCase)
                .Replace("$(var3)", _vars[4], StringComparison.CurrentCultureIgnoreCase)
                ;
            return item;
        }

        private void Patch_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }


        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
    public class InterfaceItem
    {
        public string Name { get; init; } = string.Empty;
        public XElement Element { get; init; } = null!;
        public bool IsSelected { get; set; } = true;
    }

}
