using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace SQLControlsLib
{
    
    public enum SQLWhereConjuctions
    {
        [StringValue("AND")]
        AND,
        [StringValue("OR")]
        OR,
        [StringValue("NOT")]
        NOT
    }
    public enum SQLEqualityOperations
    {
        [StringValue("=")]
        EQUALS,
        [StringValue("<>")]
        NOTEQUALS
    }

    internal enum SQLNullEqualityOperations
    {
        [StringValue(" IS NULL")]
        ISNULL,
        [StringValue(" IS NOT NULL")]
        NOTNULL
    }

    public enum JoinTypes
    {
        [StringValue("JOIN")]
        JOIN,
        [StringValue("FULL JOIN")]
        FULLJOIN
    }

    public struct whereObject
    {
        public DatabaseTableObject databaseObject;
        public SQLWhereConjuctions conjunction;
        public bool notModifier;

        public whereObject(DatabaseTableObject databaseObject, SQLWhereConjuctions conjunction, bool not)
        {
            this.databaseObject = databaseObject;
            this.conjunction = conjunction;
            notModifier = not;
        }

        public whereObject(DatabaseTableObject databaseObject)
        {
            this.databaseObject = databaseObject;
            conjunction = SQLWhereConjuctions.AND;
            notModifier = false;
        }

        public whereObject(DatabaseTableObject databaseObject, SQLWhereConjuctions conjunction)
        {
            this.databaseObject = databaseObject;
            this.conjunction = conjunction;
            notModifier = false;
        }

    }

    
    public class DatabaseTableObject
    {
        public int? Id;

        [DatabaseColumn(SQLIgnore = true)]
        private Dictionary<string, bool> _forceUseFields;
        protected Dictionary<string, bool> forceUseFields
        {
            get
            {
                if (_forceUseFields == null) _forceUseFields = new Dictionary<string, bool>();
                return _forceUseFields;
            }
        }

        [DatabaseColumn(SQLIgnore = true)]
        private Dictionary<string, SQLEqualityOperations> _overwriteEquality;
        protected Dictionary<string, SQLEqualityOperations> overwriteEquality
        {
            get
            {
                if (_overwriteEquality == null) _overwriteEquality = new Dictionary<string, SQLEqualityOperations>();
                return _overwriteEquality;
            }
        }

        [DatabaseColumn(SQLIgnore = true)]
        protected SQLEqualityOperations DefaultEquality = SQLEqualityOperations.EQUALS;
        [DatabaseColumn(SQLIgnore = true)]
        protected bool DefaultForceUseFields = false;

        public void setFieldOptions(string fieldName, bool force)
        {
            forceUseFields[fieldName] = force;
        }
        public void setFieldOptions(string fieldName, SQLEqualityOperations equality)
        {
            overwriteEquality[fieldName] = equality;
        }

        public void setFieldOptions(string fieldName, SQLEqualityOperations equality, bool force)
        {
            setFieldOptions(fieldName, equality);
            setFieldOptions(fieldName, force);
        }

        public bool getForceUse(string field)
        {
            bool ret = DefaultForceUseFields;
            if (forceUseFields.ContainsKey(field))
                ret = forceUseFields[field];

            return ret;
        }

        public SQLEqualityOperations getEquality(string field)
        {
            SQLEqualityOperations ret = DefaultEquality;
            if (overwriteEquality.ContainsKey(field))
                ret = overwriteEquality[field];

            return ret;
        }
    }

    public struct JoinPair
    {
        public JoinTypes joinType;
        public string leftTable;
        public string rightTable;
        public JoinOnPair[] ons;
        public JoinPair(string leftTable, string rightTable, JoinOnPair[] ons, JoinTypes joinType = JoinTypes.JOIN)
        {
            this.leftTable = leftTable;
            this.rightTable = rightTable;
            this.ons = ons;
            this.joinType = joinType;
        }
    }

    public struct JoinOnPair
    {
        public string leftTableCol;
        public string rightTableCol;
        public SQLEqualityOperations op;
        public SQLWhereConjuctions conjunc;
        public JoinOnPair(string leftTableColumn, string rightTableColumn, SQLEqualityOperations op = SQLEqualityOperations.EQUALS, SQLWhereConjuctions conjuction = SQLWhereConjuctions.AND)
        {
            leftTableCol = leftTableColumn;
            rightTableCol = rightTableColumn;
            this.op = op;
            conjunc = conjuction;
        }
    }

    public abstract class DatabaseOutputObject
    {
        public List<JoinPair> joins = new List<JoinPair>();
        public List<whereObject> whereobs = new List<whereObject>();

        protected void buildSingleJoin(string left, string right, string leftcol, string rightcol)
        {
            JoinOnPair[] ons = new JoinOnPair[] { new JoinOnPair(leftcol, rightcol) };
            joins.Add(new JoinPair(left, right, ons));
        }
        protected void buildSingleWhere(string table, string column, object info)
        {
            whereobs.Add(new whereObject(SharedUtils.buildDatabaseObjectSingleField(table, info, column), SQLWhereConjuctions.AND, false));
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class DatabaseColumnAttribute : Attribute
    {
        public bool SQLIgnore = false;
        public string columnName;
    }
    

    internal class SharedUtils
    {
        public static bool doNonQuery(string sql)
        {
            SqlCommand cmd = new SqlCommand(sql);
            return doNonQuery(cmd);

        }

        public static bool doNonQuery(SqlCommand cmd)
        {
            bool success = false;

            using (SqlConnection conn = new SqlConnection(Settings.ConnectionString))
            {
                conn.Open();
                cmd.Connection = conn;
                try {
                    if (cmd.ExecuteNonQuery() != 0)
                        success = true;
                }catch (Exception e)
                {
                    throw new Exception("SQLQuery: '" + cmd.CommandText + "'", e);
                }
            }

            return success;
        }

        internal static bool getSingleEntry<T>(SqlCommand cmd, string columnName, out T output)
        {
            bool success = false;
            output = default(T);

            List<T> entries = getData<T>(cmd, columnName);
            if (entries.Count > 0)
            {
                output = entries[0];
                success = true;
            }

            return success;
        }

        internal static bool getSingleEntry<T>(string sql, string columnName, out T output)
        {
            return getSingleEntry(new SqlCommand(sql), columnName, out output);
        }

        internal static List<TYPE> getData<TYPE>(SqlCommand cmd, string column)
        {
            DataTableReader reader = SharedUtils.getDataReader(cmd);

            List<TYPE> returnList = new List<TYPE>();

            while (reader.Read())
            {

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (reader.GetName(i).Equals(column))
                    {
                        var entry = reader[i];
                        if (entry is TYPE)
                            returnList.Add((TYPE)reader[i]);
                    }
                }
            }

            return returnList;
        }


        internal static List<Dictionary<string, object>> getData(SqlCommand cmd)
        {
            DataTableReader reader = getDataReader(cmd);

            List<Dictionary<string, object>> returnList = new List<Dictionary<string, object>>();
            while (reader.Read())
            {
                Dictionary<string, object> row = new Dictionary<string, object>();

                for (int i = 0; i < reader.FieldCount; i++)
                    row.Add(reader.GetName(i), reader[i]);
            }

            return returnList;
        }

        internal static List<TYPE> getData<TYPE>(string sql)
        {

            return getData<TYPE>(new SqlCommand(sql));
        }


        internal static List<TYPE> getData<TYPE>(SqlCommand cmd)
        {
            DataTableReader reader = getDataReader(cmd);

            List<TYPE> returnList = new List<TYPE>();

            while (reader.Read())
            {

                TYPE ob = (TYPE)Activator.CreateInstance(typeof(TYPE));

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string s = reader.GetName(i);
                    FieldInfo field = ob.GetType().GetField(reader.GetName(i));
                    if (field.FieldType == typeof(string[]))
                        field.SetValue(ob, ((string)reader[i]).Split(','));
                    else
                        field.SetValue(ob, reader[i]);
                }

                returnList.Add(ob);
            }

            return returnList;
        }


        internal static DataTableReader getDataReader(SqlCommand cmd)
        {
            DataSet dataSet = new DataSet();
            SqlDataAdapter adapter = new SqlDataAdapter();

            using (SqlConnection conn = new SqlConnection(Settings.ConnectionString))
            {
                cmd.Connection = conn;
                adapter.SelectCommand = cmd;
                conn.Open();
                adapter.Fill(dataSet);

            }

            return dataSet.CreateDataReader();
        }

        internal static DataTableReader getDataReader(string sql)
        {
            return getDataReader(new SqlCommand(sql));
        }



        internal static object formatValue(object value)
        {
            if (value is DateTime)
                return ((DateTime)value).ToString("yyyy/MM/dd HH:mm:ss");
            else if (value is int || value is string)
                return value;
            else if (value is string[])
                return string.Join(",", value as string[]);

            return null;
        }

        internal static dynamic buildDatabaseObject(string table, Dictionary<string, object> values)
        {
            List<string> defaultFields = new List<string>();
            foreach (FieldInfo field in typeof(DatabaseTableObject).GetFields())
                if (!sqlIgnore(field)) defaultFields.Add(field.Name);
            
            List<DatabaseTypeBuilder.Field> newFields = new List<DatabaseTypeBuilder.Field>();
            for (int i = 0; i < values.Keys.Count; i++)
            {
                string key = values.Keys.ElementAt(i);
                if (!defaultFields.Contains(key))
                {
                    Type type = values[key].GetType();
                    if (type == typeof(int)) type = typeof(int?); // to allow nulls
                    newFields.Add(new DatabaseTypeBuilder.Field(key, values[key].GetType()));
                }
            }

            Type tempType = DatabaseTypeBuilder.GetType(table, newFields.ToArray());

            dynamic ob = Convert.ChangeType(Activator.CreateInstance(tempType), tempType);

            foreach (FieldInfo field in tempType.GetFields())
            {
                object value;
                values.TryGetValue(field.Name, out value);
                field.SetValue(ob, value);
            }

            return ob;
        }

        internal static dynamic buildDatabaseObjectSingleField(string table, object info, string column)
        {
            Dictionary<string, object> conditions = new Dictionary<string, object>();
            conditions.Add(column, info);
            return SharedUtils.buildDatabaseObject(table, conditions);
        }

        internal static dynamic buildDatabaseObjectNoFields(string table)
        {
            Dictionary<string, object> conditions = new Dictionary<string, object>();
            return SharedUtils.buildDatabaseObject(table, conditions);
        }

        internal static string getWhere(whereObject[] whereObs, ref SqlCommand cmd, string paramPrefix)
        {
            string query = "";
            

            for (int t = 0; t < whereObs.Length; t++)
            {
                whereObject where = whereObs[t];
                DatabaseTableObject dbOb = where.databaseObject;

                Type dbObType = dbOb.GetType();

                FieldInfo[] fields = dbObType.GetFields();
                if (fields.Length > 0)
                {
                    if (t > 0)
                        query += " " + where.conjunction.GetStringValue();

                    if (where.notModifier)
                        query += " NOT";

                    query += " (";
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (!sqlIgnore(fields[i]))
                        {
                            string fName = fields[i].Name;

                            var value = formatValue(fields[i].GetValue(dbOb));

                            if (value != null || dbOb.getForceUse(fName))
                            {
                                string paramName = "";
                                string equality = dbOb.getEquality(fName).GetStringValue();

                                if (value == null)
                                {
                                    switch (dbOb.getEquality(fName))
                                    {
                                        case SQLEqualityOperations.EQUALS:
                                            equality = SQLNullEqualityOperations.ISNULL.GetStringValue();
                                            break;
                                        case SQLEqualityOperations.NOTEQUALS:
                                            equality = SQLNullEqualityOperations.NOTNULL.GetStringValue();
                                            break;
                                    }
                                }
                                else
                                {
                                    SqlParameter tempParam = new SqlParameter();
                                    tempParam.ParameterName = "@" + paramPrefix + i + Regex.Replace(fName, "[^A-Za-z0-9 _]", "");

                                    if (value is string)
                                        tempParam.Value = ((string)value).Trim();
                                    else tempParam.Value = value;

                                    cmd.Parameters.Add(tempParam);

                                    paramName = tempParam.ParameterName;
                                }

                                query += dbObType.Name + "." + fName + equality + paramName + " AND ";
                            }
                        }
                    }

                    query = query.Remove(query.LastIndexOf(" AND ")) + ")";  
                }
            }

            return (whereObs.Length > 0 ? " WHERE" : "") + query;
        }

        private static bool sqlIgnore(FieldInfo field)
        {
            bool ignore = false;
            DatabaseColumnAttribute[] attrs = field.GetCustomAttributes(typeof(DatabaseColumnAttribute), false) as DatabaseColumnAttribute[];
            if (attrs.Length > 0)
                ignore = attrs[0].SQLIgnore;

            return ignore;
        }

    }
}
