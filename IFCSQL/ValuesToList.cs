using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.SqlServer.Server;
// ReSharper disable CheckNamespace

[Serializable, SqlUserDefinedAggregate(Format.UserDefined, MaxByteSize = -1)]
public class ValuesToList : IBinarySerialize
{
    List<IfcValue> _values;

    public void Init()
    {
        if (_values == null) _values = new List<IfcValue>();
    }

    public void Accumulate(IfcValue value)
    {
        _values.Add(value);
    }

    public void Merge(ValuesToList other)
    {
        _values.AddRange(other._values);
    }

    public IfcValue Terminate()
    {
        return new IfcValue(IfcValueType.LIST, _values);
    }

    public void Read(BinaryReader r)
    {
        var rVal = new IfcValue(IfcValueType.NULL, null);
        rVal.Read(r);
        _values = rVal.Value as List<IfcValue>;
    }

    public void Write(BinaryWriter w)
    {
        var rVal = new IfcValue(IfcValueType.LIST, _values);
        rVal.Write(w);
    }
}