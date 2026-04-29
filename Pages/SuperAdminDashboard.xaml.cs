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
    public partial class SuperAdminDashboard : Page
    {
        private AppDbContext _db;
        private DateTime _currentStartDate;
        private DateTime _currentEndDate;

        public class ProductSalesReport
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public int TotalQuantity { get; set; }
            public decimal TotalRevenue { get; set; }
            public int OrderCount { get; set; }
        }

        public class OrderView
        {
            public int Id { get; set; }
            public string BillNumber { get; set; }
            public string UserName { get; set; }
            public DateTime OrderDate { get; set; }
            public decimal TotalAmount { get; set; }
            public string Status { get; set; }
        }

        public class UserView
        {
            public int Id { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
            public int OrderCount { get; set; }
            public decimal TotalSpent { get; set; }
        }

        public SuperAdminDashboard()
        {
            InitializeComponent();
            _db = new AppDbContext();

            // Set default dates
            _currentStartDate = DateTime.Today;
            _currentEndDate = DateTime.Today;
            StartDatePicker.SelectedDate = _currentStartDate;
            EndDatePicker.SelectedDate = _currentEndDate;

            WelcomeText.Text = $"Welcome, {UserSession.LoggedInUserName}!";
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTodayReport();
            LoadAllOrders();
            LoadUsers();
        }

        private void TodayReport_Click(object sender, RoutedEventArgs e)
        {
            _currentStartDate = DateTime.Today;
            _currentEndDate = DateTime.Today;
            StartDatePicker.SelectedDate = _currentStartDate;
            EndDatePicker.SelectedDate = _currentEndDate;
            GenerateReport();
        }

        private void WeekReport_Click(object sender, RoutedEventArgs e)
        {
            _currentStartDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            _currentEndDate = DateTime.Today;
            StartDatePicker.SelectedDate = _currentStartDate;
            EndDatePicker.SelectedDate = _currentEndDate;
            GenerateReport();
        }

        private void MonthReport_Click(object sender, RoutedEventArgs e)
        {
            _currentStartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _currentEndDate = DateTime.Today;
            StartDatePicker.SelectedDate = _currentStartDate;
            EndDatePicker.SelectedDate = _currentEndDate;
            GenerateReport();
        }

        private void AllTimeReport_Click(object sender, RoutedEventArgs e)
        {
            _currentStartDate = DateTime.MinValue;
            _currentEndDate = DateTime.MaxValue;
            StartDatePicker.SelectedDate = null;
            EndDatePicker.SelectedDate = null;
            GenerateReport();
        }

        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            if (StartDatePicker.SelectedDate.HasValue)
                _currentStartDate = StartDatePicker.SelectedDate.Value;
            if (EndDatePicker.SelectedDate.HasValue)
                _currentEndDate = EndDatePicker.SelectedDate.Value;

            GenerateReport();
        }

        private void LoadTodayReport()
        {
            TodayReport_Click(null, null);
        }

        private void GenerateReport()
        {
            try
            {
                // Get orders within date range
                var orders = _db.Orders
                    .Where(o => o.OrderDate >= _currentStartDate && o.OrderDate <= _currentEndDate)
                    .ToList();

                var orderIds = orders.Select(o => o.Id).ToList();

                // Get order items for these orders
                var orderItems = _db.OrderItems
                    .Where(oi => orderIds.Contains(oi.OrderId))
                    .ToList();

                // Get all products for names
                var products = _db.Products.ToList();

                // Generate product sales report
                var salesReport = orderItems
                    .GroupBy(oi => oi.ProductId)
                    .Select(g => new ProductSalesReport
                    {
                        ProductId = g.Key,
                        ProductName = products.FirstOrDefault(p => p.ProductId == g.Key)?.ProductName ?? "Unknown",
                        TotalQuantity = g.Sum(x => x.Quantity),
                        TotalRevenue = g.Sum(x => x.Subtotal),
                        OrderCount = g.Select(x => x.OrderId).Distinct().Count()
                    })
                    .OrderByDescending(r => r.TotalRevenue)
                    .ToList();

                SalesReportGrid.ItemsSource = salesReport;

                // Update summary cards
                decimal totalSales = orders.Sum(o => o.TotalAmount);
                int totalItemsSold = orderItems.Sum(oi => oi.Quantity);
                decimal avgOrder = orders.Any() ? totalSales / orders.Count() : 0;
                int uniqueProducts = salesReport.Count;

                TotalSalesText.Text = $"₱{totalSales:N2}";
                TotalItemsSoldText.Text = totalItemsSold.ToString();
                AverageOrderText.Text = $"₱{avgOrder:N2}";
                UniqueProductsText.Text = $"{uniqueProducts} Products";

                TotalOrdersCount.Text = $"{orders.Count()} Orders";

                // Get customer stats
                var uniqueCustomers = orders.Select(o => o.UserId).Distinct().Count();
                TotalCustomersText.Text = uniqueCustomers.ToString();

                // New customers in this period
                var newCustomers = _db.Users
                    .Where(u => u.CreatedDate >= _currentStartDate && u.CreatedDate <= _currentEndDate)
                    .Count();
                NewCustomersText.Text = $"{newCustomers} New";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAllOrders()
        {
            try
            {
                var orders = _db.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                var users = _db.Users.ToDictionary(u => u.Id, u => u.Name);

                var orderViews = orders.Select(o => new OrderView
                {
                    Id = o.Id,
                    BillNumber = o.BillNumber,
                    UserName = users.ContainsKey(o.UserId) ? users[o.UserId] : "Unknown",
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status
                }).ToList();

                AllOrdersGrid.ItemsSource = orderViews;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading orders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUsers()
        {
            try
            {
                var users = _db.Users.ToList();
                var orders = _db.Orders.ToList();
                var orderItems = _db.OrderItems.ToList();

                var userViews = users.Select(u => new UserView
                {
                    Id = u.Id,
                    FullName = u.Name,
                    Email = u.Email,
                    Role = u.Role.ToString(),
                    OrderCount = orders.Count(o => o.UserId == u.Id),
                    TotalSpent = orders
                        .Where(o => o.UserId == u.Id)
                        .Sum(o => o.TotalAmount)
                }).ToList();

                UsersGrid.ItemsSource = userViews;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewProductDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int productId = (int)button.Tag;

            var product = _db.Products.FirstOrDefault(p => p.ProductId == productId);
            if (product != null)
            {
                var orders = _db.OrderItems
     .Where(oi => oi.ProductId == productId)
     .Select(oi => new
     {
         oi.Order.BillNumber,
         oi.Order.OrderDate,
         oi.Quantity,
         oi.Subtotal,
         Customer = _db.Users.FirstOrDefault(u => u.Id == oi.Order.UserId) == null ? "Unknown" : _db.Users.FirstOrDefault(u => u.Id == oi.Order.UserId).Name
     })
     .OrderByDescending(o => o.OrderDate)
     .ToList();

                string details = $"Product: {product.ProductName}\nPrice: ₱{product.ProductPrice:N2}\n\nOrder History:\n";
                details += "=".PadRight(60, '=') + "\n";
                details += $"{"Bill No",-15} {"Customer",-15} {"Date",-12} {"Qty",5} {"Total",10}\n";
                details += "-".PadRight(60, '-') + "\n";

                foreach (var order in orders)
                {
                    details += $"{order.BillNumber,-15} {(order.Customer?.Length > 15 ? order.Customer.Substring(0, 12) + "..." : order.Customer ?? "Unknown"),-15} {order.OrderDate:yyyy-MM-dd,-12} {order.Quantity,5} ₱{order.Subtotal,8:N2}\n";
                }

                var scrollViewer = new ScrollViewer { Height = 400, Width = 600 };
                var textBox = new TextBox { Text = details, FontFamily = new System.Windows.Media.FontFamily("Consolas"), FontSize = 11, TextWrapping = TextWrapping.Wrap };
                scrollViewer.Content = textBox;

                var window = new Window
                {
                    Title = $"Product Details - {product.ProductName}",
                    Content = scrollViewer,
                    Width = 650,
                    Height = 500,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                window.ShowDialog();
            }
        }

        private void ViewOrderBill_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string billNumber = button.Tag.ToString();

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string pdfPath = Path.Combine(desktopPath, "ShoppingBills", $"{billNumber}.pdf");
            string txtPath = Path.Combine(desktopPath, "ShoppingBills", $"{billNumber}.txt");

            if (File.Exists(pdfPath))
            {
                Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
            }
            else if (File.Exists(txtPath))
            {
                Process.Start(new ProcessStartInfo(txtPath) { UseShellExecute = true });
            }
            else
            {
                // Try to find the file
                string shoppingBillsFolder = Path.Combine(desktopPath, "ShoppingBills");
                if (Directory.Exists(shoppingBillsFolder))
                {
                    var files = Directory.GetFiles(shoppingBillsFolder, $"{billNumber}.*");
                    if (files.Any())
                    {
                        Process.Start(new ProcessStartInfo(files.First()) { UseShellExecute = true });
                        return;
                    }
                }
                MessageBox.Show($"No bill file found for: {billNumber}", "Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ViewUserOrders_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int userId = (int)button.Tag;

            var user = _db.Users.FirstOrDefault(u => u.Id == userId);
            var userOrders = _db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            var displayText = $"Orders for: {user?.Name} ({user?.Email})\n";
            displayText += "=".PadRight(60, '=') + "\n\n";

            foreach (var order in userOrders)
            {
                displayText += $"Bill: {order.BillNumber}\n";
                displayText += $"Date: {order.OrderDate:yyyy-MM-dd HH:mm}\n";
                displayText += $"Total: ₱{order.TotalAmount:N2}\n";
                displayText += $"Status: {order.Status}\n";
                displayText += "-".PadRight(40, '-') + "\n";
            }

            var scrollViewer = new ScrollViewer { Height = 400, Width = 500 };
            var textBox = new TextBox { Text = displayText, FontFamily = new System.Windows.Media.FontFamily("Consolas"), FontSize = 11, TextWrapping = TextWrapping.Wrap };
            scrollViewer.Content = textBox;

            var window = new Window
            {
                Title = $"User Orders - {user?.Name}",
                Content = scrollViewer,
                Width = 550,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            window.ShowDialog();
        }

        private void ManageProducts_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new AdminDashBoard());
        }

        //private void ManageUsers_Click(object sender, RoutedEventArgs e)
        //{
        //    //Navigate to User Management page(if you have one)
        //    MessageBox.Show("User management feature coming soon!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        //}

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