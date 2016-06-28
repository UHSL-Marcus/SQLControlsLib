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
