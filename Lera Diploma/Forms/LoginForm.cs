using System;
using System.Drawing;
using System.Windows.Forms;
using Lera_Diploma.Infrastructure;
using Lera_Diploma.Security;
using Lera_Diploma.Services;
using Lera_Diploma.UI;

namespace Lera_Diploma.Forms
{
    public partial class LoginForm : Form
    {
        private readonly AuthService _auth = new AuthService();

        public LoginForm()
        {
            InitializeComponent();
            BackColor = UiTheme.PageBackground;
            ForeColor = UiTheme.TextPrimary;
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                if (_auth.TryLogin(txtLogin.Text, txtPassword.Text, out var err))
                {
                    new AuditService().Write(CurrentUserContext.UserId, "Login", "User", CurrentUserContext.Login, "Успешный вход");
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show(this, err ?? "Ошибка входа.", "Вход", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                ExceptionLogger.Log(ex);
                MessageBox.Show(this, "Системная ошибка при входе.", "Вход", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
