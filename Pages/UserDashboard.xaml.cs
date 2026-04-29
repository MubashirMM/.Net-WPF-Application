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
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
  
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

            // Initialize QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;

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

                    // Generate PDF Bill using QuestPDF
                    GeneratePDFBill(order);

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

        private void GeneratePDFBill(Order order)
        {
            try
            {
                var orderItems = _db.OrderItems.Where(oi => oi.OrderId == order.Id).ToList();
                decimal subtotal = orderItems.Sum(i => i.Subtotal);
                decimal tax = subtotal * 0.12m;

                // Create PDF document
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(40);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        // Header
                        page.Header()
                            .AlignCenter()
                            .Column(col =>
                            {
                                col.Item().Text("SHOPPING CENTER").FontSize(18).Bold();
                                col.Item().Text("OFFICIAL RECEIPT").FontSize(12).Bold();
                                col.Item().PaddingTop(10).LineHorizontal(1);
                            });

                        // Content
                        page.Content()
                            .Column(col =>
                            {
                                // Bill Details
                                col.Item().PaddingTop(10).Column(details =>
                                {
                                    details.Item().Text($"Bill Number: {order.BillNumber}");
                                    details.Item().Text($"Date: {order.OrderDate:dddd, MMMM dd, yyyy hh:mm tt}");
                                    details.Item().Text($"Cashier: {UserSession.LoggedInUserName}");
                                });

                                col.Item().PaddingTop(10).LineHorizontal(1);

                                // Items Table
                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                    });

                                    // Header
                                    table.Header(header =>
                                    {
                                        header.Cell().Text("Item").Bold();
                                        header.Cell().Text("Qty").Bold().AlignCenter();
                                        header.Cell().Text("Price").Bold().AlignRight();
                                        header.Cell().Text("Total").Bold().AlignRight();
                                    });

                                    // Items
                                    foreach (var item in orderItems)
                                    {
                                        table.Cell().Text(item.ProductName);
                                        table.Cell().Text(item.Quantity.ToString()).AlignCenter();
                                        table.Cell().Text($"${item.UnitPrice:N2}").AlignRight();
                                        table.Cell().Text($"${item.Subtotal:N2}").AlignRight();
                                    }
                                });

                                col.Item().PaddingTop(10).LineHorizontal(1);

                                // Totals 
                                col.Item().AlignRight().Column(totals => 
                                {
                                    totals.Item().Text($"Subtotal: ${subtotal:N2}");
                                    totals.Item().Text($"Tax (12%): ${tax:N2}").FontColor(Colors.Orange.Darken1);
                                    totals.Item().Text($"TOTAL: ${order.TotalAmount:N2}").Bold().FontColor(Colors.Red.Darken2);
                                });
                            });

                        // Footer
                        page.Footer()
                            .AlignCenter()
                            .Column(col => 
                            {
                                col.Item().PaddingTop(20).LineHorizontal(1);
                                col.Item().Text("Thank you for shopping with us!").FontSize(8).FontColor(Colors.Grey.Darken1);
                                col.Item().Text("This is a computer generated receipt.").FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                    });
                });

                // Save to Desktop
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string billsFolder = Path.Combine(desktopPath, "ShoppingBills");

                if (!Directory.Exists(billsFolder))
                    Directory.CreateDirectory(billsFolder);

                string safeFileName = order.BillNumber.Replace(":", "_").Replace("/", "_").Replace("\\", "_");
                string fullPath = Path.Combine(billsFolder, $"{safeFileName}.pdf");

                document.GeneratePdf(fullPath);

                // Verify and open
                if (File.Exists(fullPath))
                {
                    MessageBox.Show($"✅ Bill generated successfully!\nBill Number: {order.BillNumber}\n\nLocation: {fullPath}",
                                  "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF Generation Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewPDF_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                string billNumber = button.Tag.ToString();
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string pdfPath = Path.Combine(desktopPath, "ShoppingBills", $"{billNumber}.pdf");

                if (File.Exists(pdfPath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show($"File not found at:\n{pdfPath}\n\nPlease check if the bill was generated.",
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

