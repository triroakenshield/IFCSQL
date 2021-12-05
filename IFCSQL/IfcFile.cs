using Microsoft.SqlServer.Server;

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
// ReSharper disable CheckNamespace

[Serializable, SqlUserDefinedType(Format.UserDefined, MaxByteSize = -1)]
public struct IfcFile : IBinarySerialize, INullable
{
    public List<IfcObj> Head;
    public List<IfcObj> Data;
    Dictionary<int, IfcObj> _dataDict;

    [SqlMethod(OnNullCall = false)]
    public static IfcFile Empty()
    {
        var res = new IfcFile
        {
            Head = new List<IfcObj>
            {
                IfcObj._parse("FILE_DESCRIPTION(('ViewDefinition [CoordinationView_V2.0]'),'2;1');"),
                IfcObj._parse("FILE_NAME('','',(''),(''),'','','');"),
                IfcObj._parse("FILE_SCHEMA(('IFC2X3'));")
            },
            Data = new List<IfcObj>(),
            _dataDict = new Dictionary<int, IfcObj>()
        };
        return res;
    }

    private static List<IfcObj> ParseData(string pattern, string ifcString)
    {
        var reg = new Regex(pattern);
        var sArr = Regex.Split(reg.Match(ifcString).Groups["data"].Value, ";\r\n");
        var rList = new List<IfcObj>();
        //IfcObj obj;
        foreach (var str in sArr)
        {
            if (!string.IsNullOrEmpty(str) & (str != "\r\n"))
            {
                //obj = IfcObj._Parse(str);
                rList.Add(IfcObj._parse(str));
            }
        }
        return rList;
    }

    [SqlMethod(OnNullCall = false)]
    public static IfcFile Parse(SqlString s)
    {
        return s.IsNull ? new IfcFile() : new IfcFile(s.Value);
    }

    public IfcFile(string data)
    {
        var headpattern = @"HEADER;\r\n(?<data>(.*\r\n)*?)ENDSEC;";
        var datapattern = @"DATA;\r\n(?<data>(.*\r\n)*?)ENDSEC;";

        Head = ParseData(headpattern, data);
        Data = ParseData(datapattern, data);

        _dataDict = new Dictionary<int, IfcObj>();

        foreach (var obj in Data)
        {
            _dataDict.Add(obj.Id, obj);
        }
    }

    [SqlMethod(OnNullCall = false)]
    public static IfcFile OpenIfc(string filename)
    {
        return new IfcFile(File.ReadAllText(filename));
    }

    [SqlMethod(OnNullCall = false)]
    public bool SaveIfc(string filename)
    {
        File.WriteAllText(filename, ToString());
        return true;
    }

    public bool IsNull => false;

    public static IfcFile Null => new IfcFile();

    [SqlMethod(OnNullCall = false)]
    public IfcFile AddHeadItem(IfcObj nval)
    {
        Head.Add(nval);
        return this;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcFile DelHeadItem(int index)
    {
        if ((index >= 0) & (index < Head.Count)) Head.Remove(Head[index]);
        return this;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcObj GetHeadItem(int index)
    {
        if ((index >= 0) & (index < Head.Count)) return Head[index];
        return IfcObj.Null;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcFile SetHeadItem(int index, IfcObj nVal)
    {
        if ((index >= 0) & (index < Data.Count)) Head[index] = nVal;
        return this;
    }

    private int GetNextId()
    {
        return _dataDict.Keys.Max();
    }

    private void _addDataItem(IfcObj nVal)
    {
        if (nVal.Id > 0)
        {
            if (_dataDict.ContainsKey(nVal.Id)) nVal.Id = GetNextId();
        }
        else
        {
            nVal.Id = GetNextId();
        }
        Data.Add(nVal);
        _dataDict.Add(nVal.Id, nVal);
    }

    [SqlMethod(OnNullCall = false)]
    public IfcFile AddDataItem(IfcObj nVal)
    {
        _addDataItem(nVal);
        return this;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcFile AddDataItems(IfcValue nList)
    {
        if (nList.Type == IfcValueType.LIST)
        {
            if (!(nList.Value is List<IfcValue> values)) return this;
            foreach (var v in values.Where(v => v.Type == IfcValueType.OBJ))
            {
                _addDataItem((IfcObj) v.Value);
            }
        }
        return this;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcFile DelDataItem(int index)
    {
        if ((index >= 0) & (index < Data.Count)) Data.Remove(Data[index]);
        return this;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcObj GetDataItem(int index)
    {
        if ((index >= 0) & (index < Data.Count)) return Data[index];
        return IfcObj.Null;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcObj GetDataItemById(int tid)
    {
        return _dataDict.ContainsKey(tid) ? _dataDict[tid] : IfcObj.Null;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcFile SetDataItem(int index, IfcObj nVal)
    {
        if ((index >= 0) & (index < Data.Count)) Data[index] = nVal;
        return this;
    }

    public List<IfcObj> _getLinks(int sid)
    {
        var rList = new List<IfcObj>();
        List<IfcValue> refs;
        var sObj = GetDataItemById(sid);
        if (!sObj.IsNull)
        {
            rList.Add(sObj);
            refs = sObj._getRefs();
            foreach (var ro in refs)
            {
                var nid = (int)ro.Value;
                rList.AddRange(_getLinks(nid));
            }
        }
        return rList;
    }

    public List<IfcObj> _getLinks2(int sid)
    {
        var rList = new List<IfcObj>();
        var sObj = GetDataItemById(sid);
        rList.Add(sObj);
        return rList;
    }

    [SqlMethod()]
    public IfcValue GetLinks(int sid)
    {
        return IfcValue.GetList(_getLinks(sid));
    }

    public override string ToString()
    {
        return $"ISO-10303-21;\nHEADER;\n{string.Join("\n", Head)}\nENDSEC;\n" +
               $"DATA;\n{string.Join("\n", Data)}\nENDSEC;\nEND-ISO-10303-21;";
    }

    public void Read(BinaryReader r)
    {
        Head = new List<IfcObj>();
        Data = new List<IfcObj>();
        _dataDict = new Dictionary<int, IfcObj>();

        IfcObj nObj;

        var wCount = (int)r.ReadUInt32();
        for (var i = 0; i < wCount; i++)
        {
            nObj = new IfcObj();
            nObj.Read(r);
            Head.Add(nObj);
        }
        //
        wCount = (int)r.ReadUInt32();
        if (wCount > 0)
        {
            for (var i = 0; i < wCount; i++)
            {
                nObj = new IfcObj();
                nObj.Read(r);
                Data.Add(nObj);
            }
        }
        
        foreach (var obj in Data)
        {
            _dataDict.Add(obj.Id, obj);
        }
    }

    public void Write(BinaryWriter w)
    {
        var ui = (uint)Head.Count;
        w.Write(ui);
        for (var i = 0; i < Head.Count; i++)
        {
            Head[i].Write(w);
        }
        ui = (uint)Data.Count;
        w.Write(ui);
        for (var i = 0; i < Data.Count; i++)
        {
            Data[i].Write(w);
        }
    }
}