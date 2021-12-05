using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.SqlServer.Server;

[Serializable, SqlUserDefinedAggregate(Format.UserDefined, MaxByteSize = -1)]
public class ValuesToList : IBinarySerialize
{
    List<IfcValue> Values;

    public void Init()
    {
        if (Values == null) Values = new List<IfcValue>();
    }

    public void Accumulate(IfcValue value)
    {
        Values.Add(value);
    }

    public void Merge(ValuesToList other)
    {
        Values.AddRange(other.Values);
    }

    public IfcValue Terminate()
    {
        return new IfcValue(IfcValueType.LIST, Values);
    }

    public void Read(BinaryReader r)
    {
        var rVal = new IfcValue(IfcValueType.NULL, null);
        rVal.Read(r);
        Values = rVal.Value as List<IfcValue>;
    }

    public void Write(BinaryWriter w)
    {
        var rVal = new IfcValue(IfcValueType.LIST, Values);
        rVal.Write(w);
    }
}