using Microsoft.Extensions.Configuration;

namespace Regulator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IConfigurationRoot config = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json")
               .AddEnvironmentVariables()
               .Build();

            int numberVir = config.GetValue<int>("vir");
            int numberSection = config.GetValue<int>("section");
            string filePath = config.GetValue<string>("filePath");
            var generationUnits = config.GetSection("generationUnits").Get<List<List<int>>>();

            Console.WriteLine($"vir = {numberVir}");
            Console.WriteLine($"section = {numberSection}");
            Console.WriteLine($"filePath = {filePath}");
            foreach (var unitArray in generationUnits)
            {
                Console.WriteLine(string.Join(", ", unitArray));
            }
        }
    }
    public class AppConfig
    {
        public List<List<int>> GenerationUnits { get; set; }
    }
}
