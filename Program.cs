using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using RTScheduler.Models;
using RTScheduler.Services;

// Настройка кодировки консоли
Console.OutputEncoding = Encoding.UTF8;

// Создание папки для отчётов
string projectDirectory = Directory.GetCurrentDirectory();

string reportsDirectory = Path.Combine(
    projectDirectory,
    "docs");

Directory.CreateDirectory(reportsDirectory);

// Имя файла отчёта
string reportFile = Path.Combine(
    reportsDirectory,
    $"analysis_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");

// Подключение записи результатов в консоль и файл
var originalConsole = Console.Out;

using var reportWriter = new StreamWriter(
    reportFile,
    false,
    Encoding.UTF8);

Console.SetOut(new TeeTextWriter(originalConsole, reportWriter));

try
{
    // Поиск входного JSON-файла
    string filePath = Path.Combine(
        AppContext.BaseDirectory,
        "data",
        "tasks.json");

    if (!File.Exists(filePath))
    {
        filePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "tasks.json");
    }

    if (!File.Exists(filePath))
    {
        Console.WriteLine("Ошибка: файл data/tasks.json не найден.");
        return;
    }

    // Загрузка исходных данных
    TaskSetModel? taskSet;

    try
    {
        string json = File.ReadAllText(filePath);

        taskSet = JsonSerializer.Deserialize<TaskSetModel>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка чтения JSON: {ex.Message}");
        return;
    }

    if (taskSet == null ||
        taskSet.Tasks == null ||
        taskSet.Tasks.Count == 0)
    {
        Console.WriteLine("Ошибка: список задач пуст или некорректен.");
        return;
    }

    // Заголовок
    Console.WriteLine("========================================");
    Console.WriteLine($"Система: {taskSet.SystemName}");
    Console.WriteLine("Анализ периодических задач реального времени");
    Console.WriteLine("========================================");

    // Исходные параметры
    Console.WriteLine("\nИсходные параметры задач:");
    Console.WriteLine("ID | Название | C | T | D");

    foreach (var task in taskSet.Tasks)
    {
        Console.WriteLine(
            $"{task.Id,2} | {task.Name} | " +
            $"{task.ExecutionTime,4} | " +
            $"{task.Period,4} | " +
            $"{task.Deadline,4}");
    }

    // ========================================
    // RMS
    // ========================================

    Console.WriteLine("\n========== RMS ==========");

    var rmsScheduler = new RmsScheduler();
    var rmsResult = rmsScheduler.Analyze(taskSet.Tasks);

    Console.WriteLine(
        $"Загрузка процессора U: {rmsResult.Utilization:F3}");

    Console.WriteLine(
        $"Граница Liu-Layland: {rmsResult.UtilizationBound:F3}");

    Console.WriteLine(
        rmsResult.LiuLaylandPassed
            ? "Тест Liu-Layland: пройден"
            : "Тест Liu-Layland: не пройден — гарантии теста нет");

    // Анализ времени отклика
    Console.WriteLine("\nАнализ времени отклика:");
    Console.WriteLine(
        "Задача | Приоритет | Время отклика R | Дедлайн D | Результат");

    foreach (var result in rmsResult.TaskResults)
    {
        Console.WriteLine(
            $"{result.Task.Name} | " +
            $"{result.Priority} | " +
            $"{result.ResponseTime:F2} | " +
            $"{result.Task.Deadline:F2} | " +
            $"{(result.IsSchedulable ? "Успевает" : "Не успевает")}");
    }

    bool rmsSchedulable = rmsResult.TaskResults.All(
        x => x.IsSchedulable);

    Console.WriteLine(
        "\nИтог RMS по анализу времени отклика: " +
        (rmsSchedulable
            ? "система планируема"
            : "система не планируема"));

    // ========================================
    // EDF
    // ========================================

    Console.WriteLine("\n========== EDF ==========");

    var edfScheduler = new EdfScheduler();
    var edfResult = edfScheduler.Analyze(taskSet.Tasks);

    Console.WriteLine(
        $"Загрузка процессора U: {edfResult.Utilization:F3}");

    Console.WriteLine(
        $"Допустимая загрузка EDF: {edfResult.UtilizationLimit:F3}");

    Console.WriteLine(
        edfResult.IsSchedulable
            ? "EDF: система планируема"
            : "EDF: система не планируема");

    // ========================================
    // Гиперпериод
    // ========================================

    var ganttService = new GanttChartService();

    int hyperperiod = ganttService.CalculateHyperperiod(
        taskSet.Tasks);

    Console.WriteLine($"\nГиперпериод системы: {hyperperiod}");

    // ========================================
    // Генерация расписаний
    // ========================================

    var rmsGantt = ganttService.GenerateRms(taskSet.Tasks);
    var edfGantt = ganttService.GenerateEdf(taskSet.Tasks);

    // ========================================
    // Построение графиков OxyPlot
    // ========================================

    Console.WriteLine("\nФормирование диаграмм Ганта...");

    var ganttPlotService = new GanttPlotService();

    ganttPlotService.ShowChart(
        rmsGantt,
        "Диаграмма Ганта — алгоритм RMS");

    ganttPlotService.ShowChart(
        edfGantt,
        "Диаграмма Ганта — алгоритм EDF");

    Console.WriteLine("\nДиаграммы Ганта сформированы.");

    Console.WriteLine("\nАнализ завершён.");
}
catch (Exception ex)
{
    Console.WriteLine($"\nОшибка выполнения программы: {ex.Message}");
}
finally
{
    // Восстановление стандартного вывода
    Console.Out.Flush();
    Console.SetOut(originalConsole);

    Console.WriteLine($"\nОтчёт сохранён: {reportFile}");
}
