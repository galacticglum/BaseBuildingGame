using System;
using System.Collections.Generic;
using System.Threading;
using MoonSharp.Interpreter;
using UnityEngine;

// Temperature is calculated using a deviation of: https://en.wikipedia.org/wiki/Heat_equation.
[MoonSharpUserData]
public class Temperature
{
    /// <summary>
    /// Must be between 0 and 1!
    /// </summary>
    public static float DefaultThermalDiffusivity { get; private set; }

    public float UpdateInterval { get; set; }

    private readonly Dictionary<Furniture, TemperatureUpdateEventHandler> sinksAndSources;
    private readonly float[][] temperature;
    private readonly float[] thermalDiffusivity;

    private readonly int width;
    private readonly int height;
    private float elapsed;
    private int stateOffset;

    public Temperature(int width, int height)
    {
        this.width = width;
        this.height = height;

        DefaultThermalDiffusivity = 1f;
        UpdateInterval = 0.1f;

        temperature = new[]
        {
            new float[this.width * this.height],
            new float[this.width * this.height],
        };

        thermalDiffusivity = new float[this.width * this.height];
        for (int y = 0; y < this.height; y++)
        {
            for (int x = 0; x < this.width; x++)
            {
                int index = GetIndex(x, y);
                temperature[0][index] = 0f;
                thermalDiffusivity[index] = 1f;
            }
        }

        sinksAndSources = new Dictionary<Furniture, TemperatureUpdateEventHandler>();     
    }

    public void Update()
    {
        elapsed += Time.deltaTime;
        if (!(elapsed >= UpdateInterval)) return;

        Process(Time.deltaTime);
        elapsed = 0;
    }

    private void Process(float deltaTime)
    {
        if (sinksAndSources != null)
        {
            foreach (TemperatureUpdateEventHandler eventHandler in sinksAndSources.Values)
            {
                if (eventHandler != null)
                {
                    eventHandler(this, new UpdateEventArgs(deltaTime));
                }
            }
        }

        Thread processThread = new Thread(ProcessTemperature);
        processThread.Start();
    }

    private void ProcessTemperature()
    {
        // Store references
        float[] currentTemperature = temperature[1 - stateOffset];
        float[] oldTemperature = temperature[stateOffset];

        // deltaTime * 0.23f * 0.5 (average thermalDiffusivity)
        // Make sure C is always between 0 and 0.5*0.25 (exclusive).
        const float coeff = 0.23f * 0.5f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Get neighbouring indicies
                int index = GetIndex(x, y);
                int indexUp = GetIndex(x, y + 1);
                int indexDown = GetIndex(x, y - 1);
                int indexLeft = GetIndex(x - 1, y);
                int indexRight = GetIndex(x + 1, y);

                currentTemperature[index] = oldTemperature[index];

                if (WorldController.Instance.GetTileAtWorldCoordinate(new Vector3(x, y, 0)).Room == null)
                {
                    currentTemperature[index] = 0f;
                }

                if (x > 0)
                {
                    currentTemperature[index] += coeff * Mathf.Min(thermalDiffusivity[index], thermalDiffusivity[indexLeft]) * (oldTemperature[indexLeft] - oldTemperature[index]);
                }

                if (y > 0)
                {
                    currentTemperature[index] += coeff * Mathf.Min(thermalDiffusivity[index], thermalDiffusivity[indexDown]) * (oldTemperature[indexDown] - oldTemperature[index]);
                }

                if (x < width - 1)
                {
                    currentTemperature[index] += coeff * Mathf.Min(thermalDiffusivity[index], thermalDiffusivity[indexRight]) * (oldTemperature[indexRight] - oldTemperature[index]);
                }

                if (y < height - 1)
                {
                    currentTemperature[index] += coeff * Mathf.Min(thermalDiffusivity[index], thermalDiffusivity[indexUp]) * (oldTemperature[indexUp] - oldTemperature[index]);
                }
            }
        }

        stateOffset = 1 - stateOffset;
    }

    public void RegisterSinkOrSource(Furniture provider)
    {
        // TODO: This need to be implemented
        sinksAndSources[provider] = (sender, args) => 
        {
            provider.EventManager.Trigger("TemperatureUpdated", provider, args.DeltaTime);
        };     
    }

    public void UnregisterSinkOrSource(Furniture provider)
    {
        if (sinksAndSources.ContainsKey(provider))
        {
            sinksAndSources.Remove(provider);
        }
    }

    public float GetTemperature(int x, int y)
    {
        return temperature[stateOffset][GetIndex(x, y)];
    }

    public void SetTemperature(int x, int y, float temperatureValue)
    {
        if (IsValidTemperature(temperatureValue))
        {
            temperature[stateOffset][GetIndex(x, y)] = temperatureValue;
        }
    }

    public void ModifyTemperature(int x, int y, float slope)
    {
        if (IsValidTemperature(temperature[stateOffset][GetIndex(x, y)] + slope))
        {
            temperature[stateOffset][GetIndex(x, y)] += slope;
        }
    }

    /// <summary>
    /// Public interface to thermal diffusivity model. Each tile has a value (say alpha) that
    /// tells  how the heat flows into that tile. Lower value means heat flows much slower (like trough a wall)
    /// while a value of 1 means the temperature "moves" faster. Think of it as a kind of isolation factor.
    /// TODO: walls should set the coefficient to 0.1?
    /// </summary>
    /// <param name="x">x coord.</param>
    /// <param name="y">y coord.</param>
    /// <returns>thermal diffusivity alpha at x,y.</returns>
    public float GetThermalDiffusivity(int x, int y)
    {
        return thermalDiffusivity[GetIndex(x, y)];
    }

    public void SetThermalDiffusivity(int x, int y, float value)
    {
        if (IsValidThermalDiffusivity(value))
        {
            thermalDiffusivity[GetIndex(x, y)] = value;
        }
    }

    /// <summary>
    /// Public interface to thermal diffusivity model. Change the value of thermal diffusivity at x,y by incr.
    /// </summary>
    /// <param name="x">x coord.</param>
    /// <param name="y">y coord.</param>
    /// <param name="slope">thermal diffusifity to set at x,y.</param>
    public void ModifyThermalDiffusivity(int x, int y, float slope)
    {
        if (IsValidThermalDiffusivity(thermalDiffusivity[GetIndex(x, y)] + slope))
        {
            thermalDiffusivity[GetIndex(x, y)] += slope;
        }
    }

    public bool IsValidTemperature(float temperatureValue)
    {
        return temperatureValue >= 0 && temperatureValue < Mathf.Infinity;
    }

    public bool IsValidThermalDiffusivity(float thermalDiffuse)
    {
        return thermalDiffuse >= 0 && thermalDiffuse <= 1;
    }

    private int GetIndex(int x, int y)
    {
        return y * width + x;
    }  
}
