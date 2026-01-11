using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using BetterQuicksave.Utils;
using TaleWorlds.InputSystem;

// MCM v5
using MCM.Abstractions;

namespace BetterQuicksave
{
    [XmlRoot("BetterQuicksaveConfig")]
    public class Config
    {
        public static ModInfoData ModInfo => Instance.InstanceModInfo;

        // Prefer MCM values when available, otherwise fall back to XML config.
        public static string QuicksavePrefix => Regex.Replace(McmSettings?.QuicksavePrefix ?? Instance.InstanceQuicksavePrefix, @"[^\w\-. ]", "");
        public static int MaxQuicksaves => McmSettings?.MaxQuicksaves ?? Instance.InstanceMaxQuicksaves;
        public static bool MultipleQuicksaves => MaxQuicksaves > 1;
        public static InputKey QuickloadKey => (InputKey)(McmSettings?.QuickloadKeyCode ?? Instance.InstanceQuickloadKey);
        public static bool PerCharacterSaves => McmSettings?.PerCharacterSaves ?? Instance.InstancePerCharacterSaves;

        private static readonly string ModBasePath =
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", ".."));
        private static readonly string ConfigFilename =
            Path.Combine(ModBasePath, "ModuleData", "BetterQuicksaveConfig.xml");

        private static BetterQuicksaveMCMSettings _mcmSettings;
        private static BetterQuicksaveMCMSettings McmSettings
        {
            get
            {
                if (_mcmSettings != null)
                {
                    return _mcmSettings;
                }

                try
                {
                    _mcmSettings = BetterQuicksaveMCMSettings.Instance;
                }
                catch
                {
                    // If MCM isn't present, fall back to XML config without failing.
                    _mcmSettings = null;
                }

                return _mcmSettings;
            }
        }

        private static Config _instance;
        private static Config Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                _instance = LoadConfig();
                return _instance;
            }
        }

        private ModInfoData InstanceModInfo { get; } = new ModInfoData();
        [XmlElement("MaxQuicksaves")]
        public int InstanceMaxQuicksaves { get; set; } = 3;
        [XmlElement("QuicksavePrefix")]
        public string InstanceQuicksavePrefix { get; set; } = "quicksave_";
        [XmlElement("QuickloadKey")]
        public int InstanceQuickloadKey { get; set; } = (int)InputKey.F10;
        [XmlElement("PerCharacterSaves")]
        public bool InstancePerCharacterSaves { get; set; }

        private Config() { }

        private static Config LoadConfig()
        {
            Config config = DeserializeConfig();
            if (config == null)
            {
                SerializeConfig();
                config = DeserializeConfig();
            }
            var assemblyName = Assembly.GetExecutingAssembly().GetName();
            config.InstanceModInfo.Name = new DirectoryInfo(ModBasePath).Name;
            config.InstanceModInfo.Version = assemblyName.Version?.ToString() ?? "1.0.0";
            return config;
        }

        private static void SerializeConfig()
        {
            XmlSerialization.Serialize(ConfigFilename, new Config());
        }

        private static Config DeserializeConfig()
        {
            return XmlSerialization.Deserialize<Config>(ConfigFilename);
        }
    }

    public class ModInfoData
    {
        public string Name { get; set; } = "BetterQuicksave";
        public string Version { get; set; } = "1.0.0";
    }
}
