using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1.Model;
using Project.Model;

namespace WpfApp1.Pages
{
    public partial class AdminDashBoard : Page
    {
        // Class-level variables to keep the connection and list alive
        private AppDbContext _db;
        public ObservableCollection<Products> ProductList { get; set; }

        // Tracking variable: null means "Add Mode", not null means "Edit Mode"
        private Products _selectedProductForEdit = null;

        public AdminDashBoard()
        {
            InitializeComponent();
            _db = new AppDbContext();
            LoadData();
        }

        private void LoadData()
        {
            // Fetch all products (or filter by UserSession.LoggedInUserId if needed)
            var list = _db.Products.ToList();
            ProductList = new ObservableCollection<Products>(list);
            ProductDataGrid.ItemsSource = ProductList;
        }
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs()) return;

            // Use the class-level _db context for all operations
            if (_selectedProductForEdit == null)
            {
                // --- ADD MODE ---
                Products newProduct = new Products
                {
                    ProductName = ProductName.Text,
                    ProductPrice = decimal.Parse(ProductPrice.Text),
                    ProductStock = decimal.Parse(ProductStock.Text),
                    UserId = UserSession.LoggedInUserId
                };

                _db.Products.Add(newProduct);
                _db.SaveChanges();

                ProductList.Add(newProduct); // Update UI
                MessageBox.Show("Product Added Successfully!");
            }
            else
            {
                // --- EDIT/UPDATE MODE ---
                _selectedProductForEdit.ProductName = ProductName.Text;
                _selectedProductForEdit.ProductPrice = decimal.Parse(ProductPrice.Text);
                _selectedProductForEdit.ProductStock = decimal.Parse(ProductStock.Text);

                _db.SaveChanges();

                //Force refresh by reassigning ItemsSource
        ProductDataGrid.ItemsSource = null;
                ProductDataGrid.ItemsSource = ProductList;


                MessageBox.Show("Product Updated Successfully!");
                 
                // Reset to Add Mode
                _selectedProductForEdit = null;
                SubmitButton.Content = "Add Product"; 
            }

            ClearForm();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the product linked to the specific row's button
            _selectedProductForEdit = (sender as Button).DataContext as Products;

            if (_selectedProductForEdit != null)
            {
                // Fill the TextBoxes
                ProductName.Text = _selectedProductForEdit.ProductName;
                ProductPrice.Text = _selectedProductForEdit.ProductPrice.ToString();
                ProductStock.Text = _selectedProductForEdit.ProductStock.ToString();

                // Visual cue for the user
                SubmitButton.Content = "Update Product";
                ProductName.Focus();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var product = (sender as Button).DataContext as Products;

            if (product != null)
            {
                var result = MessageBox.Show($"Delete {product.ProductName} permanently?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _db.Products.Remove(product);
                    _db.SaveChanges();
                    ProductList.Remove(product); // Auto-removes from Grid
                }
            }
        }

        private bool ValidateInputs()
        {
            // Hide all error messages first
            ErrorProductName.Visibility = Visibility.Collapsed;
            ErrorProductPrice.Visibility = Visibility.Collapsed;
            ErrorProductPriceZero.Visibility = Visibility.Collapsed;
            ErrorProductStock.Visibility = Visibility.Collapsed;
            ErrorProductStockNegative.Visibility = Visibility.Collapsed;

            bool isValid = true;

            if (string.IsNullOrWhiteSpace(ProductName.Text))
            {
                ErrorProductName.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (!decimal.TryParse(ProductPrice.Text, out decimal price) || price <= 0)
            {
                if (string.IsNullOrWhiteSpace(ProductPrice.Text)) ErrorProductPrice.Visibility = Visibility.Visible;
                else ErrorProductPriceZero.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (!decimal.TryParse(ProductStock.Text, out decimal stock) || stock < 0)
            {
                if (string.IsNullOrWhiteSpace(ProductStock.Text)) ErrorProductStock.Visibility = Visibility.Visible;
                else ErrorProductStockNegative.Visibility = Visibility.Visible;
                isValid = false;
            }

            return isValid;
        }

        private void ClearForm()
        {
            ProductName.Clear();
            ProductPrice.Clear();
            ProductStock.Clear();
            _selectedProductForEdit = null;
            SubmitButton.Content = "Add Product";

            // Hide errors on clear
            ErrorProductName.Visibility = Visibility.Collapsed;
            ErrorProductPrice.Visibility = Visibility.Collapsed;
            ErrorProductPriceZero.Visibility = Visibility.Collapsed;
            ErrorProductStock.Visibility = Visibility.Collapsed;
            ErrorProductStockNegative.Visibility = Visibility.Collapsed;
        }
    }
}