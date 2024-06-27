namespace Parser_GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;

public partial class MainPage : ContentPage
{
	Unzip zipper;
    string eventName;
    string targetPath;
    StreamReader file;
    JsonTextReader reader;
    JObject o2;
    string adbPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName, "platform-tools", "adb");
    int selectedFileIndex;
    int selectedFolderIndex;
    string filePath;
    IFileSaver fileSaver;

    public MainPage(IFileSaver fileSaver)
	{
		InitializeComponent();
        pathPicker.SelectedIndexChanged += OnPathPickerSelectedIndexChanged;
        eventPicker.SelectedIndexChanged += OnEventPickerSelectedIndexChanged;
        this.fileSaver = fileSaver;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Display an await message when the page appears
        await DisplayAlert("Welcome", "To use: \n 1) Upload a file \n 2) Select a Run \n 3) Select an Event \n 4) Press Confirm", "OK");
    }
    
    private async void OnCounterClicked(object sender, EventArgs e)
	{
		var result = await FilePicker.PickAsync(new PickOptions
		{PickerTitle = "Pick a file" });

		string filePath = result.FullPath;
		path.Text = "Desired File: "+filePath.Substring(filePath.LastIndexOf('/')+1);
        zipper = new Unzip(filePath);
        List<string> folders = zipper.getFolderList();
        for (int i = 0; i < folders.Count; i++)
        {
            int lastSlashIndex = folders[i].LastIndexOf('/');
            folders[i] = folders[i].Substring(lastSlashIndex + 1);
        }
        pathPicker.ItemsSource = folders;        
    }
    private void OnPathPickerSelectedIndexChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;
        selectedFolderIndex = picker.SelectedIndex;
        eventPicker.IsVisible = true;
        List<string> files = zipper.getFilesList(selectedFolderIndex);
        List<string> displayFiles = new List<string>(files);
        for (int i = 0; i < displayFiles.Count; i++)
        {
            int lastSlashIndex = displayFiles[i].LastIndexOf('/');
            displayFiles[i] = displayFiles[i].Substring(lastSlashIndex + 1);
        }
        eventPicker.ItemsSource = displayFiles;
      
    }
    private void OnEventPickerSelectedIndexChanged(object sender, EventArgs e)
    {
        List<string> files = zipper.getFilesList(selectedFolderIndex);
        selectedFileIndex = eventPicker.SelectedIndex; //ISSUES
        filePath = files[selectedFileIndex];
    }

    async void OnConfirmClicked(System.Object sender, System.EventArgs e)
    {
        var watch = new Stopwatch();
        watch.Start();
        string tempFolder = Path.GetTempFileName();
        File.Delete(tempFolder);
        Directory.CreateDirectory(tempFolder);
        targetPath = tempFolder;

        zipper.Run(filePath);
        string destination = zipper.currentFile;
        string[] split = destination.Split('/'); 
        eventName = split.Last();
        string text = File.ReadAllText($"{destination}");
        string newText = text.Replace("nan,", "null,");
        File.WriteAllText($"{zipper.currentFile}.tmp", newText);
        file = File.OpenText($"{zipper.currentFile}.tmp");
        reader = new JsonTextReader(file);

        var deletionPath = Path.GetDirectoryName(targetPath);
        targetPath += "/" + eventName;

        o2 = (JObject)JToken.ReadFrom(reader);
        IGTracks t = new IGTracks(o2, targetPath); 
        IGBoxes b = new IGBoxes(o2, targetPath); 
        file.Close();
        var totaljson = JsonConvert.SerializeObject(new { b.jetDatas, b.EEData, b.EBData, b.ESData, b.HEData, b.HBData, b.HOData, b.HFData, b.superClusters, b.muonChamberDatas, t.globalMuonDatas, t.trackerMuonDatas, t.standaloneMuonDatas, t.electronDatas, t.trackDatas }, Formatting.Indented);
        File.WriteAllText($"{targetPath}//totalData.json", totaljson);
        File.WriteAllText($"{Directory.GetCurrentDirectory()}//totalData.json", totaljson);
        await DisplayAlert("Alert", "Data Processed. Starting Upload.", "OK");
        string temp_name = Path.GetFileNameWithoutExtension(Path.GetFileName(targetPath)); // i.e. tmp900y20.tmp
        var cleanup = new Cleanup(temp_name, deletionPath);


        AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
        {
            cleanup.callCleanUp();
        };
        zipper.destroyStorage();
        try
        {
            Communicate bridge = new Communicate(adbPath);
            if (bridge.UploadFiles(targetPath))
            {
                await DisplayAlert("Alert", $"Your files have been uploaded. Total Execution Time: {watch.ElapsedMilliseconds} ms", "OK");
            }
            await DisplayAlert("Alert", "An ADB exception has been thrown.\nPlease check that the Oculus is connected to the computer.", "OK");

        }
        catch (Exception exception)
        {
            //Environment.Exit(1);
            await DisplayAlert("Alert", "An unexpected error occurred: " + exception.Message, "OK");
        }

    }


    private async void OnSaveFilesClicked(object sender, EventArgs e)
    {

        try
        {
            string tempFolder = Path.GetTempFileName();
            File.Delete(tempFolder);
            Directory.CreateDirectory(tempFolder);
            targetPath = tempFolder;

            zipper.Run(filePath);
            string destination = zipper.currentFile;
            string[] split = destination.Split('/');
            eventName = split.Last();
            string text = File.ReadAllText($"{destination}");

            string newText = text.Replace("nan,", "null,");
            File.WriteAllText($"{zipper.currentFile}.tmp", newText);

            file = File.OpenText($"{zipper.currentFile}.tmp");
            reader = new JsonTextReader(file);
            var deletionPath = Path.GetDirectoryName(targetPath);
            targetPath += "/" + eventName;

            o2 = (JObject)JToken.ReadFrom(reader);
            IGTracks t = new IGTracks(o2, targetPath);
            IGBoxes b = new IGBoxes(o2, targetPath);
            file.Close();

            var totaljson = JsonConvert.SerializeObject(new { b.jetDatas, b.EEData, b.EBData, b.ESData, b.HEData, b.HBData, b.HOData, b.HFData, b.superClusters, b.muonChamberDatas, t.globalMuonDatas, t.trackerMuonDatas, t.standaloneMuonDatas, t.electronDatas, t.trackDatas }, Formatting.Indented);

            File.WriteAllText($"{targetPath}//totalData.json", totaljson);
            File.WriteAllText($"{Directory.GetCurrentDirectory()}//totalData.json", totaljson);
            string temp_name = Path.GetFileNameWithoutExtension(Path.GetFileName(targetPath)); // i.e. tmp900y20.tmp
            var cleanup = new Cleanup(temp_name, deletionPath);

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var file in new DirectoryInfo(targetPath).GetFiles())
                    {
                        var entry = archive.CreateEntry(file.Name);
                        using (var entryStream = entry.Open())
                        using (var fileStream = file.OpenRead())
                        {
                            await fileStream.CopyToAsync(entryStream);
                        }
                    }
                }

                // Save the zip file using fileSaver.SaveAsync
                memoryStream.Seek(0, SeekOrigin.Begin);
                await fileSaver.SaveAsync("data.zip", memoryStream,default);
            }

            Console.WriteLine("Zip file saved successfully.");
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                cleanup.callCleanUp();
            };
            zipper.destroyStorage();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

    }
}


