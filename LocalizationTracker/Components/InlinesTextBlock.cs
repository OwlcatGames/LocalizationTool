using System.Windows;
using System.Windows.Controls;

namespace LocalizationTracker.Components
{
	public class InlinesTextBlock : TextBlock
	{
		public InlinesWrapper? InlinesWrapper { get; set; }

		public static readonly DependencyProperty LocaleEntryProperty =
			DependencyProperty.Register("InlinesWrapper", typeof(InlinesWrapper), typeof(InlinesTextBlock), new UIPropertyMetadata(null, InlinesContentPropertyChanged));

		private static void InlinesContentPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
            if (sender is not InlinesTextBlock textBlock)
                return;

            textBlock.Inlines.Clear();
            if (e.NewValue is not InlinesWrapper wrapper)
				return;

			textBlock.Inlines.AddRange(wrapper.Inlines);
		}
	}
}