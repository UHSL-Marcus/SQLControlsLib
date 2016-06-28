using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;

namespace SQLControlsLib
{
    public class Get
    {

        public static bool doSelectAll<TYPE>(out List<TYPE> output)
        {
            return doSelect(new whereObject[0], typeof(TYPE).Name, "*", out output);
        }
        public static bool doSelectByColumn<TYPE, inT>(inT info, string column, out List<TYPE> output)
        {
            return doSelect<TYPE>(SharedUtils.buildDatabaseObjectSingleField(typeof(TYPE).Name, info, column), "*", out output);
        }

        public static bool doSelectByID<TYPE>(TYPE ob, out List<TYPE> output) where TYPE:DatabaseTableObject
        {
            output = new List<TYPE>();

            string IDColumn = SharedUtils.getTypeIDColumn(typeof(TYPE));
            if (IDColumn.Length > 0)
                return doSelectByColumn(ob.getObjectFieldValue(IDColumn), IDColumn, out output);

            return false;
        }

        public static bool doSelectByID<TYPE, inT>(inT ID, out List<TYPE> output) where TYPE : DatabaseTableObject
        {
            output = new List<TYPE>();

            string IDColumn = SharedUtils.getTypeIDColumn(typeof(TYPE));
            if (IDColumn.Length > 0)
                return doSelectByColumn(ID, IDColumn, out output);

            return false;
        }

        public static bool doSelectSingleColumnByColumn<TYPE, outT, inT>(inT checkInfo, string inColumn, string outColumn, out outT output)
        {
            return doSelectSingleColumnByColumn(checkInfo, typeof(TYPE).Name, inColumn, outColumn, out output);
        }
        public static bool doSelectSingleColumnByColumn<outT, inT>(inT checkInfo, string table, string inColumn, string outColumn, out outT output)
        {
            return doSelectSingleColumn(SharedUtils.buildDatabaseObjectSingleField(table, checkInfo, inColumn), outColumn, out output, false);
        }

        public static bool doSelectIDByColumn<TYPE, inT, outT>(inT info, string column, out outT output)
        {
            output = default(outT);
            string idColumn = SharedUtils.getTypeIDColumn(typeof(TYPE));

            if (idColumn.Length > 0)
                return doSelectSingleColumnByColumn(info, typeof(TYPE).Name, column, SharedUtils.getTypeIDColumn(typeof(TYPE)), out output);

            return false;
        }

        public static bool getEntryExistsByColumn<TYPE, inT>(inT info, string column)
        {
            object i;
            return doSelectIDByColumn<TYPE, inT, object>(info, column, out i);
        }

        public static bool doSelectEntryExists(DatabaseTableObject ob, bool notModifer = false)
        {
            int? i;
            return doSelectID(ob, out i, notModifer);
        }
        public static bool doSelectID<outT>(DatabaseTableObject ob, out outT output, bool notModifer = false)
        {
            output = default(outT);
            string idColumn = SharedUtils.getTypeIDColumn(ob.GetType());

            if (idColumn.Length > 0)
                return doSelectSingleColumn(ob, idColumn, out output, notModifer);

            return false;
        }

        public static bool doSelectSingleColumn<outT>(DatabaseTableObject ob, string column, out outT output, bool notModifer = false)
        {
            SqlCommand cmd = new SqlCommand();
            string query = getSelectQuery(ob, ref cmd, column, "", notModifer);
            cmd.CommandText = query;
            return SharedUtils.getSingleEntry(cmd, column, out output);
        }

        public static bool doSelectAllSingleColumn<TYPE, outType>(DatabaseTableObject ob, string selArg, string columnName, out List<outType> output, bool notModifer = false)
        {
            SqlCommand cmd = new SqlCommand();
            string query = getSelectQuery(ob, ref cmd, columnName, "", notModifer);
            cmd.CommandText = query;
            output = SharedUtils.getData<outType>(cmd, columnName);
            return (output.Count > 0);

        }

        public static bool doSelect<TYPE>(DatabaseTableObject where, string selArg, out List<TYPE> output, bool notModifer = false)
        {
            return doSelect(new whereObject[] { new whereObject(where, SQLWhereConjuctions.AND, notModifer) }, typeof(TYPE).Name, selArg, out output);
        }

        public static bool doSelect<TYPE>(whereObject[] whereobs, string table, string selArg, out List<TYPE> output)
        {
            SqlCommand cmd = new SqlCommand();
            string query = getSelectQuery(whereobs, table, ref cmd, selArg, "");
            cmd.CommandText = query;
            output = SharedUtils.getData<TYPE>(cmd);
            return (output.Count > 0);
        }
        public static bool doSelect<TYPE>(string selArg, out List<TYPE> output)
        {
            return doSelect(new whereObject[0], typeof(TYPE).Name, selArg, out output);
        }
        public static bool doSelect<TYPE>(string selArg, Dictionary<string, object> values, out List<TYPE> output)
        {
            Type type = typeof(TYPE);
            return doSelect<TYPE>(SharedUtils.buildDatabaseObject(type.Name, values), selArg, out output);
        }

        
        public static bool doJoinSelect<TYPE>(TYPE outputOb, out List<TYPE> output) where TYPE : DatabaseOutputObject
        {
            SqlCommand cmd = new SqlCommand();
            string query = getJoinSelectQuery(ref cmd, getSelArgument<TYPE>(), outputOb.whereobs.ToArray(), outputOb.joins.ToArray());
            cmd.CommandText = query;
            output = SharedUtils.getData<TYPE>(cmd);
            return (output.Count > 0);
        }

        public static bool doJoinSelect(string selArg, out List<Dictionary<string, object>> output, JoinPair[] joins)
        {
            return doJoinSelect(new whereObject[0], selArg, out output, joins);
        }

        public static bool doJoinSelect(DatabaseTableObject where, string selArg, out List<Dictionary<string, object>> output, JoinPair[] joins, bool notModifer = false)
        {
            return doJoinSelect(new whereObject[] { new whereObject(where, SQLWhereConjuctions.AND, notModifer) }, selArg, out output, joins);
        }
        public static bool doJoinSelect(whereObject[] whereobs, string selArg, out List<Dictionary<string, object>> output, JoinPair[] joins)
        {
            SqlCommand cmd = new SqlCommand();
            string query = getJoinSelectQuery(ref cmd, selArg, whereobs, joins);
            cmd.CommandText = query;
            output = SharedUtils.getData(cmd);
            return (output.Count > 0);
        }

        internal static string getSelArgument<TYPE>() where TYPE:DatabaseOutputObject
        {
            Type type = typeof(TYPE);
            string selectArgs = "";

            FieldInfo[] fields = type.GetFields();
            if (fields.Length > 0)
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    string columnName = "";

                    DatabaseColumnAttribute[] attrs = fields[i].GetCustomAttributes(typeof(DatabaseColumnAttribute), false) as DatabaseColumnAttribute[];
                    if (attrs.Length > 0)
                    {
                        if (!attrs[0].SQLIgnore)
                        {
                            columnName = fields[i].Name;
                            if (attrs[0].columnName != null)
                            {
                                columnName = attrs[0].columnName + " AS " + columnName;
                            }
                            
                            columnName += ",";
                        }
                    }

                    selectArgs += columnName;

                }
            }

            return selectArgs.Remove(selectArgs.Length-1);
        }

        private delegate bool CanAddJoin(JoinPair join); 
        internal static string getJoinSelectQuery(ref SqlCommand cmd, string selectArg, whereObject[] whereObs, JoinPair[] joins)
        {
            List<JoinPair> done = new List<JoinPair>();

            string joinString = buildJoin(joins[0]);
            done.Add(joins[0]);

            CanAddJoin canAddJoin = null;
            canAddJoin = delegate (JoinPair join)
            {
                bool canAdd = false;
                

                if (done.Contains(join)) return true;

                foreach (JoinPair doneJoin in done)
                {
                    if ((join.leftTable.Equals(doneJoin.leftTable) || join.leftTable.Equals(doneJoin.rightTable)) &&
                        (!join.rightTable.Equals(doneJoin.leftTable) && !join.rightTable.Equals(doneJoin.rightTable)))
                    {
                        canAdd = true;
                        break;
                    }
                    else
                    {
                        foreach (JoinPair todoJoin in joins)
                        {
                            if (join.leftTable.Equals(todoJoin.rightTable))
                            {
                                canAdd = canAddJoin(todoJoin);
                                break;
                            }
                        }
                    }
                }

                if (canAdd)
                {
                    joinString += buildJoin(join);
                    done.Add(joins[0]);
                }

                return canAdd;
            };

            for (int i = 1; i < joins.Length; i++)
            {
                canAddJoin(joins[i]);
            }

            return getSelectQuery(whereObs, joins[0].leftTable, ref cmd, selectArg, joinString);
        }

        private static string buildJoin(JoinPair join)
        {
            string joinString = join.joinType.GetStringValue() + " " + join.rightTable + " ON ";
            JoinOnPair[] ons = join.ons;
            for(int i = 0; i < ons.Length; i++)
            {
                if (i != 0)
                    joinString += ons[i].conjunc.GetStringValue();

                joinString += join.leftTable + "." + ons[i].leftTableCol + ons[i].op.GetStringValue() + join.rightTable + "." + ons[i].rightTableCol + " ";
            }
            return joinString;
        }


        internal static string getSelectQuery(whereObject[] obs, string table, ref SqlCommand cmd, string selectArg, string preWhereExtra)
        {
            return "SELECT " + selectArg + " FROM " + table + " " + preWhereExtra + SharedUtils.getWhere(obs, ref cmd, "SEL_"); 
        }

        


        internal static string getSelectQuery(DatabaseTableObject ob, ref SqlCommand cmd, string selectArg, string preWhereExtra, bool notModifer)
        {
            return getSelectQuery(new whereObject[] { new whereObject(ob, SQLWhereConjuctions.AND, notModifer) }, ob.GetType().Name, ref cmd, selectArg, preWhereExtra);
        }
 
    }
}
