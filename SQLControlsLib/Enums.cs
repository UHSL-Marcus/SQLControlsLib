using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
