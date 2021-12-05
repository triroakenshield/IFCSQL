using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.SqlServer.Server;
// ReSharper disable CheckNamespace

[Serializable, SqlUserDefinedAggregate(Format.UserDefined, MaxByteSize = -1)]
public class ObjToList : IBinarySerialize
{
    List<IfcObj> _values;

    public void Init()
    {
        if (_values == null) _values = new List<IfcObj>();
    }

    public void Accumulate(IfcObj value)
    {
        _values.Add(value);
    }

    public void Merge(ObjToList other)
    {
        _values.AddRange(other._values);
    }

    public IfcValue Terminate()
    {
        var nList = new List<IfcValue>();
        if (_values != null)
        {
            foreach (var o in _values)
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
        if (_values == null) _values = new List<IfcObj>();
        if (nList == null) return;
        foreach (var v in nList)
        {
            _values.Add((IfcObj) v.Value);
        }
    }

    public void Write(BinaryWriter w)
    {
        var nList = new List<IfcValue>();
        if (_values != null)
        {
            foreach (var o in _values)
            {
                nList.Add(new IfcValue(IfcValueType.OBJ, o));
            }
        }
        var rVal = new IfcValue(IfcValueType.LIST, nList);
        rVal.Write(w);
    }
}