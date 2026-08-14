using System.Diagnostics;
using Godot;

namespace CounterTool.Utils;

public static class DiagnosticUtils
{
    extension(Stopwatch stopwatch)
    {
        public void LogAndRestart(string segment)
        {
            stopwatch.Stop();
            GD.Print($"Finished {segment} in {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.Restart();
        }
    }
}