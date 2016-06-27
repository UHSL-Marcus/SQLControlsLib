using SQLControlsLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {
        class Tester : DatabaseTableObject
        {
            [DatabaseID]
            public int? ID;
            public string Info;
        }
        static void Main(string[] args)
        {
            Settings.SetConnectionString(@"Data Source=ENT-ML15AAF-1\SQLEXPRESS;Initial Catalog=SmartSocketDB;Integrated Security=True;Pooling=False");

            Tester t = new Tester();
            t.Info = "new";

            int? id;
            if (!Set.doInsertReturnID(t, out id))
                Console.WriteLine("Insert failed");

            Tester gt = new Tester();
            gt.ID = id;

            List<Tester> list;
            if (!Get.doSelectByID(gt, out list) || list.Count < 1)
                Console.WriteLine("Get Failed (" + list.Count + ")");

            if (!list[0].Info.Equals(t.Info))
                Console.WriteLine("reteived data incorrect");

            gt.Info = "updated";
            if (!Update.doUpdateByID(gt))
                Console.WriteLine("Update failed");

            if (!Get.doSelectByID(gt, out list) || list.Count < 1)
                Console.WriteLine("Get Failed (" + list.Count + ")");


            Console.WriteLine("New info: " + list[0].Info + " Refrence: " + gt.Info);
        }
    }
}
