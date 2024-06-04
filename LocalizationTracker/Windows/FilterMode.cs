namespace LocalizationTracker.Windows
{
	public enum FilterMode
	{
		Normal,
		Spelling_Errors,
		Key_Duplicates,
		Updated_Source,
		Updated_Trait,
		Tags_Mismatch,
		Unreal_Unused,
		Glossary_Mismatch,
	}

	public static class FilterModeUtililty
	{
		public static FilterMode[] AllValues { get; } =
		{
			FilterMode.Normal,
			FilterMode.Spelling_Errors,
			FilterMode.Key_Duplicates,
			FilterMode.Updated_Source,
			FilterMode.Updated_Trait,
			FilterMode.Tags_Mismatch,
			FilterMode.Unreal_Unused,
			FilterMode.Glossary_Mismatch,
		};
	}
}
