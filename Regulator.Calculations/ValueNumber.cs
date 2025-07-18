namespace Regulator.Domain
{
    public struct ValueNumber
    {
        private const int NUMBER_RANGE_MIN = 1;
        private const int NUMBER_RANGE_MAX = 2_147_483_647;

        private int _value;

        public int Value 
        {
            readonly get => _value;
            private set
            {
                if (
                    value < NUMBER_RANGE_MIN
                    || value > NUMBER_RANGE_MAX
                )
                { 
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _value = value;
            }
        }

        public ValueNumber(int number)
        {
            Value = number;
        }
    }
}
