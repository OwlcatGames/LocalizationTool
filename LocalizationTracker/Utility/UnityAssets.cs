using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using LocalizationTracker.Data;
using LocalizationTracker.Logic;

namespace LocalizationTracker.Utility
{
	public static class UnityAssets // this is actually blueprints, not unity assets!
	{
		private static readonly ConcurrentDictionary<string, AssetsFolder> s_CachedFolders = new();

		public static void ClearFoldersCache()
		{
			s_CachedFolders.Clear();
		}

		public static StringEntry? FindParent(StringEntry se)
		{
			string? relativePath = Path.GetDirectoryName(se.PathRelativeToStringsFolder);
			if (relativePath == null)
				throw new IOException("Could not find assets folder for string: " + se.PathRelativeToStringsFolder);

			relativePath = relativePath.Replace("\\", "/");
            if (relativePath.StartsWith("Strings/"))
                relativePath = relativePath.Substring("Strings/".Length);

			if (relativePath.StartsWith("Mechanics/Blueprints/"))
                relativePath = relativePath.Substring("Mechanics/Blueprints/".Length);

			string absolutePath = Path.Combine(AppConfig.Instance.BlueprintsFolder, relativePath);

			var folder = s_CachedFolders.GetOrAdd(absolutePath, AssetsFolder.Load);

			return folder.FindParent(se);
		}
	}

	internal class Asset
	{
        class AssetData
        {
			[JsonInclude]
            public string AssetId;

            public class Blueprint
            {
                [JsonInclude]
                public string? ParentAsset;

                public class String
                {
                    [JsonInclude]
                    [JsonPropertyName("m_Key")]
                    public string? Key;
                }

                [JsonInclude]
                public String? Text;

			}

            [JsonInclude]
            public Blueprint? Data;

            public string? ParentAsset => Data?.ParentAsset;
            public string? Key => Data?.Text?.Key;
        }

        private readonly AssetData Data;

        internal string Guid => Data.AssetId;

        internal string? ParentGuid => Data?.ParentAsset;

        internal readonly StringEntry? String;

        private Asset(AssetData data, StringEntry? se)
        {
			Data = data;
			String = se;
        }

		public static Asset? Load(string filePath)
		{
            try
            {
                var json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<AssetData>(json, JsonSerializerHelpers.JsonSerializerOptions);
				if (data == null) 
					return null;

				StringEntry? se = null;
                if (data.Key != null)
                {
                    StringManager.StringsByKey.TryGetValue(data.Key, out se);
                }
				return new Asset(data, se);

            }
            catch (System.Exception)
            {
                // todo: log the exception?
            }
			return null;
        }
    }

	internal class AssetsFolder
	{
		private readonly string m_Path;

		private readonly IReadOnlyDictionary<string, Asset> m_Assets;

		public AssetsFolder(string path, IReadOnlyDictionary<string, Asset> assets)
		{
			m_Path = path;
			m_Assets = assets;
		}

		public static AssetsFolder Load(string path)
		{
			if (!Directory.Exists(path))
				return new(path, ImmutableDictionary<string, Asset>.Empty);

			var assets = (from assetPath in Directory.EnumerateFiles(path, "*.jbp")
						  let asset = Asset.Load(assetPath)
						  where asset != null
						  select asset)
						 .ToImmutableDictionary(v => v.Guid, v => v);
			return new AssetsFolder(path, assets);
        }

		public StringEntry? FindParent(StringEntry se)
        {
            var ownerId = se.Data.OwnerLink; // for Unity strings, this is the Guid of owner asset
			if(!m_Assets.TryGetValue(ownerId, out  var asset))
				return null;

			for (int guard = 0; guard < 1000; guard++)
			{
				if (asset.ParentGuid == null)
					return null;

				m_Assets.TryGetValue(asset.ParentGuid, out var parent);
				if (parent == null)
					return null;

				if (parent.String != null)
					return parent.String;

				asset = parent;
			}

			return null;
		}
	}

}