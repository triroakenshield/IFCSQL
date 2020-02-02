using System;
using System.IO;
using System.Collections;
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
public struct IfcFile : IBinarySerialize, INullable
{

    public List<IfcObj> Head;
    public List<IfcObj> Data;
    Dictionary<int, IfcObj> DataDict;

    [SqlMethod(OnNullCall = false)]
    public static IfcFile Empty()
    {
        IfcFile res = new IfcFile();
        res.Head = new List<IfcObj>();
        res.Head.Add(IfcObj._parse("FILE_DESCRIPTION(('ViewDefinition [CoordinationView_V2.0]'),'2;1');"));
        res.Head.Add(IfcObj._parse("FILE_NAME('','',(''),(''),'','','');"));
        res.Head.Add(IfcObj._parse("FILE_SCHEMA(('IFC2X3'));"));
        res.Data = new List<IfcObj>();
        res.DataDict = new Dictionary<int, IfcObj>();
        return res;
    }

    private static List<IfcObj> ParseData(string pattern, string ifcstring)
    {
        Regex reg = new Regex(pattern);
        string[] sarr = Regex.Split(reg.Match(ifcstring).Groups["data"].Value, ";\r\n");
        List<IfcObj> rList = new List<IfcObj>();
        //IfcObj obj;
        foreach (string str in sarr)
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
        if (s.IsNull) return new IfcFile();
        else return new IfcFile(s.Value);
    }

    public IfcFile(string data)
    {
        string headpattern = @"HEADER;\r\n(?<data>(.*\r\n)*?)ENDSEC;";
        string datapattern = @"DATA;\r\n(?<data>(.*\r\n)*?)ENDSEC;";

        this.Head = IfcFile.ParseData(headpattern, data);
        this.Data = IfcFile.ParseData(datapattern, data);

        this.DataDict = new Dictionary<int, IfcObj>();

        foreach (IfcObj obj in this.Data)
        {
            this.DataDict.Add(obj.id, obj);
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
        File.WriteAllText(filename, this.ToString());
        return true;
    }

    public bool IsNull
    {
        get
        {
            return false;
        }
    }

    public static IfcFile Null
    {
        get
        {
            return new IfcFile();
        }
    }

    [SqlMethod(OnNullCall = false)]
    public IfcFile AddHeadItem(IfcObj nval)
    {
        this.Head.Add(nval);
        return this;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcFile DelHeadItem(int index)
    {
        if ((index >= 0) & (index < this.Head.Count)) this.Head.Remove(this.Head[index]);
        return this;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcObj GetHeadItem(int index)
    {
        if ((index >= 0) & (index < this.Head.Count)) return this.Head[index];
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
        if ((index >= 0) & (index < this.Data.Count)) this.Head[index] = nval;
        return this;
    }

    private int GetNextID()
    {
        return this.DataDict.Keys.Max();
    }

    private void _addDataItem(IfcObj nval)
    {
        if (nval.id > 0)
        {
            if (this.DataDict.ContainsKey(nval.id)) nval.id = this.GetNextID();
        }
        else
        {
            nval.id = this.GetNextID();
        }
        //
        this.Data.Add(nval);
        this.DataDict.Add(nval.id, nval);
    }

    [SqlMethod(OnNullCall = false)]
    public IfcFile AddDataItem(IfcObj nval)
    {
        //this.Data.Add(nval);
        this._addDataItem(nval);
        return this;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcFile AddDataItems(IfcValue nlist)
    {
        if (nlist.type == IfcValueType.LIST)
        {
            List<IfcValue> Values = nlist.value as List<IfcValue>;
            foreach (IfcValue v in Values)
            {
                if (v.type == IfcValueType.OBJ) this._addDataItem((IfcObj)v.value);
            }
        }
        return this;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcFile DelDataItem(int index)
    {
        if ((index >= 0) & (index < this.Data.Count)) this.Data.Remove(this.Data[index]);
        return this;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcObj GetDataItem(int index)
    {
        if ((index >= 0) & (index < this.Data.Count)) return this.Data[index];
        return IfcObj.Null;
    }

    [SqlMethod(OnNullCall = false)]
    public IfcObj GetDataItemByID(int tid)
    {
        if (this.DataDict.ContainsKey(tid)) return this.DataDict[tid];
        else return IfcObj.Null;
        //return this.DataDict[tid];
    }

    [SqlMethod(OnNullCall = false)]
    public IfcFile SetDataItem(int index, IfcObj nval)
    {
        if ((index >= 0) & (index < this.Data.Count)) this.Data[index] = nval;
        return this;
    }

    public List<IfcObj> _getLinks(int sid)
    {
        List<IfcObj> rList = new List<IfcObj>();
        List<IfcValue> refs;
        IfcObj sobj = this.GetDataItemByID(sid);
        if (!sobj.IsNull)
        {
            rList.Add(sobj);
            refs = sobj._getRefs();
            foreach (IfcValue ro in refs)
            {
                int nid = (int)ro.value;
                rList.AddRange(this._getLinks(nid));
            }
        }
        return rList;
    }

    public List<IfcObj> _getLinks2(int sid)
    {
        List<IfcObj> rList = new List<IfcObj>();
        //List<IfcValue> refs, nrefs = new List<IfcValue>();
        IfcObj sobj = this.GetDataItemByID(sid);
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
        return IfcValue.GetList(this._getLinks(sid));
    }

    public override string ToString()
    {
        return $"ISO-10303-21;\nHEADER;\n{string.Join("\n", this.Head)}\nENDSEC;\nDATA;\n{string.Join("\n", this.Data)}\nENDSEC;\nEND-ISO-10303-21;";
    }

    public void Read(BinaryReader r)
    {
        //throw new NotImplementedException();

        this.Head = new List<IfcObj>();
        this.Data = new List<IfcObj>();
        this.DataDict = new Dictionary<int, IfcObj>();

        IfcObj nobj;

        int wcount = (int)r.ReadUInt32();
        for (int i = 0; i < wcount; i++)
        {
            nobj = new IfcObj();
            nobj.Read(r);
            this.Head.Add(nobj);
        }
        //
        wcount = (int)r.ReadUInt32();
        if (wcount > 0)
        {
            for (int i = 0; i < wcount; i++)
            {
                nobj = new IfcObj();
                nobj.Read(r);
                this.Data.Add(nobj);
            }
        }
        
        foreach (IfcObj obj in this.Data)
        {
            this.DataDict.Add(obj.id, obj);
        }

    }

    public void Write(BinaryWriter w)
    {
        //throw new NotImplementedException();
        uint ui = (uint)this.Head.Count;
        w.Write(ui);
        for (int i = 0; i < this.Head.Count; i++)
        {
            this.Head[i].Write(w);
        }
        ui = (uint)this.Data.Count;
        w.Write(ui);
        for (int i = 0; i < this.Data.Count; i++)
        {
            this.Data[i].Write(w);
        }
    }
}
