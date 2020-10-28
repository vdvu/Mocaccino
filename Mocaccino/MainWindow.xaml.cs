using Microsoft.WindowsAPICodePack.Dialogs;
using Mocaccino.Security;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mocaccino
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _currentPath;
        public string CurrentPath
        {
            get { return _currentPath; }
            set
            {
                _currentPath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentPath"));
            }
        }

        private string _currentProcess;
        public string CurrentProcess
        {
            get { return _currentProcess; }
            set
            {
                _currentProcess = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentProcess"));
            }
        }

        private string _currentFileName;
        public string CurrentFileName
        {
            get { return _currentFileName; }
            set
            {
                _currentFileName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentFileName"));
            }
        }

        private string _currentFilePath;
        public string CurrentFilePath
        {
            get { return _currentFilePath; }
            set
            {
                _currentFilePath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentFilePath"));
            }
        }

        private int _numberOfProcessedFiles;
        public int NumberOfProcessedFiles
        {
            get { return _numberOfProcessedFiles; }
            set
            {
                _numberOfProcessedFiles = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("NumberOfProcessedFiles"));
            }
        }

        private int _numberOfFiles;
        public int NumberOfFiles
        {
            get { return _numberOfFiles; }
            set
            {
                _numberOfFiles = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("NumberOfFiles"));
            }
        }

        private bool _executeButtonIsEnabled = true;
        public bool ExecuteButtonIsEnabled
        {
            get { return _executeButtonIsEnabled; }
            set
            {
                _executeButtonIsEnabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ExecuteButtonIsEnabled"));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }
        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void Minimize_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }
        private void Close_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private List<string> _paths;
        private void PathButton_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog commonOpenFileDialog = new CommonOpenFileDialog();
            _paths = new List<string>();
            if ((bool)FileRadioButton.IsChecked)
            {
                commonOpenFileDialog.Multiselect = true;
                if (commonOpenFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    _paths = commonOpenFileDialog.FileNames.ToList();
                    CurrentPath = _paths[0];
                }
            }
            else if ((bool)FolderRadioButton.IsChecked)
            {
                commonOpenFileDialog.IsFolderPicker = true;
                if (commonOpenFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string path = commonOpenFileDialog.FileName;
                    _paths = Directory.GetFiles(path, "*", SearchOption.AllDirectories).ToList();
                    CurrentPath = path;
                }
            }
        }

        private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            ExecuteButtonIsEnabled = false;
            string password = PasswordBox.Password;
            GCHandle gCHandle = GCHandle.Alloc(password, GCHandleType.Pinned);

            NumberOfFiles = _paths.Count;
            int numberOfSuccessFiles = 0;

            NumberOfProcessedFiles = 0;

            if ((bool)EncryptRadioButton.IsChecked)
            {
                CurrentProcess = "Encrypt";
                foreach (string path in _paths)
                {
                    CurrentFileName = Path.GetFileName(path);
                    CurrentFilePath = path;

                    numberOfSuccessFiles += await Task.Run(() => Crypter.FileEncrypt(path, password)) ? 1 : 0;
                    ++NumberOfProcessedFiles;
                }
            }
            else if ((bool)DecryptRadioButton.IsChecked)
            {
                CurrentProcess = "Decrypt";
                foreach (string path in _paths)
                {
                    CurrentFileName = Path.GetFileName(path);
                    CurrentFilePath = path;

                    numberOfSuccessFiles += await Task.Run(() => Crypter.FileDecrypt(path, password)) ? 1 : 0;
                    ++NumberOfProcessedFiles;
                }
            }

            Crypter.ZeroMemory(gCHandle.AddrOfPinnedObject(), password.Length * 2);
            gCHandle.Free();

            ExecuteButtonIsEnabled = true;

            string msg = $"Process completed!\nSuccess: {numberOfSuccessFiles}/{NumberOfFiles}.";
            MessageBoxImage messageBoxImage = MessageBoxImage.Information;
            if (numberOfSuccessFiles < NumberOfFiles)
            {
                msg += "\nView log file for more information!";
                messageBoxImage = MessageBoxImage.Warning;
            }
            MessageBox.Show(msg, "Mocaccino", MessageBoxButton.OK, messageBoxImage);
        }
    }
}
