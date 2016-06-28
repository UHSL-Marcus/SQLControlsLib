using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SQLControlsLib
{
    public class Set
    {
        public static bool doInsert(string table, Dictionary<string, object> values)
        {
            DatabaseTableObject ob = (DatabaseTableObject)SharedUtils.buildDatabaseObject(table, values);
            ob.setForceUseAll();
            return doInsert(ob);
        }
        public static bool doInsert<TYPE>(TYPE ob) where TYPE:DatabaseTableObject
        {
            SqlCommand cmd = new SqlCommand();
            string query = getInsertQuery(ob, ref cmd, "", "");
            cmd.CommandText = query;
            return SharedUtils.doNonQuery(cmd);
        }

        public static bool doInsertReturnID<TYPE>(TYPE ob, out int? output) where TYPE:DatabaseTableObject
        {
            string IDColumn = SharedUtils.getTypeIDColumn(typeof(TYPE));
            output = null;
            Type type = typeof(TYPE);

            string declaration = "DECLARE @outputTable table( " + IDColumn + " int NOT NULL) ";
            string outputExtra = " OUTPUT INSERTED."+ IDColumn +" INTO @outputTable";
            string select = "; SELECT "+ IDColumn +" FROM @outputTable;";

            SqlCommand cmd = new SqlCommand();
            string query = getInsertQuery(ob, ref cmd, outputExtra, select);
            cmd.CommandText = declaration + query;


            return SharedUtils.getSingleEntry(cmd, IDColumn, out output);

        }

        internal static string getInsertQuery<TYPE>(TYPE ob, ref SqlCommand cmd, string queryNameExtra, string queryValuesExtra) where TYPE: DatabaseTableObject
        {
            Type type = typeof(TYPE);
            string queryName = "INSERT INTO " + type.Name + " (";
            string queryValues = "";

            FieldInfo[] fields = type.GetFields();
            for (int i = 0; i < fields.Length; i++)
            {

                if (!fields[i].Name.Equals(SharedUtils.getTypeIDColumn(typeof(TYPE))))
                {
                    var value = SharedUtils.formatValue(fields[i].GetValue(ob));

                    if (value != null || ob.getForceUse(fields[i].Name))
                    {

                        if (queryValues.Length < 1)
                            queryValues += " VALUES(";
                        

                        queryName += type.Name + "." + fields[i].Name + ",";

                        SqlParameter tempParam = new SqlParameter();
                        tempParam.ParameterName = "@INS_" + Regex.Replace(fields[i].Name, "[^A-Za-z0-9 _]", "");

                        string newValue = tempParam.ParameterName;

                        if (value is string)
                            tempParam.Value = ((string)value).Trim();
                        else if (value == null)
                            newValue = "NULL";
                        else tempParam.Value = value;

                        cmd.Parameters.Add(tempParam);

                        queryValues += newValue + ",";
                    }
                }
            }

            queryName = queryName.Remove(queryName.Length - 1) + ")";

            if (queryValues.Length > 0)
                queryValues = queryValues.Remove(queryValues.Length-1) + ")";
            else queryValues = " DEFAULT VALUES";

            return queryName + queryNameExtra + queryValues + queryValuesExtra;
        }

        
    }
}
