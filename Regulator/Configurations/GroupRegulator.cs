namespace Regulator.Configurations
{
    public class GroupRegulator
    {
        public string Name { get; set; }
        public HashSet<int> Regulators { get; set; }

        public GroupRegulator(string name, HashSet<int> regulators)
        {
            Name = name;
            Regulators = regulators;
        }
    }
}
