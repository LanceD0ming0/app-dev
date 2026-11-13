using StockSmart.Models;
using StockSmart.Services;

namespace StockSmart.Forms;

public class LoginForm : Form
{
    private readonly OfflineDataService _db;
    private readonly TextBox _username = new() { Width = 230 };
    private readonly TextBox _password = new() { Width = 230, UseSystemPasswordChar = true };
    public AppUser? CurrentUser { get; private set; }

    public LoginForm(OfflineDataService db)
    {
        _db = db;
        Text = "StockSmart - Login";
        Width = 390; Height = 280; StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
        BackColor = Color.FromArgb(245, 243, 255);
        var title = new Label { Text = "StockSmart", Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.FromArgb(91, 61, 181), AutoSize = true, Left = 30, Top = 20 };
        var userLabel = new Label { Text = "Username", Left = 32, Top = 83, AutoSize = true };
        _username.Left = 125; _username.Top = 78;
        var passLabel = new Label { Text = "Password", Left = 32, Top = 123, AutoSize = true };
        _password.Left = 125; _password.Top = 118;
        var login = new Button { Text = "Log in", Left = 125, Top = 165, Width = 110, Height = 34,
            BackColor = Color.FromArgb(91, 61, 181), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        login.Click += (_, _) => DoLogin();
        AcceptButton = login;
        Controls.AddRange(new Control[] { title, userLabel, _username, passLabel, _password, login });
    }

    private void DoLogin()
    {
        CurrentUser = _db.Authenticate(_username.Text.Trim(), _password.Text);
        if (CurrentUser is null)
        {
            MessageBox.Show("Invalid username or password.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        DialogResult = DialogResult.OK;
        Close();
    }
}
