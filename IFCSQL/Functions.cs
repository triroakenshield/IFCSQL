using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using System.Data.SqlTypes;
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

}

