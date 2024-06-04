namespace Kingmaker.Localization.Shared
{
	public enum LocaleTrait
	{
		CheckMe,
		Translated,
		Final
	}

	public static class LocaleTraitExtensions
	{
		public static readonly LocaleTrait[] Values =
		{
			LocaleTrait.CheckMe,
			LocaleTrait.Translated,
			LocaleTrait.Final
		};
	}
}