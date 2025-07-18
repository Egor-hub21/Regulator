using ConnectionRastr;

namespace Regulator.RastrAcces
{
    public class Generator
    {
        internal const string GENERATOR = "Generator";
        internal const string GENERATOR_NUMBER = "Num";
        internal const string GENERATOR_NAME = "Name";
        internal const string GENERATOR_STATE = "sta";
        internal const string GENERATOR_ACTIVE_POWER_RATING = "Pnom";
        internal const string GENERATOR_CURRENT_ACTIVE_POWER = "P";
        internal const string GENERATOR_CURRENT_REACTIVE_POWER = "Q";
        internal const string GENERATOR_MIN_REACTIVE_POWER = "Qmin";
        internal const string GENERATOR_MAX_REACTIVE_POWER = "Qmax";
        internal const string GENERATOR_PQ_DIAGRAM_NUMBER = "NumPQ";
        internal const string GENERATOR_NODE_NUMBER = "Node";

        internal const string NODE = "node";
        internal const string NODE_NUMBER = "ny";
        internal const string NODE_VOTAGE_SERPOINT = "vzd";

        private readonly RastrWrapper _provider;
        private int _number;
        private int _index = -1;
        private int _indexNode = -1;

        public Generator(int number, RastrWrapper provider)
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
                        new KeyValuePair<string, object>(GENERATOR_NUMBER, value)
                    )
                )
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        $"уже существует генератор с таким {GENERATOR_NUMBER}"
                    );
                }

                _number = value;
                SetValue(GENERATOR_NUMBER, value);
            }
        }

        public string Name
        {
            get => GetValue<string>(GENERATOR_NAME);
            set => SetValue(GENERATOR_NAME, value);
        }
        public double ActivePowerRating
        {
            get => GetValue<double>(GENERATOR_ACTIVE_POWER_RATING);
            set => SetValue(GENERATOR_ACTIVE_POWER_RATING, value);
        }
        public double CurrentActivePower
        {
            get => GetValue<double>(GENERATOR_CURRENT_ACTIVE_POWER);
            set => SetValue(GENERATOR_CURRENT_ACTIVE_POWER, value);
        }
        public double CurrentReactivePower
        {
            get => GetValue<double>(GENERATOR_CURRENT_REACTIVE_POWER);
            set => SetValue(GENERATOR_CURRENT_REACTIVE_POWER, value);
        }
        public bool IsInService
        {
            get => !GetValue<bool>(GENERATOR_STATE);
            set => SetValue(GENERATOR_STATE, !value);
        }
        public int NodeNumber
        {
            get => GetValue<int>(GENERATOR_NODE_NUMBER);
            set => SetValue(GENERATOR_NODE_NUMBER, value);
        }
        public double MaxReactivePower
        {
            get => GetValue<double>(GENERATOR_MAX_REACTIVE_POWER);
            set => SetValue(GENERATOR_MAX_REACTIVE_POWER, value);
        }
        public double MinReactivePower
        {
            get => GetValue<double>(GENERATOR_MIN_REACTIVE_POWER);
            set => SetValue(GENERATOR_MIN_REACTIVE_POWER, value);
        }
        public double VoltageSetpoint
        {
            get
            {
                bool check = NodeEntityTable.TryGetColumnValue(
                    NODE_VOTAGE_SERPOINT,
                    out double? value,
                    _indexNode,
                    GetKeyValuePairNode
                );

                if (!check || value is null)
                {
                    throw new InvalidOperationException(
                        $"{nameof(_indexNode)}:{_indexNode}; {nameof(NodeNumber)}:{NodeNumber}");
                }
                return (double)value;
            }
            set
            {
                bool check = NodeEntityTable.TrySetColumnValue(
                    NODE_VOTAGE_SERPOINT,
                    value,
                    ref _indexNode,
                    GetKeyValuePairNode
                );
                if (!check)
                {
                    throw new InvalidOperationException(
                    $"{nameof(_indexNode)}:{_indexNode}; {nameof(NodeNumber)}:{NodeNumber}");
                }
            }
        }
        public int PQDiagramNumber
        {
            get => GetValue<int>(GENERATOR_PQ_DIAGRAM_NUMBER);
            set => SetValue(GENERATOR_PQ_DIAGRAM_NUMBER, value);
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

        private Table EntityTable => _provider.Tables[GENERATOR];
        private Table NodeEntityTable => _provider.Tables[NODE];


        private KeyValuePair<string, object> GetKeyValuePair
           => new(GENERATOR_NUMBER, _number);
        private KeyValuePair<string, object> GetKeyValuePairNode
            => new(NODE_NUMBER, NodeNumber);
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
