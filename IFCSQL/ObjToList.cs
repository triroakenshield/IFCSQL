using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.SqlServer.Server;

[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, MaxByteSize = -1)]
public class ObjToList : IBinarySerialize
{
    List<IfcObj> Values;

    public void Init()
    {
        if (this.Values == null) this.Values = new List<IfcObj>();
    }

    public void Accumulate(IfcObj value)
    {
        this.Values.Add(value);
    }

    public void Merge(ObjToList other)
    {
        this.Values.AddRange(other.Values);
    }

    public IfcValue Terminate()
    {
        List<IfcValue> nList = new List<IfcValue>();
        if (this.Values != null)
        {
            foreach (IfcObj o in Values)
            {
                nList.Add(new IfcValue(IfcValueType.OBJ, o));
            }
        }
        return new IfcValue(IfcValueType.LIST, nList);
    }

    public void Read(BinaryReader r)
    {
        IfcValue rVal = new IfcValue(IfcValueType.NULL, null);
        rVal.Read(r);
        List<IfcValue> nList = rVal.value as List<IfcValue>;
        if (this.Values == null) this.Values = new List<IfcObj>();
        foreach (IfcValue v in nList)
        {
            Values.Add((IfcObj)v.value);
        }
    }

    public void Write(BinaryWriter w)
    {
        List<IfcValue> nList = new List<IfcValue>();
        if (this.Values != null)
        {
            foreach (IfcObj o in Values)
            {
                nList.Add(new IfcValue(IfcValueType.OBJ, o));
            }
        }
        IfcValue rVal = new IfcValue(IfcValueType.LIST, nList);
        rVal.Write(w);
    }
}