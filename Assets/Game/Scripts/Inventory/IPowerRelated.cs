using System;

public interface IPowerRelated
{
    float PowerValue { get; }
    bool IsConsumingPower { get; }
    bool HasPower();

    event Action<IPowerRelated> PowerValueChanged;
}
