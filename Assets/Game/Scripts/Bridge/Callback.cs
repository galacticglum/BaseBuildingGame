using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

public delegate void CallbackHandler(object sender);
public delegate void CallbackHandler<in T>(object sender, T args);

//public delegate TReturn CallbackHandler<in T, out TReturn>(object sender, T args);

[MoonSharpUserData]
public class Callback 
{
    private event CallbackHandler callbackHandler;
    private readonly List<Closure> functions;

    public Callback()
    {
        functions = new List<Closure>();
    }

    public Callback(Closure function) : this()
    {
        functions.Add(function);
    }

    public Callback(CallbackHandler callbackHandler) : this()
    {
        this.callbackHandler = callbackHandler;
    }

    public Callback(Callback callback)
    {
        callbackHandler = callback.callbackHandler;
        functions = callback.functions;
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
            Lua.Call(function, this);
        }
    }

    public void AddHandler(Closure function) { functions.Add(function); }
    public void RemoveHandler(Closure function) { functions.Remove(function); }

    public static Callback AddHandler(Callback left, CallbackHandler right)
    {
        left.callbackHandler += right;
        return left;
    }

    public static Callback AddHandler(Callback left, Closure function)
    {
        left.functions.Add(function);
        return left;
    }

    public static Callback operator +(Callback left, CallbackHandler right) { return AddHandler(left, right); }
    public static Callback operator +(Callback left, Closure right) { return AddHandler(left, right); }


    public static Callback RemoveHandler(Callback left, CallbackHandler right)
    {
        left.callbackHandler -= right;
        return left;
    }

    public static Callback RemoveHandler(Callback left, Closure function)
    {
        left.functions.Remove(function);
        return left;
    }

    public static Callback operator -(Callback  left, CallbackHandler right) { return RemoveHandler(left, right); }
    public static Callback operator -(Callback left, Closure right) { return RemoveHandler(left, right); }

    public static implicit operator Callback(CallbackHandler callbackHandler) { return new Callback(callbackHandler); }
    public static implicit operator Callback(Closure function) { return new Callback(function); }
}

[MoonSharpUserData]
public class Callback<T> 
{
    private event CallbackHandler<T> callbackHandler;
    private readonly List<Closure> functions;

    public Callback()
    {
        functions = new List<Closure>();
    }

    public Callback(Closure function) : this()
    {
        functions.Add(function);
    }

    public Callback(CallbackHandler<T> callbackHandler) : this()
    {
        this.callbackHandler = callbackHandler;
    }

    public Callback(Callback<T> callback)
    {
        callbackHandler = callback.callbackHandler;
        functions = callback.functions;
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
            Lua.Call(function, this, args);
        }
    }

    public void AddHandler(Closure function) { functions.Add(function); }
    public void RemoveHandler(Closure function) { functions.Remove(function); }

    public static Callback<T> AddHandler(Callback<T> left, CallbackHandler<T> right)
    {
        left.callbackHandler += right;
        return left;
    }

    public static Callback<T> AddHandler(Callback<T> left, Closure function)
    {
        left.functions.Add(function);
        return left;
    }

    public static Callback<T> operator +(Callback<T> left, CallbackHandler<T> right) { return AddHandler(left, right); }
    public static Callback<T> operator +(Callback<T> left, Closure right) { return AddHandler(left, right); }


    public static Callback<T> RemoveHandler(Callback<T> left, CallbackHandler<T> right)
    {
        left.callbackHandler -= right;
        return left;
    }

    public static Callback<T> RemoveHandler(Callback<T> left, Closure function)
    {
        left.functions.Remove(function);
        return left;
    }

    public static Callback<T> operator -(Callback<T> left, CallbackHandler<T> right) { return RemoveHandler(left, right); }
    public static Callback<T> operator -(Callback<T> left, Closure right) { return RemoveHandler(left, right); }

    public static implicit operator Callback<T>(CallbackHandler<T> callbackHandler) { return new Callback<T>(callbackHandler); }
    public static implicit operator Callback<T>(Closure function) { return new Callback<T>(function); }
}

