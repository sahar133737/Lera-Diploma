using System;
using System.Drawing;
using System.Windows.Forms;
using Lera_Diploma.Forms;

namespace Lera_Diploma.UI
{
    public static class MaterialStyle
    {
        public static void StyleDataGrid(DataGridView g)
        {
            g.BorderStyle = BorderStyle.None;
            g.BackgroundColor = UiTheme.CardSurface;
            g.GridColor = UiTheme.Divider;
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            g.RowHeadersVisible = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.MultiSelect = false;
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(237, 231, 246);
            g.DefaultCellStyle.SelectionForeColor = UiTheme.TextPrimary;
            g.DefaultCellStyle.BackColor = UiTheme.CardSurface;
            g.DefaultCellStyle.ForeColor = UiTheme.TextPrimary;
            g.DefaultCellStyle.Font = new Font("Segoe UI", 9f);
            g.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.PageBackground;
            g.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.TextPrimary;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            g.ColumnHeadersHeight = 36;
            g.RowTemplate.Height = 28;
        }

        public static void StyleToolbarButton(Button b, bool primary = false)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.Cursor = Cursors.Hand;
            b.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            b.Height = 32;
            b.Margin = new Padding(0, 4, 8, 4);
            b.Padding = new Padding(12, 0, 12, 0);
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            if (primary)
            {
                b.BackColor = UiTheme.Primary;
                b.ForeColor = Color.White;
                b.FlatAppearance.MouseOverBackColor = UiTheme.PrimaryDark;
            }
            else
            {
                b.BackColor = Color.FromArgb(236, 236, 241);
                b.ForeColor = UiTheme.TextPrimary;
                b.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 220, 228);
            }
            ApplyButtonMinWidth(b);
        }

        public static void StyleOutlinedButton(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = UiTheme.Divider;
            b.FlatAppearance.BorderSize = 1;
            b.BackColor = UiTheme.CardSurface;
            b.ForeColor = UiTheme.TextPrimary;
            b.Cursor = Cursors.Hand;
            b.Font = new Font("Segoe UI", 9f);
            b.Height = 32;
            b.Margin = new Padding(0, 4, 8, 4);
            b.Padding = new Padding(12, 0, 12, 0);
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ApplyButtonMinWidth(b);
        }

        /// <summary>Гарантирует ширину под русский текст в FlowLayoutPanel.</summary>
        public static void ApplyButtonMinWidth(Button b)
        {
            if (string.IsNullOrEmpty(b.Text))
                return;
            var pad = b.Padding.Left + b.Padding.Right + 8;
            var w = TextRenderer.MeasureText(b.Text, b.Font).Width + pad;
            b.MinimumSize = new Size(Math.Max(b.MinimumSize.Width, w), b.Height);
        }

        public static void StyleTextBox(TextBox t)
        {
            t.BorderStyle = BorderStyle.FixedSingle;
            t.BackColor = UiTheme.CardSurface;
            t.ForeColor = UiTheme.TextPrimary;
            t.Font = new Font("Segoe UI", 9f);
        }

        public static void StyleCombo(ComboBox c)
        {
            c.FlatStyle = FlatStyle.Flat;
            c.Font = new Font("Segoe UI", 9f);
            c.BackColor = UiTheme.CardSurface;
            c.ForeColor = UiTheme.TextPrimary;
        }

        public static Panel WrapInCard(Control inner, Padding? cardPadding = null)
        {
            var card = new Panel
            {
                BackColor = UiTheme.CardSurface,
                Padding = cardPadding ?? new Padding(16),
                Margin = new Padding(0, 0, 0, 12)
            };
            inner.Dock = DockStyle.Fill;
            card.Controls.Add(inner);
            card.Paint += (_, e) =>
            {
                var r = card.ClientRectangle;
                r.Width--;
                r.Height--;
                using (var pen = new Pen(UiTheme.Divider))
                    e.Graphics.DrawRectangle(pen, r);
            };
            return card;
        }
    }
}
