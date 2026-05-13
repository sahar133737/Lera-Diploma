using System.Collections.Generic;
using System.Windows.Forms;

namespace Lera_Diploma.UI
{
    /// <summary>Заголовки колонок DataGridView (как BGSK1.GridHeaderMap).</summary>
    public static class GridHeaderMap
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Maps =
            new Dictionary<string, Dictionary<string, string>>
            {
                ["documents"] = new Dictionary<string, string>
                {
                    { "Id", "Внутр. №" },
                    { "Number", "Номер" },
                    { "DocumentDate", "Дата документа" },
                    { "Тип", "Тип" },
                    { "Статус", "Статус" },
                    { "Контрагент", "Контрагент" },
                    { "Ответственный", "Ответственный" }
                },
                ["dashboard_recent"] = new Dictionary<string, string>
                {
                    { "Id", "Внутр. №" },
                    { "Number", "Номер" },
                    { "DocumentDate", "Дата документа" },
                    { "Тип", "Тип" },
                    { "Статус", "Статус" }
                },
                ["ledger"] = new Dictionary<string, string>
                {
                    { "Id", "Внутр. №" },
                    { "FinancialDocumentId", "Док. №" },
                    { "Number", "Документ" },
                    { "DocumentDate", "Дата" },
                    { "Status", "Статус" },
                    { "DocType", "Тип" },
                    { "Debit", "Дебет" },
                    { "Credit", "Кредит" },
                    { "Amount", "Сумма" },
                    { "Purpose", "Назначение" }
                },
                ["counterparties"] = new Dictionary<string, string>
                {
                    { "Id", "Внутр. №" },
                    { "Name", "Наименование" },
                    { "Inn", "ИНН" },
                    { "Kpp", "КПП" },
                    { "Kind", "Тип" }
                },
                ["budget"] = new Dictionary<string, string>
                {
                    { "Id", "Внутр. №" },
                    { "Code", "Код" },
                    { "Name", "Наименование" }
                },
                ["accounts"] = new Dictionary<string, string>
                {
                    { "Id", "Внутр. №" },
                    { "Code", "Код" },
                    { "Name", "Наименование" }
                },
                ["users"] = new Dictionary<string, string>
                {
                    { "Id", "Внутр. №" },
                    { "Login", "Логин" },
                    { "FullName", "ФИО" },
                    { "IsActive", "Активен" },
                    { "Роли", "Роли" }
                },
                ["audit"] = new Dictionary<string, string>
                {
                    { "CreatedAtUtc", "Время (UTC)" },
                    { "UserId", "Пользователь" },
                    { "Action", "Действие" },
                    { "EntityType", "Сущность" },
                    { "EntityKey", "Ключ" },
                    { "Details", "Детали" }
                }
            };

        public static void Apply(DataGridView grid, string mapKey, params string[] hiddenColumns)
        {
            if (grid == null || !Maps.TryGetValue(mapKey, out var map))
                return;

            foreach (var h in hiddenColumns)
            {
                if (!string.IsNullOrEmpty(h) && grid.Columns.Contains(h))
                    grid.Columns[h].Visible = false;
            }

            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (map.TryGetValue(col.Name, out var header))
                    col.HeaderText = header;
            }
        }
    }
}
