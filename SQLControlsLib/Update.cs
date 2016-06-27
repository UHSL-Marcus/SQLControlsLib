using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SQLControlsLib
{
    public class Update
    {
        internal static string getUpdateQuery(DatabaseTableObject ob, ref SqlCommand cmd, whereObject[] whereObs)
        {
            Type type = ob.GetType();

            string query = "UPDATE " + type.Name + " SET ";
            string where = SharedUtils.getWhere(whereObs, ref cmd, "UPD_");

            FieldInfo[] fields = type.GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                string fName = fields[i].Name;

                if (!fName.Equals(SharedUtils.getTypeIDColumn(ob.GetType())))
                {
                    var value = SharedUtils.formatValue(fields[i].GetValue(ob));

                    if (value != null || ob.getForceUse(fName))
                    {
                        SqlParameter tempParam = new SqlParameter();
                        tempParam.ParameterName = "@UPDSET_" + Regex.Replace(fName, "[^A-Za-z0-9 _]", "");

                        string newValue = tempParam.ParameterName;

                        if (value is string)
                            tempParam.Value = ((string)value).Trim();
                        else if (value == null)
                            newValue = "NULL";
                        else tempParam.Value = value;

                        cmd.Parameters.Add(tempParam);

                        query += type.Name + "." + fName + "=" + newValue + ",";
                    }
                }
                
            }

            return query.Remove(query.Length-1) + (whereObs.Length > 0 ? where : "");
        }

        public static bool doUpdateOrInsert<TYPE>(TYPE ob, string testColumn, object testInfo) where TYPE : DatabaseTableObject
        {
            return doUpdateOrInsert(ob, 
                new whereObject[] { new whereObject(SharedUtils.buildDatabaseObjectSingleField(typeof(TYPE).Name, testInfo, testColumn), SQLWhereConjuctions.AND, false) });
        }
        public static bool doUpdateOrInsert<TYPE>(TYPE ob, whereObject[] whereObs) where TYPE:DatabaseTableObject
        {
            Type type = typeof(TYPE);

            bool success = false;
            SqlCommand cmd = new SqlCommand();
            string query = string.Format(@" SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
                                            BEGIN TRANSACTION;
                                                {0};
                                                IF @@ROWCOUNT = 0
                                                BEGIN
                                                {1};
                                                END
                                            COMMIT TRANSACTION;", getUpdateQuery(ob, ref cmd, whereObs), Set.getInsertQuery(ob, ref cmd, "", ""));
            cmd.CommandText = query;
            success = SharedUtils.doNonQuery(cmd);

            return success;
        }

        public static bool doUpdate<TYPE>(TYPE ob, whereObject[] whereObs) where TYPE: DatabaseTableObject
        {
            Type type = typeof(TYPE);
            SqlCommand cmd = new SqlCommand();

            cmd.CommandText = getUpdateQuery(ob, ref cmd, whereObs);

            return SharedUtils.doNonQuery(cmd);
        }

        public static bool doUpdate<TYPE>(TYPE ob, string testColumn, string testInfo) where TYPE : DatabaseTableObject
        {
            return doUpdate(ob, new whereObject[] { new whereObject(SharedUtils.buildDatabaseObjectSingleField(typeof(TYPE).Name, testInfo, testColumn), SQLWhereConjuctions.AND, false) });
        }

        public static bool doUpdateByID<TYPE>(TYPE ob) where TYPE : DatabaseTableObject
        {
            string IDColumn = SharedUtils.getTypeIDColumn(typeof(TYPE));
            return doUpdate(ob, IDColumn, ob.getObjectFieldValue(IDColumn).ToString());
        }
    }
}
