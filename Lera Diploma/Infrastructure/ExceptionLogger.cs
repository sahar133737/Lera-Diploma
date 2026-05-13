using System;
using System.IO;
using System.Windows.Forms;

namespace Lera_Diploma.Infrastructure
{
    public static class ExceptionLogger
    {
        public static void Log(Exception ex)
        {
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LeraDiploma", "logs");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, "errors.log");
                File.AppendAllText(path, $"{DateTime.UtcNow:O}\r\n{ex}\r\n---\r\n");
            }
            catch
            {
                // ignore
            }
        }

        public static void ShowFatal(IWin32Window owner, Exception ex)
        {
            Log(ex);
            MessageBox.Show(owner, "Произошла ошибка. Подробности записаны в журнал.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
