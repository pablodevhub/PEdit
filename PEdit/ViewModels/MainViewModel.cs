using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using PEdit.Models;
using PEdit.Services;
using PEdit.Utilities;
using PEdit.Views;

namespace PEdit.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<DocumentModel> Documents { get; set; } = new ObservableCollection<DocumentModel>();
        public ObservableCollection<TreeItem> TreeItems { get; set; } = new ObservableCollection<TreeItem>();
        public ConfigurationService Config => ConfigurationService.Instance;

        private int _untitledCounter = 1;
        private int _statusMessageId = 0;

        private DocumentModel _selectedDocument;
        public DocumentModel SelectedDocument
        {
            get => _selectedDocument;
            set { _selectedDocument = value; OnPropertyChanged(); }
        }

        private Visibility _explorerVisibility = Visibility.Collapsed;
        public Visibility ExplorerVisibility
        {
            get => _explorerVisibility;
            set { _explorerVisibility = value; OnPropertyChanged(); }
        }

        private string _statusActionMessage = "";
        public string StatusActionMessage
        {
            get => _statusActionMessage;
            set { _statusActionMessage = value; OnPropertyChanged(); }
        }

        public RelayCommand NewTabCommand { get; }
        public RelayCommand CloseTabCommand { get; }
        public RelayCommand CloseSpecificTabCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand OpenFileCommand { get; }
        public RelayCommand OpenFolderCommand { get; }
        public RelayCommand PreviewMarkdownCommand { get; }

        public MainViewModel()
        {
            NewTabCommand = new RelayCommand(_ => AddNewDocument());
            CloseTabCommand = new RelayCommand(_ => CloseDocument(SelectedDocument), _ => SelectedDocument != null);
            CloseSpecificTabCommand = new RelayCommand(doc => CloseDocument(doc as DocumentModel));
            SaveCommand = new RelayCommand(_ => SaveCurrentDocument(), _ => SelectedDocument != null);
            OpenFileCommand = new RelayCommand(_ => ExecuteOpenFile());
            OpenFolderCommand = new RelayCommand(_ => ExecuteOpenFolder());

            PreviewMarkdownCommand = new RelayCommand(_ => ExecutePreviewMarkdown(), _ => SelectedDocument != null && SelectedDocument.FileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase));

            ProcessStartupArgs();
            if (Documents.Count == 0) AddNewDocument();
        }

        public async void ShowActionStatus(string message)
        {
            StatusActionMessage = message;
            int currentId = ++_statusMessageId;

            await Task.Delay(5000);

            if (_statusMessageId == currentId)
            {
                StatusActionMessage = "";
            }
        }

        private void ExecutePreviewMarkdown()
        {
            if (SelectedDocument == null) return;
            var previewWindow = new PEdit.Views.MarkdownPreviewWindow(SelectedDocument);
            previewWindow.Show();
        }

        private void ProcessStartupArgs()
        {
            if (App.StartupArgs == null || App.StartupArgs.Length == 0) return;

            bool isFirstFolder = true;
            foreach (var arg in App.StartupArgs)
            {
                if (File.Exists(arg))
                {
                    OpenFile(arg);
                }
                else if (Directory.Exists(arg))
                {
                    // Carica la prima cartella pulendo il tree, le successive le aggiunge in coda
                    LoadDirectoryTree(arg, append: !isFirstFolder);
                    isFirstFolder = false;
                }
            }
        }

        private void AddNewDocument()
        {
            var doc = new DocumentModel { TextContent = "", TempTitle = $"Untitled {_untitledCounter++}" };
            Documents.Add(doc);
            SelectedDocument = doc;
        }

        private void ExecuteOpenFile()
        {
            var dialog = new OpenFileDialog { Filter = "All files (*.*)|*.*", Multiselect = true };
            if (dialog.ShowDialog() == true)
            {
                foreach (string file in dialog.FileNames) OpenFile(file);
            }
        }

        private void ExecuteOpenFolder()
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadDirectoryTree(dialog.FolderName, append: false);
            }
        }

        // Il parametro append permette di aggiungere più radici al TreeView
        public void LoadDirectoryTree(string path, bool append = false)
        {
            ExplorerVisibility = Visibility.Visible;
            if (!append)
            {
                TreeItems.Clear();
            }
            TreeItems.Add(new FileSystemService().GetDirectoryTree(path));
        }

        private void CloseDocument(DocumentModel docToClose)
        {
            if (docToClose == null) return;

            if (docToClose.IsDirty)
            {
                SelectedDocument = docToClose;
                var result = MessageBox.Show($"Save changes to {docToClose.FileName}?", "Save", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes) SaveCurrentDocument();
                else if (result == MessageBoxResult.Cancel) return;
            }

            int currentIndex = Documents.IndexOf(docToClose);
            if (currentIndex == -1) return;

            bool wasSelected = (SelectedDocument == docToClose);

            if (Documents.Count == 1)
            {
                var newDoc = new DocumentModel { TextContent = "", TempTitle = $"Untitled {_untitledCounter++}" };
                Documents.Add(newDoc);
                SelectedDocument = newDoc;
                Documents.Remove(docToClose);
            }
            else
            {
                if (wasSelected)
                {
                    int newIndex = (currentIndex == Documents.Count - 1) ? currentIndex - 1 : currentIndex + 1;
                    SelectedDocument = Documents[newIndex];
                }
                Documents.Remove(docToClose);
            }
        }

        private void SaveCurrentDocument()
        {
            if (SelectedDocument == null) return;
            if (string.IsNullOrEmpty(SelectedDocument.FilePath))
            {
                var dialog = new SaveFileDialog { Filter = "All files (*.*)|*.*" };
                if (dialog.ShowDialog() == true) SelectedDocument.FilePath = dialog.FileName;
                else return;
            }
            File.WriteAllText(SelectedDocument.FilePath, SelectedDocument.TextContent);
            SelectedDocument.IsDirty = false;
        }

        public void OpenFile(string path)
        {
            var existingDoc = Documents.FirstOrDefault(d => d.FilePath == path);
            if (existingDoc != null) { SelectedDocument = existingDoc; return; }
            if (IsBinaryFile(path)) return;

            var content = File.ReadAllText(path);
            var doc = new DocumentModel { FilePath = path, TextContent = content, IsDirty = false };
            Documents.Add(doc);
            SelectedDocument = doc;
        }

        private bool IsBinaryFile(string filePath)
        {
            try
            {
                using (var stream = File.OpenRead(filePath))
                {
                    int bytesToCheck = Math.Min(1024, (int)stream.Length);
                    if (bytesToCheck == 0) return false;
                    byte[] buffer = new byte[bytesToCheck];
                    stream.Read(buffer, 0, bytesToCheck);
                    return buffer.Contains((byte)0);
                }
            }
            catch { return false; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}