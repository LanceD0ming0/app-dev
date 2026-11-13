using StockSmart.Models;
using StockSmart.Services;

namespace StockSmart.Forms;

public class MainForm : Form
{
    private readonly OfflineDataService _db;
    private readonly AppUser _user;
    private readonly Panel _content = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(247, 248, 252), Padding = new Padding(26) };
    private readonly Panel _sidebar = new() { Dock = DockStyle.Left, Width = 220, BackColor = Color.FromArgb(42, 31, 76) };
    private readonly Label _pageTitle = new() { AutoSize = true, Font = new Font("Segoe UI", 21, FontStyle.Bold), ForeColor = Color.FromArgb(37, 34, 51) };
    private readonly DataGridView _productsGrid = Grid();
    private readonly DataGridView _customersGrid = Grid();
    private readonly DataGridView _reportGrid = Grid();
    private readonly TextBox _pName = new(), _pCategory = new(), _pPrice = new(), _pStock = new(), _pReorder = new(), _pSearch = new();
    private readonly TextBox _cName = new(), _cContact = new(), _cPayment = new();
    private readonly TextBox _saleQty = new() { Text = "1", Width = 65 };
    private readonly ComboBox _saleProduct = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230 };
    private readonly ComboBox _saleCustomer = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 210 };
    private readonly ComboBox _paymentType = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
    private readonly DateTimePicker _from = new() { Value = DateTime.Today.AddDays(-30), Width = 135 };
    private readonly DateTimePicker _to = new() { Value = DateTime.Today.AddDays(1), Width = 135 };
    private readonly TextBox _salesCustomerFilter = new() { Width = 135, PlaceholderText = "Customer name" };
    private readonly List<SaleItem> _cart = new();
    private Guid? _selectedProductId;
    private decimal _cartTotal;

    public MainForm(OfflineDataService db, AppUser user)
    {
        _db = db; _user = user;
        Text = "StockSmart | Store Management";
        Width = 1380; Height = 850; MinimumSize = new Size(1120, 720);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);
        Controls.Add(_content); Controls.Add(_sidebar);
        BuildSidebar();
        RefreshAll();
        if (_user.Role == "Owner") ShowDashboard();
        else ShowSales();
    }

    private static DataGridView Grid() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
        BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
        RowHeadersVisible = false, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
        GridColor = Color.FromArgb(235, 235, 242), RowTemplate = { Height = 34 },
        EnableHeadersVisualStyles = false
    };

    private void StyleGrid(DataGridView grid)
    {
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 238, 250);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(65, 49, 105);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 9);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 224, 250);
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(40, 30, 65);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 253);
    }

    private void BuildSidebar()
    {
        var brand = new Panel { Dock = DockStyle.Top, Height = 112, Padding = new Padding(20, 22, 10, 10) };
        brand.Controls.Add(new Label { Text = "StockSmart", AutoSize = true, ForeColor = Color.White,
            Font = new Font("Segoe UI", 20, FontStyle.Bold), Location = new Point(12, 14) });
        brand.Controls.Add(new Label { Text = "STORE MANAGEMENT", AutoSize = true, ForeColor = Color.FromArgb(190, 178, 222),
            Font = new Font("Segoe UI", 8, FontStyle.Bold), Location = new Point(15, 53) });
        _sidebar.Controls.Add(brand);

        var userPanel = new Panel { Dock = DockStyle.Bottom, Height = 76, Padding = new Padding(16), BackColor = Color.FromArgb(34, 25, 62) };
        userPanel.Controls.Add(new Label { Text = $"{_user.Username}  •  {_user.Role}", ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, Location = new Point(14, 17) });
        var logout = new Button { Text = "Log out", ForeColor = Color.FromArgb(218, 208, 242), BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat, Width = 80, Height = 25, Location = new Point(10, 42) };
        logout.FlatAppearance.BorderSize = 0;
        logout.Click += (_, _) => { DialogResult = DialogResult.Retry; Close(); };
        userPanel.Controls.Add(logout); _sidebar.Controls.Add(userPanel);

        var nav = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(10, 12, 10, 10), AutoScroll = true };
        if (_user.Role == "Owner")
        {
            AddNav(nav, "▦   Dashboard", ShowDashboard);
            AddNav(nav, "▣   Products", ShowProducts);
        }
        AddNav(nav, "▤   Sales / POS", ShowSales);
        if (_user.Role == "Owner")
        {
            AddNav(nav, "♙   Customers & Utang", ShowCustomers);
            AddNav(nav, "▥   Reports", ShowReports);
        }
        _sidebar.Controls.Add(nav);
        nav.BringToFront();
    }

    private void AddNav(FlowLayoutPanel nav, string title, Action action)
    {
        var b = new Button { Text = title, Width = 194, Height = 46, TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0), FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(226, 220, 240),
            BackColor = Color.FromArgb(42, 31, 76), Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(0, 3, 0, 3) };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(76, 57, 125);
        b.Click += (_, _) => action(); nav.Controls.Add(b);
    }

    private void PreparePage(string title, string subtitle)
    {
        _content.Controls.Clear();
        var heading = new Panel { Dock = DockStyle.Top, Height = 76 };
        _pageTitle.Text = title; _pageTitle.Location = new Point(0, 0);
        heading.Controls.Add(_pageTitle);
        heading.Controls.Add(new Label { Text = subtitle, AutoSize = true, ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 9), Location = new Point(2, 42) });
        _content.Controls.Add(heading);
    }

    private Panel Card(string title, string value, string note, Color accent)
    {
        var card = new Panel { BackColor = Color.White, Width = 245, Height = 116, Margin = new Padding(0, 0, 16, 14), Padding = new Padding(16) };
        card.Controls.Add(new Label { Text = title.ToUpper(), AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 8, FontStyle.Bold), Location = new Point(16, 13) });
        card.Controls.Add(new Label { Text = value, AutoSize = true, ForeColor = accent, Font = new Font("Segoe UI", 21, FontStyle.Bold), Location = new Point(14, 35) });
        card.Controls.Add(new Label { Text = note, AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 8), Location = new Point(16, 82) });
        return card;
    }

    private void ShowDashboard()
    {
        PreparePage("Dashboard", "Overview of your sari-sari store today.");
        var products = _db.GetProducts();
        var customers = _db.GetCustomers();
        var sales = _db.GetSales(DateTime.Today, DateTime.Today.AddDays(1));
        var low = products.Count(x => x.Stock <= x.ReorderLevel);
        var cards = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 145, WrapContents = true, Padding = new Padding(0, 12, 0, 0) };
        cards.Controls.Add(Card("Today's sales", $"₱{sales.Sum(x => x.Total):N2}", $"{sales.Count} transactions today", Color.FromArgb(91, 61, 181)));
        cards.Controls.Add(Card("Products", products.Count.ToString(), "Products in inventory", Color.FromArgb(36, 145, 112)));
        cards.Controls.Add(Card("Low stock", low.ToString(), "Products at or below reorder level", Color.FromArgb(218, 139, 43)));
        cards.Controls.Add(Card("Unpaid utang", $"₱{customers.Sum(x => x.Balance):N2}", $"{customers.Count(x => x.Balance > 0)} customers with balance", Color.FromArgb(190, 75, 100)));
        _content.Controls.Add(cards);

        var lower = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 8, 0, 0) };
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58)); lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        var recent = Grid(); StyleGrid(recent);
        recent.DataSource = _db.GetSales().OrderByDescending(x => x.SaleDate).Take(8).Select(x => new { Date = x.SaleDate.ToString("g"), x.CustomerName, x.PaymentType, x.Total }).ToList();
        lower.Controls.Add(Section("Recent sales", recent), 0, 0);
        var lowGrid = Grid(); StyleGrid(lowGrid);
        lowGrid.DataSource = products.Where(x => x.Stock <= x.ReorderLevel).Select(x => new { x.Name, x.Stock, x.ReorderLevel }).ToList();
        lower.Controls.Add(Section("Low-stock products", lowGrid), 1, 0);
        _content.Controls.Add(lower);
    }

    private Panel Section(string title, Control body)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(14), Margin = new Padding(0, 0, 12, 0) };
        panel.Controls.Add(body);
        panel.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 34, Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.FromArgb(55, 45, 78) });
        return panel;
    }

    private Button Btn(string text, EventHandler click)
    {
        var b = new Button { Text = text, AutoSize = true, Height = 34, BackColor = Color.FromArgb(91, 61, 181),
            ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(4) };
        b.FlatAppearance.BorderSize = 0; b.Click += click; return b;
    }
    private static Label Lbl(string text) => new() { Text = text, AutoSize = true, Padding = new Padding(3, 8, 3, 3) };
    private static FlowLayoutPanel Toolbar() => new() { Dock = DockStyle.Top, Height = 105, WrapContents = true, Padding = new Padding(0, 5, 0, 5) };

    private void ShowProducts()
    {
        PreparePage("Products", "Add, update, search and monitor your store inventory.");
        var top = Toolbar();
        foreach (var pair in new[] { ("Product name", _pName), ("Category", _pCategory), ("Price", _pPrice), ("Stock", _pStock), ("Reorder level", _pReorder) })
        { pair.Item2.Width = 112; top.Controls.Add(Lbl(pair.Item1)); top.Controls.Add(pair.Item2); }
        top.Controls.Add(Btn("＋ Add", (_, _) => AddProduct()));
        top.Controls.Add(Btn("Update", (_, _) => UpdateProduct()));
        top.Controls.Add(Btn("Delete", (_, _) => DeleteProduct()));
        top.Controls.Add(Lbl("Search")); _pSearch.Width = 130; top.Controls.Add(_pSearch);
        top.Controls.Add(Btn("Search", (_, _) => LoadProducts()));
        top.Controls.Add(Btn("Low stock", (_, _) => LoadProducts(true)));
        StyleGrid(_productsGrid); _productsGrid.DataSource = null;
        _productsGrid.SelectionChanged -= ProductSelected;
        _productsGrid.SelectionChanged += ProductSelected;
        _content.Controls.Add(_productsGrid); _content.Controls.Add(top);
        LoadProducts();
    }
    private void ProductSelected(object? sender, EventArgs e)
    {
        if (_productsGrid.CurrentRow?.DataBoundItem is Product p)
        { _selectedProductId = p.Id; _pName.Text = p.Name; _pCategory.Text = p.Category; _pPrice.Text = p.Price.ToString(); _pStock.Text = p.Stock.ToString(); _pReorder.Text = p.ReorderLevel.ToString(); }
        else _selectedProductId = null;
    }
    private void LoadProducts(bool lowStockOnly = false)
    {
        var list = _db.GetProducts();
        var term = _pSearch.Text.Trim();
        if (term.Length > 0) list = list.Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || p.Category.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        if (lowStockOnly) list = list.Where(p => p.Stock <= p.ReorderLevel).ToList();
        _productsGrid.DataSource = list;
    }
    private Product ReadProduct()
    {
        if (string.IsNullOrWhiteSpace(_pName.Text)) throw new ArgumentException("Product name is required.");
        if (!decimal.TryParse(_pPrice.Text, out var price) || price < 0) throw new ArgumentException("Enter a valid price.");
        if (!int.TryParse(_pStock.Text, out var stock) || stock < 0) throw new ArgumentException("Enter a valid stock.");
        if (!int.TryParse(_pReorder.Text, out var reorder) || reorder < 0) throw new ArgumentException("Enter a valid reorder level.");
        return new Product { Name = _pName.Text.Trim(), Category = _pCategory.Text.Trim(), Price = price, Stock = stock, ReorderLevel = reorder };
    }
    private void AddProduct() { try { _db.AddProduct(ReadProduct()); ClearProduct(); LoadProducts(); } catch (Exception ex) { Error(ex); } }
    private void UpdateProduct()
    {
        try
        {
            if (_selectedProductId is null)
                throw new InvalidOperationException("Select a product first.");

            // Keep the selected in-memory product ID when updating the record.
            var updated = ReadProduct();
            updated.Id = _selectedProductId.Value;
            bool updatedSuccessfully = _db.UpdateProduct(updated);
            if (!updatedSuccessfully)
                throw new InvalidOperationException("The selected product no longer exists. Refresh the list and try again.");

            ClearProduct();
            LoadProducts();
        }
        catch (Exception ex) { Error(ex); }
    }
    private void DeleteProduct() { try { if (_selectedProductId is null) throw new InvalidOperationException("Select a product first."); if (MessageBox.Show("Delete this product?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return; _db.DeleteProduct(_selectedProductId.Value); ClearProduct(); LoadProducts(); } catch (Exception ex) { Error(ex); } }
    private void ClearProduct() { _pName.Clear(); _pCategory.Clear(); _pPrice.Clear(); _pStock.Clear(); _pReorder.Clear(); _selectedProductId = null; }

    private void ShowSales()
    {
        PreparePage("Sales / Point of Sale", "Select products, build the cart and complete cash or credit transactions.");
        var top = Toolbar();
        top.Controls.Add(Lbl("Product")); top.Controls.Add(_saleProduct);
        top.Controls.Add(Lbl("Quantity")); top.Controls.Add(_saleQty);
        top.Controls.Add(Btn("Add to cart", (_, _) => AddToCart()));
        top.Controls.Add(Lbl("Payment type"));
        var previousPaymentType = _paymentType.SelectedItem?.ToString() ?? "Cash";
        _paymentType.Items.Clear();
        _paymentType.Items.AddRange(new object[] { "Cash", "Credit" });
        _paymentType.SelectedItem = previousPaymentType;
        if (_paymentType.SelectedIndex < 0) _paymentType.SelectedIndex = 0;
        top.Controls.Add(_paymentType);
        top.Controls.Add(Lbl("Customer for credit")); top.Controls.Add(_saleCustomer);
        top.Controls.Add(Btn("Complete sale", (_, _) => CompleteSale()));
        var cartGrid = Grid(); StyleGrid(cartGrid);
        cartGrid.DataSource = _cart.Select(x => new { x.ProductName, x.UnitPrice, x.Quantity, x.LineTotal }).ToList();
        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.White };
        bottom.Controls.Add(new Label { Text = $"CART TOTAL     ₱{_cart.Sum(x => x.LineTotal):N2}", AutoSize = true, Font = new Font("Segoe UI", 17, FontStyle.Bold), ForeColor = Color.FromArgb(91, 61, 181), Location = new Point(15, 12) });
        var clear = Btn("Clear cart", (_, _) => { _cart.Clear(); ShowSales(); }); clear.Location = new Point(310, 12); bottom.Controls.Add(clear);
        _content.Controls.Add(cartGrid); _content.Controls.Add(bottom); _content.Controls.Add(top);
        LoadProductChoices(); LoadCustomerChoices();
    }
    private void AddToCart()
    {
        try
        {
            if (_saleProduct.SelectedItem is not Product p) throw new InvalidOperationException("Select a product.");
            if (!int.TryParse(_saleQty.Text, out int qty) || qty <= 0) throw new ArgumentException("Quantity must be a positive whole number.");
            int already = _cart.Where(x => x.ProductId == p.Id).Sum(x => x.Quantity);
            if (already + qty > p.Stock) throw new InvalidOperationException("Quantity exceeds available stock.");
            var item = _cart.FirstOrDefault(x => x.ProductId == p.Id);
            if (item == null) _cart.Add(new SaleItem { ProductId = p.Id, ProductName = p.Name, UnitPrice = p.Price, Quantity = qty, LineTotal = p.Price * qty });
            else { item.Quantity += qty; item.LineTotal = item.UnitPrice * item.Quantity; }
            ShowSales();
        } catch (Exception ex) { Error(ex); }
    }
    private void CompleteSale()
    {
        try
        {
            if (_cart.Count == 0) throw new InvalidOperationException("Cart is empty.");
            string type = _paymentType.SelectedItem?.ToString() ?? "Cash";
            var customer = _saleCustomer.SelectedItem as Customer;
            if (type == "Credit" && customer == null) throw new InvalidOperationException("Select a customer for a credit sale.");
            var sale = new Sale { SaleDate = DateTime.Now, CashierUsername = _user.Username, PaymentType = type,
                CustomerId = type == "Credit" ? customer!.Id : null, CustomerName = type == "Credit" ? customer!.Name : "",
                Total = _cart.Sum(x => x.LineTotal), Items = _cart.Select(x => new SaleItem { ProductId = x.ProductId, ProductName = x.ProductName, UnitPrice = x.UnitPrice, Quantity = x.Quantity, LineTotal = x.LineTotal }).ToList() };
            _db.SaveSale(sale); _cart.Clear(); MessageBox.Show("Sale saved successfully. Inventory has been updated.", "StockSmart", MessageBoxButtons.OK, MessageBoxIcon.Information); RefreshAll(); ShowSales();
        } catch (Exception ex) { Error(ex); }
    }

    private void ShowCustomers()
    {
        PreparePage("Customers & Utang", "Manage customer records and record payments against outstanding balances.");
        var top = Toolbar();
        foreach (var pair in new[] { ("Customer name", _cName), ("Contact number", _cContact), ("Payment amount", _cPayment) })
        { pair.Item2.Width = 150; top.Controls.Add(Lbl(pair.Item1)); top.Controls.Add(pair.Item2); }
        top.Controls.Add(Btn("Add customer", (_, _) => AddCustomer()));
        top.Controls.Add(Btn("Record payment", (_, _) => RecordPayment()));
        StyleGrid(_customersGrid); _customersGrid.DataSource = null;
        _content.Controls.Add(_customersGrid); _content.Controls.Add(top); LoadCustomers();
    }
    private void LoadCustomers() { _customersGrid.DataSource = _db.GetCustomers(); }
    private void AddCustomer() { try { if (string.IsNullOrWhiteSpace(_cName.Text)) throw new ArgumentException("Customer name is required."); _db.AddCustomer(new Customer { Name = _cName.Text.Trim(), ContactNumber = _cContact.Text.Trim() }); _cName.Clear(); _cContact.Clear(); LoadCustomers(); LoadCustomerChoices(); } catch (Exception ex) { Error(ex); } }
    private void RecordPayment()
    {
        try
        {
            if (_customersGrid.CurrentRow?.DataBoundItem is not Customer c) throw new InvalidOperationException("Select a customer.");
            if (!decimal.TryParse(_cPayment.Text, out var amount) || amount <= 0) throw new ArgumentException("Enter a valid payment amount.");
            _db.RecordPayment(c, amount, _user.Username); _cPayment.Clear(); LoadCustomers();
            MessageBox.Show("Payment recorded. Customer balance updated.");
        } catch (Exception ex) { Error(ex); }
    }

    private void ShowReports()
    {
        PreparePage("Reports & Monitoring", "Review sales performance, best sellers, low stock and unpaid customer balances.");
        var top = Toolbar();
        top.Controls.Add(Lbl("From")); top.Controls.Add(_from); top.Controls.Add(Lbl("To")); top.Controls.Add(_to);
        top.Controls.Add(Lbl("Customer")); top.Controls.Add(_salesCustomerFilter);
        top.Controls.Add(Btn("Sales summary", (_, _) => LoadSalesReport()));
        top.Controls.Add(Btn("Best sellers", (_, _) => LoadBestSellers()));
        top.Controls.Add(Btn("Low stock", (_, _) => LoadLowStockReport()));
        top.Controls.Add(Btn("Unpaid balances", (_, _) => LoadUnpaidReport()));
        StyleGrid(_reportGrid); _content.Controls.Add(_reportGrid); _content.Controls.Add(top);
    }
    private void LoadSalesReport()
    {
        var start = _from.Value.Date; var end = _to.Value.Date.AddDays(1);
        var sales = _db.GetSales(start, end);
        var term = _salesCustomerFilter.Text.Trim();
        if (term.Length > 0) sales = sales.Where(x => x.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        _reportGrid.DataSource = sales.Select(x => new { x.SaleDate, x.CustomerName, x.PaymentType, x.Total, x.CashierUsername }).ToList();
        MessageBox.Show($"Sales total: ₱{sales.Sum(x => x.Total):N2}\nTransactions: {sales.Count}");
    }
    private void LoadBestSellers() { var result = _db.GetSales().SelectMany(s => s.Items).GroupBy(i => i.ProductName).Select(g => new { Product = g.Key, QuantitySold = g.Sum(x => x.Quantity), Sales = g.Sum(x => x.LineTotal) }).OrderByDescending(x => x.QuantitySold).ToList(); _reportGrid.DataSource = result; }
    private void LoadLowStockReport() { _reportGrid.DataSource = _db.GetProducts().Where(p => p.Stock <= p.ReorderLevel).Select(p => new { p.Name, p.Category, p.Stock, p.ReorderLevel }).ToList(); }
    private void LoadUnpaidReport() { _reportGrid.DataSource = _db.GetUnpaidCustomers().Select(c => new { c.Name, c.ContactNumber, c.Balance }).ToList(); }

    private void LoadProductChoices() { var list = _db.GetProducts(); _saleProduct.DataSource = list; _saleProduct.DisplayMember = "Name"; _saleProduct.ValueMember = "Id"; }
    private void LoadCustomerChoices() { var list = _db.GetCustomers(); _saleCustomer.DataSource = list; _saleCustomer.DisplayMember = "Name"; _saleCustomer.ValueMember = "Id"; }
    private void RefreshAll() { LoadProductChoices(); LoadCustomerChoices(); }
    private void Error(Exception ex) => MessageBox.Show(ex.Message, "StockSmart", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
