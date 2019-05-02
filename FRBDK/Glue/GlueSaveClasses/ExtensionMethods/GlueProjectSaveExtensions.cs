﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.SaveClasses
{
    public static class GlueProjectSaveExtensions
    {
        public static void TestSave(this GlueProjectSave glueProjectSave, string tag)
        {
            string serializedToString;

            var whatToSave = glueProjectSave.ConvertToPartial(tag);

            FileManager.XmlSerialize(whatToSave, out serializedToString);
        }

        public static bool Save(this GlueProjectSave glueProjectSave, string tag, string fileName, out Exception lastException)
        {
            int failures = 0;
            // This gives Glue 2 chances to save....but not like weekend sales at RC Willey
            const int maxFailures = 3;

            bool succeeded = false;
            lastException = null;

            while (failures < maxFailures)
            {
                try
                {
                    glueProjectSave.SaveToFile(fileName, tag);

                    succeeded = true;
                    break;
                }
                catch (IOException ioe)
                {
                    lastException = ioe;
                    System.Threading.Thread.Sleep(200);
                    failures++;
                }
                catch (UnauthorizedAccessException uae)
                {
                    lastException = uae;
                    System.Threading.Thread.Sleep(200);
                    failures++;
                }
            }

            return succeeded;
        }

        private static void SaveToFile(this GlueProjectSave glueProjectSave, string fileName, string tag)
        {
            var toSave = glueProjectSave.ConvertToPartial(tag);
            string convertedFileName = fileName.ConvertToPartialName(tag);
            FileManager.XmlSerialize(toSave, convertedFileName);
        }

        private static GlueProjectSave ConvertToPartial(this GlueProjectSave glueProjectSave, string tag)
        {
            GlueProjectSave returnValue;
            if (tag == "GLUE")
            {
                //Remove other elements
                returnValue = glueProjectSave.Clone();

                //Entities
                returnValue.Entities.RemoveAll(t => !t.Tags.Contains(tag) && t.Tags.Count != 0);

                //Screens
                returnValue.Screens.RemoveAll(t => !t.Tags.Contains(tag) && t.Tags.Count != 0);
            }
            else
            {
                returnValue = new GlueProjectSave();

                //Entities
                returnValue.Entities.RemoveAll(t => !t.Tags.Contains(tag));

                //Screens
                returnValue.Screens.RemoveAll(t => !t.Tags.Contains(tag));
            }

            return returnValue;


        }

        private static string ConvertToPartialName(this string fileName, string tag)
        {
            if (tag == "GLUE")
                return fileName;

            var newFileName = fileName.Remove(fileName.Length - 5);
            newFileName += "." + tag + ".Generated.glux";

            return newFileName;
        }

        public static GlueProjectSave Load(string fileName)
        {
            var main = FileManager.XmlDeserialize<GlueProjectSave>(fileName).MarkTags("GLUE");

            var files =
                Directory.GetFiles(Path.GetDirectoryName(fileName) + @"\");



            foreach (var file in files.Where(item=>item.ToLower().EndsWith(".generated.glux")))
            {
                string withoutExtension = FileManager.RemoveExtension(file);
                string withoutGenerated = FileManager.RemoveExtension(withoutExtension);

                if (withoutGenerated == null) continue;
                var tag = FileManager.GetExtension(withoutGenerated);

                main.Merge(FileManager.XmlDeserialize<GlueProjectSave>(file).MarkTags(tag));
            }
            return main;
        }

        public static void Save(this GlueProjectSave glueprojectsave, string tag, string fileName)
        {
            glueprojectsave.SaveToFile(fileName, tag);
        }

        private static GlueProjectSave Clone(this GlueProjectSave obj)
        {
            var toReturn = FileManager.CloneObject(obj);

            return toReturn;
        }

        private static void Merge(this GlueProjectSave origSave, GlueProjectSave newSave)
        {
            //Entities
            foreach (var entitySave in newSave.Entities)
            {
                var save = entitySave;
                if (origSave.Entities.All(t => t.Name != save.Name))
                {
                    origSave.Entities.Add(save);
                }
                else
                {

                    //Do stuff for when it already exists
                }
            }

            //Screens
            foreach (var screenSave in newSave.Screens)
            {
                var save = screenSave;
                if (origSave.Screens.All(t => t.Name != save.Name))
                {
                    origSave.Screens.Add(save);
                }
                else
                {
                    //Do stuff for when it already exists
                }
            }
        }

        private static GlueProjectSave MarkTags(this GlueProjectSave origSave, string tag)
        {
            //Entities
            foreach (var entitySave in origSave.Entities)
            {
                entitySave.Source = tag;
                entitySave.Tags.Clear();
                entitySave.Tags.Add(tag);

                // Eventually need to add this
                //foreach (var rfs in entitySave.ReferencedFiles)
                //{
                //    rfs.Tags.Clear();
                //    rfs.Tags.Add(tag);
                //}
            }

            //Screens
            foreach (var screenSave in origSave.Screens)
            {
                screenSave.Source = tag;
                screenSave.Tags.Clear();
                screenSave.Tags.Add(tag);
            }

            return origSave;
        }
    }
}
