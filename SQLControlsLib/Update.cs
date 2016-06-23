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
        internal static string getUpdateQuery(DatabaseTableObject ob, ref SqlCommand cmd, whereObject[] whereObs, bool includeNulls)
        {
            Type type = ob.GetType();

            string query = "UPDATE " + type.Name + " SET ";
            string where = SharedUtils.getWhere(whereObs, ref cmd, "UPD_");

            FieldInfo[] fields = type.GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                string fName = fields[i].Name;

                if (!fName.Equals("Id"))
                {
                    var value = SharedUtils.formatValue(fields[i].GetValue(ob));

                    if (value != null || includeNulls)
                    {

                        SqlParameter tempParam = new SqlParameter();
                        tempParam.ParameterName = "@UPDSET_" + Regex.Replace(fName, "[^A-Za-z0-9 _]", "");

                        if (value is string)
                            tempParam.Value = ((string)value).Trim();
                        else tempParam.Value = value;

                        cmd.Parameters.Add(tempParam);

                        query += type.Name + "." + fName + "=" + tempParam.ParameterName + ",";
                    }
                }
                
            }

            return query.Remove(query.Length-1) + (whereObs.Length > 0 ? where : "");
        }

        public static bool doUpdateOrInsert<TYPE>(TYPE ob, string testColumn, object testInfo, bool includeNulls = false) where TYPE : DatabaseTableObject
        {
            return doUpdateOrInsert(ob, 
                new whereObject[] { new whereObject(SharedUtils.buildDatabaseObjectSingleField(typeof(TYPE).Name, testInfo, testColumn), SQLWhereConjuctions.AND, false) }, 
                includeNulls);
        }
        public static bool doUpdateOrInsert<TYPE>(TYPE ob, whereObject[] whereObs, bool includeNulls = false) where TYPE:DatabaseTableObject
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
                                            COMMIT TRANSACTION;", getUpdateQuery(ob, ref cmd, whereObs, includeNulls), Set.getInsertQuery(ob, ref cmd, "", "", includeNulls));
            cmd.CommandText = query;
            success = SharedUtils.doNonQuery(cmd);

            return success;
        }
    }
}
