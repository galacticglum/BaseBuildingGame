using System;
using System.Collections.Generic;
using System.Linq;

public class PowerSystem
{

    private float powerLevel;
    public float PowerLevel
    {
        get { return powerLevel; }
        private set
        {
            if (powerLevel.Equals(value)) return;

            powerLevel = value;
            NotifyPowerConsumers();
        }
    }

    public event PowerChangedEventHandler PowerLevelChanged;
    public void OnPowerLevelChanged(PowerEventArgs args)
    {
        PowerChangedEventHandler powerChanged = PowerLevelChanged;
        if (powerChanged != null)
        {
            powerChanged(this, args);
        }
    }

    private readonly HashSet<IPowerRelated> powerGrid;

    public PowerSystem()
    {
        powerGrid = new HashSet<IPowerRelated>();
    }

    public bool AddToPowerGrid(IPowerRelated powerRelated)
    {
        if (PowerLevel + powerRelated.PowerValue < 0)
        {
            return false;
        }

        powerGrid.Add(powerRelated);
        UpdatePowerLevel();
        powerRelated.PowerValueChanged += OnPowerValueChanged;

        Furniture furniture = (Furniture)powerRelated;
        furniture.FurnitureRemoved += (sender, args) => RemoveFromPowerGrid(args.Furniture);

        return true;
    }

    public void RemoveFromPowerGrid(IPowerRelated powerRelated)
    {
        powerGrid.Remove(powerRelated);
        UpdatePowerLevel();
    }

    public bool RequestPower(IPowerRelated powerRelated)
    {
        return powerGrid.Contains(powerRelated);
    }

    private void UpdatePowerLevel()
    {
        PowerLevel = powerGrid.Sum(related => related.PowerValue);
        if (PowerLevel < 0.0f)
        {
            RemovePowerConsumer();
        }
    }

    private void RemovePowerConsumer()
    {
        IPowerRelated powerConsumer = powerGrid.FirstOrDefault(powerRelated => powerRelated.IsConsumingPower);
        if (powerConsumer == null)
        {
            return;
        }

        RemoveFromPowerGrid(powerConsumer);
    }

    private void NotifyPowerConsumers()
    {
        foreach (IPowerRelated powerRelated in powerGrid.Where(powerRelated => powerRelated.IsConsumingPower))
        {
            OnPowerLevelChanged(new PowerEventArgs(powerRelated));
        }
    }

    private void OnPowerValueChanged(object sender, PowerEventArgs args)
    {
        RemoveFromPowerGrid(args.PowerRelated);
        AddToPowerGrid(args.PowerRelated);
    }
}
