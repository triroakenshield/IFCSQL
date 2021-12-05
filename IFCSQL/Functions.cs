using Microsoft.SqlServer.Server;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local

public class Functions
{
    private static void FillValues(object obj, out IfcObj theValue)
    {
        theValue = (IfcObj)obj;
    }

    private static void FillValues2(object obj, out IfcObj theValue)
    {
        var val = (IfcValue)obj;
        theValue = val.Type == IfcValueType.OBJ ? (IfcObj) val.Value : IfcObj.Null;
    }

    private static void FillValues3(object obj, out IfcValue theValue)
    {
        theValue = (IfcValue)obj;
    }

    [SqlFunction(FillRowMethodName = "FillValues", TableDefinition = "obj IfcObj")]
    public static IEnumerable GetHeadItems(IfcFile wFile)
    {
        return wFile.Head;
    }

    [SqlFunction(FillRowMethodName = "FillValues", TableDefinition = "obj IfcObj")]
    public static IEnumerable GetDataItems(IfcFile wFile)
    {
        return wFile.Data;
    }

    [SqlFunction(FillRowMethodName = "FillValues3", TableDefinition = "obj IfcValue")]
    public static IEnumerable ValuesListToTable(IfcValue wList)
    {
        return wList.Type == IfcValueType.LIST ? (List<IfcValue>) wList.Value : new List<IfcValue> {wList};
    }

    [SqlFunction(FillRowMethodName = "FillValues2", TableDefinition = "obj IfcObj")]
    public static IEnumerable ObjListToTable(IfcValue wList)
    {
        if (wList.Type == IfcValueType.LIST) return (List<IfcValue>)wList.Value;
        else return new List<IfcValue> { wList };
    }

    [SqlFunction]
    public static string NewGlobalId()
    {
        return GlobalId.Format(Guid.NewGuid());
    }

    private static void _calcRefs(IDictionary<int, List<int>> wDict, int wobjId, IEnumerable<IfcValue> refs)
    {
        foreach (var r in refs)
        {
            if (r.Type != IfcValueType.ENTITY_INSTANCE_NAME) continue;
            var refVal = (int)r.Value;
            if (!wDict.ContainsKey(refVal)) wDict.Add(refVal, new List<int>());
            wDict[refVal].Add(wobjId);
        }
    }

    private static void FillRefsValues1(object obj, out int oid, out IfcValue refsVal)
    {
        var var = (KeyValuePair<int, List<int>>)obj;
        var refs = var.Value.Select(rid => new IfcValue(IfcValueType.ENTITY_INSTANCE_NAME, rid)).ToList();

        oid = var.Key;
        refsVal = new IfcValue(IfcValueType.LIST, refs);
    }

    [SqlFunction(DataAccess = DataAccessKind.Read, FillRowMethodName = "FillRefsValues1", TableDefinition = "oid int; refs IfcValue")]
    public static IEnumerable CalcRefCount(string tableName, string fieldName)
    {
        using (var connection = new SqlConnection("context connection=true"))
        {
            var queryStr = $"select {fieldName} from {tableName}";
            IfcObj var;
            var wList = new List<IfcObj>();
            var wDict = new Dictionary<int, List<int>>();

            connection.Open();

            var command1 = new SqlCommand(queryStr) {Connection = connection};
            var reader = command1.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    var = reader.GetFieldValue<IfcObj>(0);
                    wList.Add(var);
                    if (!wDict.ContainsKey(var.Id)) wDict.Add(var.Id, new List<int>());
                    _calcRefs(wDict, var.Id, var._getRefs());
                }
            }

            reader.Close();
            return wDict;
        }
    }
}