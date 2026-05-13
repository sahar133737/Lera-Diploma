using System;
using System.Threading;
using System.Windows.Forms;
using Lera_Diploma.Forms;
using Lera_Diploma.Infrastructure;
using Lera_Diploma.Security;
using Lera_Diploma.Services;

namespace Lera_Diploma
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, args) =>
            {
                ExceptionLogger.Log(args.Exception);
                MessageBox.Show("Произошла ошибка. Подробности записаны в журнал.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                if (ex != null)
                    ExceptionLogger.Log(ex);
            };

            try
            {
                DatabaseBootstrapper.EnsureDatabase();
            }
            catch (Exception ex)
            {
                ExceptionLogger.Log(ex);
                MessageBox.Show(
                    "Не удалось инициализировать базу данных. Убедитесь, что установлен SQL Server LocalDB и доступна строка подключения FinanceContext в App.config.\r\n\r\n" + ex.Message,
                    "База данных",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            while (true)
            {
                using (var login = new LoginForm())
                {
                    if (login.ShowDialog() != DialogResult.OK)
                        return;
                }

                using (var main = new MainForm())
                {
                    main.ShowDialog();
                    if (!main.LogoutRequested)
                        return;
                }

                CurrentUserContext.Clear();
                RolePermissionService.Clear();
                Thread.Sleep(200);
            }
        }
    }
}
