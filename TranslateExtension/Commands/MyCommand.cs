using Microsoft.VisualStudio.Text;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace TranslateExtension
{
    [Command(PackageIds.MyCommand)]
    internal sealed class MyCommand : BaseCommand<MyCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await Package.JoinableTaskFactory.SwitchToMainThreadAsync();

            DocumentView documentView = await VS.Documents.GetActiveDocumentViewAsync();
            if(documentView == null)
            {
                return;
            }

            SnapshotPoint position = documentView.TextView.Caret.Position.BufferPosition;
            try
            {
                var selectedText = documentView.TextBuffer.CurrentSnapshot.GetText(documentView.TextView.Selection.SelectedSpans.First());
                var translatedText = await TranslateTextAsync(selectedText);

                documentView.TextBuffer?.Insert(position, translatedText);
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync($"Error during translation of the text: {Environment.NewLine}{ex.Message}");
            }
        }

        private async Task<string> TranslateTextAsync(string sourceText)
        {
            var sourceLang = "en";
            var targetLang = "de";
            HttpClient httpClient = new HttpClient();

            var url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl=" + sourceLang + "&tl=" + targetLang + "&dt=t&q=" + Uri.EscapeDataString(sourceText);

            var result = await httpClient.GetAsync(url);
            if (!result.IsSuccessStatusCode)
            {
                return sourceText;
            }

            var content = await result.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(content);

            var translatedElement = jsonDocument
                .RootElement
                .EnumerateArray()
                .FirstOrDefault() // get first element from root array
                .EnumerateArray()
                .FirstOrDefault() // get first element from first inner array
                .EnumerateArray()
                .FirstOrDefault()
                .ToString();

            return translatedElement ?? sourceText;
        }
    }
}
