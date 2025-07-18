using ConnectionRastr;

namespace Regulator.RastrAcces
{
    public class Section
    {
        internal const string SECTION = "sechen";
        internal const string SECTION_NUMBER = "ns";
        internal const string SECTION_NAME = "name";
        internal const string SECTION_ACTIVE_POWER_FLOW = "psech";

        private readonly RastrWrapper _provider;
        private int _number;
        private int _index = -1;

        public Section(int number, RastrWrapper provider)
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
                if (
                    EntityTable.TryFindIndex(
                        out _,
                        new KeyValuePair<string, object>(SECTION_NUMBER, value)
                    )
                )
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        $"уже существует сечение с таким {SECTION_NUMBER}"
                    );
                }

                _number = value;
                SetValue(SECTION_NUMBER, value);
            }
        }

        public double ActivePowerFlow 
            => GetValue<double>(SECTION_ACTIVE_POWER_FLOW);

        public string Name
        {
            get => GetValue<string>(SECTION_NAME);
            set => SetValue(SECTION_NAME, value);
        }

        public bool Exists
        {
            get
            {
                if (
                    EntityTable.CheckValidityOfIndex(
                        _index,
                        GetKeyValuePair
                    )
                )
                {
                    return true;
                }

                return false;
            }
        }

        private Table EntityTable => _provider.Tables[SECTION];
        private KeyValuePair<string, object> GetKeyValuePair
            => new(SECTION_NUMBER, _number);
        private T GetValue<T>(string columnName)
        {
            bool check = EntityTable.TryGetColumnValue(
                columnName,
                out T? value,
                _index,
                GetKeyValuePair
            );

            if (!check || value is null)
            {
                throw new InvalidOperationException(
                    $"{nameof(_index)}:{_index}; {nameof(_number)}:{_number}");
            }
            return value;
        }

        private void SetValue<T>(string columnName, T value)
        {

            bool check = EntityTable.TrySetColumnValue(
                columnName,
                value,
                ref _index,
                GetKeyValuePair
            );
            if (!check)
            {
                throw new InvalidOperationException(
                    $"{nameof(_index)}:{_index}; {nameof(_number)}:{_number}");
            }
        }
    }
}
