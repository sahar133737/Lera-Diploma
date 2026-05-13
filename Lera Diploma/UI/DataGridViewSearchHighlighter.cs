using System;
using System.Drawing;
using System.Windows.Forms;
using Lera_Diploma.Forms;

namespace Lera_Diploma.UI
{
    /// <summary>Подсветка вхождения строки поиска в текстовых ячейках грида.</summary>
    public static class DataGridViewSearchHighlighter
    {
        public static void Attach(DataGridView grid, TextBox searchBox)
        {
            if (grid == null || searchBox == null)
                return;
            string Needle() => searchBox.Text?.Trim() ?? "";

            void InvalidateGrid(object _, EventArgs __)
            {
                grid.Invalidate();
            }

            searchBox.TextChanged += InvalidateGrid;
            grid.CellPainting += (sender, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.RowIndex >= grid.Rows.Count)
                    return;
                var needle = Needle();
                if (needle.Length == 0)
                    return;
                var col = grid.Columns[e.ColumnIndex];
                if (col is DataGridViewImageColumn || col is DataGridViewButtonColumn || col is DataGridViewCheckBoxColumn)
                    return;
                var raw = e.FormattedValue?.ToString() ?? "";
                if (raw.Length == 0)
                    return;
                var idx = raw.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                    return;

                e.PaintBackground(e.CellBounds, true);
                e.Paint(e.CellBounds, DataGridViewPaintParts.Border);

                var r = e.CellBounds;
                r.Inflate(-4, -2);
                using (var baseFont = e.CellStyle.Font ?? grid.Font)
                using (var bold = new Font(baseFont, FontStyle.Bold))
                {
                    var g = e.Graphics;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    var x = r.Left;
                    void DrawSeg(string text, Font font, Color color)
                    {
                        if (string.IsNullOrEmpty(text))
                            return;
                        var sz = TextRenderer.MeasureText(g, text, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                        TextRenderer.DrawText(g, text, font, new Rectangle(x, r.Top, sz.Width + 2, r.Height), color, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                        x += sz.Width;
                    }

                    var pre = raw.Substring(0, idx);
                    var mid = raw.Substring(idx, Math.Min(needle.Length, raw.Length - idx));
                    var post = raw.Substring(idx + mid.Length);
                    DrawSeg(pre, baseFont, e.CellStyle.ForeColor);
                    DrawSeg(mid, bold, UiTheme.PrimaryDark);
                    DrawSeg(post, baseFont, e.CellStyle.ForeColor);
                }
                e.Handled = true;
            };
        }
    }
}
