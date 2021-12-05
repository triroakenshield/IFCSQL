using Microsoft.SqlServer.Server;

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

[Serializable, SqlUserDefinedType(Format.UserDefined, MaxByteSize = -1)]
public struct IfcFile : IBinarySerialize, INullable
{
    public List<IfcObj> Head;
    public List<IfcObj> Data;
    Dictionary<int, IfcObj> DataDict;

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
            DataDict = new Dictionary<int, IfcObj>()
        };
        return res;
    }

    private static List<IfcObj> ParseData(string pattern, string ifcstring)
    {
        var reg = new Regex(pattern);
        var sarr = Regex.Split(reg.Match(ifcstring).Groups["data"].Value, ";\r\n");
        var rList = new List<IfcObj>();
        //IfcObj obj;
        foreach (var str in sarr)
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

        DataDict = new Dictionary<int, IfcObj>();

        foreach (var obj in Data)
        {
            DataDict.Add(obj.Id, obj);
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

    //[SqlMethod(Name = "GetHeadItems", TableDefinition = "obj IfcObj")]
    //public IEnumerable GetHeadItems()
    //{      
    //    return this.Head.ToArray<IfcObj>();
    //}

    [SqlMethod(OnNullCall = false)]
    public IfcFile SetHeadItem(int index, IfcObj nval)
    {
        if ((index >= 0) & (index < Data.Count)) Head[index] = nval;
        return this;
    }

    private int GetNextID()
    {
        return DataDict.Keys.Max();
    }

    private void _addDataItem(IfcObj nval)
    {
        if (nval.Id > 0)
        {
            if (DataDict.ContainsKey(nval.Id)) nval.Id = GetNextID();
        }
        else
        {
            nval.Id = GetNextID();
        }
        //
        Data.Add(nval);
        DataDict.Add(nval.Id, nval);
    }

    [SqlMethod(OnNullCall = false)]
    public IfcFile AddDataItem(IfcObj nval)
    {
        //this.Data.Add(nval);
        _addDataItem(nval);
        return this;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcFile AddDataItems(IfcValue nlist)
    {
        if (nlist.Type == IfcValueType.LIST)
        {
            var Values = nlist.Value as List<IfcValue>;
            foreach (var v in Values)
            {
                if (v.Type == IfcValueType.OBJ) _addDataItem((IfcObj)v.Value);
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
    public IfcObj GetDataItemByID(int tid)
    {
        return DataDict.ContainsKey(tid) ? DataDict[tid] : IfcObj.Null;
        //return this.DataDict[tid];
    }

    [SqlMethod(OnNullCall = false)]
    public IfcFile SetDataItem(int index, IfcObj nval)
    {
        if ((index >= 0) & (index < Data.Count)) Data[index] = nval;
        return this;
    }

    public List<IfcObj> _getLinks(int sid)
    {
        var rList = new List<IfcObj>();
        List<IfcValue> refs;
        var sobj = GetDataItemByID(sid);
        if (!sobj.IsNull)
        {
            rList.Add(sobj);
            refs = sobj._getRefs();
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
        //List<IfcValue> refs, nrefs = new List<IfcValue>();
        var sobj = GetDataItemByID(sid);
        rList.Add(sobj);
        //if (!sobj.IsNull)
        //{
        //    rList.Add(sobj);
        //    refs = sobj._getRefs();

        //    do
        //    {
        //        nrefs.Clear();
        //        foreach (IfcValue ro in refs)
        //        {

        //            int nid = (int)ro.value;
        //            sobj = this.GetDataItemByID(sid);
        //            if (!sobj.IsNull)
        //            {
        //                rList.Add(sobj);
        //                nrefs.AddRange(sobj._getRefs());
        //            }

        //            //rList.AddRange(this._getLinks(nid));
        //        }
        //        refs = nrefs;
        //    } while (refs.Count > 0);
        //}
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
        //throw new NotImplementedException();

        Head = new List<IfcObj>();
        Data = new List<IfcObj>();
        DataDict = new Dictionary<int, IfcObj>();

        IfcObj nobj;

        var wcount = (int)r.ReadUInt32();
        for (var i = 0; i < wcount; i++)
        {
            nobj = new IfcObj();
            nobj.Read(r);
            Head.Add(nobj);
        }
        //
        wcount = (int)r.ReadUInt32();
        if (wcount > 0)
        {
            for (var i = 0; i < wcount; i++)
            {
                nobj = new IfcObj();
                nobj.Read(r);
                Data.Add(nobj);
            }
        }
        
        foreach (IfcObj obj in Data)
        {
            DataDict.Add(obj.Id, obj);
        }
    }

    public void Write(BinaryWriter w)
    {
        //throw new NotImplementedException();
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