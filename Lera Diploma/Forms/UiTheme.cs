using System.Drawing;

namespace Lera_Diploma.Forms
{
    /// <summary>Светлая палитра в духе Material Dashboard (карточки, фиолетовый акцент).</summary>
    public static class UiTheme
    {
        public static readonly Color PageBackground = Color.FromArgb(245, 245, 246);
        public static readonly Color CardSurface = Color.White;
        public static readonly Color SidebarBg = Color.White;
        public static readonly Color SidebarBorder = Color.FromArgb(230, 230, 235);
        public static readonly Color HeaderBg = Color.White;
        public static readonly Color Primary = Color.FromArgb(156, 39, 176);
        public static readonly Color PrimaryDark = Color.FromArgb(123, 31, 162);
        public static readonly Color Success = Color.FromArgb(76, 175, 80);
        public static readonly Color Warning = Color.FromArgb(255, 152, 0);
        public static readonly Color Danger = Color.FromArgb(244, 67, 54);
        public static readonly Color Info = Color.FromArgb(0, 188, 212);
        public static readonly Color TextPrimary = Color.FromArgb(51, 51, 51);
        public static readonly Color TextMuted = Color.FromArgb(117, 117, 117);
        public static readonly Color Divider = Color.FromArgb(224, 224, 224);

        /// <summary>Фон основной рабочей области (совместимость со старым кодом).</summary>
        public static readonly Color Background = PageBackground;

        /// <summary>Фон карточек / таблиц.</summary>
        public static readonly Color Card = CardSurface;

        /// <summary>Боковая панель.</summary>
        public static readonly Color Sidebar = SidebarBg;

        /// <summary>Акцентные элементы.</summary>
        public static readonly Color Accent = Primary;

        /// <summary>Вторичный текст.</summary>
        public static readonly Color Muted = TextMuted;
    }
}
