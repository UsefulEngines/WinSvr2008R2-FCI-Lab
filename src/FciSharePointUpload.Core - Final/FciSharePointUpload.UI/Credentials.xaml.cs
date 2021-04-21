using System.Windows;

namespace FciSharePointUpload.UI
{
    /// <summary>
    /// Interaction logic for Credentials.xaml
    /// </summary>
    public partial class Credentials : Window
    {
        public string Username
        {
            get
            {
                return TextBoxUsername.Text.Trim();
            }
        }

        public string Password
        {
            get
            {
                return TextBoxPassword.Password;
            }
        }

        public Credentials()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(Credentials_Loaded);
        }

        private void Credentials_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxUsername.Text = string.Empty;
            TextBoxPassword.Password = string.Empty;
            ButtonAccept.IsEnabled = false;
            TextBoxUsername.KeyUp += new System.Windows.Input.KeyEventHandler(TextBoxUsername_KeyUp);
            TextBoxPassword.KeyUp += new System.Windows.Input.KeyEventHandler(TextBoxPassword_KeyUp);
            ButtonAccept.Click += new RoutedEventHandler(ButtonAccept_Click);
            ButtonCancel.Click += new RoutedEventHandler(ButtonCancel_Click);
        }

        public bool IsInputValid()
        {
            return (Username.Length > 0 && Password.Length > 0) ||
                   (Username.Length == 0 && Password.Length == 0);
        }

        private void TextBoxUsername_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ButtonAccept.IsEnabled = IsInputValid();
        }

        private void TextBoxPassword_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ButtonAccept.IsEnabled = IsInputValid();
        }

        private void ButtonAccept_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
