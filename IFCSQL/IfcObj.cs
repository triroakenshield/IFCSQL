using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
//
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

[Serializable]
[SqlUserDefinedType(Format.UserDefined, MaxByteSize = -1)]
public struct IfcObj : IBinarySerialize, INullable
{
    public int id;
    public string name;
    public List<IfcValue> attributes;

    public bool IsNull
    {
        get
        {
            return this.name == "IFCNULL";
        }
    }

    public static IfcObj Null
    {
        get
        {
            return IfcObj.Parse("IFCNULL()");
        }
    }

    [SqlMethod(OnNullCall = false)]
    public static IfcObj Parse(SqlString s)
    {
        if (s.IsNull) return IfcObj.Null;
        else return IfcObj._parse(s.Value);
    }

    [SqlMethod(OnNullCall = false)]
    public static IfcObj Create1(SqlInt32 nid, SqlString nname, IfcValue nlist)
    {
        IfcObj res = new IfcObj();
        res.id = nid.Value;
        res.name = nname.Value;
        if (nlist.type == IfcValueType.LIST) res.attributes = (List<IfcValue>)nlist.value;
        return res;
    }

    public static IfcObj _parse(string pstr)
    {
        string pattern1 = @"#(?<id>\d+)\s?=\s?(?<ifcobj>\w+)\((?<attr>,?.+)\);?";
        Regex reg = new Regex(pattern1);
        if (reg.IsMatch(pstr))
        {
            Match m = reg.Match(pstr);
            IfcObj res = new IfcObj();
            res.id = int.Parse(m.Groups["id"].Value);
            res.name = m.Groups["ifcobj"].Value;
            res.attributes = IfcValue.ParseAttributeList(m.Groups["attr"].Value);
            return res;
        }

        string pattern2 = @"(?<ifcobj>\w+)\((?<attr>,?.+)\);?";
        Regex reg2 = new Regex(pattern2);
        if (reg2.IsMatch(pstr))
        {
            Match m = reg2.Match(pstr);
            IfcObj res = new IfcObj();
            res.name = m.Groups["ifcobj"].Value;
            res.attributes = IfcValue.ParseAttributeList(m.Groups["attr"].Value);
            return res;
        }
        return IfcObj.Null;
    }

    [SqlMethod(OnNullCall = false)]
    public int GetID()
    {
        return this.id;
    }

    [SqlMethod(OnNullCall = false)]
    public string GetName()
    {
        return this.name;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcValue GetAttribute(int index)
    {
        if ((index < 0) || (index >= this.attributes.Count)) return new IfcValue(IfcValueType.NULL, null);
        return this.attributes[index];
    }

    [SqlMethod(OnNullCall = false)]
    public IfcObj SetAttribute(int index, IfcValue nval)
    {
        if ((index >= 0) & (index < this.attributes.Count)) this.attributes[index] = nval;
        return this;

    }
    
    public List<IfcValue> _getRefs()
    {
        List<IfcValue> rList = new List<IfcValue>();
        foreach (IfcValue val in this.attributes)
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
        if (id > 0) return $"#{id} = {name}({string.Join(",", this.attributes)});";
        else return $"{name}({string.Join(",", this.attributes)});";
    }

    public void Read(BinaryReader r)
    {
        this.id = r.ReadInt32();
        this.name = r.ReadString();
        this.attributes = new List<IfcValue>();
        int acount = (int)r.ReadUInt32();
        IfcValue nval;
        //
        for (int i = 0; i < acount; i++)
        {
            nval = new IfcValue();
            nval.Read(r);
            this.attributes.Add(nval);
        }
    }

    public void Write(BinaryWriter w)
    {
        w.Write(this.id);
        w.Write(this.name);
        uint ui = (uint)this.attributes.Count;
        w.Write(ui);
        for (int i = 0; i < this.attributes.Count; i++)
        {
            this.attributes[i].Write(w);
        }
    }

}
