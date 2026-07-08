using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace PEdit.Models
{
    public class DocumentModel : INotifyPropertyChanged
    {
        private string _filePath;
        private string _textContent;
        private bool _isDirty;
        private int _caretLine = 1;
        private int _caretColumn = 1;

        // Proprietà per il nome progressivo quando il file non è ancora salvato su disco
        public string TempTitle { get; set; } = "Untitled";

        public string FilePath
        {
            get => _filePath;
            set { _filePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(FileName)); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string FileName => string.IsNullOrEmpty(_filePath) ? TempTitle : Path.GetFileName(_filePath);

        public string DisplayName => _isDirty ? $"{FileName}*" : FileName;

        public string TextContent
        {
            get => _textContent;
            set { _textContent = value; OnPropertyChanged(); }
        }

        public bool IsDirty
        {
            get => _isDirty;
            set { _isDirty = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public int CaretLine { get => _caretLine; set { _caretLine = value; OnPropertyChanged(); } }
        public int CaretColumn { get => _caretColumn; set { _caretColumn = value; OnPropertyChanged(); } }
        public string EncodingName => "UTF-8";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}