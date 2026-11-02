
using System;
using System.Collections.Generic;
using System.Linq;
using RTScheduler.Models;

namespace RTScheduler.Services
{
    public class GanttChartService
    {
        private class Job
        {
            public PeriodicTask Task { get; set; } = new();

            public int ReleaseTime { get; set; }

            public int AbsoluteDeadline { get; set; }

            public double RemainingTime { get; set; }
        }

        public List<GanttSegment> GenerateRms(
            List<PeriodicTask> tasks)
        {
            var orderedTasks = tasks
                .OrderBy(t => t.Period)
                .ThenBy(t => t.Id)
                .ToList();

            return GenerateSchedule(
                orderedTasks,
                useEdf: false);
        }

        public List<GanttSegment> GenerateEdf(
            List<PeriodicTask> tasks)
        {
            return GenerateSchedule(
                tasks,
                useEdf: true);
        }

        private List<GanttSegment> GenerateSchedule(
            List<PeriodicTask> tasks,
            bool useEdf)
        {
            if (tasks == null || tasks.Count == 0)
                throw new ArgumentException("Список задач пуст.");

            if (tasks.Any(t =>
                t.ExecutionTime <= 0 ||
                t.Period <= 0 ||
                t.Deadline <= 0 ||
                t.ExecutionTime != Math.Floor(t.ExecutionTime) ||
                t.Period != Math.Floor(t.Period) ||
                t.Deadline != Math.Floor(t.Deadline)))
            {
                throw new ArgumentException(
                    "Для построения диаграммы значения C, T и D должны быть положительными целыми числами.");
            }

            int hyperperiod = CalculateHyperperiod(tasks);

            var jobs = new List<Job>();
            var segments = new List<GanttSegment>();

            // Создание экземпляров периодических задач.
            foreach (var task in tasks)
            {
                for (int release = 0;
                     release < hyperperiod;
                     release += (int)task.Period)
                {
                    jobs.Add(new Job
                    {
                        Task = task,
                        ReleaseTime = release,
                        AbsoluteDeadline =
                            release + (int)task.Deadline,
                        RemainingTime = task.ExecutionTime
                    });
                }
            }

            // Моделирование процессора с шагом в одну единицу времени.
            for (int time = 0; time < hyperperiod; time++)
            {
                var readyJobs = jobs
                    .Where(j =>
                        j.ReleaseTime <= time &&
                        j.RemainingTime > 0)
                    .ToList();

                Job? selectedJob;

                if (useEdf)
                {
                    selectedJob = readyJobs
                        .OrderBy(j => j.AbsoluteDeadline)
                        .ThenBy(j => j.Task.Id)
                        .FirstOrDefault();
                }
                else
                {
                    selectedJob = readyJobs
                        .OrderBy(j => j.Task.Period)
                        .ThenBy(j => j.Task.Id)
                        .FirstOrDefault();
                }

                string taskName = selectedJob == null
                    ? "Простой"
                    : selectedJob.Task.Name;

                // Объединяем соседние интервалы одной задачи.
                if (segments.Count > 0 &&
                    segments[^1].TaskName == taskName)
                {
                    segments[^1].EndTime = time + 1;
                }
                else
                {
                    segments.Add(new GanttSegment
                    {
                        TaskName = taskName,
                        StartTime = time,
                        EndTime = time + 1
                    });
                }

                if (selectedJob != null)
                    selectedJob.RemainingTime -= 1;
            }

            return segments;
        }

        public int CalculateHyperperiod(
            List<PeriodicTask> tasks)
        {
            int result = (int)tasks[0].Period;

            foreach (var task in tasks.Skip(1))
            {
                result = Lcm(
                    result,
                    (int)task.Period);
            }

            return result;
        }

        private int Lcm(int a, int b)
        {
            return a / Gcd(a, b) * b;
        }

        private int Gcd(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }

            return a;
        }
    }

    public class GanttSegment
    {
        public string TaskName { get; set; } = string.Empty;

        public int StartTime { get; set; }

        public int EndTime { get; set; }
    }
}
