using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Cathei.BakingSheet;
using DocumentFormat.OpenXml.Spreadsheet;
using LocalizationTracker.Tools.GoogleSheet;

namespace LocalizationTracker.Tools.GlossaryTools;

public class SheetImporter
{
    public async void ReadSheetFromGoogle()
    {
        await ReadSheetAsync();
    }
    
    public async void ReadSheetFromJson()
    {
        await ReadSheetFromDisc();
    }

    private async Task ReadSheetAsync()
    {
        var logger = new Logger();
        var sheetContainer = new GlossarySheetContainer(logger);
        if (!File.Exists(AppConfig.Instance.Glossary.GoogleCredentialPath))
        {
            MessageBox.Show("Glossary cant find Credentials!", "Glossary error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);
            return;
        }
        
        string googleCredential = await File.ReadAllTextAsync(AppConfig.Instance.Glossary.GoogleCredentialPath);
        var googleConverter = new GoogleSheetConverter(AppConfig.Instance.Glossary.GlossaryGSheetId, googleCredential);
        await sheetContainer.Bake(googleConverter);
        var jsonConverter = new JsonSheetConverter(AppConfig.Instance.Glossary.GlossaryJSONPath);
        await sheetContainer.Bake(googleConverter);
        if (sheetContainer.Main != null)
        {
            await sheetContainer.Store(jsonConverter);
            //Glossary.Instance.Initialize(sheetContainer.Main, true);
            MessageBox.Show($"Glossary updated! \n Total terms: {sheetContainer.Main.Count}", "Glossary updated!", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.Yes);

        }
        else
        {
            MessageBox.Show("Glossary Main sheet is empty!", "Glossary error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);
        }
    }

    private async Task ReadSheetFromDisc()
    {
        var logger = new Logger();
        var sheetContainer = new GlossarySheetContainer(logger);
        var jsonConverter = new JsonSheetConverter(AppConfig.Instance.Glossary.GlossaryJSONPath);
        await sheetContainer.Bake(jsonConverter);
        //if (sheetContainer.Main != null)
        //{
        //    Glossary.Instance.Initialize(sheetContainer.Main, false);
        //}
    }
}
