using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using PEdit.ViewModels;

namespace PEdit.Views
{
    public partial class OptionsWindow : Window
    {
        public OptionsWindow()
        {
            InitializeComponent();
            DataContext = new OptionsViewModel();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).EnsureHandle();
            int dark = 1;
            MainWindow.DwmSetWindowAttribute(hwnd, MainWindow.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
        }

        private void NumericOnly(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}