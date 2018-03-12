using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO.Csv;
using System.Collections.ObjectModel;
using FlatRedBall.Utilities;

namespace FlatRedBall.Localization
{
    public static class LocalizationManager
    {
        #region Fields/Properties

        public static ReadOnlyCollection<string> Languages
        {
            get;
            set;
        }

        static Dictionary<string, string[]> mStringDatabase = new Dictionary<string, string[]>();

        public static bool HasDatabase
        {
            get;
            private set;
        }

        public static int CurrentLanguage
        {
            get;
            set;
        }

        #endregion

        static LocalizationManager()
        {
            Languages = new ReadOnlyCollection<string>(new List<string>());
        }

        public static void AddDatabase(string fileName, char delimiter)
        {

            RuntimeCsvRepresentation rcr;

            char oldDelimiter = CsvFileManager.Delimiter;
            CsvFileManager.Delimiter = delimiter;
            Dictionary<string, string[]> entryDictionary = new Dictionary<string, string[]>();

            CsvFileManager.CsvDeserializeDictionary<string, string[]>(fileName, entryDictionary, out rcr);
            CsvFileManager.Delimiter = oldDelimiter;


            List<string> headerList = new List<string>();

            foreach (CsvHeader header in rcr.Headers)
            {
                headerList.Add(header.Name);
            }

            AddDatabase(entryDictionary, headerList);
        }

        public static void AddDatabase(Dictionary<string, string[]> entryDictionary, List<string> headerList)
        {
            Languages = new ReadOnlyCollection<string>(headerList);
            mStringDatabase = entryDictionary;
            HasDatabase = true;
        }

        public static void ClearDatabase()
        {
            mStringDatabase.Clear();
            HasDatabase = false;
            Languages = new ReadOnlyCollection<string>(new List<string>());
        }

        public static string TranslateForLanguage(string stringID, int language)
        {

            if (stringID == null)
            {
                return "NULL STRING";
            }
            else if (mStringDatabase.ContainsKey(stringID))
            {
                return mStringDatabase[stringID][language];
            }
            else if (ShouldExcludeFromTranslation(stringID))
            {
                return stringID;
            }
            else
            {
                return stringID + " - UNTRANSLATED";
            }
        }

        private static bool ShouldExcludeFromTranslation(string stringID)
        {
            if (string.IsNullOrEmpty(stringID))
            {
                return true;
            }
            else if (StringFunctions.IsNumber(stringID))
            {
                return true;
            }
            else if (IsPercentage(stringID))
            {
                return true;
            }
            else if (IsTime(stringID))
            {
                return true;
            }
            else if (stringID == "!" || stringID == "+" || stringID == "-" ||
                stringID == "*" || stringID == "/" || stringID == "#" || stringID == ":")
            {
                return true;
            }
            return false;
        }

        private static bool IsTime(string stringID)
        {
            for (int i = 0; i < stringID.Length; i++)
            {
                char cAti = stringID[i];
                if (char.IsDigit(cAti) == false && (cAti == ':') == false && (cAti == '.') == false)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsPercentage(string stringID)
        {
            return stringID.CountOf('%') == 1 && stringID.Length > 1 && stringID.EndsWith("%") && StringFunctions.IsNumber(stringID.Substring(0, stringID.Length - 1));

        }

        public static string Translate(string stringID)
        {
            return TranslateForLanguage(stringID, CurrentLanguage);

        }

        public static string Translate(string stringID, params object[] args)
        {
            string translated =
                string.Format(Translate(stringID),
                args);

            return translated;
        }

		/// <summary>
		/// Grabs a dictionary with a copy of all the localization database keys and the current language values. 
		/// </summary>
		/// <returns></returns>
		public static Dictionary<string, string> GetLanguageDatabaseCopy()
		{
			var dict = new Dictionary<string, string>();
			foreach (var key in mStringDatabase.Keys)
			{
				dict.Add(key, mStringDatabase[key][CurrentLanguage]);
			}
			return dict;
		}
	}
}
