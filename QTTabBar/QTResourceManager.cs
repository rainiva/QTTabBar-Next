//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2024  Quizo, Paul Accisano
//
//    QTTabBar is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    QTTabBar is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with QTTabBar.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace QTTabBarLib {
    // Task 3.3 extraction: language-file reading and text-resource validation moved
    // out of QTUtility. Behavior is identical; QTUtility forwards to these via one-line
    // facades so cross-file callers are unchanged.
    //
    // Deliberately NOT named ResourceManager to avoid clashing with
    // System.Resources.ResourceManager (used by the GetResourceStrings extension).
    //
    // Publish semantics (Task 2.5, unchanged): the no-arg ValidateTextResources builds
    // the dictionary off to the side, then publishes it under lock(QTUtility.syncRoot)
    // in one shot. ResourceCache.TextResourcesDic is the single source of truth; the
    // ResMain / ResMisc snapshots are refreshed from the just-published dictionary.
    internal static class QTResourceManager {

        public static Dictionary<string, string[]> ReadLanguageFile(string path) {
          //  const string linebreak = "\r\n";
          //  const string linebreakLiteral = @"\r\n";

            //We have to remove the first linebreak in the XML element's value, before we can split 
            //on the linebreak. It's there in the XML, when the XML is created using the editor.
            //Other linebreaks should be left in place, even if the line is empty, in order to preserve
            //the relative places of the other substrings.
            //The simplest way to do this is with a regular expression.

            try {
               /* var dictionary = XElement.Load(path).Elements().ToDictionary(
                    element => element.Name.ToString(),
                    element => {
                        string[] substrings =
                            ((string)element)
                            .Replace(singleLinebreakAtStart, "")
                            .Split(new[] { linebreak }, StringSplitOptions.None)
                            .Select(
                                s => s.Replace(linebreakLiteral, linebreak)
                            )
                            .ToArray();
                        return substrings;
                    }
                );*/
                const string newValue = "\r\n";
                const string oldValue = @"\r\n";
                Dictionary<string, string[]> dictionary = new Dictionary<string, string[]>();

                using (XmlTextReader reader = new XmlTextReader(path))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType != XmlNodeType.Element || reader.Name == "root") continue;
                        string[] str = reader.ReadString().Split(new string[] { newValue }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < str.Length; i++)
                        {
                            str[i] = str[i].Replace(oldValue, newValue);
                        }
                        dictionary[reader.Name] = str;
                    }
                    reader.Close();
                }
                return dictionary;
            } catch (XmlException xmlException) {
                string msg = String.Join("\r\n", new[] {
                    "Invalid language file.",
                    "",
                    xmlException.SourceUri,
                    "Line: " + xmlException.LineNumber,
                    "Position: " + xmlException.LinePosition,
                    "Detail: " + xmlException.Message
                });
                MessageBox.Show(msg);
                return null;
            } catch (Exception exception) {
                QTLogger.MakeErrorLog(exception);
                return null;
            }
        }

        public static void ValidateTextResources() {
            Dictionary<string, string[]> dict = ResourceCache.TextResourcesDic;
            ValidateTextResources(ref dict);
            lock(QTUtility.syncRoot) {
                ResourceCache.TextResourcesDic = dict;
            }
            Resx.UpdateAll();
        }

        public static void ValidateTextResources(ref Dictionary<string, string[]> dict)
        {
            // URL keys that must never be overwritten from the built-in resources.
            string[] urlKeys = { "SiteURL", "PayPalURL" };

            // Guard against a null dictionary.
            if (dict == null)
            {
                dict = new Dictionary<string, string[]>();
            }

            // Select the built-in resource set for the configured language index.
            IEnumerable<KeyValuePair<string, string>> keyValuePairs = null;
            switch (Config.Lang.BuiltInLangSelectedIndex)
            {
                case 0: keyValuePairs = Resources_String.ResourceManager.GetResourceStrings(); break;
                case 1: keyValuePairs = Resource_String_zh_CN.ResourceManager.GetResourceStrings(); break;
                case 2: keyValuePairs = Resources_String_de_DE.ResourceManager.GetResourceStrings(); break;
                case 3: keyValuePairs = Resources_String_pt_BR.ResourceManager.GetResourceStrings(); break;
                case 4: keyValuePairs = Resources_String_es_ES.ResourceManager.GetResourceStrings(); break;
                case 5: keyValuePairs = Resources_String_fr_FR.ResourceManager.GetResourceStrings(); break;
                case 6: keyValuePairs = Resources_String_tr_TR.ResourceManager.GetResourceStrings(); break;
                case 7: keyValuePairs = Resources_String_ru_RU.ResourceManager.GetResourceStrings(); break;
            }

            // Fall back to the default resource set when nothing was selected.
            if (null == keyValuePairs)
            {
                keyValuePairs = Resources_String.ResourceManager.GetResourceStrings();
            }

            // When not using an external language file, take the built-in strings verbatim.
            if ( !Config.Lang.UseLangFile )
            {
                foreach (var pair in keyValuePairs)
                {
                    dict[pair.Key] = pair.Value.Split(QTUtility.SEPARATOR_CHAR);
                }
            }
            else // Using an external language file.
            {
                // Merge built-in strings, keeping the external values where present.
                foreach (var pair in keyValuePairs)
                {
                    if (urlKeys.Contains(pair.Key)) continue;
                    // Semicolon-separated string -> string array.
                    string[] buildinValue = pair.Value.Split(QTUtility.SEPARATOR_CHAR);
                    string[] res;
                    dict.TryGetValue(pair.Key, out res);
                    if (res == null) // Key missing in dict: use the built-in value so it is not overridden.
                    {
                        dict[pair.Key] = buildinValue;
                    }
                    else if (res.Length < buildinValue.Length)// External value shorter than built-in: pad to the same length.
                    {
                        int len = res.Length;
                        Array.Resize(ref res, buildinValue.Length);
                        Array.Copy(buildinValue, len, res, len, buildinValue.Length - len);
                        dict[pair.Key] = res;
                    }
                }
            }
        }
    }
}
