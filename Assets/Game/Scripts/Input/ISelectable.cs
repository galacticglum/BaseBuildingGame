using System.Collections.Generic;

public interface ISelectable
{
    string GetName();
    string GetDescription();
    IEnumerable<string> GetAdditionalInfo();
}