using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;


namespace HeliosProfilePatcher
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private XDocument? _document;
        private string? _filePath;
        private string[] _vars = new string[5] { "Helios", "", "", "", "" };
        private char[] _originalChars = new char[] { '\\' };
        private char[] _replacementChars = new char[] { '#' };
        private readonly string _charMessage = "Number of characters not equal";   

        public bool PatchEnabled
        {
            get => _originalChars.Length == _replacementChars.Length;
            set
            {
                OnPropertyChanged(nameof(PatchEnabled));
            }
        }
        public string CharMessage
        {
            get => _originalChars.Length == _replacementChars.Length ? "" : _charMessage;
            set
            {
                OnPropertyChanged(nameof(CharMessage));
            }
        }
        public string OriginalCharactersValue
        {
            get => new string(_originalChars);
            set
            {
                _originalChars = value.ToCharArray();
                CharMessage = _originalChars.Length == _replacementChars.Length ? "" : _charMessage;
                PatchEnabled = _originalChars.Length == _replacementChars.Length;
                OnPropertyChanged(nameof(OriginalCharactersValue));
            }
        }
        public string ReplacementCharactersValue
        {
            get => new string(_replacementChars);
            set
            {
                _replacementChars = value.ToCharArray();
                CharMessage = _originalChars.Length == _replacementChars.Length ? "" : _charMessage;
                PatchEnabled = _originalChars.Length == _replacementChars.Length;
                OnPropertyChanged(nameof(ReplacementCharactersValue));
            }
        }
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
            OpenFileDialog dialog = new OpenFileDialog
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


            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Helios Profiles (*.hpf)|*.hpf"
            };


            if (dialog.ShowDialog() != true)
                return;


            XDocument patchDoc = XDocument.Load(dialog.FileName);

            XElement? targetRoot = _document.Root;
            XElement? sourceRoot = patchDoc.Root;

            if (targetRoot?.Name != "HeliosProfile" || sourceRoot?.Name != "HeliosProfile")
            {
                MessageBox.Show("Both files must have <HeliosProfile> as root");
                return;
            }

            List<XElement> interfacesToAdd = new List<XElement>();
            List<XElement> bindingsToAdd = new List<XElement>();

            PatchPlanner planner = new PatchPlanner(_document, patchDoc);
            PreviewWindow preview = new PreviewWindow(planner.Interfaces, planner.Bindings, _vars)
            {
                Owner = this
            };


            if (preview.ShowDialog() != true)
                return;

            StringBuilder report = new StringBuilder();
            report.AppendLine("Helios Profile Patch Report");
            report.AppendLine(DateTime.Now.ToString());
            report.AppendLine();

            string[] variableNames = new string[5] { "$(HeliosPath)", "$(VehicleName)", "$(var1)", "$(var2)", "$(var3)" };
            report.AppendLine("Variable Substitutions");
            for (int i=0; i<_vars.Length; i++)
            {
                report.AppendLine($"variable {variableNames[i]} = {_vars[i]}");
            }

            XElement? targetInterfaces = _document.Root!.Element("Interfaces");
            XElement? targetBindings = _document.Root!.Element("Bindings");


            foreach (InterfaceItem i in preview.Interfaces.Where(i => i.IsSelected))
            {
                targetInterfaces!.Add(new XElement(i.Element));
                report.AppendLine($"Added interface: {i.Name}");
            }


            foreach (BindingItem b in preview.Bindings.Where(b => b.IsSelected))
            {
                targetBindings!.Add(b.ToElement());
                report.AppendLine($"Added binding: {b.Xml}");
            }

            if (targetBindings != null)
            {
                targetBindings
                    .Descendants("Binding")
                    .Where(b =>
                        b.Elements("Action")
                         .Any(a => (string?)a.Attribute("Name") == "send keys"))
                    .SelectMany(b => b.Elements("StaticValue"))
                    .Where(sv => sv.Value.Contains("\\"))
                    .ToList()
                    .ForEach(sv => {
                        var text = sv.Value;
                        for (int i = 0; i < _originalChars.Length; i++)
                            text = text.Replace(_originalChars[i], _replacementChars[i]);

                        sv.Value = text;
                    });
            }

            File.WriteAllText(Path.ChangeExtension(_filePath!, ".patched.log"), report.ToString());

            MessageBoxResult result = MessageBox.Show(report.ToString(),
            "Patch Preview",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Information);


            if (result != MessageBoxResult.OK)
                return;

            MessageBox.Show("Patch complete");
        }
    }
}