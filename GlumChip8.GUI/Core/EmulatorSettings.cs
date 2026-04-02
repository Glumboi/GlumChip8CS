using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace GlumChip8.GUI.Core
{
    class EmulatorSettings
    {
        [DllImport("kernel32.dll")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        readonly string DEFAULT_CONFIG_FILE = Path.Combine(
   AppDomain.CurrentDomain.BaseDirectory,
   "C8EmuCfg.ini"
);
        public string RomLocation { get; private set; }

        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, DEFAULT_CONFIG_FILE);
        }

        public string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp, 255, DEFAULT_CONFIG_FILE);
            return temp.ToString();
        }

        private void InitDefault()
        {
            File.Create(DEFAULT_CONFIG_FILE).Close();
            IniWriteValue("Config", "RomLoc", "./Roms");
        }

        public void LoadConfigFile()
        {
            if (!File.Exists(DEFAULT_CONFIG_FILE)) { InitDefault(); }

            RomLocation = IniReadValue("Config", "RomLoc");
        }
    }
}
