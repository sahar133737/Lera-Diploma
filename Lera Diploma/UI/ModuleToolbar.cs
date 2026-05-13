using System.Drawing;
using System.Windows.Forms;

namespace Lera_Diploma.UI
{
    /// <summary>Единый тулбар модулей: перенос строк и высота под русские подписи кнопок.</summary>
    public static class ModuleToolbar
    {
        public static FlowLayoutPanel CreateDockTopToolbar()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(0, 48),
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 0, 0, 8)
            };
        }
    }
}
