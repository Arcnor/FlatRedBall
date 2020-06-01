﻿using System;
using System.Globalization;
using FlatRedBall.IO;
using System.Collections.Generic;

namespace BuildServerUploaderConsole.Processes
{
    class UpdateAssemblyVersions : ProcessStep
    {
        public static readonly string VersionString =
            DateTime.Today.Date.ToString("d", CultureInfo.CreateSpecificCulture("ja-JP")).Replace("/", ".") + "." +
            (int) DateTime.Now.TimeOfDay.TotalMinutes;

        public UpdateAssemblyVersions(IResults results) : base("Updates the AssemblyVersion in all FlatRedBall projects.", results)
        {
        }

        public override void ExecuteStep()
        {
            string engineDirectory = DirectoryHelper.EngineDirectory;

            List<string> folders = new List<string>();
            folders.Add(engineDirectory + @"\FlatRedBallXNA\");
            folders.Add(engineDirectory + @"\FlatRedBallMDX\");
            


            foreach (string folder in folders)
            {
                List<string> files = FileManager.GetAllFilesInDirectory(folder, "cs");

                foreach (string file in files)
                {
                    if (file.ToLower().EndsWith("assemblyinfo.cs"))
                    {
                        ModifyAssemblyInfoVersion(file, VersionString);
                        Results.WriteMessage("Modified " + file + " to " + VersionString);
                    }
                }
            }
            //ModifyVersionInfo(engineDirectory + @"\FlatRedBallXNA\FlatRedBall\Properties\AssemblyInfo.cs", VersionString);
            //Results.WriteMessage("XNA assembly version updated to " + VersionString);

            //ModifyVersionInfo(engineDirectory + @"\FlatRedBallXNA\FlatRedBall.Content\Properties\AssemblyInfo.cs", VersionString);
            //Results.WriteMessage("XNA content assembly version updated to " + VersionString);

            //ModifyVersionInfo(engineDirectory + @"\FlatRedBallMDX\AssemblyInfo.cs", VersionString);
            //Results.WriteMessage("MDX assembly version updated to " + VersionString);

            //ModifyVersionInfo(engineDirectory + @"\FlatSilverBall\FlatSilverBall\Properties\AssemblyInfo.cs", VersionString);
            //Results.WriteMessage("FlatSilverBall assembly version updated to " + VersionString);

            //ModifyAssemblyInfoVersion(DirectoryHelper.FrbdkDirectory + @"\Glue\Glue\Properties\AssemblyInfo.cs", VersionString);
            ModifyCsprojAssemblyInfoVersion(DirectoryHelper.FrbdkDirectory + @"\Glue\Glue\GlueFormsCore.csproj", VersionString);
            ModifyAssemblyInfoVersion(DirectoryHelper.FrbdkDirectory + @"Glue\GlueSaveClasses\Properties\AssemblyInfo.cs", VersionString);

            Results.WriteMessage("Glue assembly versions updated to " + VersionString);



            //Save Version String for uploading
            FileManager.SaveText(VersionString, DirectoryHelper.ReleaseDirectory + @"\SingleDlls\VersionInfo.txt");
            Results.WriteMessage("VersionInfo file created.");
        }

        private static void ModifyAssemblyInfoVersion(string assemblyInfoLocation, string versionString)
        {
            string assemblyInfoText = FileManager.FromFileText(assemblyInfoLocation);

            assemblyInfoText = System.Text.RegularExpressions.Regex.Replace(assemblyInfoText,
                        "AssemblyVersion\\(\"[0-9]+\\.[0-9]+\\.[0-9]+\\.[0-9]+",
                        "AssemblyVersion(\"" + versionString);
            assemblyInfoText = System.Text.RegularExpressions.Regex.Replace(assemblyInfoText,
                        "AssemblyFileVersion\\(\"[0-9]+\\.[0-9]+\\.[0-9]+\\.[0-9]+",
                        "AssemblyFileVersion(\"" + versionString);
            FileManager.SaveText(assemblyInfoText, assemblyInfoLocation);

        }

        private static void ModifyCsprojAssemblyInfoVersion(string csprojLocation, string versionString)
        {
            // Look for <Version>1.1.0.0</Version>
            string assemblyInfoText = FileManager.FromFileText(csprojLocation);

            assemblyInfoText = System.Text.RegularExpressions.Regex.Replace(assemblyInfoText,
                        "<Version>[0-9]*.[0-9]*.[0-9]*.[0-9]*</Version>",
                        $"<Version>{versionString}</Version>");
            FileManager.SaveText(assemblyInfoText, assemblyInfoText);

        }

    }
}
