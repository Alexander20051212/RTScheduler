
using System;
using System.Collections.Generic;
using System.Linq;
using RTScheduler.Models;

namespace RTScheduler.Services
{
    public class RmsScheduler
    {
        /// <summary>
        /// Анализ набора задач по алгоритму RMS.
        /// </summary>
        public RmsAnalysisResult Analyze(List<PeriodicTask> tasks)
        {
            if (tasks == null || tasks.Count == 0)
                throw new ArgumentException("Список задач пуст.");

            if (tasks.Any(t =>
                t.ExecutionTime <= 0 ||
                t.Period <= 0 ||
                t.Deadline <= 0))
            {
                throw new ArgumentException(
                    "Параметры задач должны быть положительными.");
            }

            // RMS: меньший период означает более высокий приоритет.
            var sortedTasks = tasks
                .OrderBy(t => t.Period)
                .ThenBy(t => t.Id)
                .ToList();

            double utilization = sortedTasks.Sum(
                t => t.ExecutionTime / t.Period);

            int n = sortedTasks.Count;

            // Тест Liu & Layland.
            double utilizationBound =
                n * (Math.Pow(2.0, 1.0 / n) - 1.0);

            var taskResults = new List<RmsTaskResult>();

            for (int i = 0; i < sortedTasks.Count; i++)
            {
                var task = sortedTasks[i];

                // Все задачи перед текущей имеют более высокий приоритет.
                var higherPriorityTasks = sortedTasks
                    .Take(i)
                    .ToList();

                double responseTime =
                    CalculateResponseTime(task, higherPriorityTasks);

                taskResults.Add(new RmsTaskResult
                {
                    Task = task,
                    Priority = i + 1,
                    ResponseTime = responseTime,
                    IsSchedulable = responseTime <= task.Deadline
                });
            }

            return new RmsAnalysisResult
            {
                Utilization = utilization,
                UtilizationBound = utilizationBound,
                LiuLaylandPassed = utilization <= utilizationBound,
                TaskResults = taskResults
            };
        }

        /// <summary>
        /// Расчёт времени отклика задачи методом итераций.
        /// </summary>
        private double CalculateResponseTime(
            PeriodicTask task,
            List<PeriodicTask> higherPriorityTasks)
        {
            double responseTime = task.ExecutionTime;

            for (int iteration = 0; iteration < 1000; iteration++)
            {
                double nextResponseTime = task.ExecutionTime;

                foreach (var higherTask in higherPriorityTasks)
                {
                    nextResponseTime +=
                        Math.Ceiling(responseTime / higherTask.Period)
                        * higherTask.ExecutionTime;
                }

                // Если значение перестало изменяться,
                // достигнуто время отклика.
                if (Math.Abs(nextResponseTime - responseTime) < 0.000001)
                    return nextResponseTime;

                // Если время отклика превышает дедлайн,
                // задача уже не укладывается в ограничение.
                if (nextResponseTime > task.Deadline)
                    return nextResponseTime;

                responseTime = nextResponseTime;
            }

            return double.PositiveInfinity;
        }
    }

    public class RmsTaskResult
    {
        public PeriodicTask Task { get; set; } = new();

        public int Priority { get; set; }

        public double ResponseTime { get; set; }

        public bool IsSchedulable { get; set; }
    }

    public class RmsAnalysisResult
    {
        public double Utilization { get; set; }

        public double UtilizationBound { get; set; }

        public bool LiuLaylandPassed { get; set; }

        public List<RmsTaskResult> TaskResults { get; set; } = new();
    }
}
