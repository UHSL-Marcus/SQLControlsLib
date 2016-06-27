using Microsoft.VisualStudio.TestTools.UnitTesting;
using SQLControlsLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLControlsLib.Tests
{
    [TestClass()]
    public class UpdateTests
    {
        class Tester : DatabaseTableObject
        {
            [DatabaseID]
            public int? ID;
            public string Info;
        }
        [TestMethod()]
        public void doUpdateByIDTest()
        {

            Settings.SetConnectionString(@"Data Source=ENT-ML15AAF-1\SQLEXPRESS;Initial Catalog=SmartSocketDB;Integrated Security=True;Pooling=False");

            Tester t = new Tester();
            t.Info = "new";

            int? id;
            if (!Set.doInsertReturnID(t, out id))
                Assert.Fail("Insert failed");

            Tester gt = new Tester();
            gt.ID = id;

            List<Tester> list;
            if (!Get.doSelectByID(gt, out list) || list.Count < 1)
                Assert.Fail("Get Failed (" + list.Count + ")");

            if (!list[0].Info.Equals(t.Info))
                Assert.Fail("reteived data incorrect");

            gt.Info = "updated";
            if (!Update.doUpdateByID(gt))
                Assert.Fail("Update failed");

            if (!Get.doSelectByID(gt, out list) || list.Count < 1)
                Assert.Fail("Get Failed (" + list.Count + ")");

 
            Assert.IsTrue(list[0].Info.Equals(gt.Info));
            
        }
    }
}