using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
//
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

public enum IfcValueType
{
    NULL = 1,
    DERIVE = 2,
    INTEGER = 3,
    REAL = 4,
    STRING = 5,
    ENTITY_INSTANCE_NAME = 6,
    ENUMERATION = 7,
    LIST = 8,
    OBJ = 9,
    BINARY = 10
    //BOOL = 11,
    //LOGICAL = 12
}

[Serializable]
[SqlUserDefinedType(Format.UserDefined, MaxByteSize = -1)]
public struct IfcValue : IBinarySerialize, INullable
{

    public IfcValueType type;
    public object value;

    public bool IsNull
    {
        get
        {
            return false; //type == IfcValueType.NULL;
        }
    }

    public static IfcValue Null
    {
        get
        {
            return new IfcValue(IfcValueType.NULL, null);
        }
    }

    public static IfcValueType GetType(string tstr)
    {
        string[] patterns = {
                @"(?<null>^\$$)",
                @"(?<der>^\*$)",
                @"(?<int>^(-|\+)?\d+$)",
                @"(?<real>^(-|\+)?\d*\.\d*(E)?(-|\+)?\d*$)",
                @"(?<str>^\'.*\')",
                @"(?<ent>^#\d+$)",
                @"(?<enum>^\..+\.$)",
                @"(?<list>^\((.+,)*.*\)$)",
                @"(?<obj>^(#\d+\s?=\s?)?\w+\(.+\);?$)"
            };
        string pattern = String.Join("|", patterns);
        Regex reg = new Regex(pattern);
        string[] gnames = { "null", "der", "int", "real", "str", "ent", "enum", "list", "obj" };
        Match m = reg.Match(tstr);
        foreach (string gname in gnames)
        {
            if (m.Groups[gname].Success)
            {
                switch (gname)
                {
                    case "null": return IfcValueType.NULL;
                    case "der": return IfcValueType.DERIVE;
                    case "int": return IfcValueType.INTEGER;
                    case "real": return IfcValueType.REAL;
                    case "str": return IfcValueType.STRING;
                    case "ent": return IfcValueType.ENTITY_INSTANCE_NAME;
                    case "enum": return IfcValueType.ENUMERATION;
                    case "list": return IfcValueType.LIST;
                    case "obj": return IfcValueType.OBJ;
                }
            }
        }
        return IfcValueType.STRING;
    }


    //public IfcValue()
    //{
    //    this.type = IfcValueType.NULL;
    //    this.value = null;
    //}

    public IfcValue(IfcValueType ntype, object nvalue)
    {
        this.type = ntype;
        this.value = nvalue;
    }

    public static List<IfcValue> ParseAttributeList(string pstr)
    {
        List<IfcValue> rList = new List<IfcValue>();
        if (pstr.Length == 0) return rList;
        string ntoken = string.Empty;
        int level1 = 0, level2 = 0;
        bool slist = false, sobj = false;
        for (int i = 0; i < pstr.Length; i++)
        {
            char ch = pstr[i];
            if (level1 == 0 & level2 == 0 & (ch == '#' | char.IsLetter(ch))) sobj = true;
            switch (ch) //obj!!!
            {
                case '(':
                    level1++;
                    slist = true;
                    if (sobj) ntoken += ch;
                    break;
                case ')':
                    level1--;
                    if (sobj) ntoken += ch;
                    break;
                case '\'':
                    if (level2 == 0) level2++;
                    else level2--;
                    if (sobj) ntoken += ch;
                    break;
                case ',':
                    if (level1 == 0 & level2 == 0)
                    {
                        if (sobj)
                        {
                            rList.Add(new IfcValue(ntoken));
                            sobj = false;
                            slist = false;
                        }
                        else if (slist)
                        {
                            rList.Add(new IfcValue(IfcValueType.LIST, IfcValue.ParseAttributeList(ntoken)));
                            slist = false;
                        }
                        else rList.Add(new IfcValue(ntoken));
                        ntoken = string.Empty;
                    }
                    else ntoken += ch;
                    break;
                default:
                    ntoken += ch;
                    break;
            }
        }
        if (sobj)
        {
            rList.Add(new IfcValue(ntoken));
            slist = false;
        }
        else
        {
            if (slist) rList.Add(new IfcValue(IfcValueType.LIST, IfcValue.ParseAttributeList(ntoken)));
            else rList.Add(new IfcValue(ntoken));
        }

        return rList;
    }

    public IfcValue(string nvalue)
    {
        nvalue = nvalue.Trim();
        this.type = IfcValue.GetType(nvalue);
        this.value = null;
        switch (this.type)
        {
            case IfcValueType.NULL:
                //this.value = null;
                break;
            case IfcValueType.DERIVE:
                //this.value = null;
                break;
            case IfcValueType.STRING:
                this.value = nvalue;
                break;
            case IfcValueType.REAL:
                this.value = double.Parse(nvalue);
                break;
            case IfcValueType.INTEGER:
                this.value = int.Parse(nvalue);
                break;
            case IfcValueType.ENTITY_INSTANCE_NAME: //!!!
                this.value = int.Parse(nvalue.Substring(1));
                break;
            case IfcValueType.ENUMERATION: //!!!
                this.value = nvalue.Substring(1, nvalue.Length - 2);
                break;
            case IfcValueType.LIST:
                this.value = IfcValue.ParseAttributeList(nvalue.Substring(1, nvalue.Length - 2));
                break;
            case IfcValueType.OBJ:
                this.value = IfcObj.Parse(nvalue);
                break;
        }
    }

    [SqlMethod(OnNullCall = false)]
    public static IfcValue Parse(SqlString s)
    {
        if (s.IsNull) return new IfcValue();
        else return new IfcValue(s.Value);
    }

    public static string HexToStr(string tstr)
    {
        string rstr = "";
        for (int i = 0; i < tstr.Length; i += 4)
            rstr += (char)Int16.Parse(tstr.Substring(i, 4), NumberStyles.AllowHexSpecifier);
        return rstr;
    }

    public static string toUTF(string instr)
    {
        Regex rgx = new Regex(@"\\X2\\(?<val>[0-9a-fA-F]+)\\X0\\");
        var res = rgx.Matches(instr);
        foreach (Match mt in res)
        {
            instr = instr.Replace(mt.Value, HexToStr(mt.Groups["val"].Value));
        }
        return instr;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcValue AddItem(IfcValue nval)
    {
        List<IfcValue> wList;
        if (this.type != IfcValueType.LIST)
        {
            IfcValue oval = new IfcValue(this.type, this.value);
            this.type = IfcValueType.LIST;
            wList = new List<IfcValue>();
            this.value = wList;
            wList.Add(oval);
            wList.Add(nval);
        }
        else ((List<IfcValue>)this.value).Add(nval);
        return this;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcValue DelItem(int index)
    {
        if (this.type == IfcValueType.LIST)
        {
            List<IfcValue> wList = (List<IfcValue>)this.value;
            if ((index >= 0) & (index < wList.Count)) wList.Remove(wList[index]);
        }
        return this;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcValue GetItem(int index)
    {
        if (this.type == IfcValueType.LIST)
        {
            List<IfcValue> wList = (List<IfcValue>)this.value;
            if ((index >= 0) & (index < wList.Count)) return wList[index];
        }
        return new IfcValue();
    }

    [SqlMethod(OnNullCall = false)]
    public IfcValue SetItem(int index, IfcValue nval)
    {
        if (this.type == IfcValueType.LIST)
        {
            List<IfcValue> wList = (List<IfcValue>)this.value;
            if ((index >= 0) & (index < wList.Count)) wList[index] = nval;
        }
        return this;
    }

    [SqlMethod(IsDeterministic = true, IsPrecise = true)]
    public string GetValueType()
    {
        return this.type.ToString();
    }

    public static IfcValue GetList(List<IfcObj> wList)
    {
        List<IfcValue> vList = new List<IfcValue>();

        foreach (IfcObj o in wList)
        {
            vList.Add(new IfcValue(IfcValueType.OBJ, o));
        }

        return new IfcValue(IfcValueType.LIST, vList); 
    }

    public static List<IfcValue> GetRefs(IfcValue val)
    {
        List<IfcValue> rList = new List<IfcValue>();
        switch (val.type)
        {
            case IfcValueType.ENTITY_INSTANCE_NAME: //!!!
                rList.Add(val);
                break;
            case IfcValueType.LIST:
                List<IfcValue> wList = val.value as List<IfcValue>;
                foreach (IfcValue sval in wList)
                {
                    rList.AddRange(IfcValue.GetRefs(sval));
                }
                break;
            case IfcValueType.OBJ:
                rList.AddRange(((IfcObj)val.value)._getRefs());
                break;
            default:
                break;
        }
        return rList;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcValue ReplaceID(IfcValue oid, IfcValue nid)
    {
        if ((this.type == IfcValueType.LIST) &
             (oid.type == IfcValueType.LIST) &
             (nid.type == IfcValueType.LIST))
        {
            List<IfcValue> oList = (List<IfcValue>)oid.value;
            List<IfcValue> nList = (List<IfcValue>)nid.value;
            IfcValue nextoidv, nextnidv, tattr, ov;
            int tid;

            Dictionary<int, int> DictID = new Dictionary<int, int>();
            for (int i = 0; i < oList.Count; i++)
            {
                nextoidv = oList[i];
                nextnidv = nList[i];
                if ((nextnidv.type == IfcValueType.ENTITY_INSTANCE_NAME) &
                    (nextoidv.type == IfcValueType.ENTITY_INSTANCE_NAME))
                {
                    DictID.Add((int)nextoidv.value, (int)nextnidv.value);
                }
            }

            List<IfcValue> wList = this.value as List<IfcValue>;
            for (int i = 0; i < wList.Count; i++)
            {
                //
                ov = wList[i];
                if (ov.type == IfcValueType.OBJ)
                {
                    IfcObj obj = (IfcObj)ov.value;
                    if (DictID.ContainsKey(obj.id))
                    {
                        obj.id = DictID[obj.id];
                        ov.value = obj;
                        wList[i] = ov;
                    }

                    for (int j = 0; j < obj.attributes.Count; j++)
                    {
                        tattr = obj.attributes[j];

                        if (tattr.type == IfcValueType.ENTITY_INSTANCE_NAME)
                        {
                            tid = (int)tattr.value;
                            if (DictID.ContainsKey(tid)) tattr.value = DictID[tid];
                        }
                    }
                }
            }
        }
        return this;
    }

    public override string ToString()
    {
        switch (this.type)
        {
            case IfcValueType.NULL:
                return "$";
            case IfcValueType.DERIVE:
                return "*";
            case IfcValueType.STRING:
                return $"'{IfcValue.toUTF((string)this.value)}'";
            case IfcValueType.REAL:
                return $"{((double)value).ToString("#.0###")}";
            case IfcValueType.INTEGER:
                return $"{((int)value).ToString()}";
            case IfcValueType.ENTITY_INSTANCE_NAME: //!!!
                return $"#{((int)value).ToString()}";
            case IfcValueType.ENUMERATION: //!!!
                return $".{((string)value).ToUpper()}.";
            case IfcValueType.LIST:
                return $"({string.Join(",", ((List<IfcValue>)value).ConvertAll(x => x.ToString()))})";
            case IfcValueType.OBJ:
                return ((IfcObj)value).ToString();
            default: return (string)value;
        }
    }

    public void Read(BinaryReader r)
    {
        //throw new NotImplementedException();
        byte bt = r.ReadByte();
        this.type = (IfcValueType)bt;
        //
        switch (this.type)
        {
            case IfcValueType.NULL:
                break;
            case IfcValueType.DERIVE:
                break;
            case IfcValueType.STRING:
                this.value = r.ReadString();
                break;
            case IfcValueType.REAL:
                this.value = r.ReadDouble();
                break;
            case IfcValueType.INTEGER:
                this.value = r.ReadInt32();
                break;
            case IfcValueType.ENTITY_INSTANCE_NAME: //!!!
                this.value = r.ReadInt32();
                break;
            case IfcValueType.ENUMERATION: //!!!
                this.value = r.ReadString();
                break;
            case IfcValueType.LIST: //!!!
                List<IfcValue> wList = new List<IfcValue>();
                IfcValue nval;
                int lcount = (int)r.ReadUInt32();
                //
                if (lcount > 0)
                {
                    for (int i = 0; i < lcount; i++)
                    {
                        nval = new IfcValue();
                        nval.Read(r);
                        wList.Add(nval);
                    }
                }
                this.value = wList;
                break;
            case IfcValueType.OBJ:
                IfcObj obj = new IfcObj();
                obj.Read(r);
                this.value = obj;
                break;
            default:
                this.value = r.ReadString();
                break;
        }
    }

    public void Write(BinaryWriter w)
    {
        //throw new NotImplementedException();
        w.Write((byte)type);
        //
        switch (this.type)
        {
            case IfcValueType.NULL:
                break;
            case IfcValueType.DERIVE:
                break;
            case IfcValueType.STRING:
                w.Write((string)this.value);
                break;
            case IfcValueType.REAL:
                w.Write((double)this.value);
                break;
            case IfcValueType.INTEGER:
                w.Write((int)this.value);
                break;
            case IfcValueType.ENTITY_INSTANCE_NAME: //!!!
                w.Write((int)this.value);
                break;
            case IfcValueType.ENUMERATION: //!!!
                w.Write(((string)value).ToUpper());
                break;
            case IfcValueType.LIST: //!!!
                List<IfcValue> wList = this.value as List<IfcValue>;
                if (wList != null)
                {
                    uint ui = (uint)wList.Count();
                    w.Write(ui);
                    foreach (IfcValue val in wList)
                    {
                        val.Write(w);
                    }
                }
                else w.Write(0);
                break;
            case IfcValueType.OBJ:
                IfcObj obj = (IfcObj)this.value;
                obj.Write(w);
                break;
            default:
                w.Write((string)this.value);
                break;
        }
    }


}
