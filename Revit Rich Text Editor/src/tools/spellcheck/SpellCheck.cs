//   Revit Rich Text Editor
//   Copyright (C) 2014 Centek Engineering

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Web;
using NHunspell;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using CTEK_Rich_Text_Editor.src.tools.spellcheck;

namespace CTEK_Rich_Text_Editor
{
    class SpellCheck
    {
        private Dictionary<string, SpellCheckDictionary> availableDicts;
        private Dictionary<string, Word> cachedSuggestions;

        public SpellCheck()
        {
            availableDicts = findAvailableDicts();
            cachedSuggestions = new Dictionary<string, Word>();
        }

        public string respond(string request)
        {
            try
            {
                Dictionary<string, List<string>> param = getParams(request);

                string method = getParam("method", "spellcheck", param)[0];
                string lang = getParam("lang", "en_US", param)[0];
                string text = getParam("text", (string)null, param)[0];

                string aff = availableDicts[lang].path;
                string dic = aff.Substring(0, aff.Length - 3) + "dic";

                Properties.Settings.Default.language = lang;
                Properties.Settings.Default.Save();

                HashSet<string> words = getWords(text);

                Dictionary<string, List<string>> suggestions = new Dictionary<string, List<string>>();

                string bb = new FileInfo(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath).Directory.FullName;
                Hunspell.NativeDllPath = bb;

                // If it hangs up here, that means that the Hunspell DLL can't be found in Hunspell.NativeDllPath

                // NOT THREAD SAFE
                using (Hunspell hunspell = new Hunspell(aff, dic))
                {
                    // Process words
                    foreach (string word in words)
                    {
                        //check if exists in cached dictionary
                        
                        if (cachedSuggestions.ContainsKey(word)) //if found in cache
                        {
                            Word wordObj = cachedSuggestions[word];
                            if (!wordObj.spelledCorrectly())
                            {
                                suggestions.Add(word, wordObj.getSuggestions());
                            }
                        }
                        else //not found in cache
                        {
                            if (!hunspell.Spell(word))      // spelled incorrectly
                            {
                                List<String> hunspellSuggestions = hunspell.Suggest(word);
                                suggestions.Add(word, hunspellSuggestions);      // Get suggestions
                                cachedSuggestions.Add(word, new Word(false, hunspellSuggestions));
                            }
                            else //spelled correctly
                            {
                                cachedSuggestions.Add(word, new Word(true, null));
                            }
                        }
                       
                    }
                }

                // END

                string json = new JavaScriptSerializer().Serialize(suggestions);

                return json;
            }
            catch (Exception)
            {
                // TODO test
                return "[]";
            }
        }

        // Gets a list of all the words from the text
        private HashSet<string> getWords(string text)
        {
            HashSet<string> result = new HashSet<string>();

            string[] words = text.Split((char[])null);

            // Match all words
            Match match = Regex.Match(text, @"([\w'-]{1,})");
            while (match.Success)
            {
                string val = match.Value;
                if (val.Trim().Length == 0)
                    continue;

                // Exclude words with numbers in them
                if (!Regex.IsMatch(val, @"(\d{1,})"))
                    result.Add(val);

                match = match.NextMatch();
            }

            return result;
        }

        private List<string> getParam(string name, string def, Dictionary<string, List<string>> param)
        {
            List<string> temp = new List<string>();
            temp.Add(def);

            return getParam(name, temp, param);
        }

        private List<string> getParam(string name, List<string> def, Dictionary<string, List<string>> param)
        {
            if (param.ContainsKey(name))
                return param[name];
            else
                return def;
        }

        // Extracts the params from a post string
        private Dictionary<string, List<string>> getParams(string post)
        {
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();

            using (StreamReader reader = new StreamReader(generateStreamFromString(post)))
            {
                string postedData = reader.ReadToEnd();

                foreach (string item in postedData.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] tokens = item.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length < 2)
                        continue;

                    string name = tokens[0];
                    string encodedValues = tokens[1];
                    string[] encodedSplitValues = encodedValues.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    List<String> valuesList = new List<string>();

                    foreach (string encodedValue in encodedSplitValues)
                    {
                        string decodedValue = HttpUtility.UrlDecode(encodedValue);

                        valuesList.Add(decodedValue);
                    }

                    result.Add(name, valuesList);
                }
            }

            return result;
        }

        private Stream generateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public bool spelledCorrectly()
        {
            return false;
        }

        public Dictionary<string, SpellCheckDictionary> getAvailabledDicts()
        {
            return availableDicts;
        }

        private Dictionary<string, SpellCheckDictionary> findAvailableDicts()
        {
            string bb = new FileInfo(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath).Directory.FullName;
            String path = Path.Combine(bb, @"dictionaries");

            Dictionary<string, SpellCheckDictionary> result = new Dictionary<string, SpellCheckDictionary>();

            string[] fileEntries = Directory.GetFiles(path, "*.aff", SearchOption.AllDirectories);
            foreach (string filePath in fileEntries)
            {
                String justName = Path.GetFileNameWithoutExtension(filePath);
                String displayName = "";

                try
                {
                    CultureInfo ci = CultureInfo.GetCultureInfo(justName.Replace('_', '-'));
                    displayName = ci.DisplayName.Trim();
                }
                catch (CultureNotFoundException)
                {
                    displayName = justName;
                }

                result.Add(justName, new SpellCheckDictionary(justName, displayName, filePath));
            }

            return result;
        }
    }
}
