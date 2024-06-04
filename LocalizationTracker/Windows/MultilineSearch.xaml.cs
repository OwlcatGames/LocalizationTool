using LocalizationTracker.Logic;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace LocalizationTracker.Windows
{
    /// <summary>
    /// Interaction logic for MultilineSearch.xaml
    /// </summary>
    public partial class MultilineSearch : Window
    {
        public bool IsClosed { get; set; }

        public event EventHandler WindowClosed;
        public StringsFilter Filter => StringManager.Filter;

        public MultilineSearch(object dataContext)
        {
            InitializeComponent();
            DataContext = dataContext;

            string multilineSearchIcon = AppConfig.Instance.MultilineSearchIcon;
            if (File.Exists(multilineSearchIcon))
            {
                BitmapImage multilineSearchIconImage = new BitmapImage(new Uri(multilineSearchIcon, UriKind.RelativeOrAbsolute));
                this.Icon = multilineSearchIconImage;
            }
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            IsClosed = true;
            WindowClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}
