using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LocalizationTracker.Windows
{
    public partial class AuthorizationWindow : Window
    {
        public string Username { get; private set; }
        public string Password { get; private set; }

        public AuthorizationWindow()
        {
            InitializeComponent();
        }

        private void OnSubmitClick(object sender, RoutedEventArgs e)
        {
            Username = UsernameTextBox.Text;
            Password = PasswordBox.Password;
            DialogResult = true;
            Close();
        }
    }

}
