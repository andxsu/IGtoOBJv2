using System.IO;
using System.IO.Compression;

namespace Parser_GUI
{
    class Unzip
    {
        private string directoryName { get; set; }
        public string currentFile;
        private string tempStorageDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"/Temp/IGtoOBJGenExtraction";
        private string tempTransmitDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"/Temp/IGtoOBJGenTransmission";
        public Unzip(string filename)
        {
            if (Directory.Exists(tempStorageDirectory))
            {
                Directory.Delete(tempStorageDirectory, true);
                Directory.CreateDirectory(tempStorageDirectory);
            }
            else
            {
                Directory.CreateDirectory(tempStorageDirectory);
            }
            string extractPath = tempDirectoryPath();
            ZipFile.ExtractToDirectory(filename, tempStorageDirectory);
            directoryName = tempStorageDirectory;

        }
        public void Run(string filepath)
        {
            //string runFolder = selectFolderFromFolder(directoryName + "\\Events");
            //string file = selectFileFromFolder(runFolder);
            //currentFile = file;
            currentFile = filepath;
        }
        public void destroyStorage()
        {
            Directory.Delete(directoryName, true);
            Console.WriteLine("Temp storage cleared!");
        }
        private static string tempDirectoryPath()
        {
            string tempFolder = Path.GetTempFileName();
            File.Delete(tempFolder);
            Directory.CreateDirectory(tempFolder);
            return tempFolder;
        }
        public List<string> getFolderList()
        {
            try
            {
                string path = directoryName + "/Events";
                string[] folders = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
                List<string> foldersList = new List<string>();
                foreach (string folder in folders)
                {
                    foldersList.Add(folder);
                }
                return foldersList;
            }
            catch (Exception ex)
            {
                // Handle the exception
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }

        public List<string> getFilesList(int index)
        {
            try
            {
                string path = directoryName + "/Events";
                string[] folders = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
                string[] files = Directory.GetFiles(folders[index]);
                List<string> filesList = new List<string>();
                foreach (string file in files)
                {
                    filesList.Add(file);
                }
                return filesList;
            }
            catch (Exception ex)
            {
                // Handle the exception
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }


        public string selectFolderFromFolder(string path)
        {
            string[] folders = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
            
            foreach (string folder in folders)
            {
                int index = Array.IndexOf(folders, folder);
                Console.WriteLine($"{index}) {folder}");
            }
            Console.WriteLine("Enter ID # of desired path:");
            int selection = int.Parse(Console.ReadLine());
            return folders[selection];
        }
        public static string selectFileFromFolder(string path)
        {
            string[] files = Directory.GetFiles(path);
            foreach (string file in files)
            {
                int index = Array.IndexOf(files, file);
                Console.WriteLine($"{index}) {file}");
            }
            Console.WriteLine("Enter ID # of desired event file:");

            int selection = int.Parse(Console.ReadLine());

            Console.WriteLine(files[selection]);

            return files[selection];
        }
    }
}