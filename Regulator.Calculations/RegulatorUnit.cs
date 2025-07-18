namespace Regulator.Domain
{
    public class RegulatorUnit(string name, HashSet<int> regulators)
    {
        public string Name { get; private set; } = name;
        public IReadOnlyList<ValueNumber> Regulators { get; private set; } 
            = regulators
                .Select(n => new ValueNumber(n))
                .ToList();
    }
}
