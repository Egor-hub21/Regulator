using ConnectionRastr;

namespace Regulator.RastrAcces
{
    internal static class DataTableExtension
    {
        /// <summary>
        /// Возвращает индекс строки.
        /// </summary>
        /// <param name="table">Обертка ITable.</param>
        /// <param name="columnValuePairs">
        /// Пары (название колонки; искомое значение).
        /// </param>
        /// <returns>Индекс.</returns>
        /// <remarks>
        /// Вернет -1 в случае отсутствия совпадений.
        /// </remarks>
        public static int FindIndex(
            this Table table,
            params KeyValuePair<string, object>[] columnValuePairs
        )
        {
            return table
             .SelectRow(
                string.Join(
                    "&",
                    columnValuePairs
                        .Select(kvp => $"{kvp.Key}={kvp.Value}")
                )
            );
        }

        /// <summary>
        /// Проверяет, существует ли указанный индекс в таблице
        /// заданным парам "столбец-значение".
        /// </summary>
        /// <param name="table">Таблица данных, в которой выполняется проверка.</param>
        /// <param name="index">
        /// Индекс строки, который необходимо проверить.
        /// Если индекс равен -1, метод возвращает false.
        /// </param>
        /// <param name="columnValuePairs">
        /// Пары "столбец-значение", которые должны
        /// соответствовать значениям в указанной строке.
        /// </param>
        /// <returns>
        /// Возвращает true, если значения в строке по указанному индексу
        /// соответствуют всем переданным парам "столбец-значение";
        /// в противном случае возвращает false.
        /// </returns>
        public static bool CheckValidityOfIndex(
            this Table table,
            int index,
            params KeyValuePair<string, object>[] columnValuePairs
        )
        {
            if (!table.CheckIndex(index))
            {
                return false;
            }
            if (
                columnValuePairs.Any(kvp =>
                    table.Columns[kvp.Key][index] != kvp.Value
                )
            )
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Проверяет наличие строки в таблице,
        /// соответствующей заданным парам "столбец-значение".
        /// </summary>
        /// <param name="table">
        /// Таблица данных, в которой выполняется поиск.
        /// </param>
        /// <param name="index">
        /// Выходной параметр, который будет содержать индекс найденной строки.
        /// Если строка не найдена, значение будет равно -1.
        /// </param>
        /// <param name="columnValuePairs">
        /// Пары "столбец-значение", которые должны соответствовать значениям в строке.
        /// </param>
        /// <returns>
        /// Возвращает true, если строка с соответствующими значениями найдена;
        /// в противном случае возвращает false.
        /// </returns>
        public static bool TryFindIndex(
            this Table table,
            out int index,
            params KeyValuePair<string, object>[] columnValuePairs
        )
        {
            index = table.FindIndex(columnValuePairs);
            if (!table.CheckIndex(index))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Попытка получить значение из указанного столбца таблицы данных по заданному индексу.
        /// Если индекс недействителен, метод пытается найти новый индекс на основе переданных пар "ключ-значение".
        /// </summary>
        /// <typeparam name="T">Тип значения, которое будет получено из столбца.</typeparam>
        /// <param name="table">Таблица данных, из которой будет получено значение.</param>
        /// <param name="columnName">Имя столбца, из которого будет получено значение.</param>
        /// <param name="value">Выходной параметр, который будет содержать полученное значение,
        /// если оно успешно извлечено.</param>
        /// <param name="index">Индекс строки, из которой будет получено значение.</param>
        /// <param name="columnValuePairs">Дополнительные пары "ключ-значение",
        /// которые могут использоваться для проверки или поиска индекса.</param>
        /// <returns>Возвращает true, если значение успешно получено, иначе false.</returns>
        public static bool TryGetColumnValue<T>(
            this Table table,
            string columnName,
            out T? value,
            int index,
            params KeyValuePair<string, object>[] columnValuePairs
        )
        {
            if (table.CheckValidityOfIndex(index, columnValuePairs))
            {
                value = (T)table.Columns[columnName][index];
                return true;
            }
            if (
                columnValuePairs.Length > 0 
                && table.TryFindIndex(out int newIndex, columnValuePairs)
            )
            {
                index = newIndex;
                value = (T)table.Columns[columnName][index];
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Попытка установить значение в указанном столбце таблицы данных по заданному индексу.
        /// Если значение равно null, метод возвращает false. 
        /// Если индекс недействителен, метод пытается найти новый индекс на основе переданных пар "ключ-значение".
        /// </summary>
        /// <typeparam name="T">Тип значения, которое будет установлено в столбец.</typeparam>
        /// <param name="table">Таблица данных, в которой будет установлено значение.</param>
        /// <param name="columnName">Имя столбца, в который будет установлено значение.</param>
        /// <param name="value">Значение, которое необходимо установить в столбец.</param>
        /// <param name="index">Индекс строки, в которой будет установлено значение.
        /// Этот параметр передается по ссылке и может быть изменен.</param>
        /// <param name="columnValuePairs">Дополнительные пары "ключ-значение",
        /// которые могут использоваться для проверки или поиска индекса.</param>
        /// <returns>Возвращает true, если значение успешно установлено, иначе false.</returns>
        public static bool TrySetColumnValue<T>(
            this Table table,
            string columnName,
            T value,
            ref int index,
            params KeyValuePair<string, object>[] columnValuePairs
        )
        {
            if (value is null)
            {
                return false;
            }
            if (table.CheckValidityOfIndex(index, columnValuePairs))
            {
                table.Columns[columnName][index] = value;
                return true;
            }
            if (
                columnValuePairs.Length > 0
                && table.TryFindIndex(out int newIndex, columnValuePairs)
            )
            {
                index = newIndex;
                table.Columns[columnName][index] = value;
                return true;
            }
            
            return false;
        }

        private static bool CheckIndex(
            this Table table,
            int index
        ) => index >= 0  
            && index < table.Count;
    }
}
