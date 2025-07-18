namespace Regulator.Configurations
{
    public class Configuration
    {
        public string FilePath { get; set; } = "";
        public string ResultPath { get; set; } = "";
        public int Vir { get; set; }
        public int Section { get; set; }
        public double StepFactor { get; set; }
        public List<GroupRegulator> GroupRegulators { get; set; } = [];
    }
}
