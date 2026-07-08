using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using PEdit.Services;
using PEdit.Utilities;

namespace PEdit.ViewModels
{
    public class OptionsViewModel : INotifyPropertyChanged
    {
        public ConfigurationService Config => ConfigurationService.Instance;

        public List<string> SystemFonts => System.Windows.Media.Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(f => f).ToList();

        public RelayCommand PickBgColorCommand { get; }
        public RelayCommand PickFgColorCommand { get; }
        public RelayCommand CloseCommand { get; }

        public OptionsViewModel()
        {
            PickBgColorCommand = new RelayCommand(_ => ChooseColor(true));
            PickFgColorCommand = new RelayCommand(_ => ChooseColor(false));
            CloseCommand = new RelayCommand(CloseWindow);
        }

        private void ChooseColor(bool isBackground)
        {
            using (var dialog = new System.Windows.Forms.ColorDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string hex = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
                    if (isBackground) Config.BackgroundColor = hex;
                    else Config.ForegroundColor = hex;
                }
            }
        }

        private void CloseWindow(object windowParameter)
        {
            if (windowParameter is Window window) window.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}