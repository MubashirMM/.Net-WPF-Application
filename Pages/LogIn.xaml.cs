using Project.Model;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1.Model;

namespace WpfApp1.Pages
{
    /// <summary>
    /// Interaction logic for LogIn.xaml
    /// </summary>
    public partial class LogIn : Page
    {
        public LogIn()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string email = LoginEmail.Text;
            string pass = LoginPassword.Password;

            using (var db = new AppDbContext())
            {
                var user = db.Users.FirstOrDefault(u => u.Email == email);

                if (user == null)
                {
                    // Email wrong message
                    MessageBox.Show("Invalid Email!", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    LoginEmail.Focus();
                    return;
                }

                if (!BCrypt.Net.BCrypt.Verify(pass, user.Password))
                {
                    // Password wrong message
                    MessageBox.Show("Invalid Password!", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    LoginPassword.Password = "";
                    LoginPassword.Focus();
                    return;
                }

                // Login Success - Store in Session (only ID and Name)
                UserSession.LoggedInUserId = user.Id;
                UserSession.LoggedInUserName = user.Name; // Assuming FullName property exists

                // Navigate based on Role (3 movements)
                if (user.Role == UserRole.SuperAdmin)
                {
                    MessageBox.Show($"Welcome Super Admin: {user.Name}", "Login Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigationService?.Navigate(new SuperAdminDashboard());
                }
                else if (user.Role == UserRole.Admin)
                {
                    MessageBox.Show($"Welcome Admin: {user.Name}", "Login Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigationService?.Navigate(new AdminDashBoard());
                }
                else // Regular User
                {
                    MessageBox.Show($"Welcome: {user.Name}", "Login Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigationService?.Navigate(new UserDashboard());
                }
            }
        }
    }
}