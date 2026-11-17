using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;

namespace RTScheduler.Services
{
    public class GanttPlotService
    {
        public void ShowChart(List<GanttSegment> segments, string title)
        {
            if (segments == null || segments.Count == 0)
                return;

            Thread thread = new Thread(() =>
            {
                ShowChartOnSta(segments, title);
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        private void ShowChartOnSta(
            List<GanttSegment> segments,
            string title)
        {
            // Список дорожек: каждая задача занимает отдельную строку
            var taskNames = segments
                .Select(x => x.TaskName)
                .Distinct()
                .ToList();

            var colors = new[]
            {
                OxyColors.SteelBlue,
                OxyColors.Orange,
                OxyColors.SeaGreen,
                OxyColors.MediumPurple,
                OxyColors.Gray
            };

            var model = new PlotModel
            {
                Title = title,
                Background = OxyColors.White,
                PlotAreaBorderColor = OxyColors.Gray
            };

            // Ось X — время
            double maxTime = segments.Max(x => x.EndTime);

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Время",
                Minimum = 0,
                Maximum = maxTime,
                MajorStep = 10,
                MinorStep = 2,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColors.LightGray,
                MinorGridlineColor = OxyColors.Gainsboro
            });

            // Ось Y — задачи
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Задачи",
                Minimum = -0.5,
                Maximum = taskNames.Count - 0.5,
                MajorStep = 1,
                MinorStep = 1,
                StartPosition = 1,
                EndPosition = 0,
                MajorGridlineStyle = LineStyle.Solid,
                LabelFormatter = value =>
                {
                    int index = (int)Math.Round(value);

                    return index >= 0 && index < taskNames.Count
                        ? taskNames[index]
                        : "";
                }
            });

            // Каждый интервал рисуется отдельным прямоугольником
            for (int i = 0; i < taskNames.Count; i++)
            {
                string taskName = taskNames[i];

                OxyColor color = taskName == "Простой"
                    ? OxyColors.LightGray
                    : colors[i % colors.Length];

                foreach (var segment in segments.Where(
                    x => x.TaskName == taskName))
                {
                    model.Annotations.Add(new RectangleAnnotation
                    {
                        MinimumX = segment.StartTime,
                        MaximumX = segment.EndTime,
                        MinimumY = i - 0.32,
                        MaximumY = i + 0.32,
                        Fill = color,
                        Stroke = OxyColors.Black,
                        StrokeThickness = 1,
                        Layer = AnnotationLayer.AboveSeries
                    });
                }
            }

            var plotView = new OxyPlot.WindowsForms.PlotView
            {
                Dock = DockStyle.Fill,
                Model = model
            };

            var form = new Form
            {
                Text = title,
                Width = 1300,
                Height = 750,
                StartPosition = FormStartPosition.CenterScreen
            };

            form.Controls.Add(plotView);
            Application.Run(form);
        }
    }
}
