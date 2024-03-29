﻿using Microsoft.SqlServer.Server;

using System;
using System.Collections.Generic;

using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

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

[Serializable, SqlUserDefinedType(Format.UserDefined, MaxByteSize = -1)]
public struct IfcValue : IBinarySerialize, INullable
{
    public IfcValueType Type;
    public object Value;

    public bool IsNull => false; //type == IfcValueType.NULL;

    public static IfcValue Null => new IfcValue(IfcValueType.NULL, null);

    public static IfcValueType GetType(string tStr)
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
        var pattern = string.Join("|", patterns);
        var reg = new Regex(pattern);
        string[] gNames = { "null", "der", "int", "real", "str", "ent", "enum", "list", "obj" };
        var m = reg.Match(tStr);
        foreach (string gName in gNames)
        {
            if (m.Groups[gName].Success)
            {
                switch (gName)
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

    public IfcValue(IfcValueType nType, object nValue)
    {
        Type = nType;
        Value = nValue;
    }

    public static List<IfcValue> ParseAttributeList(string pStr)
    {
        var rList = new List<IfcValue>();
        if (pStr.Length == 0) return rList;
        var nToken = string.Empty;
        int level1 = 0, level2 = 0;
        bool sList = false, sObj = false;
        for (var i = 0; i < pStr.Length; i++)
        {
            var ch = pStr[i];
            if (level1 == 0 & level2 == 0 & (ch == '#' | char.IsLetter(ch))) sObj = true;
            switch (ch) //obj!!!
            {
                case '(':
                    level1++;
                    sList = true;
                    if (sObj) nToken += ch;
                    break;
                case ')':
                    level1--;
                    if (sObj) nToken += ch;
                    break;
                case '\'':
                    if (level2 == 0) level2++;
                    else level2--;
                    if (sObj) nToken += ch;
                    break;
                case ',':
                    if (level1 == 0 & level2 == 0)
                    {
                        if (sObj)
                        {
                            rList.Add(new IfcValue(nToken));
                            sObj = false;
                            sList = false;
                        }
                        else if (sList)
                        {
                            rList.Add(new IfcValue(IfcValueType.LIST, ParseAttributeList(nToken)));
                            sList = false;
                        }
                        else rList.Add(new IfcValue(nToken));
                        nToken = string.Empty;
                    }
                    else nToken += ch;
                    break;
                default:
                    nToken += ch;
                    break;
            }
        }
        if (sObj)
        {
            rList.Add(new IfcValue(nToken));
            sList = false;
        }
        else
        {
            rList.Add(sList ? new IfcValue(IfcValueType.LIST, ParseAttributeList(nToken)) : new IfcValue(nToken));
        }

        return rList;
    }

    public IfcValue(string nValue)
    {
        nValue = nValue.Trim();
        Type = GetType(nValue);
        Value = null;
        switch (Type)
        {
            case IfcValueType.NULL:
                //this.value = null;
                break;
            case IfcValueType.DERIVE:
                //this.value = null;
                break;
            case IfcValueType.STRING:
                Value = nValue;
                break;
            case IfcValueType.REAL:
                Value = double.Parse(nValue, CultureInfo.InvariantCulture);
                break;
            case IfcValueType.INTEGER:
                Value = int.Parse(nValue);
                break;
            case IfcValueType.ENTITY_INSTANCE_NAME: //!!!
                Value = int.Parse(nValue.Substring(1));
                break;
            case IfcValueType.ENUMERATION: //!!!
                Value = nValue.Substring(1, nValue.Length - 2);
                break;
            case IfcValueType.LIST:
                Value = ParseAttributeList(nValue.Substring(1, nValue.Length - 2));
                break;
            case IfcValueType.OBJ:
                Value = IfcObj.Parse(nValue);
                break;
        }
    }

    [SqlMethod(OnNullCall = false)]
    public static IfcValue Parse(SqlString s)
    {
        return s.IsNull ? new IfcValue() : new IfcValue(s.Value);
    }

    public static string HexToStr(string tStr)
    {
        var rStr = "";
        for (var i = 0; i < tStr.Length; i += 4)
            rStr += (char)short.Parse(tStr.Substring(i, 4), NumberStyles.AllowHexSpecifier);
        return rStr;
    }

    public static string toUTF(string instr)
    {
        var rgx = new Regex(@"\\X2\\(?<val>[0-9a-fA-F]+)\\X0\\");
        var res = rgx.Matches(instr);
        foreach (Match mt in res)
        {
            instr = instr.Replace(mt.Value, HexToStr(mt.Groups["val"].Value));
        }
        return instr;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcValue AddItem(IfcValue nVal)
    {
        List<IfcValue> wList;
        if (Type != IfcValueType.LIST)
        {
            var oval = new IfcValue(Type, Value);
            Type = IfcValueType.LIST;
            wList = new List<IfcValue>();
            Value = wList;
            wList.Add(oval);
            wList.Add(nVal);
        }
        else ((List<IfcValue>)Value).Add(nVal);
        return this;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcValue DelItem(int index)
    {
        if (Type != IfcValueType.LIST) return this;
        var wList = (List<IfcValue>)Value;
        if ((index >= 0) & (index < wList.Count)) wList.Remove(wList[index]);
        return this;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcValue GetItem(int index)
    {
        if (Type != IfcValueType.LIST) return new IfcValue();
        var wList = (List<IfcValue>)Value;
        if ((index >= 0) & (index < wList.Count)) return wList[index];
        return new IfcValue();
    }

    [SqlMethod(OnNullCall = false)]
    public IfcValue SetItem(int index, IfcValue nval)
    {
        if (Type == IfcValueType.LIST)
        {
            var wList = (List<IfcValue>)Value;
            if ((index >= 0) & (index < wList.Count)) wList[index] = nval;
        }
        return this;
    }

    [SqlMethod(IsDeterministic = true, IsPrecise = true)]
    public string GetValueType()
    {
        return Type.ToString();
    }

    public static IfcValue GetList(List<IfcObj> wList)
    {
        var vList = new List<IfcValue>();

        foreach (var o in wList)
        {
            vList.Add(new IfcValue(IfcValueType.OBJ, o));
        }

        return new IfcValue(IfcValueType.LIST, vList); 
    }

    public static List<IfcValue> GetRefs(IfcValue val)
    {
        var rList = new List<IfcValue>();
        switch (val.Type)
        {
            case IfcValueType.ENTITY_INSTANCE_NAME: //!!!
                rList.Add(val);
                break;
            case IfcValueType.LIST:
                var wList = val.Value as List<IfcValue>;
                foreach (var sval in wList)
                {
                    rList.AddRange(GetRefs(sval));
                }
                break;
            case IfcValueType.OBJ:
                rList.AddRange(((IfcObj)val.Value)._getRefs());
                break;
        }
        return rList;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcValue ReplaceID(IfcValue oid, IfcValue nid)
    {
        if ((Type == IfcValueType.LIST) &
             (oid.Type == IfcValueType.LIST) &
             (nid.Type == IfcValueType.LIST))
        {
            var oList = (List<IfcValue>)oid.Value;
            var nList = (List<IfcValue>)nid.Value;
            IfcValue nextoidv, nextnidv, tattr, ov;
            int tid;

            var DictID = new Dictionary<int, int>();
            for (var i = 0; i < oList.Count; i++)
            {
                nextoidv = oList[i];
                nextnidv = nList[i];
                if ((nextnidv.Type == IfcValueType.ENTITY_INSTANCE_NAME) &
                    (nextoidv.Type == IfcValueType.ENTITY_INSTANCE_NAME))
                {
                    DictID.Add((int)nextoidv.Value, (int)nextnidv.Value);
                }
            }

            var wList = Value as List<IfcValue>;
            for (var i = 0; i < wList?.Count; i++)
            {
                ov = wList[i];
                if (ov.Type == IfcValueType.OBJ)
                {
                    var obj = (IfcObj)ov.Value;
                    if (DictID.ContainsKey(obj.Id))
                    {
                        obj.Id = DictID[obj.Id];
                        ov.Value = obj;
                        wList[i] = ov;
                    }

                    for (int j = 0; j < obj.Attributes.Count; j++)
                    {
                        tattr = obj.Attributes[j];

                        if (tattr.Type == IfcValueType.ENTITY_INSTANCE_NAME)
                        {
                            tid = (int)tattr.Value;
                            if (DictID.ContainsKey(tid)) tattr.Value = DictID[tid];
                        }
                    }
                }
            }
        }
        return this;
    }

    public override string ToString()
    {
        switch (Type)
        {
            case IfcValueType.NULL:
                return "$";
            case IfcValueType.DERIVE:
                return "*";
            case IfcValueType.STRING:
                return $"'{toUTF((string)Value)}'";
            case IfcValueType.REAL:
                return $"{(double)Value:#.0###}";
            case IfcValueType.INTEGER:
                return $"{(int)Value}";
            case IfcValueType.ENTITY_INSTANCE_NAME: //!!!
                return $"#{(int)Value}";
            case IfcValueType.ENUMERATION: //!!!
                return $".{((string)Value).ToUpper()}.";
            case IfcValueType.LIST:
                return $"({string.Join(",", ((List<IfcValue>)Value).ConvertAll(x => x.ToString()))})";
            case IfcValueType.OBJ:
                return ((IfcObj)Value).ToString();
            default: return (string)Value;
        }
    }

    public void Read(BinaryReader r)
    {
        //throw new NotImplementedException();
        var bt = r.ReadByte();
        Type = (IfcValueType)bt;
        //
        switch (Type)
        {
            case IfcValueType.NULL:
                break;
            case IfcValueType.DERIVE:
                break;
            case IfcValueType.STRING:
                Value = r.ReadString();
                break;
            case IfcValueType.REAL:
                Value = r.ReadDouble();
                break;
            case IfcValueType.INTEGER:
                Value = r.ReadInt32();
                break;
            case IfcValueType.ENTITY_INSTANCE_NAME: //!!!
                Value = r.ReadInt32();
                break;
            case IfcValueType.ENUMERATION: //!!!
                Value = r.ReadString();
                break;
            case IfcValueType.LIST: //!!!
                var wList = new List<IfcValue>();
                IfcValue nval;
                var lcount = (int)r.ReadUInt32();
                //
                if (lcount > 0)
                {
                    for (var i = 0; i < lcount; i++)
                    {
                        nval = new IfcValue();
                        nval.Read(r);
                        wList.Add(nval);
                    }
                }
                Value = wList;
                break;
            case IfcValueType.OBJ:
                IfcObj obj = new IfcObj();
                obj.Read(r);
                Value = obj;
                break;
            default:
                Value = r.ReadString();
                break;
        }
    }

    public void Write(BinaryWriter w)
    {
        //throw new NotImplementedException();
        w.Write((byte)Type);
        //
        switch (Type)
        {
            case IfcValueType.NULL:
                break;
            case IfcValueType.DERIVE:
                break;
            case IfcValueType.STRING:
                w.Write((string)Value);
                break;
            case IfcValueType.REAL:
                w.Write((double)Value);
                break;
            case IfcValueType.INTEGER:
                w.Write((int)Value);
                break;
            case IfcValueType.ENTITY_INSTANCE_NAME: //!!!
                w.Write((int)Value);
                break;
            case IfcValueType.ENUMERATION: //!!!
                w.Write(((string)Value).ToUpper());
                break;
            case IfcValueType.LIST: //!!!
                if (Value is List<IfcValue> wList)
                {
                    var ui = (uint)wList.Count();
                    w.Write(ui);
                    foreach (var val in wList)
                    {
                        val.Write(w);
                    }
                }
                else w.Write(0);
                break;
            case IfcValueType.OBJ:
                IfcObj obj = (IfcObj)Value;
                obj.Write(w);
                break;
            default:
                w.Write((string)Value);
                break;
        }
    }
}