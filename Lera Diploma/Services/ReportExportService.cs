using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ClosedXML.Excel;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Lera_Diploma.Services
{
    public static class ReportExportService
    {
        public static void ExportToExcel(DataTable table, string reportTitle, string filePath)
        {
            var header = ReportHeaderService.GetHeaderLines();
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Отчёт");
                var row = 1;
                foreach (var line in header)
                {
                    ws.Range(row, 1, row, Math.Max(4, table.Columns.Count)).Merge();
                    ws.Cell(row, 1).Value = line;
                    ws.Cell(row, 1).Style.Font.Bold = true;
                    row++;
                }
                if (header.Count > 0)
                    row++;
                ws.Range(row, 1, row, Math.Max(4, table.Columns.Count)).Merge();
                ws.Cell(row, 1).Value = reportTitle ?? "Отчёт";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontSize = 12;
                row++;

                for (var c = 0; c < table.Columns.Count; c++)
                    ws.Cell(row, c + 1).Value = table.Columns[c].ColumnName;
                row++;
                for (var r = 0; r < table.Rows.Count; r++)
                {
                    for (var c = 0; c < table.Columns.Count; c++)
                        ws.Cell(row + r, c + 1).Value = table.Rows[r][c]?.ToString() ?? "";
                }
                ws.Columns().AdjustToContents();
                wb.SaveAs(filePath);
            }
        }

        public static void ExportToPdf(DataTable table, string title, string filePath)
        {
            var doc = new PdfDocument();
            doc.Info.Title = title;
            var page = doc.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 9);
            var smallFont = new XFont("Arial", 8);
            var headerFont = new XFont("Arial", 11, XFontStyleEx.Bold);
            double y = 36;
            var left = 40d;
            var width = page.Width.Point - 80;

            var headerLines = ReportHeaderService.GetHeaderLines().ToList();
            foreach (var line in headerLines)
            {
                gfx.DrawString(line, smallFont, XBrushes.Black, new XRect(left, y, width, 14), XStringFormats.TopLeft);
                y += 12;
            }
            if (headerLines.Count > 0)
                y += 6;
            gfx.DrawString(title ?? "Отчёт", headerFont, XBrushes.Black, new XRect(left, y, width, 20), XStringFormats.TopLeft);
            y += 24;

            foreach (DataRow dr in table.Rows)
            {
                var line = string.Join("  |  ", GetRowValues(table, dr));
                gfx.DrawString(line, font, XBrushes.Black, new XRect(left, y, width, 16), XStringFormats.TopLeft);
                y += 14;
                if (y > page.Height.Point - 50)
                {
                    page = doc.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    y = 36;
                }
            }

            doc.Save(filePath);
        }

        private static string[] GetRowValues(DataTable table, DataRow row)
        {
            var arr = new string[table.Columns.Count];
            for (var i = 0; i < table.Columns.Count; i++)
                arr[i] = row[i]?.ToString() ?? "";
            return arr;
        }

        private static string[] GetColumnNames(DataTable table)
        {
            var arr = new string[table.Columns.Count];
            for (var i = 0; i < table.Columns.Count; i++)
                arr[i] = table.Columns[i].ColumnName;
            return arr;
        }

        public static void PrintDataTable(DataTable table, string title, IWin32Window owner)
        {
            var headerLines = ReportHeaderService.GetHeaderLines();
            var doc = new PrintDocument();
            var currentRow = 0;
            doc.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                var font = new Font("Segoe UI", 9);
                var smallFont = new Font("Segoe UI", 8);
                var headerFont = new Font("Segoe UI", 11, FontStyle.Bold);
                float y = e.MarginBounds.Top;

                if (currentRow == 0)
                {
                    foreach (var line in headerLines)
                    {
                        g.DrawString(line, smallFont, Brushes.Black, e.MarginBounds.Left, y);
                        y += smallFont.GetHeight(g) + 2;
                    }
                    if (headerLines.Count > 0)
                        y += 4;
                    g.DrawString(title, headerFont, Brushes.Black, e.MarginBounds.Left, y);
                    y += headerFont.GetHeight(g) + 8;
                }

                if (currentRow == 0)
                {
                    var headers = string.Join(" | ", GetColumnNames(table));
                    g.DrawString(headers, font, Brushes.DimGray, new RectangleF(e.MarginBounds.Left, y, e.MarginBounds.Width, 40));
                    y += font.GetHeight(g) + 4;
                }

                while (currentRow < table.Rows.Count && y < e.MarginBounds.Bottom - font.GetHeight(g))
                {
                    var line = string.Join(" | ", GetRowValues(table, table.Rows[currentRow]));
                    g.DrawString(line, font, Brushes.Black, new RectangleF(e.MarginBounds.Left, y, e.MarginBounds.Width, font.GetHeight(g) + 2));
                    y += font.GetHeight(g) + 2;
                    currentRow++;
                }

                e.HasMorePages = currentRow < table.Rows.Count;
            };

            using (var preview = new PrintPreviewDialog())
            {
                preview.Document = doc;
                preview.Width = 900;
                preview.Height = 700;
                preview.ShowDialog(owner);
            }
        }
    }
}
