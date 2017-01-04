using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

public delegate void CallbackHandler(object sender);
public delegate void CallbackHandler<in T>(object sender, T args);

[MoonSharpUserData]
public class Callback 
{
    private event CallbackHandler callbackHandler;
    private readonly List<Closure> functions;

    public Callback()
    {
        UserData.RegisterType<EventArgs>();
        UserData.RegisterType<Callback>();
        functions = new List<Closure>();
    }

    public Callback(CallbackHandler callbackHandler) : this()
    {
        this.callbackHandler = callbackHandler;
    }

    public void Invoke()
    {
        // Grab our event handler and convert it to the current dispatcher context (thread).
        CallbackHandler currentDispatchContext = callbackHandler;
        if (currentDispatchContext != null)
        {
            currentDispatchContext(this);
        }

        foreach (Closure function in functions)
        {
            function.Call(this);
        }
    }

    public void Add(Closure function) { functions.Add(function); }
    public void Remove(Closure function) { functions.Remove(function); }

    public static Callback Add(Callback left, CallbackHandler right)
    {
        left.callbackHandler += right;
        return left;
    }

    public static Callback Add(Callback left, Closure function)
    {
        left.functions.Add(function);
        return left;
    }

    public static Callback operator +(Callback left, CallbackHandler right) { return Add(left, right); }
    public static Callback operator +(Callback left, Closure right) { return Add(left, right); }


    public static Callback Remove(Callback left, CallbackHandler right)
    {
        left.callbackHandler -= right;
        return left;
    }

    public static Callback Remove(Callback left, Closure function)
    {
        left.functions.Remove(function);
        return left;
    }

    public static Callback operator -(Callback  left, CallbackHandler right) { return Remove(left, right); }
    public static Callback operator -(Callback left, Closure right) { return Remove(left, right); }
}

[MoonSharpUserData]
public class Callback<T> 
{
    private event CallbackHandler<T> callbackHandler;
    private readonly List<Closure> functions;

    public Callback()
    {
        UserData.RegisterType<Callback<T>>();
        functions = new List<Closure>();
    }

    public Callback(CallbackHandler<T> callbackHandler) : this()
    {
        this.callbackHandler = callbackHandler;
    }

    public void Invoke(T args)
    {
        // Grab our event handler and convert it to the current dispatcher context (thread).
        CallbackHandler<T> currentDispatchContext = callbackHandler;
        if (currentDispatchContext != null)
        {
            currentDispatchContext(this, args);
        }

        foreach (Closure function in functions)
        {
            function.Call(this, args);
        }
    }

    public void Add(Closure function) { functions.Add(function); }
    public void Remove(Closure function) { functions.Remove(function); }

    public static Callback<T> Add(Callback<T> left, CallbackHandler<T> right)
    {
        left.callbackHandler += right;
        return left;
    }

    public static Callback<T> Add(Callback<T> left, Closure function)
    {
        left.functions.Add(function);
        return left;
    }

    public static Callback<T> operator +(Callback<T> left, CallbackHandler<T> right) { return Add(left, right); }
    public static Callback<T> operator +(Callback<T> left, Closure right) { return Add(left, right); }


    public static Callback<T> Remove(Callback<T> left, CallbackHandler<T> right)
    {
        left.callbackHandler -= right;
        return left;
    }

    public static Callback<T> Remove(Callback<T> left, Closure function)
    {
        left.functions.Remove(function);
        return left;
    }

    public static Callback<T> operator -(Callback<T> left, CallbackHandler<T> right) { return Remove(left, right); }
    public static Callback<T> operator -(Callback<T> left, Closure right) { return Remove(left, right); }
}

