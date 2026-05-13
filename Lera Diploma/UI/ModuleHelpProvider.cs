using System.Windows.Forms;
using Lera_Diploma.Forms;

namespace Lera_Diploma.UI
{
    /// <summary>Справка по модулям: HTML из папки Help (F11 и кнопки «Справка»).</summary>
    public static class ModuleHelpProvider
    {
        public static void ShowHelp(string moduleKey, IWin32Window owner)
        {
            var key = string.IsNullOrWhiteSpace(moduleKey) ? "default" : moduleKey.Trim();
            using (var f = new HelpForm(key))
                f.ShowDialog(owner);
        }

        public static void BindF11(Form form, string defaultModuleKey)
        {
            if (form == null)
                return;
            form.KeyPreview = true;
            form.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.F11)
                    return;
                e.Handled = true;
                e.SuppressKeyPress = true;
                ShowHelp(defaultModuleKey, form);
            };
        }
    }
}
