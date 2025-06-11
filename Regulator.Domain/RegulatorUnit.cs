namespace Regulator.Domain
{
    public class RegulatorUnit
    {
        public RegulatorUnit(string name, IEnumerable<int> regulators) 
        {
            Name = name;
            Regulators = regulators.ToList();
        }

        public string Name { get; private set; }
        public IReadOnlyList<int> Regulators { get; private set; }
    }
}
