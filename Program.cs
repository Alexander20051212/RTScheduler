using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using RTScheduler.Models;
using RTScheduler.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;

string filePath = Path.Combine(AppContext.BaseDirectory, "data", "tasks.json");

if (!File.Exists(filePath))
{
    filePath = Path.Combine(Directory.GetCurrentDirectory(), "data", "tasks.json");
}

if (!File.Exists(filePath))
{
    Console.WriteLine("Ошибка: файл data/tasks.json не найден.");
    return;
}

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

if (taskSet == null || taskSet.Tasks == null || taskSet.Tasks.Count == 0)
{
    Console.WriteLine("Ошибка: список задач пуст или некорректен.");
    return;
}

Console.WriteLine("========================================");
Console.WriteLine($"Система: {taskSet.SystemName}");
Console.WriteLine("Анализ периодических задач реального времени");
Console.WriteLine("========================================");

Console.WriteLine("\nИсходные параметры задач:");
Console.WriteLine("ID | Название | C | T | D");

foreach (var task in taskSet.Tasks)
{
    Console.WriteLine(
        $"{task.Id,2} | {task.Name} | " +
        $"{task.ExecutionTime,4} | {task.Period,4} | {task.Deadline,4}");
}

// RMS analysis
Console.WriteLine("\n========== RMS ==========");

var rmsScheduler = new RmsScheduler();
var rmsResult = rmsScheduler.Analyze(taskSet.Tasks);

Console.WriteLine($"Загрузка процессора U: {rmsResult.Utilization:F3}");
Console.WriteLine($"Граница Liu-Layland: {rmsResult.UtilizationBound:F3}");
Console.WriteLine(
    rmsResult.LiuLaylandPassed
        ? "Тест Liu-Layland: пройден"
        : "Тест Liu-Layland: не пройден — гарантии теста нет");

Console.WriteLine("\nАнализ времени отклика:");
Console.WriteLine("Задача | Приоритет | Время отклика R | Дедлайн D | Результат");

foreach (var result in rmsResult.TaskResults)
{
    Console.WriteLine(
        $"{result.Task.Name} | " +
        $"{result.Priority} | " +
        $"{result.ResponseTime:F2} | " +
        $"{result.Task.Deadline:F2} | " +
        $"{(result.IsSchedulable ? "Успевает" : "Не успевает")}");
}

bool rmsSchedulable = rmsResult.TaskResults.All(x => x.IsSchedulable);

Console.WriteLine(
    $"\nИтог RMS по анализу времени отклика: " +
    $"{(rmsSchedulable ? "система планируема" : "система не планируема")}");

// EDF analysis
Console.WriteLine("\n========== EDF ==========");

var edfScheduler = new EdfScheduler();
var edfResult = edfScheduler.Analyze(taskSet.Tasks);

Console.WriteLine($"Загрузка процессора U: {edfResult.Utilization:F3}");
Console.WriteLine($"Допустимая загрузка EDF: {edfResult.UtilizationLimit:F3}");
Console.WriteLine(
    edfResult.IsSchedulable
        ? "EDF: система планируема"
        : "EDF: система не планируема");

// Hyperperiod and Gantt charts
var ganttService = new GanttChartService();
int hyperperiod = ganttService.CalculateHyperperiod(taskSet.Tasks);

Console.WriteLine($"\nГиперпериод системы: {hyperperiod}");

Console.WriteLine("\n========== Диаграмма Ганта RMS ==========");

var rmsGantt = ganttService.GenerateRms(taskSet.Tasks);

foreach (var segment in rmsGantt)
{
    Console.WriteLine(
        $"{segment.StartTime,3} - {segment.EndTime,3} | {segment.TaskName}");
}

Console.WriteLine("\n========== Диаграмма Ганта EDF ==========");

var edfGantt = ganttService.GenerateEdf(taskSet.Tasks);

foreach (var segment in edfGantt)
{
    Console.WriteLine(
        $"{segment.StartTime,3} - {segment.EndTime,3} | {segment.TaskName}");
}

Console.WriteLine("\nАнализ завершён.");
