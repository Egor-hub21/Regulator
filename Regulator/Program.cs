using Microsoft.Extensions.Configuration;
using Regulator.Configurations;
using Regulator.Domain;
using Regulator.Calculations;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Text.Json.Serialization;

namespace Regulator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json")
               .AddEnvironmentVariables()
               .Build();

            // Считывание данных в объект Configuration
            var config = new Configuration();
            configuration.Bind(config);

            // Преобразование данных в нужные типы
            var generationUnits = config.GroupRegulators.Select(unit =>
                new Domain.RegulatorUnit(unit.Name, new HashSet<int>(unit.Regulators))).ToList();

            // Пример использования
            Console.WriteLine($"FilePath: {config.FilePath}");
            Console.WriteLine($"ResultPath: {config.ResultPath}");
            Console.WriteLine($"StepFactor: {config.StepFactor}");
            Console.WriteLine($"Vir: {config.Vir}");
            Console.WriteLine($"Section: {config.Section}");
            foreach (var unit in generationUnits)
            {
                Console.WriteLine(
                    $"Unit Name: {unit.Name},"
                    + $" Regulators: {string.Join(", ", unit.Regulators.Select(r => r.Value))}");
            }

            ValueNumber section = new(config.Section);
            ValueNumber vir = new(config.Vir);
            double stepFactor = config.StepFactor;
            string filePath = new(config.FilePath);
            string resultPath = new(config.ResultPath);


            Calculation[] calculations = generationUnits.Select(g => 
                new Calculation(section.Value, vir.Value, g, 10, stepFactor)
            ).ToArray();

            Result[] results = new Result[calculations.Length];
            Console.WriteLine("___RUN___\n");
            //for (int i = 0; i < calculations.Length; i++)
            //{
            //    var item = calculations[i];
            //    item.Run(filePath);
            //    results[i] = new(item.Unit.Name, item.Factor, item.Result);
            //    Console.WriteLine($" - {item.Unit.Name}");
            //}

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount // или меньше
            };
            Parallel.For(0, calculations.Length, options, i =>
            {
                calculations[i].Run(filePath);
                results[i] = new Result(
                    calculations[i].Unit.Name,
                    calculations[i].Factor,
                    calculations[i].Result,
                    calculations[i].Reserve.open,
                    calculations[i].Reserve.locked
                );
                Console.WriteLine($" - {calculations[i].Unit.Name}");
            });
            Console.WriteLine("\n___FINISH___");
            foreach (var item in results)
            {
                Console.WriteLine($"\n{item.Name}");
                Console.WriteLine($"{item.Factor}");
                foreach (var item1 in item.Diagram)
                {
                    Console.WriteLine($"\t{item1.X} : {item1.Y}");
                }
            }

            string jsonStr = JsonSerializer.Serialize(
                results,
                new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.Create(
                        UnicodeRanges.BasicLatin,
                        UnicodeRanges.Cyrillic
                    ),
                    WriteIndented = true,
                    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals // ← Добавьте эту строку
                }
            );
            Console.WriteLine(jsonStr);
            // Запись JSON в файл
            File.WriteAllText(
                resultPath,
                jsonStr
            );
        }
    }
    public class AppConfig
    {
        public List<List<int>> GenerationUnits { get; set; } = [];
    }
}
