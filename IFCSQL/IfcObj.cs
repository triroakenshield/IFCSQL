using Microsoft.SqlServer.Server;

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Text.RegularExpressions;
// ReSharper disable CheckNamespace
// ReSharper disable StringLiteralTypo

[Serializable, SqlUserDefinedType(Format.UserDefined, MaxByteSize = -1)]
public struct IfcObj : IBinarySerialize, INullable
{
    public int Id;
    public string Name;
    public List<IfcValue> Attributes;

    public bool IsNull => Name == "IFCNULL";

    public static IfcObj Null => Parse("IFCNULL()");

    [SqlMethod(OnNullCall = false)]
    public static IfcObj Parse(SqlString s)
    {
        return s.IsNull ? Null : _parse(s.Value);
    }

    [SqlMethod(OnNullCall = false)]
    public static IfcObj Create1(SqlInt32 nid, SqlString nName, IfcValue nList)
    {
        var res = new IfcObj {Id = nid.Value, Name = nName.Value};
        if (nList.Type == IfcValueType.LIST) res.Attributes = (List<IfcValue>)nList.Value;
        return res;
    }

    public static IfcObj _parse(string pStr)
    {
        var pattern1 = @"#(?<id>\d+)\s?=\s?(?<ifcobj>\w+)\((?<attr>,?.+)\);?";
        var reg = new Regex(pattern1);
        if (reg.IsMatch(pStr))
        {
            var m = reg.Match(pStr);
            var res = new IfcObj
            {
                Id = int.Parse(m.Groups["id"].Value),
                Name = m.Groups["ifcobj"].Value,
                Attributes = IfcValue.ParseAttributeList(m.Groups["attr"].Value)
            };
            return res;
        }

        var pattern2 = @"(?<ifcobj>\w+)\((?<attr>,?.+)\);?";
        var reg2 = new Regex(pattern2);
        if (!reg2.IsMatch(pStr)) return Null;
        {
            var m = reg2.Match(pStr);
            var res = new IfcObj
            {
                Name = m.Groups["ifcobj"].Value, Attributes = IfcValue.ParseAttributeList(m.Groups["attr"].Value)
            };
            return res;
        }
    }

    [SqlMethod(OnNullCall = false)]
    public int GetId()
    {
        return Id;
    }

    [SqlMethod(OnNullCall = false)]
    public string GetName()
    {
        return Name;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcValue GetAttribute(int index)
    {
        if ((index < 0) || (index >= Attributes.Count)) return new IfcValue(IfcValueType.NULL, null);
        return Attributes[index];
    }

    [SqlMethod(OnNullCall = false)]
    public IfcObj SetAttribute(int index, IfcValue nVal)
    {
        if ((index >= 0) & (index < Attributes.Count)) Attributes[index] = nVal;
        return this;
    }
    
    public List<IfcValue> _getRefs()
    {
        var rList = new List<IfcValue>();
        foreach (var val in Attributes)
        {
            rList.AddRange(IfcValue.GetRefs(val));
        }
        return rList;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcValue GetRefs()
    {
        return new IfcValue(IfcValueType.LIST, _getRefs());
    }

    public override string ToString()
    {
        return Id > 0 ? $"#{Id} = {Name}({string.Join(",", Attributes)});" : $"{Name}({string.Join(",", Attributes)});";
    }

    public void Read(BinaryReader r)
    {
        Id = r.ReadInt32();
        Name = r.ReadString();
        Attributes = new List<IfcValue>();
        var aCount = (int)r.ReadUInt32();
        //
        for (var i = 0; i < aCount; i++)
        {
            var nVal = new IfcValue();
            nVal.Read(r);
            Attributes.Add(nVal);
        }
    }

    public void Write(BinaryWriter w)
    {
        w.Write(Id);
        w.Write(Name);
        var ui = (uint)Attributes.Count;
        w.Write(ui);
        for (var i = 0; i < Attributes.Count; i++)
        {
            Attributes[i].Write(w);
        }
    }
}