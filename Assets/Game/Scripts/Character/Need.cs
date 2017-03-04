using System.Collections.Generic;
using System.Xml;

public class Need : IPrototypable
{
    public string Name { get; private set; }
    public string Type { get; private set; }
    public float RestorationTime { get; private set; }
    public bool CompleteOnFail { get; private set; }

    public Character Character { get; set; }
    public Furniture RestorationFurniture { get; private set; }

    private float amount;
    public float Amount
    {
        get { return amount; }
        set
        {
            float amountValue = value;
            if (amountValue < 0)
            {
                amountValue = 0;
            }
            if (amountValue > 100)
            {
                amountValue = 100;
            }

            amount = amountValue;
        }
    }

    public string PercentageAmount
    {
        get
        {
            if (highToLow)
            {
                return 100 - (int)Amount + "%";
            }

            return (int)Amount + "%";
        }
    }

    public string LocalizationCode { get; private set; }

    private float growthRate;
    private bool highToLow = true;
    private float restorationAmount = 100;

    private float vacuumGrowthRate;
    private float dps;
    private string[] luaUpdate;
    private bool luaOnly;

    public Need()
    {
        Amount = 0;
    }

    protected Need (Need need)
    {
        Amount = 0;
        Type = need.Type;
        LocalizationCode = need.LocalizationCode;
        Name = need.Name;
        growthRate = need.growthRate;
        highToLow = need.highToLow;
        RestorationFurniture = need.RestorationFurniture;
        RestorationTime = need.RestorationTime;
        restorationAmount = need.restorationAmount;
        CompleteOnFail = need.CompleteOnFail;
        vacuumGrowthRate = need.vacuumGrowthRate;
        dps = need.dps;
        luaUpdate = need.luaUpdate;
        luaOnly = need.luaOnly;
    }

    public Need Clone()
    {
        return new Need(this);
    }

    public void Update (float deltaTime)
    {
        NeedActions.CallFunctionsWithNeed (luaUpdate, this, deltaTime);
        if (luaOnly) return;

        Amount += growthRate * deltaTime;
        if (Character != null && Character.CurrentTile.Room != null && Character.CurrentTile.Room.GetGasPressure ("O2") < 0.15)
        {
            Amount += (vacuumGrowthRate - vacuumGrowthRate * (Character.CurrentTile.Room.GetGasPressure ("O2") * 5)) * deltaTime;
        }

        if (Character != null && Amount > 75 && Character.Job.IsNeed == false)
        {
            Character.AbandonJob (false);
        }
        if (Character != null && (Amount == 100 && Character.Job.Critical == false && CompleteOnFail))
        {
            Character.AbandonJob (false);
        }

        if (Amount == 100)
        {
            //FIXME: Fail damage code here.
        }
    }

    public void CompleteJob(object sender, JobEventArgs args)
    {
        Amount -= restorationAmount;
    }
    public void CompleteJobCritical(object sender, JobEventArgs args)
    {
        Amount -= restorationAmount / 4;
    }

    public void ReadXmlPrototype(XmlReader parentReader)
    {
        Type = parentReader.GetAttribute("needType");
        XmlReader reader = parentReader.ReadSubtree();
        List<string> luaActions = new List<string> ();

        while (reader.Read())
        {
            switch (reader.Name)
            {
            case "Name":
                reader.Read();
                Name = reader.ReadContentAsString();
                break;
            case "RestoreNeedFurnitureType":
                reader.Read();
                RestorationFurniture = PrototypeManager.Furnitures[reader.ReadContentAsString()];
                break;
            case "RestoreNeedTime":
                reader.Read();
                RestorationTime = reader.ReadContentAsFloat();
                break;
            case "Damage":
                reader.Read();
                dps = reader.ReadContentAsFloat();
                break;
            case "CompleteOnFail":
                reader.Read();
                CompleteOnFail = reader.ReadContentAsBoolean();
                break;
            case "HighToLow":
                reader.Read();
                highToLow = reader.ReadContentAsBoolean();
                break;
            case "GrowthRate":
                reader.Read ();
                growthRate = reader.ReadContentAsFloat();
                break;
            case "GrowthInVacuum":
                reader.Read ();
                vacuumGrowthRate = reader.ReadContentAsFloat();
                break;
            case "RestoreNeedAmount":
                reader.Read ();
                restorationAmount = reader.ReadContentAsFloat();
                break;
            case "Localization":
                reader.Read ();
                LocalizationCode = reader.ReadContentAsString();
                break;
            case "LuaProcessingOnly":
                luaOnly = true;
                break;
            case "OnUpdate":
                reader.Read ();
                luaActions.Add(reader.ReadContentAsString());
                break;
            }
        }

        luaUpdate = luaActions.ToArray();
    }
}
