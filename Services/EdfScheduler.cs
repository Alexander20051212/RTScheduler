using System;
using System.Collections.Generic;
using System.Linq;
using RTScheduler.Models;

namespace RTScheduler.Services
{
    public class EdfScheduler
    {
        /// <summary>
        /// Проверка планируемости набора периодических задач
        /// по алгоритму EDF.
        /// </summary>
        public EdfAnalysisResult Analyze(List<PeriodicTask> tasks)
        {
            if (tasks == null || tasks.Count == 0)
            {
                throw new ArgumentException(
                    "Список задач пуст.");
            }

            if (tasks.Any(t =>
                t.ExecutionTime <= 0 ||
                t.Period <= 0 ||
                t.Deadline <= 0))
            {
                throw new ArgumentException(
                    "Параметры задач должны быть положительными.");
            }

            // Проверяем, что используются неявные дедлайны: D = T.
            if (tasks.Any(t =>
                Math.Abs(t.Deadline - t.Period) > 0.000001))
            {
                throw new ArgumentException(
                    "Тест EDF по загрузке применим при Deadline = Period.");
            }

            double utilization = tasks.Sum(
                t => t.ExecutionTime / t.Period);

            return new EdfAnalysisResult
            {
                Utilization = utilization,
                UtilizationLimit = 1.0,
                IsSchedulable = utilization <= 1.0
            };
        }
    }

    public class EdfAnalysisResult
    {
        public double Utilization { get; set; }

        public double UtilizationLimit { get; set; }

        public bool IsSchedulable { get; set; }
    }
}
