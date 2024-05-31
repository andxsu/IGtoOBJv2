using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser_GUI
{
    //Cleanup class that handles cleanup processes for the parser.
    internal class Cleanup
    {
        private string temp_name;
        private string deletionPath;

        public Cleanup(string temp_name, string deletionPath)
        {
            this.temp_name = temp_name;
            this.deletionPath = deletionPath;
        }

        public void callCleanUp()
        {
            // Code inside this block will be executed just before the program exits
            Console.WriteLine("Executing cleanup before exit...");
            try
            {
                if (deletionPath != null)
                {
                    CleanupTempFiles();
                }
                else
                {
                    // Handle the case when deletionPath is null (if needed)
                    Console.WriteLine("deletionPath is null. Unable to perform cleanup.");
                }
            }
            catch (Exception ex)
            {
                // Handle the exception
                Console.WriteLine($"An error occurred during cleanup: {ex.Message}");
            }
        }
        public void CleanupTempFiles()
        {
            // Verify if the target directory exists
            if (Directory.Exists(deletionPath))
            {
                //obtain list of items in the target directory
                string[] filesAndFolders = Directory.GetFileSystemEntries(deletionPath);

                foreach (var fileOrFolder in filesAndFolders) //loop through items
                {
                    try
                    {
                        if (fileOrFolder.Contains(temp_name)) //if file or folder has the temp prefix, delete it
                        {
                            if (File.Exists(fileOrFolder))
                            {
                                File.Delete(fileOrFolder); // Delete file
                                Console.WriteLine($"Deleted file: {fileOrFolder}");
                            }
                            else if (Directory.Exists(fileOrFolder))
                            {
                                Directory.Delete(fileOrFolder, true); // Delete directory and its contents recursively
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete: {fileOrFolder}. Error: {ex.Message}");
                    }
                }

                Console.WriteLine("Temp Files Deleted");
            }
            else
            {
                Console.WriteLine($"Target directory not found: {deletionPath}");
            }
        }
    }
}