using System.ComponentModel;
using System.Runtime.CompilerServices;
using PEdit.Utilities;

namespace PEdit.ViewModels
{
    public class SearchViewModel : INotifyPropertyChanged
    {
        private string _searchText;
        private string _replaceText;
        private bool _isRegex;
        private string _logMessage;

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); }
        }

        public string ReplaceText
        {
            get => _replaceText;
            set { _replaceText = value; OnPropertyChanged(); }
        }

        public bool IsRegex
        {
            get => _isRegex;
            set { _isRegex = value; OnPropertyChanged(); }
        }

        public string LogMessage
        {
            get => _logMessage;
            set { _logMessage = value; OnPropertyChanged(); }
        }

        public RelayCommand FindNextCommand { get; set; }
        public RelayCommand FindCurrentCommand { get; set; }
        public RelayCommand FindAllCommand { get; set; }

        public RelayCommand ReplaceNextCommand { get; set; }
        public RelayCommand ReplaceCurrentCommand { get; set; }
        public RelayCommand ReplaceAllCommand { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}