using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Text;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Document;
using PEdit.Services;
using PEdit.ViewModels;

namespace PEdit.Views
{
    public partial class SearchWindow : Window
    {
        private MainWindow _mainWindow;
        private SearchService _searchService;

        public SearchWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _searchService = new SearchService();

            var vm = new SearchViewModel();
            // Comandi di Ricerca
            vm.FindNextCommand = new Utilities.RelayCommand(_ => ExecuteFindNext());
            vm.FindCurrentCommand = new Utilities.RelayCommand(_ => ExecuteFindCurrent());
            vm.FindAllCommand = new Utilities.RelayCommand(_ => ExecuteFindAll());

            // Comandi di Sostituzione
            vm.ReplaceNextCommand = new Utilities.RelayCommand(_ => ExecuteReplaceNext(), _ => CanReplace(vm));
            vm.ReplaceCurrentCommand = new Utilities.RelayCommand(_ => ExecuteReplaceCurrent(), _ => CanReplace(vm));
            vm.ReplaceAllCommand = new Utilities.RelayCommand(_ => ExecuteReplaceAll(), _ => CanReplace(vm));

            DataContext = vm;

            // 1. Forza sempre il default "Start from beginning" quando si apre la finestra
            ConfigurationService.Instance.SearchFromBeginning = true;

            // 2. Assicura il focus fisico usando il Dispatcher per aspettare il rendering
            this.Loaded += (s, e) =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (SearchInputBox != null)
                    {
                        SearchInputBox.Focus();
                        SearchInputBox.SelectAll();
                    }
                }), System.Windows.Threading.DispatcherPriority.Input);
            };
        }

        // INTERCETTA IL TASTO INVIO PER LA RICERCA VELOCE
        private void SearchInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true; // Evita il suono "ding" di Windows
                ExecuteFindNext();
            }
        }

        private bool CanReplace(SearchViewModel vm)
        {
            return !string.IsNullOrEmpty(vm.SearchText) && !string.IsNullOrEmpty(vm.ReplaceText);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).EnsureHandle();
            int dark = 1;
            MainWindow.DwmSetWindowAttribute(hwnd, MainWindow.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
        }

        private ICSharpCode.AvalonEdit.TextEditor GetActiveEditor()
        {
            var tabControl = _mainWindow.FindName("MainTabControl") as System.Windows.Controls.TabControl;
            if (tabControl == null) return null;
            return VisualTreeHelperUtilities.FindVisualChild<ICSharpCode.AvalonEdit.TextEditor>(tabControl);
        }

        // --- GESTIONE COMANDI DI RICERCA ---

        private void ExecuteFindCurrent()
        {
            var vm = (SearchViewModel)DataContext;
            var mainVm = _mainWindow.DataContext as MainViewModel;
            if (mainVm == null || string.IsNullOrEmpty(vm.SearchText) || mainVm.SelectedDocument == null) return;

            var currentEditor = GetActiveEditor();
            if (currentEditor == null) return;

            bool searchFromBeginning = ConfigurationService.Instance.SearchFromBeginning;
            int currentCaretOffset = currentEditor.SelectionStart + currentEditor.SelectionLength;

            var results = _searchService.FindAll(currentEditor.Text, vm.SearchText, vm.IsRegex);
            if (results.Any())
            {
                SearchResult validMatch = null;
                if (!searchFromBeginning) validMatch = results.FirstOrDefault(r => r.Index >= currentCaretOffset);
                if (validMatch == null) validMatch = results.FirstOrDefault();

                if (validMatch != null)
                {
                    ClearHighlights(currentEditor);
                    currentEditor.Select(validMatch.Index, validMatch.Length);
                    currentEditor.ScrollTo(currentEditor.Document.GetLocation(validMatch.Index).Line, currentEditor.Document.GetLocation(validMatch.Index).Column);
                    vm.LogMessage = $"Match {results.IndexOf(validMatch) + 1} of {results.Count} (Tab: {mainVm.SelectedDocument.FileName})";
                    ConfigurationService.Instance.SearchFromBeginning = false;
                    return;
                }
            }
            vm.LogMessage = "No matches found in the current tab.";
        }

        private void ExecuteFindNext()
        {
            var vm = (SearchViewModel)DataContext;
            var mainVm = _mainWindow.DataContext as MainViewModel;
            if (mainVm == null || string.IsNullOrEmpty(vm.SearchText) || mainVm.Documents.Count == 0) return;

            int startingTabCount = mainVm.Documents.Count;
            int currentTabStartIndex = mainVm.Documents.IndexOf(mainVm.SelectedDocument);
            if (currentTabStartIndex == -1) currentTabStartIndex = 0;

            bool searchFromBeginning = ConfigurationService.Instance.SearchFromBeginning;
            var currentEditor = GetActiveEditor();
            int currentCaretOffset = currentEditor != null ? currentEditor.SelectionStart + currentEditor.SelectionLength : 0;

            for (int i = 0; i <= startingTabCount; i++)
            {
                int targetTabIndex = (currentTabStartIndex + i) % startingTabCount;
                var docModel = mainVm.Documents[targetTabIndex];
                bool isCurrentTab = (targetTabIndex == currentTabStartIndex);

                string textToSearch = (isCurrentTab && currentEditor != null) ? currentEditor.Text : (docModel.TextContent ?? "");
                var results = _searchService.FindAll(textToSearch, vm.SearchText, vm.IsRegex);

                if (results.Any())
                {
                    SearchResult validMatch = null;
                    if (i == 0 && !searchFromBeginning) validMatch = results.FirstOrDefault(r => r.Index >= currentCaretOffset);
                    else if (i == startingTabCount) validMatch = results.FirstOrDefault(r => r.Index < currentCaretOffset);
                    else validMatch = results.FirstOrDefault();

                    if (validMatch != null)
                    {
                        if (!isCurrentTab)
                        {
                            mainVm.SelectedDocument = docModel;
                            Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
                        }

                        var targetEditor = GetActiveEditor();
                        if (targetEditor != null)
                        {
                            if (targetEditor.Text != textToSearch) targetEditor.Text = textToSearch;
                            ClearHighlights(targetEditor);
                            targetEditor.Select(validMatch.Index, validMatch.Length);
                            targetEditor.ScrollTo(targetEditor.Document.GetLocation(validMatch.Index).Line, targetEditor.Document.GetLocation(validMatch.Index).Column);
                            vm.LogMessage = $"Match {results.IndexOf(validMatch) + 1} of {results.Count} (Tab: {docModel.FileName})";
                            ConfigurationService.Instance.SearchFromBeginning = false;
                            return;
                        }
                    }
                }
            }
            vm.LogMessage = "No matches found in any open tab.";
        }

        private void ExecuteFindAll()
        {
            var vm = (SearchViewModel)DataContext;
            var mainVm = _mainWindow.DataContext as MainViewModel;
            if (mainVm == null || string.IsNullOrEmpty(vm.SearchText)) return;

            StringBuilder report = new StringBuilder();
            int grandTotal = 0;

            foreach (var docModel in mainVm.Documents)
            {
                string text = docModel.TextContent ?? "";
                if (docModel == mainVm.SelectedDocument)
                {
                    var activeEditor = GetActiveEditor();
                    if (activeEditor != null) text = activeEditor.Text;
                }

                var results = _searchService.FindAll(text, vm.SearchText, vm.IsRegex);
                if (results.Any())
                {
                    report.AppendLine($"- {docModel.FileName}: {results.Count} matches");
                    grandTotal += results.Count;

                    if (docModel == mainVm.SelectedDocument)
                    {
                        var editor = GetActiveEditor();
                        if (editor != null)
                        {
                            ClearHighlights(editor);
                            editor.TextArea.TextView.LineTransformers.Add(new SearchHighlightTransformer(vm.SearchText, vm.IsRegex));
                            editor.TextArea.TextView.Redraw();
                        }
                    }
                }
            }

            if (grandTotal > 0)
            {
                report.AppendLine($"\n--- Found {grandTotal} occurrences across all documents.");
                vm.LogMessage = report.ToString();
            }
            else vm.LogMessage = "No matches found.";
        }

        // --- GESTIONE COMANDI DI SOSTITUZIONE (REPLACE) ---

        private void ExecuteReplaceNext()
        {
            var vm = (SearchViewModel)DataContext;
            var mainVm = _mainWindow.DataContext as MainViewModel;
            if (mainVm == null) return;

            ExecuteFindNext();

            var editor = GetActiveEditor();
            if (editor != null && editor.SelectionLength > 0)
            {
                editor.Document.Replace(editor.SelectionStart, editor.SelectionLength, vm.ReplaceText);
                vm.LogMessage += "\n(Replaced)";
            }
        }

        private void ExecuteReplaceCurrent()
        {
            var vm = (SearchViewModel)DataContext;
            var mainVm = _mainWindow.DataContext as MainViewModel;
            if (mainVm == null || mainVm.SelectedDocument == null) return;

            var editor = GetActiveEditor();
            if (editor == null) return;

            var results = _searchService.FindAll(editor.Text, vm.SearchText, vm.IsRegex);
            if (results.Any())
            {
                using (editor.Document.RunUpdate())
                {
                    for (int i = results.Count - 1; i >= 0; i--)
                    {
                        editor.Document.Replace(results[i].Index, results[i].Length, vm.ReplaceText);
                    }
                }
                vm.LogMessage = $"Replaced {results.Count} occurrences in {mainVm.SelectedDocument.FileName}.";
            }
            else vm.LogMessage = "No matches found to replace.";
        }

        private void ExecuteReplaceAll()
        {
            var vm = (SearchViewModel)DataContext;
            var mainVm = _mainWindow.DataContext as MainViewModel;
            if (mainVm == null) return;

            StringBuilder report = new StringBuilder();
            int grandTotal = 0;
            var initialTab = mainVm.SelectedDocument;

            foreach (var docModel in mainVm.Documents)
            {
                mainVm.SelectedDocument = docModel;
                Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);

                var editor = GetActiveEditor();
                if (editor == null) continue;

                var results = _searchService.FindAll(editor.Text, vm.SearchText, vm.IsRegex);
                if (results.Any())
                {
                    using (editor.Document.RunUpdate())
                    {
                        for (int i = results.Count - 1; i >= 0; i--)
                        {
                            editor.Document.Replace(results[i].Index, results[i].Length, vm.ReplaceText);
                        }
                    }
                    report.AppendLine($"- {docModel.FileName}: Replaced {results.Count} matches");
                    grandTotal += results.Count;
                }
            }

            mainVm.SelectedDocument = initialTab;

            if (grandTotal > 0)
            {
                report.AppendLine($"\n--- Replaced {grandTotal} total occurrences.");
                vm.LogMessage = report.ToString();
            }
            else vm.LogMessage = "No matches found to replace.";
        }

        private void ClearHighlights(ICSharpCode.AvalonEdit.TextEditor editor)
        {
            var transformers = editor.TextArea.TextView.LineTransformers.OfType<SearchHighlightTransformer>().ToList();
            foreach (var t in transformers) editor.TextArea.TextView.LineTransformers.Remove(t);
            editor.TextArea.TextView.Redraw();
        }
    }

    // --- CLASSI DI SUPPORTO INTERNE AL NAMESPACE ---

    public static class VisualTreeHelperUtilities
    {
        public static T FindVisualChild<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T) return (T)child;
                    T childItem = FindVisualChild<T>(child);
                    if (childItem != null) return childItem;
                }
            }
            return null;
        }
    }

    public class SearchHighlightTransformer : DocumentColorizingTransformer
    {
        private readonly string _searchTerm;
        private readonly bool _isRegex;

        public SearchHighlightTransformer(string searchTerm, bool isRegex)
        {
            _searchTerm = searchTerm;
            _isRegex = isRegex;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            if (string.IsNullOrEmpty(_searchTerm)) return;
            string text = CurrentContext.Document.GetText(line);

            if (_isRegex)
            {
                try
                {
                    var matches = System.Text.RegularExpressions.Regex.Matches(text, _searchTerm);
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        ApplyHighlight(line.Offset + match.Index, match.Length);
                    }
                }
                catch { }
            }
            else
            {
                int index = text.IndexOf(_searchTerm, StringComparison.OrdinalIgnoreCase);
                while (index >= 0)
                {
                    ApplyHighlight(line.Offset + index, _searchTerm.Length);
                    index = text.IndexOf(_searchTerm, index + _searchTerm.Length, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        private void ApplyHighlight(int startOffset, int length)
        {
            base.ChangeLinePart(startOffset, startOffset + length, element =>
            {
                element.TextRunProperties.SetBackgroundBrush(new SolidColorBrush(Color.FromArgb(80, 0, 122, 204)));
            });
        }
    }
}