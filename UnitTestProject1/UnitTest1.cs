using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestGetValueType()
        {
            string tstr = "123";
            IfcValueType res = IfcValue.GetType(tstr);
            Assert.IsTrue(res == IfcValueType.INTEGER, "bad_int1");
            tstr = "-123";
            res = IfcValue.GetType(tstr);
            Assert.IsTrue(res == IfcValueType.INTEGER, "bad_int2");
            tstr = "+12333333";
            res = IfcValue.GetType(tstr);
            Assert.IsTrue(res == IfcValueType.INTEGER, "bad_int3");
            tstr = "123.123";
            res = IfcValue.GetType(tstr);
            Assert.IsTrue(res == IfcValueType.REAL, "bad_real1");
            tstr = "-123.123";
            res = IfcValue.GetType(tstr);
            Assert.IsTrue(res == IfcValueType.REAL, "bad_real2");
            tstr = ".123";
            res = IfcValue.GetType(tstr);
            Assert.IsTrue(res == IfcValueType.REAL, "bad_real3");
            tstr = "-.123";
            res = IfcValue.GetType(tstr);
            Assert.IsTrue(res == IfcValueType.REAL, "bad_real4");
            tstr = "1.";
            res = IfcValue.GetType(tstr);
            Assert.IsTrue(res == IfcValueType.REAL, "bad_real5");
            tstr = "'Autodesk \\\'Revit, \\\\2019 (RUS)'";
            res = IfcValue.GetType(tstr);
            Assert.IsTrue(res == IfcValueType.STRING, "bad_str1");
            tstr = "#123456";
            res = IfcValue.GetType(tstr);
            Assert.IsTrue(res == IfcValueType.ENTITY_INSTANCE_NAME, "bad_ent1");
            tstr = ".ELEMENT.";
            res = IfcValue.GetType(tstr);
            Assert.IsTrue(res == IfcValueType.ENUMERATION, "bad_enum1");
            tstr = "$";
            res = IfcValue.GetType(tstr);
            Assert.IsTrue(res == IfcValueType.NULL, "bad_null1");
            tstr = "*";
            res = IfcValue.GetType(tstr);
            Assert.IsTrue(res == IfcValueType.DERIVE, "bad_der1");
            tstr = "(1.,0.,0.)";
            res = IfcValue.GetType(tstr);
            Assert.IsTrue(res == IfcValueType.LIST, "bad_list1");
            tstr = "IFCBOOLEAN(.T.)";
            res = IfcValue.GetType(tstr);
            var tobj = IfcObj._parse(tstr);
            Assert.IsTrue(res == IfcValueType.OBJ, "bad_obj1");
        }
    }
}
