using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml.Linq;


namespace HeliosProfilePatcher
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private XDocument? _document;
        private string? _filePath;
        private string[] _vars = new string[5] { "Helios", "", "", "", "" };
        public string HeliosPathValue
        {
            get => _vars[0];
            set
            {
                _vars[0] = value;
                OnPropertyChanged(nameof(VehicleValue));
            }
        }
        public string VehicleValue
        {
            get => _vars[1];
            set
            {
                _vars[1] = value;
                OnPropertyChanged(nameof(VehicleValue));
            }
        }
        public string Var1
        {
            get => _vars[2];
            set
            {
                _vars[2] = value;
                OnPropertyChanged(nameof(Var1));
            }
        }
        public string Var2
        {
            get => _vars[3];
            set
            {
                _vars[3] = value;
                OnPropertyChanged(nameof(Var2));
            }
        }
        public string Var3
        {
            get => _vars[4];
            set
            {
                _vars[4] = value;
                OnPropertyChanged(nameof(Var3));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler? PropertyChanged = delegate { };
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OpenXml_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Helios Profiles (*.hpf)|*.hpf"
            };


            if (dialog.ShowDialog() == true)
            {
                _filePath = dialog.FileName;
                _document = XDocument.Load(_filePath);
                FilePathText.Text = Path.GetFileName(_filePath);
            }
        }


        private void SaveXml_Click(object sender, RoutedEventArgs e)
        {

            if (_document == null || _filePath == null) return;
            SaveFileDialog dlg = new() {
                FileName = $"{Path.GetFileNameWithoutExtension(_filePath)}_Patched.hpf",
                DefaultExt = ".hpf",
                Filter = "Helios Profiles (.hpf)|*.hpf"
            };
            Nullable<bool> result = dlg.ShowDialog();

            if (result!.Value)
            {
                _document.Save(dlg.FileName);
                MessageBox.Show($"Patched Helios Profile saved to {dlg.FileName}");
            }
        }

        private void PatchXml_Click(object sender, RoutedEventArgs e)
        {
            if (_document == null)
            {
                MessageBox.Show("Open a base Helios Profile first");
                return;
            }


            var dialog = new OpenFileDialog
            {
                Filter = "Helios Profiles (*.hpf)|*.hpf"
            };


            if (dialog.ShowDialog() != true)
                return;


            var patchDoc = XDocument.Load(dialog.FileName);


            var targetRoot = _document.Root;
            var sourceRoot = patchDoc.Root;


            if (targetRoot?.Name != "HeliosProfile" || sourceRoot?.Name != "HeliosProfile")
            {
                MessageBox.Show("Both files must have <HeliosProfile> as root");
                return;
            }


            var interfacesToAdd = new List<XElement>();
            var bindingsToAdd = new List<XElement>();


            CollectInterfaces();
            CollectBindings();
            var interfaceItems = new List<InterfaceItem>();
            var bindingItems = new List<BindingItem>();

            var planner = new PatchPlanner(_document, patchDoc);
            var preview = new PreviewWindow(planner.Interfaces, planner.Bindings, _vars)
            {
                Owner = this
            };


            if (preview.ShowDialog() != true)
                return;

            var report = new StringBuilder();
            report.AppendLine("Helios Profile Patch Report");
            report.AppendLine(DateTime.Now.ToString());
            report.AppendLine();

            string[] variableNames = new string[5] { "$(HeliosPath)", "$(VehicleName)", "$(var1)", "$(var2)", "$(var3)" };
            report.AppendLine("Variable Substitutions");
            for (int i=0; i<_vars.Length; i++)
            {
                report.AppendLine($"variable {variableNames[i]} = {_vars[i]}");
            }

            var targetInterfaces = _document.Root!.Element("Interfaces");
            var targetBindings = _document.Root!.Element("Bindings");


            foreach (var i in preview.Interfaces.Where(i => i.IsSelected))
            {
                targetInterfaces!.Add(new XElement(i.Element));
                report.AppendLine($"Added interface: {i.Name}");
            }


            foreach (var b in preview.Bindings.Where(b => b.IsSelected))
            {
                targetBindings!.Add(b.ToElement());
                report.AppendLine($"Added binding: {b.Xml}");
            }


            File.WriteAllText(Path.ChangeExtension(_filePath!, ".patched.log"), report.ToString());


            var result = MessageBox.Show(report.ToString(),
            "Patch Preview",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Information);


            if (result != MessageBoxResult.OK)
                return;

            MessageBox.Show("Patch complete");


            void CollectInterfaces()
            {
                var targetInterfaces = targetRoot.Element("Interfaces");
                var sourceInterfaces = sourceRoot.Element("Interfaces");


                if (targetInterfaces == null || sourceInterfaces == null)
                    return;


                var existingNames = new HashSet<string>(
                targetInterfaces.Elements("Interface")
                .Select(i => (string?)i.Attribute("Name"))
                .Where(n => !string.IsNullOrEmpty(n))!);


                foreach (var iface in sourceInterfaces.Elements("Interface"))
                {
                    var name = (string?)iface.Attribute("Name");
                    if (string.IsNullOrEmpty(name))
                        continue;


                    if (existingNames.Contains(name))
                        continue;


                    interfacesToAdd.Add(iface);
                    existingNames.Add(name);
                }
            }


            void CollectBindings()
            {
                var targetBindings = targetRoot.Element("Bindings");
                var sourceBindings = sourceRoot.Element("Bindings");


                if (targetBindings == null || sourceBindings == null)
                    return;


                var existing = new HashSet<string>(
                targetBindings.Elements("Binding")
                .Select(b => b.ToString(SaveOptions.DisableFormatting)));


                foreach (var binding in sourceBindings.Elements("Binding"))
                {
                    var key = binding.ToString(SaveOptions.DisableFormatting);
                    if (existing.Contains(key))
                        continue;


                    bindingsToAdd.Add(binding);
                    existing.Add(key);
                }
            }
        }

    }
}