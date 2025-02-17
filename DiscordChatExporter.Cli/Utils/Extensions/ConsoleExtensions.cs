﻿using System;
using System.Threading.Tasks;
using CliFx.Infrastructure;
using Spectre.Console;

namespace DiscordChatExporter.Cli.Utils.Extensions
{
    internal static class ConsoleExtensions
    {
        private class NoopExclusivityMode : IExclusivityMode
        {
            public T Run<T>(Func<T> func) => func();

            public Task<T> Run<T>(Func<Task<T>> func) => func();
        }

        public static IAnsiConsole CreateAnsiConsole(this IConsole console)
        {
            // Don't require exclusivity in tests.
            // Workaround for https://github.com/spectreconsole/spectre.console/issues/494
            var exclusivityMode = console is FakeConsole
                ? new NoopExclusivityMode()
                : null;

            return AnsiConsole.Create(
                new AnsiConsoleSettings
                {
                    Ansi = AnsiSupport.Detect,
                    ColorSystem = ColorSystemSupport.Detect,
                    Out = new AnsiConsoleOutput(console.Output),
                    ExclusivityMode = exclusivityMode
                }
            );
        }

        public static Progress CreateProgressTicker(this IConsole console) => console
            .CreateAnsiConsole()
            .Progress()
            .AutoClear(false)
            .AutoRefresh(true)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn {Alignment = Justify.Left},
                new ProgressBarColumn(),
                new PercentageColumn()
            );

        public static async ValueTask StartTaskAsync(
            this ProgressContext progressContext,
            string description,
            Func<ProgressTask, ValueTask> performOperationAsync)
        {
            var progressTask = progressContext.AddTask(
                // Don't recognize random square brackets as style tags
                Markup.Escape(description),
                new ProgressTaskSettings {MaxValue = 1}
            );

            try
            {
                await performOperationAsync(progressTask);
            }
            finally
            {
                progressTask.StopTask();
            }
        }
    }
}