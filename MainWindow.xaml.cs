using System;
using System.Windows;
using Project.Model; // Ensure this matches your Model's namespace
using WpfApp1.Pages;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MyFrame.Navigate(new Home());
        }

    }
}