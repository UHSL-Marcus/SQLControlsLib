using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace SQLControlsLib
{
    public class Delete
    {
        /*public static bool doDeleteByIDGetID(string id, string table, out int? output)
        {
            return doDeleteByGetID(SharedUtils.buildDatabaseObjectSingleField(table, id, "Id"), out output);
        }*/

        /*public static bool doDeleteByColumnGetID(string table, object info, string column, out int? output)
        {
            return doDeleteByGetID(SharedUtils.buildDatabaseObjectSingleField(table, info, column), out output);
        }*/

        /*public static bool doDeleteByGetID<TYPE>(TYPE ob, out int? output, bool includeNulls = false)
        {
            output = null;

            string declaration = "DECLARE @outputTable table(Id int NOT NULL) ";
            string extra = "OUTPUT INSERTED.Id INTO @outputTable ";
            string select = ";SELECT Id FROM @outputTable; ";

            SqlCommand cmd = new SqlCommand();
            string query = getDeleteQuery(ob, ref cmd, extra, includeNulls);
            cmd.CommandText = declaration + query + select;

            return SharedUtils.getSingleEntry(cmd, "Id", out output);
        }*/

        public static bool doDeleteEntryByColumn<TYPE, inT>(inT info, string column, bool not = false)
        {
            Type type = typeof(TYPE);
            SharedUtils.buildDatabaseObjectSingleField(type.Name, info, column);

            return doDelete(SharedUtils.buildDatabaseObjectSingleField(type.Name, info, column), not);
        }

        public static bool doDeleteEntryByID<TYPE, inT>(inT id, bool not = false)
        {
            string idColumn = SharedUtils.getTypeIDColumn(typeof(TYPE));

            if (idColumn.Length > 0)
                return doDeleteEntryByColumn<TYPE, inT>(id, idColumn, not);

            return false;
        }

        public static bool doDeleteEntryByColumn<inT>(string table, inT info, string column, bool not = false)
        {
            return doDelete(SharedUtils.buildDatabaseObjectSingleField(table, info, column), not);
        }

        internal static string getDeleteQuery(whereObject[] obs, string table, ref SqlCommand cmd, string preWhereExtra)
        {
            return "DELETE FROM " + table + " " + preWhereExtra + SharedUtils.getWhere(obs, ref cmd, "DEL_");
        }

        public static bool doDelete<TYPE>(TYPE ob, bool notModifer = false) where TYPE:DatabaseTableObject
        {
            SqlCommand cmd = new SqlCommand();
            string query = getDeleteQuery(new whereObject[] { new whereObject(ob, SQLWhereConjuctions.AND, notModifer) }, typeof(TYPE).Name, ref cmd, ""); 
            cmd.CommandText = query;
            return SharedUtils.doNonQuery(cmd);
        }

        public static bool doDelete(string table, Dictionary<string, object> values)
        {
            return doDelete(SharedUtils.buildDatabaseObject(table, values));
        }
    }
}
