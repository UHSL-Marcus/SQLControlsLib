using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SQLControlsLib
{
    public static class Extensions
    {
        internal static object getObjectFieldValue<TYPE>(this TYPE ob, string name) where TYPE:DatabaseTableObject
        {
            return typeof(TYPE).GetField(name).GetValue(ob);
        }
    }
}
