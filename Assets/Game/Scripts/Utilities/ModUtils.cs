using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public static class ModUtils
{
    public static float Clamp01(float value) 
    {
        return Mathf.Clamp01(value); 
    }

    public static int FloorToInt(float value)
    {
        return Mathf.FloorToInt(value);
    }

    public static void Log(object obj) 
    {
        Debug.Log(obj);
    }

    public static void LogWarning(object obj) 
    {
        Debug.LogWarning(obj);
    }

    public static void LogError(object obj) 
    {
        Debug.LogError(obj);
    }

    public static void ULogChannel(string channel, string message)
    {
    }

    public static void ULogWarningChannel(string channel, string message)
    {
    }

    public static void ULogErrorChannel(string channel, string message)
    {
    }

    public static void ULog(string message)
    {
    }

    public static void ULogWarning(string message)
    {
    }

    public static void ULogError(string message)
    {
    }
}
