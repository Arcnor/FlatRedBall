﻿using System.IO;
using Ionic.Zip;
using ToolsUtilities;

namespace FRBDKUpdater.Actions
{
    public class CleanAndZipAction
    {
        public static void CleanAndZip(string userAppPath, string directoryToClear, string zipFile, string extractionPath)
        {
            if (!string.IsNullOrEmpty(directoryToClear))
            {
                var directoryInfo = new DirectoryInfo(directoryToClear);
                // Let's clear it:
                FileManager.DeleteDirectory(directoryToClear);
                System.IO.Directory.CreateDirectory(directoryToClear);
            }

            //Extract downloaded zip if it's a .zip file.  
            if (FileManager.GetExtension(zipFile) == "zip" && !string.IsNullOrEmpty(extractionPath))
            {
                Logger.Log("Unzipping file " + zipFile + " to " + extractionPath);
                using (var zip = new ZipFile(zipFile))
                {
                    zip.ExtractAll(extractionPath, ExtractExistingFileAction.OverwriteSilently);
                }
                Logger.Log("Unzip complete");
            }
        }
    }
}
