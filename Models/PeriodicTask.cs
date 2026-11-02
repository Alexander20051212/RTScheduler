
namespace RTScheduler.Models
{
    /// <summary>
    /// Модель периодической задачи системы реального времени.
    /// </summary>
    public class PeriodicTask
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Время выполнения задачи (C).
        /// </summary>
        public double ExecutionTime { get; set; }

        /// <summary>
        /// Период поступления задачи (T).
        /// </summary>
        public double Period { get; set; }

        /// <summary>
        /// Относительный дедлайн задачи (D).
        /// </summary>
        public double Deadline { get; set; }
    }
}
