using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SQLControlsLib
{

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

            List<T> entries;
            if (getData(cmd, columnName, out entries))
            {
                if (entries.Count > 0)
                {
                    output = entries[0];
                    success = true;
                }
            }

            return success;
        }

        internal static bool getSingleEntry<T>(string sql, string columnName, out T output)
        {
            return getSingleEntry(new SqlCommand(sql), columnName, out output);
        }

        internal static bool getData<TYPE>(SqlCommand cmd, string column, out List<TYPE> output)
        {
            bool success = false;
            output = new List<TYPE>();

            DataTableReader reader;
            if (getDataReader(cmd, out reader))
            {
                try
                {
                    while (reader.Read())
                    {

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (reader.GetName(i).Equals(column))
                            {
                                var entry = reader[i];
                                if (entry is TYPE)
                                    output.Add((TYPE)reader[i]);
                            }
                        }
                    }

                    success = true;
                }
                catch (Exception) { }
            }

            return success;
        }


        internal static bool getData(SqlCommand cmd, out List<Dictionary<string, object>> output)
        {
            bool success = false;
            output = new List<Dictionary<string, object>>();

            DataTableReader reader;
            if (getDataReader(cmd, out reader))
            {
                try
                {
                    while (reader.Read())
                    {
                        Dictionary<string, object> row = new Dictionary<string, object>();

                        for (int i = 0; i < reader.FieldCount; i++)
                            row.Add(reader.GetName(i), reader[i]);

                        output.Add(row);
                    }

                    success = true;
                }
                catch (Exception) { }  
            }

            return success;
        }

        internal static bool getData<TYPE>(string sql, out List<TYPE> output)
        {
            return getData(new SqlCommand(sql), out output);
        }


        internal static bool getData<TYPE>(SqlCommand cmd, out List<TYPE> output)
        {

            bool success = false;
            output = new List<TYPE>();

            DataTableReader reader;
            if (getDataReader(cmd, out reader))
            {
                try
                {
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

                        output.Add(ob);
                    }

                    success = true;

                }
                catch (Exception) { }
            }

            return success;
        }


        internal static bool getDataReader(SqlCommand cmd, out DataTableReader output)
        {
            output = default(DataTableReader);
            bool success = false;

            try
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

                output = dataSet.CreateDataReader();
                success = true;
            }
            catch (Exception) { }

            return success;
        }

        internal static bool getDataReader(string sql, out DataTableReader output)
        {
            return getDataReader(new SqlCommand(sql), out output);
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
                if (values.TryGetValue(field.Name, out value))
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

                    int loc = query.LastIndexOf(" AND ");
                    query = (loc > -1 ? query.Remove(loc) : query) + ")";  
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

        internal static string getTypeIDColumn(Type type)
        {
            FieldInfo[] fields = type.GetFields();
            foreach (FieldInfo field in fields)
            {
                DatabaseIDAttribute[] attrs = field.GetCustomAttributes(typeof(DatabaseIDAttribute), false) as DatabaseIDAttribute[];
                if (attrs.Length > 0)
                {
                    return field.Name;
                }
            }

            return "";
        }

    }
}
