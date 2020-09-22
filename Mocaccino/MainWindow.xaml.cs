using Microsoft.WindowsAPICodePack.Dialogs;
using Mocaccino.Security;
using System.Collections.Generic;
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
    public partial class MainWindow : Window
    {
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
                    PathTextBox.Text = _paths[0];
                }
            }
            else if ((bool)FolderRadioButton.IsChecked)
            {
                commonOpenFileDialog.IsFolderPicker = true;
                if (commonOpenFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string path = commonOpenFileDialog.FileName;
                    _paths = Directory.GetFiles(path, "*", SearchOption.AllDirectories).ToList();
                    PathTextBox.Text = path;
                }
            }
        }

        private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).IsEnabled = false;
            string password = PasswordBox.Password;
            GCHandle gCHandle = GCHandle.Alloc(password, GCHandleType.Pinned);

            int totalNumberOfFiles = _paths.Count;
            int numberOfSuccessFiles = 0;
            int fileNumber = 0;

            TotalNumberOfFilesTextBlock.Text = totalNumberOfFiles.ToString();


            if ((bool)EncryptRadioButton.IsChecked)
            {
                ProcessTextBlock.Text = "Encrypt";
                foreach (string path in _paths)
                {
                    PathTextBlock.Text = Path.GetFileName(path);
                    PathTextBlock.ToolTip = path;

                    numberOfSuccessFiles += await Task.Run(() => Crypter.FileEncrypt(path, password)) ? 1 : 0;
                    FileNumberTextBlock.Text = (++fileNumber).ToString();
                }
            }
            else if ((bool)DecryptRadioButton.IsChecked)
            {
                ProcessTextBlock.Text = "Decrypt";
                foreach (string path in _paths)
                {
                    PathTextBlock.Text = Path.GetFileName(path);
                    PathTextBlock.ToolTip = path;

                    numberOfSuccessFiles += await Task.Run(() => Crypter.FileDecrypt(path, password)) ? 1 : 0;
                    FileNumberTextBlock.Text = (++fileNumber).ToString();
                }
            }

            Crypter.ZeroMemory(gCHandle.AddrOfPinnedObject(), password.Length * 2);
            gCHandle.Free();

            ((Button)sender).IsEnabled = true;

            string msg = $"Process completed!\nSuccess: {numberOfSuccessFiles}/{totalNumberOfFiles}.";
            MessageBoxImage messageBoxImage = MessageBoxImage.Information;
            if (numberOfSuccessFiles < totalNumberOfFiles)
            {
                msg += "\nView log file for more information!";
                messageBoxImage = MessageBoxImage.Warning;
            }
            MessageBox.Show(msg, "Mocaccino", MessageBoxButton.OK, messageBoxImage);
        }
    }
}
