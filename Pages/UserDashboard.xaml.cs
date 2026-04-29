using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Diagnostics;
using Project.Model;
using WpfApp1.Model;

namespace WpfApp1.Pages
{
    public partial class UserDashboard : Page
    {
        private AppDbContext _db;
        private ObservableCollection<Products> _allProducts;
        private ObservableCollection<Products> _filteredProducts;
        private List<CartItem> _cart;

        public UserDashboard()
        {
            InitializeComponent();

            _db = new AppDbContext();
            _cart = new List<CartItem>();

            LoadProducts();
            UpdateCartDisplay();
            LoadOrderHistory();
            WelcomeText.Text = $"Welcome, {UserSession.LoggedInUserName ?? "User"}!";
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadProducts();
            LoadOrderHistory();
        }

        private void LoadProducts()
        {
            _allProducts = new ObservableCollection<Products>(
                _db.Products.Where(p => p.ProductStock > 0).ToList()
            );
            _filteredProducts = new ObservableCollection<Products>(_allProducts);
            ProductsGrid.ItemsSource = _filteredProducts;
        }

        private void LoadOrderHistory()
        {
            var orders = _db.Orders
                .Where(o => o.UserId == UserSession.LoggedInUserId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            OrdersGrid.ItemsSource = orders;

            if (!orders.Any())
            {
                EmptyHistoryText.Visibility = Visibility.Visible;
            }
            else
            {
                EmptyHistoryText.Visibility = Visibility.Collapsed;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterProducts();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            FilterProducts();
        }

        private void FilterProducts()
        {
            string searchTerm = SearchBox.Text?.ToLower() ?? "";
            var filtered = _allProducts.Where(p =>
                string.IsNullOrEmpty(searchTerm) ||
                p.ProductName.ToLower().Contains(searchTerm)
            ).ToList();
            _filteredProducts = new ObservableCollection<Products>(filtered);
            ProductsGrid.ItemsSource = _filteredProducts;
        }

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var product = button?.DataContext as Products;

            if (product != null)
            {
                var existingItem = _cart.FirstOrDefault(c => c.ProductId == product.ProductId);

                if (existingItem != null)
                {
                    if (existingItem.Quantity + 1 > product.ProductStock)
                    {
                        MessageBox.Show($"Only {product.ProductStock} items available in stock!",
                                      "Stock Limit", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    existingItem.Quantity++;
                }
                else
                {
                    if (1 > product.ProductStock)
                    {
                        MessageBox.Show("Product out of stock!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    _cart.Add(new CartItem
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        Price = product.ProductPrice,
                        Quantity = 1
                    });
                }

                UpdateCartDisplay();
                MessageBox.Show($"✓ {product.ProductName} added to cart!", "Success",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var cartItem = button?.Tag as CartItem;

            if (cartItem != null)
            {
                var product = _db.Products.FirstOrDefault(p => p.ProductId == cartItem.ProductId);
                if (product != null && cartItem.Quantity + 1 <= product.ProductStock)
                {
                    cartItem.Quantity++;
                    UpdateCartDisplay();
                }
                else
                {
                    MessageBox.Show($"Cannot add more. Only {product?.ProductStock} items in stock!",
                                  "Stock Limit", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var cartItem = button?.Tag as CartItem;

            if (cartItem != null && cartItem.Quantity > 1)
            {
                cartItem.Quantity--;
                UpdateCartDisplay();
            }
            else if (cartItem != null && cartItem.Quantity == 1)
            {
                _cart.Remove(cartItem);
                UpdateCartDisplay();
            }
        }

        private void RemoveFromCart_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var cartItem = button?.Tag as CartItem;

            if (cartItem != null)
            {
                _cart.Remove(cartItem);
                UpdateCartDisplay();
            }
        }

        private void ClearCart_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Clear entire cart?", "Confirm", MessageBoxButton.YesNo,
                              MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _cart.Clear();
                UpdateCartDisplay();
            }
        }

        private void UpdateCartDisplay()
        {
            CartItemsControl.ItemsSource = null;
            CartItemsControl.ItemsSource = _cart;

            int totalItems = _cart.Sum(c => c.Quantity);
            decimal subtotal = _cart.Sum(c => c.Subtotal);
            decimal tax = subtotal * 0.12m;
            decimal total = subtotal + tax;

            CartCount.Text = totalItems.ToString();
            TotalItemsText.Text = totalItems.ToString();
            SubtotalText.Text = $"₱{subtotal:N2}";
            TaxText.Text = $"₱{tax:N2}";
            TotalText.Text = $"₱{total:N2}";
        }

        private async void GenerateBill_Click(object sender, RoutedEventArgs e)
        {
            if (!_cart.Any())
            {
                MessageBox.Show("Cart is empty! Please add items to cart first.",
                              "Empty Cart", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check stock availability before processing
            foreach (var cartItem in _cart)
            {
                var product = await _db.Products.FindAsync(cartItem.ProductId);
                if (product == null || product.ProductStock < cartItem.Quantity)
                {
                    MessageBox.Show($"Insufficient stock for {cartItem.ProductName}. " +
                                  $"Available: {product?.ProductStock ?? 0}",
                                  "Stock Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            var result = MessageBox.Show("Generate and save bill?", "Confirm",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Create order
                    var order = new Order
                    {
                        UserId = UserSession.LoggedInUserId,
                        OrderDate = DateTime.Now,
                        TotalAmount = _cart.Sum(c => c.Subtotal) + (_cart.Sum(c => c.Subtotal) * 0.12m),
                        Status = "Paid",
                        BillNumber = $"INV-{DateTime.Now:yyyyMMdd-HHmmss}"
                    };

                    _db.Orders.Add(order);
                    await _db.SaveChangesAsync();

                    // Create order items and deduct stock
                    foreach (var cartItem in _cart)
                    {
                        var product = await _db.Products.FindAsync(cartItem.ProductId);

                        var orderItem = new OrderItem
                        {
                            OrderId = order.Id,
                            ProductId = cartItem.ProductId,
                            ProductName = cartItem.ProductName,
                            UnitPrice = cartItem.Price,
                            Quantity = cartItem.Quantity,
                            Subtotal = cartItem.Subtotal
                        };
                        _db.OrderItems.Add(orderItem);

                        // Deduct stock
                        product.ProductStock -= cartItem.Quantity;
                    }

                    await _db.SaveChangesAsync();

                    // Generate Bill (using text file to avoid PDF issues)
                    GenerateBillFile(order);

                    // Clear cart and refresh
                    _cart.Clear();
                    UpdateCartDisplay();
                    LoadProducts();
                    LoadOrderHistory();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error generating bill: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void GenerateBillFile(Order order)
        {
            try
            {
                var orderItems = _db.OrderItems.Where(oi => oi.OrderId == order.Id).ToList();

                // Save to Desktop
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string billsFolder = Path.Combine(desktopPath, "ShoppingBills");

                if (!Directory.Exists(billsFolder))
                    Directory.CreateDirectory(billsFolder);

                string safeFileName = order.BillNumber.Replace(":", "_").Replace("/", "_").Replace("\\", "_");
                string fullPath = Path.Combine(billsFolder, $"{safeFileName}.txt");

                // Create text receipt
                using (StreamWriter writer = new StreamWriter(fullPath))
                {
                    writer.WriteLine("=".PadRight(60, '='));
                    writer.WriteLine("                    SHOPPING CENTER");
                    writer.WriteLine("                   OFFICIAL RECEIPT");
                    writer.WriteLine("=".PadRight(60, '='));
                    writer.WriteLine();
                    writer.WriteLine($"Bill Number: {order.BillNumber}");
                    writer.WriteLine($"Date: {order.OrderDate:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"Cashier: {UserSession.LoggedInUserName}");
                    writer.WriteLine();
                    writer.WriteLine("-".PadRight(60, '-'));
                    writer.WriteLine($"{"Item",-30} {"Qty",5} {"Price",10} {"Total",12}");
                    writer.WriteLine("-".PadRight(60, '-'));

                    foreach (var item in orderItems)
                    {
                        string name = item.ProductName.Length > 28 ? item.ProductName.Substring(0, 25) + "..." : item.ProductName;
                        writer.WriteLine($"{name,-30} {item.Quantity,5} ${item.UnitPrice,9:N2} ${item.Subtotal,10:N2}");
                    }

                    writer.WriteLine("-".PadRight(60, '-'));
                    decimal subtotal = orderItems.Sum(i => i.Subtotal);
                    decimal tax = subtotal * 0.12m;
                    writer.WriteLine($"{"Subtotal:",-48} ${subtotal,10:N2}");
                    writer.WriteLine($"{"Tax (12%):",-48} ${tax,10:N2}");
                    writer.WriteLine($"{"TOTAL:",-48} ${order.TotalAmount,10:N2}");
                    writer.WriteLine("=".PadRight(60, '='));
                    writer.WriteLine();
                    writer.WriteLine("              Thank you for shopping with us!");
                    writer.WriteLine("              This is a computer generated receipt.");
                }

                // Verify and open
                if (File.Exists(fullPath))
                {
                    MessageBox.Show($"✅ Bill generated successfully!\nBill Number: {order.BillNumber}\n\nLocation: {fullPath}",
                                  "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show($"Bill file was not created at: {fullPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating bill: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewPDF_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                string billNumber = button.Tag.ToString();
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string filePath = Path.Combine(desktopPath, "ShoppingBills", $"{billNumber}.txt");

                if (File.Exists(filePath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show($"File not found at:\n{filePath}\n\nPlease check if the bill was generated.",
                                  "Missing File", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: Handle selection change
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to logout?", "Logout",
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                UserSession.ClearSession();
                NavigationService?.Navigate(new LogIn());
            }
        }
    }
}