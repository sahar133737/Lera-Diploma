using System.Windows.Forms;

namespace Lera_Diploma.UI
{
    /// <summary>Единообразные информационные сообщения после успешных операций.</summary>
    public static class UserFeedback
    {
        public static void Info(IWin32Window owner, string message, string title = "Готово") =>
            MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

        public static void Saved(IWin32Window owner, string subject = "Запись") =>
            Info(owner, $"{subject} успешно сохранена.");

        public static void Created(IWin32Window owner, string subject = "Запись") =>
            Info(owner, $"{subject} успешно создана.");

        public static void Deleted(IWin32Window owner, string subject = "Запись") =>
            Info(owner, $"{subject} удалена.");

        public static void Warning(IWin32Window owner, string message, string title = "Внимание") =>
            MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
