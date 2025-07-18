using System;
using System.Collections.Generic;
using System.Linq;
using ConnectionRastr;
using ConnectionRastr.Extension.Vir;
using ConnectionRastr.Extension.Weighting;
using Regulator.Domain;

namespace Regulator.Calculations
{
    public class Calculation
    {
        // Секции-вир
        private const string SECTION_VIR = "ut_vir_sech_vir";
        private const string SECTION_VIR_SECTION_NUM = "SechNum";
        private const string SECTION_VIR_VIR_NUM = "VirNum";
        // Генератор УР
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
        // Узлы
        internal const string NODE = "node";
        internal const string NODE_NUMBER = "ny";
        internal const string NODE_VOLTAGE_SER = "vzd";
        // Сечения
        internal const string SECTION = "sechen";
        internal const string SECTION_NUMBER = "ns";
        internal const string SECTION_NAME = "name";
        internal const string SECTION_ACTIVE_POWER_FLOW = "psech";

        private const double ACCEPTABLE_ERROR_LIMIT = 0.001;

        private readonly int _countPoints;

        public Calculation(
            int section,
            int vir,
            RegulatorUnit unit,
            int countPoints,
            double stepFactor
        )
        {
            Section = section;
            Vir = vir;
            Unit = unit;
            _countPoints = countPoints;
            Result = new (double, double)[_countPoints];
            StepFactor = stepFactor;
        }

        public int Section { get; }
        public int Vir { get; }
        public RegulatorUnit Unit { get; }
        private double StepFactor { get; }


        // Результаты: [i] = (active-flow, суммарная Q)
        public (double ActiveFlow, double SumReactivePower)[] Result { get; }

        public (double open, double locked) Reserve { get; private set; }

        // Итоговый коэффициент
        public double Factor { get; private set; }

        public void Run(string filePath)
        {
            using var wrapper = new RastrWrapper();
            wrapper.LoadFile(filePath);
            SetInitialData(wrapper, StepFactor);
            //wrapper.SaveFile("D:\\Мага\\вкр\\Файлы для расчетов\\test\\mdp_debug_2");
            var generatorsTable = wrapper.Tables[GENERATOR];
            var sectionTable = wrapper.Tables[SECTION];
            int sectionRowIdx = sectionTable.SelectRow($"{SECTION_NUMBER}={Section}");

            // 1) выбираем строки генераторов, которые идут в расчёт
            var generatorRows = GetEligibleGeneratorRows(generatorsTable);

            // 2) буферы под overflow и Q каждого генератора
            var overflow = new double[_countPoints];
            var reactivePower = InitializeReactivePowerBuffer(generatorRows);

            // 3) расчёт «полного» предела Qmin→Qmax
            wrapper.Weight();
            RecordPoint(
                overflow,
                reactivePower,
                generatorsTable,
                sectionTable,
                sectionRowIdx,
                _countPoints - 1
            );

            // суммарная верхняя граница
            double sumMaxReactivePower = generatorRows
                .Select(i =>
                    (double)wrapper
                        .Tables[GENERATOR]
                        .Columns[GENERATOR_MAX_REACTIVE_POWER][i]
                ).Sum();
                
            wrapper.SelectStates(0);

            // 4) фиксируем Qmax = текущее Q
            SetGeneratorsQmaxEqualCurrentQ(
                generatorsTable,
                generatorRows
            );
            wrapper.Weight();
            RecordPoint(
                overflow,
                reactivePower,
                generatorsTable,
                sectionTable,
                sectionRowIdx,
                0
            );
            wrapper.SelectStates(0);

            // 5) промежуточные точки: вычисляем шаг Qmax и циклим
            var qSteps = ComputeQmaxSteps(generatorRows, reactivePower);

            for (int i = 1; i < _countPoints - 1; i++)
            {
                IncreaseGeneratorsQmax(
                    generatorsTable,
                    generatorRows,
                    qSteps
                );
                wrapper.Weight();
                RecordPoint(
                    overflow,
                    reactivePower,
                    generatorsTable,
                    sectionTable,
                    sectionRowIdx,
                    i
                );
                wrapper.SelectStates(0);
            }

            // 6) финальный расчёт Factor и заполнение Result[]
            FillResultsAndComputeFactor(overflow, reactivePower);
            Reserve = (
                open: Result[^1].SumReactivePower - Result[0].SumReactivePower,
                locked: sumMaxReactivePower - Result[^1].SumReactivePower
            );
        }

        #region Вспомогательные методы

        private void SetInitialData(
            RastrWrapper provider,
            double stepFactor
        )
        {
            const string tableName = "ut_common";
            const int rowIndex = 0;

            // баланс итераций и коэффициентов
            var columnsData = new Dictionary<string, double>
            {
                { "maxs",    0.1 },
                { "iter",    1000 },
                { "sum_kfc", 0 },
            };

            foreach (var kv in columnsData)
            {
                provider.Tables[tableName]
                    .Columns[kv.Key][rowIndex] = kv.Value;
            }

            provider.ApplyVirTrajectory(Vir, stepFactor);
        }

        private int[] GetEligibleGeneratorRows(Table generatorsTable)
        {
            return Unit.Regulators
                .Select(r => generatorsTable.SelectRow($"{GENERATOR_NUMBER}={r.Value}"))
                .Where(idx => idx != -1)
                .Where(idx =>
                {
                    double qmax = (double)generatorsTable.Columns[GENERATOR_MAX_REACTIVE_POWER][idx];
                    double qmin = (double)generatorsTable.Columns[GENERATOR_MIN_REACTIVE_POWER][idx];
                    bool isOff = !(bool)generatorsTable.Columns[GENERATOR_STATE][idx];
                    return (qmax - qmin) > ACCEPTABLE_ERROR_LIMIT && isOff;
                })
                .ToArray();
        }

        private Dictionary<int, double[]> InitializeReactivePowerBuffer(int[] generatorRows)
        {
            return generatorRows.ToDictionary(idx => idx, _ => new double[_countPoints]);
        }

        private void RecordPoint(
            double[] overflow,
            Dictionary<int, double[]> reactivePower,
            Table generatorsTable,
            Table sectionTable,
            int sectionRowIdx,
            int pointIndex)
        {
            overflow[pointIndex] =
                (double)sectionTable.Columns[SECTION_ACTIVE_POWER_FLOW][sectionRowIdx];

            foreach (var kv in GetCurrentReactivePowers(generatorsTable, reactivePower.Keys))
            {
                reactivePower[kv.Key][pointIndex] = kv.Value;
            }
        }

        private static IEnumerable<KeyValuePair<int, double>> GetCurrentReactivePowers(
            Table generatorsTable,
            IEnumerable<int> generatorRows)
        {
            foreach (var idx in generatorRows)
            {
                yield return new KeyValuePair<int, double>(
                    idx,
                    (double)generatorsTable.Columns[GENERATOR_CURRENT_REACTIVE_POWER][idx]
                );
            }
        }

        private void SetGeneratorsQmaxEqualCurrentQ(Table generatorsTable, int[] generatorRows)
        {
            foreach (var idx in generatorRows)
            {
                generatorsTable.Columns[GENERATOR_PQ_DIAGRAM_NUMBER][idx] = 0;
                generatorsTable.Columns[GENERATOR_MAX_REACTIVE_POWER][idx] =
                    generatorsTable.Columns[GENERATOR_CURRENT_REACTIVE_POWER][idx];
            }
        }

        private Dictionary<int, double> ComputeQmaxSteps(
            int[] generatorRows,
            Dictionary<int, double[]> reactivePower)
        {
            return generatorRows.ToDictionary(
                idx => idx,
                idx =>
                {
                    double q0 = reactivePower[idx][0];
                    double q1 = reactivePower[idx][^1];
                    return (q1 - q0) / (_countPoints - 1);
                }
            );
        }

        private void IncreaseGeneratorsQmax(
            Table generatorsTable,
            int[] generatorRows,
            Dictionary<int, double> qSteps)
        {
            foreach (var idx in generatorRows)
            {
                double currentMaxQ = (double)generatorsTable.Columns[GENERATOR_MAX_REACTIVE_POWER][idx];
                generatorsTable.Columns[GENERATOR_MAX_REACTIVE_POWER][idx] = currentMaxQ + qSteps[idx];
            }
        }

        private void FillResultsAndComputeFactor(
            double[] overflow,
            Dictionary<int, double[]> reactivePower)
        {
            int cutsCount = _countPoints - 1;
            var localFactors = new double[cutsCount];

            for (int i = 0; i < _countPoints; i++)
            {
                double sumQ = reactivePower.Values.Sum(arr => arr[i]);
                Result[i] = (overflow[i], sumQ);

                if (i > 0)
                {
                    localFactors[i - 1] = 
                        (Result[i].ActiveFlow - Result[i - 1].ActiveFlow)
                         / (Result[i].SumReactivePower - Result[i - 1].SumReactivePower);
                }
            }

            Factor = localFactors.Sum() / cutsCount;
        }

        #endregion
    }
}
