using System.Diagnostics;
using UnityEngine;

public class Peaksensor
{
    public Peaksensor(string message, int toleranceμs = 100, bool autostart = true)
    {
        stopwatch = new Stopwatch();
        this.message = message;
        this.toleranceμs = toleranceμs;
        if (autostart)
        {
            Start();
        }
    }

    public void Start(bool reset = false)
    {
        if (reset)
        {
            stopwatch.Reset();
        }

        stopwatch.Start();
    }

    public void Stop(bool finished = true)
    {
        stopwatch.Stop();

        if (finished)
        {
            int elapsedμs = (int)((double)stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000000);
            if (elapsedμs > toleranceμs)
            {
                UnityEngine.Debug.Log(message + " took " + elapsedμs + "μs, expected " + toleranceμs + "μs or less.");
            }
        }
    }

    private Stopwatch stopwatch;
    private string message;
    private int toleranceμs;
}
