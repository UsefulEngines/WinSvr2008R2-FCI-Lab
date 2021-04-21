using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.IO;
using FciSharePointUpload.Core;
using ComboBox = System.Windows.Controls.ComboBox;
using MessageBox = System.Windows.MessageBox;

namespace FciSharePointUpload.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _username;
        private string _password;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            MenuOpenFile.Click += new RoutedEventHandler(MenuOpenFile_Click);
            MenuOpenDir.Click += new RoutedEventHandler(MenuOpenDir_Click);
            MenuClose.Click += new RoutedEventHandler(MenuClose_Click);
            MenuCredentials.Click += new RoutedEventHandler(MenuCredentials_Click);
            ListBoxFiles.KeyUp += new System.Windows.Input.KeyEventHandler(ListBoxFiles_KeyUp);
            ButtonUpload.Click += new RoutedEventHandler(ButtonUpload_Click);
            LoadActionOptions();
        }

        private void MenuOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileWindow = new OpenFileDialog();
            DialogResult dialogResult = openFileWindow.ShowDialog();
            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                string filename = openFileWindow.FileName;
                InsertFileName(filename);
            }
        }

        private void InsertFileName(string filename)
        {
            int insertAt = 0;
            for (int sortingIndex = 0; sortingIndex != ListBoxFiles.Items.Count; sortingIndex++)
            {
                string oldItem = (string)ListBoxFiles.Items[sortingIndex];
                int comparison = filename.ToUpper().CompareTo(oldItem.ToUpper());
                if (comparison == 0)
                {
                    return;
                }
                if (comparison < 0)
                {
                    insertAt = sortingIndex;
                    break;
                }
            }
            ListBoxFiles.Items.Insert(insertAt, filename);
        }

        private void MenuOpenDir_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog openFolderWindow = new FolderBrowserDialog();
            DialogResult dialogResult = openFolderWindow.ShowDialog();
            if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                for (int fileIndex = ListBoxFiles.Items.Count - 1; fileIndex >= 0; fileIndex--)
                {
                    string file = (string) ListBoxFiles.Items[fileIndex];
                    if (file.StartsWith(openFolderWindow.SelectedPath))
                    {
                        ListBoxFiles.Items.RemoveAt(fileIndex);
                    }
                }
                DirectoryInfo root = new DirectoryInfo(openFolderWindow.SelectedPath);
                InsertFolderContents(root);
            }

        }

        private void InsertFolderContents(DirectoryInfo folder)
        {
            FileInfo[] files = folder.GetFiles();
            foreach (FileInfo file in files)
            {
                InsertFileName(file.FullName);
            }
            DirectoryInfo[] subdirectories = folder.GetDirectories();
            foreach (DirectoryInfo directory in subdirectories)
            {
                InsertFolderContents(directory);
            }
        }

        private void MenuClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuCredentials_Click(object sender, RoutedEventArgs e)
        {
            var credentialsWindow = new Credentials();
            bool? credentialResults = credentialsWindow.ShowDialog();
            if (credentialResults == true)
            {
                _username = credentialsWindow.Username;
                _password = credentialsWindow.Password;
            }
        }

        private void ListBoxFiles_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                List<string> deletedFiles = new List<string>();
                foreach (string selectedFile in ListBoxFiles.SelectedItems)
                {
                    deletedFiles.Add(selectedFile);
                }
                foreach (string file in deletedFiles)
                {
                    ListBoxFiles.Items.Remove(file);
                }
            }
        }

        private void LoadActionOptions()
        {
            CopyEnumToBox(typeof (UploadSourceAction), ComboBoxSourceAction);
            CopyEnumToBox(typeof (UploadTargetAction), ComboBoxTargetAction);
            CopyEnumToBox(typeof (PropertyAction), ComboBoxPropertyAction);
        }

        private static void CopyEnumToBox(Type enumType, ComboBox box)
        {
            box.Items.Clear();
            Array values = Enum.GetValues(enumType);
            foreach (var value in values)
            {
                box.Items.Add(value);
            }
            box.SelectedIndex = 0;
        }

        private void ButtonUpload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string url = TextBoxUrl.Text;
                string libPath = TextBoxLibPath.Text;
                string name = TextBoxName.Text;
                var sourceAction = (UploadSourceAction) ComboBoxSourceAction.SelectedValue;
                var targetAction = (UploadTargetAction) ComboBoxTargetAction.SelectedValue;
                var propertyAction = (PropertyAction) ComboBoxPropertyAction.SelectedValue;
                foreach (string file in ListBoxFiles.Items)
                {
                    if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
                    {
                        Uploader.UploadToLibrary(file, url, libPath, name,
                                                 sourceAction, targetAction, propertyAction);
                    }
                    else
                    {
                        Uploader.UploadToLibrary(file, url, libPath, name,
                                                 sourceAction, targetAction, propertyAction, _username, _password);
                    }
                }
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                if (ex.InnerException != null)
                {
                    message += "\r\n" + ex.InnerException.Message;
                }
                if (ex.StackTrace != null)
                {
                    message += "\r\n" + ex.StackTrace;
                }

                MessageBox.Show(message);
            }
        }
    }
}
