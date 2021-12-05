using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.SqlServer.Server;

[Serializable, SqlUserDefinedAggregate(Format.UserDefined, MaxByteSize = -1)]
public class ObjToList : IBinarySerialize
{
    List<IfcObj> Values;

    public void Init()
    {
        if (Values == null) Values = new List<IfcObj>();
    }

    public void Accumulate(IfcObj value)
    {
        Values.Add(value);
    }

    public void Merge(ObjToList other)
    {
        Values.AddRange(other.Values);
    }

    public IfcValue Terminate()
    {
        var nList = new List<IfcValue>();
        if (Values != null)
        {
            foreach (var o in Values)
            {
                nList.Add(new IfcValue(IfcValueType.OBJ, o));
            }
        }
        return new IfcValue(IfcValueType.LIST, nList);
    }

    public void Read(BinaryReader r)
    {
        var rVal = new IfcValue(IfcValueType.NULL, null);
        rVal.Read(r);
        var nList = rVal.Value as List<IfcValue>;
        if (Values == null) Values = new List<IfcObj>();
        foreach (var v in nList)
        {
            Values.Add((IfcObj)v.Value);
        }
    }

    public void Write(BinaryWriter w)
    {
        var nList = new List<IfcValue>();
        if (Values != null)
        {
            foreach (var o in Values)
            {
                nList.Add(new IfcValue(IfcValueType.OBJ, o));
            }
        }
        var rVal = new IfcValue(IfcValueType.LIST, nList);
        rVal.Write(w);
    }
}