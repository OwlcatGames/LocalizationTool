using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Windows;

namespace LocalizationTracker
{
	public class AppConfig
	{
		public enum EngineType
		{
			Unity, Unreal
		}
		
		public class DeepLConfig
		{
			[JsonInclude]
			public string APIKey="";

            [JsonInclude]
			public Dictionary<string, string> SourceLocaleMap=new ();
            [JsonInclude]
            public Dictionary<string, string> TargetLocaleMap = new();

            [JsonInclude]
			public List<string> MFTags=new ();
			[JsonInclude]
			public List<string> GlossaryTags=new ();
		}

		public class GlossaryConfig
		{
			[JsonInclude]
			public bool GlossaryIsEnabled = true;    
			[JsonInclude]
			public string GlossaryGSheetId = "";    
			[JsonInclude]
			public string GlossaryJSONPath = "";
			[JsonInclude]
			public string GoogleCredentialPath = "";
		}

        public class SymbolsBordersConfig
        {
			[JsonInclude]
			[JsonPropertyName("common")]
			public int Common { get; set; } = 0;
			[JsonInclude]
			[JsonPropertyName("en")]
			public int En { get; set; } = 0;
			[JsonInclude]
			[JsonPropertyName("shortAnswer")]
			public int ShortAnswer { get; set; } = 0;

        }


        public static AppConfig Instance { get; set; }

		[JsonInclude]
		public EngineType Engine = EngineType.Unity;

		[JsonInclude]
		public bool ModdersVersion = false;

        [JsonInclude]
        public string Project = "";

		[JsonInclude]
		public string IconPath = "";

        [JsonInclude]
        public string MultilineSearchIcon = "";

        [JsonInclude]
		public string StringsFolder = "";

        [JsonInclude]
        public string UnitsFolder = "";

        [JsonInclude]
        public string AssetsFolder = "";

		[JsonInclude]
		public string BlueprintsFolder = "";

		[JsonInclude]
		public string LocalizationPacksFolder = "";

		[JsonInclude]
		public string[] Locales = System.Array.Empty<string>();

		[JsonInclude]
		public bool AddDefaultLocales = true;

        [JsonInclude]
        public HashSet<string> IgnoreMismatchedTags = new();
        [JsonInclude]
        public HashSet<string> NeedClosingTags = new();

		[JsonInclude]
		public bool EnableCopyData;

		[JsonInclude]
		public bool AddAIGeneratedTag { get; set; } = true;

		[JsonInclude]
		public bool CountUnusedStrings { get; set; }

		[JsonInclude]
		public string AttachmentsPath = "";

		[JsonInclude]
		public DeepLConfig DeepL = new ();
		
		[JsonInclude]
		public GlossaryConfig Glossary = new ();

		[JsonInclude]
        public SymbolsBordersConfig SymbolsBorders = new ();

        public string AbsStringsFolder { get; private set; } = "";

		public string AbsAssetsFolder { get; private set; } = "";

        public string AbsBlueprintsFolder { get; private set; } = "";

		[JsonInclude]
		public bool ShowStatusColumn;
		
		public static Visibility StatusColumnVisibility =>
			Instance.ShowStatusColumn ? Visibility.Visible : Visibility.Collapsed;

		public bool DeepLAvailable =>
			Instance != null && 
			!Instance.ModdersVersion && 
			!string.IsNullOrEmpty(Instance.DeepL.APIKey);


		public static void SetupInstance(AppConfig instance)
		{
			Instance = instance;
            var currentDirectory = Directory.GetCurrentDirectory();
            instance.AbsStringsFolder = Path.GetFullPath(Path.Combine(currentDirectory, instance.StringsFolder));
			instance.AbsAssetsFolder = string.IsNullOrWhiteSpace(instance.AssetsFolder) ? "" : Path.GetFullPath(Path.Combine(currentDirectory, instance.AssetsFolder));
			instance.AbsBlueprintsFolder = string.IsNullOrWhiteSpace(instance.BlueprintsFolder) ? "" : Path.GetFullPath(Path.Combine(currentDirectory, instance.BlueprintsFolder));
		}
	}
}