using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace SQLControlsLib
{
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

    [DataContract]
    public class DatabaseTableObject
    {
        
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

        public void setForceUseAll()
        {
            Type type = GetType();
            foreach (FieldInfo field in type.GetFields())
                setFieldOptions(field.Name, true);
            
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

    public class DatabaseOutputObject
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

    [AttributeUsage(AttributeTargets.Field)]
    public class DatabaseIDAttribute : Attribute
    {

    }
}
