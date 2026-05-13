using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace Lera_Diploma.UI
{
    /// <summary>Преобразует последовательность объектов (в т.ч. анонимных) в DataTable для сортировки в гриде.</summary>
    public static class EnumerableToDataTable
    {
        public static DataTable FromRows(IEnumerable items)
        {
            var dt = new DataTable();
            if (items == null)
                return dt;
            foreach (var item in items)
            {
                if (item == null)
                    continue;
                var t = item.GetType();
                if (dt.Columns.Count == 0)
                {
                    foreach (var p in t.GetProperties())
                    {
                        var colType = p.PropertyType;
                        if (colType.IsGenericType && colType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            colType = Nullable.GetUnderlyingType(colType) ?? colType;
                        dt.Columns.Add(p.Name, colType == typeof(void) ? typeof(object) : colType);
                    }
                }

                var row = dt.NewRow();
                foreach (DataColumn c in dt.Columns)
                {
                    var p = t.GetProperty(c.ColumnName);
                    if (p == null)
                        continue;
                    var v = p.GetValue(item);
                    row[c.ColumnName] = v ?? DBNull.Value;
                }

                dt.Rows.Add(row);
            }

            return dt;
        }
    }
}
