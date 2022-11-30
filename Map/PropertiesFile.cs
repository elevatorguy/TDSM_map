﻿using System;
using System.Collections.Generic;
using System.IO;
using TShockAPI;

namespace Map
{
    public class PropertiesFile
    {
        private const char EQUALS = '=';
        private const char HEADER = '#';

        private Dictionary<String, String> propertiesMap;

        private string propertiesPath = String.Empty;

        private List<String> header = new List<String>();

        public int Count
        {
            get { return propertiesMap.Count; }
        }

        public PropertiesFile(string propertiesPath)
        {
            propertiesMap = new Dictionary<String, String>();
            this.propertiesPath = propertiesPath;
        }

        public void Load()
        {
            //Verify that the properties file exists and we can create it if it doesn't.
            if (!File.Exists(propertiesPath))
            {
                File.WriteAllText(propertiesPath, String.Empty);
            }

            propertiesMap.Clear();
            StreamReader reader = new StreamReader(propertiesPath);
            try
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    int setterIndex = line.IndexOf(EQUALS);
                    if (setterIndex > 0 && setterIndex < line.Length && !line.StartsWith(HEADER.ToString()))
                    {
                        propertiesMap.Add(line.Substring(0, setterIndex), line.Substring(setterIndex + 1));
                    }
                    else if (line.StartsWith(HEADER.ToString()))
                    {
                        header.Add(line.Substring(1, line.Length - 1));
                    }
                }
            }
            finally
            {
                reader.Close();
            }
        }

        public void Save(bool log = true)
        {
            var tmpName = propertiesPath + ".tmp" + (uint)(DateTime.UtcNow.Ticks % uint.MaxValue);
            var writer = new StreamWriter(tmpName);
            try
            {
                foreach (string line in header)
                {
                    if (line.Trim().Length > 0)
                        writer.WriteLine(HEADER + line);
                }

                foreach (KeyValuePair<String, String> pair in propertiesMap)
                {
                    if (pair.Value != null)
                        writer.WriteLine(pair.Key + EQUALS + pair.Value);
                }
            }
            finally
            {
                writer.Close();
            }

            try
            {
                File.Replace(tmpName, propertiesPath, null, true);
                if (log)
                    TShock.Log.Info("Saved file \"" + propertiesPath + "\".");
            }
            catch (IOException e)
            {
                if (log)
                    TShock.Log.Error("Save to \"" + propertiesPath + "\" failed: " + e.Message);
            }
            catch (SystemException e)
            {
                if (log)
                    TShock.Log.Error("Save to \"" + propertiesPath + "\" failed: " + e.Message);
            }

        }

        public string getValue(string key)
        {
            if (propertiesMap.ContainsKey(key))
            {
                return propertiesMap[key];
            }
            return null;
        }

        public string getValue(string key, string defaultValue)
        {
            string value = getValue(key);
            if (value == null || value.Trim().Length < 0)
            {
                setValue(key, defaultValue);
                return defaultValue;
            }
            return value;
        }

        public int getValue(string key, int defaultValue)
        {
            int result;
            if (Int32.TryParse(getValue(key), out result))
            {
                return result;
            }

            setValue(key, defaultValue);
            return defaultValue;
        }

        public bool getValue(string key, bool defaultValue)
        {
            bool result;
            if (Boolean.TryParse(getValue(key), out result))
            {
                return result;
            }

            setValue(key, defaultValue);
            return defaultValue;
        }

        public void setValue(string key, string value)
        {
            propertiesMap[key] = value;
        }

        protected void setValue(string key, int value)
        {
            setValue(key, value.ToString());
        }

        protected void setValue(string key, bool value)
        {
            setValue(key, value.ToString());
        }

        public void AddHeaderLine(string Line)
        {
            if (!header.Contains(Line))
                header.Add(Line);
        }

        public bool RemoveHeaderLine(string Line)
        {
            return header.Remove(Line);
        }
    }
}
