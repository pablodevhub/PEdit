using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using PEdit.ViewModels;
using PEdit.Models;

namespace PEdit
{
    public partial class MainWindow : Window
    {
        [DllImport("dwmapi.dll")]
        internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        internal const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, (s, e) => MenuItem_Find_Click(null, null)));
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).EnsureHandle();
            int dark = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                if (System.Linq.Enumerable.Any(vm.Documents, doc => doc.IsDirty))
                {
                    MessageBox.Show("Ci sono file con modifiche non salvate. Salva e chiudi i tab prima di uscire.", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
                    e.Cancel = true;
                }
            }
            base.OnClosing(e);
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e) => this.Close();

        private void MenuItem_Options_Click(object sender, RoutedEventArgs e)
        {
            var optionsWindow = new Views.OptionsWindow { Owner = this };
            optionsWindow.ShowDialog();
        }

        private void MenuItem_Find_Click(object sender, RoutedEventArgs e)
        {
            var searchWindow = new Views.SearchWindow(this);
            searchWindow.Show();
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (DataContext is MainViewModel vm)
                {
                    bool isFirstFolder = true;
                    foreach (var file in files)
                    {
                        if (Directory.Exists(file))
                        {
                            vm.LoadDirectoryTree(file, append: !isFirstFolder);
                            isFirstFolder = false;
                        }
                        else
                        {
                            vm.OpenFile(file);
                        }
                    }
                }
            }
        }

        private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.TreeView treeView && treeView.SelectedItem is TreeItem selectedNode && !selectedNode.IsDirectory)
            {
                if (DataContext is MainViewModel vm) vm.OpenFile(selectedNode.FullPath);
            }
        }

        private void TextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ICSharpCode.AvalonEdit.TextEditor editor)
            {
                if (editor.DataContext is DocumentModel doc)
                {
                    if (editor.Text != doc.TextContent) editor.Text = doc.TextContent ?? string.Empty;
                }

                if (editor.Tag?.ToString() != "EventAttached")
                {
                    editor.TextArea.Caret.PositionChanged += (s, args) =>
                    {
                        if (editor.DataContext is DocumentModel d)
                        {
                            d.CaretLine = editor.TextArea.Caret.Line;
                            d.CaretColumn = editor.TextArea.Caret.Column;
                        }
                    };
                    editor.Tag = "EventAttached";
                }
            }
        }

        private void TextEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is ICSharpCode.AvalonEdit.TextEditor editor && editor.DataContext is DocumentModel doc)
            {
                if (editor.Text != doc.TextContent) editor.Text = doc.TextContent ?? string.Empty;
            }
        }

        private void TextEditor_TextChanged(object sender, EventArgs e)
        {
            if (sender is ICSharpCode.AvalonEdit.TextEditor editor && editor.DataContext is DocumentModel doc)
            {
                if (doc.TextContent != editor.Text)
                {
                    doc.TextContent = editor.Text;
                    doc.IsDirty = true;
                }
            }
        }

        private void TextEditor_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && Services.ConfigurationService.Instance.SelectWholeWordOnDoubleClick)
            {
                if (sender is ICSharpCode.AvalonEdit.TextEditor editor)
                {
                    e.Handled = true;
                    var pos = editor.GetPositionFromPoint(e.GetPosition(editor));
                    if (pos.HasValue)
                    {
                        int offset = editor.Document.GetOffset(pos.Value.Line, pos.Value.Column);
                        string text = editor.Text;
                        if (offset >= text.Length) return;

                        int start = offset;
                        while (start > 0 && !char.IsWhiteSpace(text[start - 1])) start--;
                        int end = offset;
                        while (end < text.Length && !char.IsWhiteSpace(text[end])) end++;

                        if (end > start) editor.Select(start, end - start);
                    }
                }
            }
        }

        private void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                var vm = DataContext as MainViewModel;
                if (vm == null) return;

                switch (e.Key)
                {
                    case Key.C: vm.ShowActionStatus("Copied"); break;
                    case Key.V: vm.ShowActionStatus("Pasted"); break;
                    case Key.Z: vm.ShowActionStatus("Undo"); break;
                    case Key.Y: vm.ShowActionStatus("Redo"); break;
                    case Key.A: vm.ShowActionStatus("Selected All"); break;
                }
            }
        }
    }
}