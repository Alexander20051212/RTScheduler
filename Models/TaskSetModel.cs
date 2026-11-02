
using System.Collections.Generic;

namespace RTScheduler.Models
{
    /// <summary>
    /// Модель набора периодических задач.
    /// </summary>
    public class TaskSetModel
    {
        public string SystemName { get; set; } = string.Empty;

        public List<PeriodicTask> Tasks { get; set; } = new();
    }
}
