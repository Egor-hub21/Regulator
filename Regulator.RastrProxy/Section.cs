using ConnectionRastr.Core;
using ConnectionRastr.Core.Data;

namespace Regulator.RastrAcces
{
    public class Section
    {
        const string TABLE_SECTION = "sechen";
        const string COLUMN_NUMBER = "ns";
        const string COLUMN_NAME = "name";
        const string COLUMN_ACTIVE_POWER_FLOW = "psech";

        private readonly RastrProvider _provider;
        private int _number;
        private int _index = -1;

        public Section(int number, RastrProvider provider)
        {
            if (number < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(number),
                    "не может быть меньше нуля"
                );
            }
            _number = number;
            _provider = provider;
        }

        public int Number 
        {
            get => _number;
            set 
            {
                if (value < 0)
                { 
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        "не может быть меньше нуля"
                    );
                }
                if (GetIndex(value, _provider) != -1)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        $"уже существует сечение с таким {COLUMN_NUMBER}"
                    );
                }

                _number = value;
            }
        }

        public double ActivePowerFlow => GetValue<double>(ActivePowerFlowColumn);

        public string Name => GetValue<string>(NameColumn);

        private DataTable SectionTable => _provider.Tables[TABLE_SECTION];
        private DataColumn NumberColumn => SectionTable.Columns[COLUMN_NUMBER];
        private DataColumn NameColumn => SectionTable.Columns[COLUMN_NAME];
        private DataColumn ActivePowerFlowColumn => SectionTable.Columns[COLUMN_ACTIVE_POWER_FLOW];

        private T GetValue<T>(DataColumn column)
        {
            if (CheckValidityOfIndex())
            {
                return (T)column[_index];
            }
            if (CheckNumber(out int index))
            {
                _index = index;
                return (T)column[_index];
            }

            throw new InvalidOperationException(
                $"{nameof(_index)}:{_index}; {nameof(_number)}:{_number}"
            );
        }

        /// <summary>
        ///  Возвращает индекс строки.
        /// </summary>
        /// <param name="number">Номер сечения.</param>
        /// <param name="provider">Обертка Rastr-а.</param>
        /// <returns>Индекс.</returns>
        /// <remarks>
        /// Вернет -1 в случае отсутствия совпадений.
        /// </remarks>
        private static int GetIndex(
            int number, 
            RastrProvider provider
        ) => provider.Tables[TABLE_SECTION]
             .SelectRow($"{COLUMN_NUMBER}={number}");

        private bool CheckValidityOfIndex()
        {
            if (_index == -1)
            { 
                return false;
            }
            if ((int)NumberColumn[_index] != _number)
            {
                return false;
            }
            
            return true;
        }

        private bool CheckNumber(out int index)
        {
            if (_number == -1)
            {
                index = -1;
                return false;
            }
            index = GetIndex(_number, _provider);
            if (index == -1)
            {
                return false;
            }

            return true;
        }
    }
}
