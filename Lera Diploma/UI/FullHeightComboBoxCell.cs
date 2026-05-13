using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace Lera_Diploma.UI
{
    /// <summary>
    /// Ячейка-комбо для грида: в режиме отображения стандартный DataGridView рисует кнопку раскрытия
    /// ниже высоты строки; здесь левая часть — текст, правая — системная кнопка на всю высоту ячейки.
    /// </summary>
    public sealed class FullHeightComboBoxCell : DataGridViewComboBoxCell
    {
        private static readonly Color Divider = Color.FromArgb(224, 224, 224);

        public override object Clone()
        {
            return (FullHeightComboBoxCell)base.Clone();
        }

        protected override void Paint(
            Graphics graphics,
            Rectangle clipBounds,
            Rectangle cellBounds,
            int rowIndex,
            DataGridViewElementStates elementState,
            object value,
            object formattedValue,
            string errorText,
            DataGridViewCellStyle cellStyle,
            DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            if (!ComboBoxRenderer.IsSupported)
            {
                base.Paint(graphics, clipBounds, cellBounds, rowIndex, elementState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);
                return;
            }

            if (!string.IsNullOrEmpty(errorText))
            {
                base.Paint(graphics, clipBounds, cellBounds, rowIndex, elementState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);
                return;
            }

            graphics.SetClip(clipBounds);

            try
            {
                var selected = (elementState & DataGridViewElementStates.Selected) != 0;
                var back = selected ? cellStyle.SelectionBackColor : cellStyle.BackColor;
                var fore = selected ? cellStyle.SelectionForeColor : cellStyle.ForeColor;

                if ((paintParts & DataGridViewPaintParts.Background) != 0)
                {
                    using (var b = new SolidBrush(back))
                        graphics.FillRectangle(b, cellBounds);
                }

                if ((paintParts & DataGridViewPaintParts.Border) != 0 && DataGridView != null)
                {
                    using (var p = new Pen(DataGridView.GridColor))
                        graphics.DrawRectangle(p, cellBounds.X, cellBounds.Y, cellBounds.Width - 1, cellBounds.Height - 1);
                }

                if ((paintParts & DataGridViewPaintParts.ContentForeground) == 0)
                    return;

                var inner = Rectangle.Inflate(cellBounds, -1, -1);
                if (inner.Width <= 4 || inner.Height <= 2)
                    return;

                var dropW = SystemInformation.VerticalScrollBarWidth;
                dropW = Math.Min(dropW, inner.Width / 3);
                var dropRect = new Rectangle(inner.Right - dropW + 1, inner.Y, dropW, inner.Height);
                var textRect = new Rectangle(inner.X + 3, inner.Y, Math.Max(1, inner.Width - dropW - 4), inner.Height);

                var readOnlyCell = (elementState & DataGridViewElementStates.ReadOnly) != 0;
                var gridOk = DataGridView == null || DataGridView.Enabled;
                var enabled = gridOk && !readOnlyCell && !ReadOnly;
                var cbState = enabled ? ComboBoxState.Normal : ComboBoxState.Disabled;

                var text = formattedValue?.ToString() ?? string.Empty;
                var font = cellStyle.Font ?? Control.DefaultFont;
                var flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;

                TextRenderer.DrawText(graphics, text, font, textRect, fore, flags);

                using (var p = new Pen(Divider))
                    graphics.DrawLine(p, dropRect.Left, inner.Y + 2, dropRect.Left, inner.Bottom - 2);

                ComboBoxRenderer.DrawDropDownButton(graphics, dropRect, cbState);
            }
            finally
            {
                graphics.ResetClip();
            }
        }
    }
}
