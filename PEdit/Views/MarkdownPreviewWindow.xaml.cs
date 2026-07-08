using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using PEdit.Models;
using Markdig;

namespace PEdit.Views
{
    public partial class MarkdownPreviewWindow : Window
    {
        [DllImport("dwmapi.dll")]
        internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        internal const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private readonly DocumentModel _doc;
        private readonly MarkdownPipeline _pipeline;

        public MarkdownPreviewWindow(DocumentModel doc)
        {
            InitializeComponent();
            _doc = doc;
            Title = $"Markdown Preview - {_doc.FileName}";

            // Abilita funzioni avanzate di Markdig (tabelle, liste task, ecc)
            _pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

            // Sottoscriviti alle modifiche di testo in diretta
            _doc.PropertyChanged += Doc_PropertyChanged;

            // Genera la primissima anteprima
            UpdatePreview();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).EnsureHandle();
            int dark = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
        }

        private void Doc_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DocumentModel.TextContent))
            {
                UpdatePreview();
            }
        }

        private void UpdatePreview()
        {
            if (_doc.TextContent == null) return;

            string htmlBody = Markdown.ToHtml(_doc.TextContent, _pipeline);

            // Aggiunti i META tag per forzare l'UTF-8 e il CSS per le scrollbar scure
            string fullHtml = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
                    <meta http-equiv='Content-Type' content='text/html;charset=UTF-8' />
                    <meta charset='UTF-8' />
                    <style>
                        /* Stili per la scrollbar scura nel motore IE nativo */
                        html, body {{
                            scrollbar-face-color: #424242;
                            scrollbar-track-color: #1E1E1E;
                            scrollbar-arrow-color: #D4D4D4;
                            scrollbar-shadow-color: #1E1E1E;
                            scrollbar-darkshadow-color: #1E1E1E;
                        }}
                        
                        /* Stili per la scrollbar scura per motori Webkit (Edge/Chrome) */
                        ::-webkit-scrollbar {{ width: 12px; height: 12px; background: #1E1E1E; }}
                        ::-webkit-scrollbar-thumb {{ background: #424242; border-radius: 4px; }}
                        ::-webkit-scrollbar-thumb:hover {{ background: #4F4F53; }}

                        body {{ 
                            background-color: #1E1E1E; 
                            color: #D4D4D4; 
                            font-family: 'Segoe UI', sans-serif; 
                            padding: 20px; 
                        }}
                        a {{ color: #007ACC; }}
                        code {{ background-color: #2D2D30; padding: 2px 4px; border-radius: 4px; font-family: Consolas; }}
                        pre {{ background-color: #2D2D30; padding: 10px; border-radius: 4px; overflow-x: auto; }}
                        blockquote {{ border-left: 4px solid #3E3E42; padding-left: 15px; color: #A0A0A0; }}
                        table {{ border-collapse: collapse; width: 100%; }}
                        th, td {{ border: 1px solid #3E3E42; padding: 8px; }}
                    </style>
                </head>
                <body>
                    {htmlBody}
                </body>
                </html>";

            MarkdownBrowser.NavigateToString(fullHtml);
        }

        protected override void OnClosed(EventArgs e)
        {
            // Importante staccare l'evento alla chiusura per evitare memory leak
            _doc.PropertyChanged -= Doc_PropertyChanged;
            base.OnClosed(e);
        }
    }
}