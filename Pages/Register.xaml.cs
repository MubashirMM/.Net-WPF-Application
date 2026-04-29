using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Project.Model; // Ensure this matches your namespace for AppDbContext and Users

namespace WpfApp1.Pages
{
    public partial class Register : Page
    {
        public Register()
        {
            InitializeComponent();
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            // 1. Reset Error UI
            ErrorName.Visibility = Visibility.Collapsed;
            ErrorEmail.Visibility = Visibility.Collapsed;
            ErrorPassword.Visibility = Visibility.Collapsed;
            ErrorRole.Visibility = Visibility.Collapsed;

            bool isValid = true;

            // 2. Input Validation
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                ErrorName.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text) || !EmailTextBox.Text.Contains("@"))
            {
                ErrorEmail.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(PasswordTextBox.Password))
            {
                ErrorPassword.Visibility = Visibility.Visible;
                isValid = false;
            }

            var selectedItem = (ComboBoxItem)RoleComboBox.SelectedItem;
            if (selectedItem == null || selectedItem.Tag.ToString() == "0")
            {
                ErrorRole.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (!isValid) return;

            // 3. Database Execution
            try
            {
                using (var db = new AppDbContext())
                {
                    // Check for existing email to prevent SQL Unique Constraint error
                    if (db.Users.Any(u => u.Email == EmailTextBox.Text))
                    {
                        MessageBox.Show("This email is already registered. Please use another.");
                        return;
                    }

                    // Convert Tag to Enum
                    int roleId = int.Parse(selectedItem.Tag.ToString());
                    UserRole assignedRole = (UserRole)roleId;

                    var newUser = new Users
                    {
                        Name = NameTextBox.Text,
                        Email = EmailTextBox.Text,
                        // Using BCrypt.Net-Next for security
                        Password = BCrypt.Net.BCrypt.HashPassword(PasswordTextBox.Password),
                        Role = assignedRole,
                        CreatedDate = DateTime.Now
                    };

                    db.Users.Add(newUser);
                    db.SaveChanges();

                    MessageBox.Show("Account created successfully!");

                    // Navigate to Login Page
                    if (this.NavigationService != null)
                    {
                        this.NavigationService.Navigate(new LogIn());
                    }
                }
            }
            catch (Exception ex)
            {
                // Drill down to find the actual SQL error message
                var innerMsg = ex.InnerException?.InnerException?.Message ?? ex.Message;
                MessageBox.Show("Database Save Failed: " + innerMsg);
            }
        }
    }
}