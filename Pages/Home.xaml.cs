using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfApp1.Pages;
namespace WpfApp1.Pages
{
    public partial class Home : Page
    {
        public Home()
        {
            InitializeComponent();
        }

        // Logic for the Register Button
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Register());
        }

        // Logic for the Login Button
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new LogIn());
        }
    }
}