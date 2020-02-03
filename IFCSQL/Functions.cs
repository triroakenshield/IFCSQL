using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using System.Data.SqlTypes;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;

public class Functions
{

    private static void FillValues(object obj, out IfcObj TheValue)
    {
        TheValue = (IfcObj)obj;
    }

    private static void FillValues2(object obj, out IfcObj TheValue)
    {
        IfcValue val = (IfcValue)obj;
        if (val.type == IfcValueType.OBJ) TheValue = (IfcObj)val.value;
        else TheValue = IfcObj.Null;
    }

    private static void FillValues3(object obj, out IfcValue TheValue)
    {
        TheValue = (IfcValue)obj;
    }

    [SqlFunction(FillRowMethodName = "FillValues", TableDefinition = "obj IfcObj")]
    public static IEnumerable GetHeadItems(IfcFile wfile)
    {
        return wfile.Head;
    }

    [SqlFunction(FillRowMethodName = "FillValues", TableDefinition = "obj IfcObj")]
    public static IEnumerable GetDataItems(IfcFile wfile)
    {
        return wfile.Data;
    }

    [SqlFunction(FillRowMethodName = "FillValues3", TableDefinition = "obj IfcValue")]
    public static IEnumerable ValuesListToTable(IfcValue wlist)
    {
        if (wlist.type == IfcValueType.LIST) return (List<IfcValue>)wlist.value;
        else {
            List<IfcValue> rList = new List<IfcValue>();
            rList.Add(wlist);
            return rList;
        }
    }

    [SqlFunction(FillRowMethodName = "FillValues2", TableDefinition = "obj IfcObj")]
    public static IEnumerable ObjListToTable(IfcValue wlist)
    {
        if (wlist.type == IfcValueType.LIST) return (List<IfcValue>)wlist.value;
        else
        {
            List<IfcValue> rList = new List<IfcValue>();
            rList.Add(wlist);
            return rList;
        }
    }

    [SqlFunction()]
    public static String NewGlobalId()
    {
        return GlobalId.Format(Guid.NewGuid());
    }

    private static void _calcRefs(Dictionary<int, List<int>> wDict, int wobj_id, List<IfcValue> refs)
    {
        int ref_val;
        foreach (IfcValue r in refs)
        {
            if (r.type == IfcValueType.ENTITY_INSTANCE_NAME)
            {
                ref_val = (int)r.value;
                if (!wDict.ContainsKey(ref_val)) wDict.Add(ref_val, new List<int>());
                wDict[ref_val].Add(wobj_id);
            }
        }
    }

    private static void FillRefsValues1(object obj, out int oid, out IfcValue refsval)
    {
        //TheValue = (IfcValue)obj;
        KeyValuePair<int, List<int>> var = (KeyValuePair<int, List<int>>)obj;
        List<IfcValue> refs = new List<IfcValue>();

        foreach (int rid in var.Value)
        {
            refs.Add(new IfcValue(IfcValueType.ENTITY_INSTANCE_NAME, rid));
        }

        oid = var.Key;
        refsval = new IfcValue(IfcValueType.LIST, refs);

    }

    [SqlFunction(DataAccess = DataAccessKind.Read, FillRowMethodName = "FillRefsValues1", TableDefinition = "oid int; refs IfcValue")]
    public static IEnumerable CalcRefCount(string tablename, string fieldname)
    {
        using (SqlConnection connection = new SqlConnection("context connection=true"))
        {
            string QueryStr = $"select {fieldname} from {tablename}";
            IfcObj var;
            List<IfcObj> wList = new List<IfcObj>();
            Dictionary<int, List<int>> wDict = new Dictionary<int, List<int>>();

            connection.Open();

            SqlCommand command1 = new SqlCommand(QueryStr);
            command1.Connection = connection;
            SqlDataReader reader = command1.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    var = reader.GetFieldValue<IfcObj>(0);
                    wList.Add(var);
                    if (!wDict.ContainsKey(var.id)) wDict.Add(var.id, new List<int>());
                    _calcRefs(wDict, var.id, var._getRefs());
                }
            }

            reader.Close();

/*            List<Object> rList = new List<object>();

            foreach (IfcObj o in wList)
            {
                rList.Add(new { obj = o, refs = wDict[o.id] });
            }*/

            return wDict;
        }
    }

}

