using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.SqlServer.Server;

[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, MaxByteSize = -1)]
public class ValuesToList : IBinarySerialize
{
    List<IfcValue> Values;

    public void Init()
    {
        if (this.Values == null) this.Values = new List<IfcValue>();
    }

    public void Accumulate(IfcValue value)
    {
        this.Values.Add(value);
    }

    public void Merge(ValuesToList other)
    {
        this.Values.AddRange(other.Values);
    }

    public IfcValue Terminate()
    {
        return new IfcValue(IfcValueType.LIST, this.Values);
    }

    public void Read(BinaryReader r)
    {
        IfcValue rVal = new IfcValue(IfcValueType.NULL, null);
        rVal.Read(r);
        this.Values = rVal.value as List<IfcValue>;
    }

    public void Write(BinaryWriter w)
    {
        IfcValue rVal = new IfcValue(IfcValueType.LIST, this.Values);
        rVal.Write(w);
    }
}
