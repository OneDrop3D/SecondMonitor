namespace SecondMonitor.PCarsConnector.Enums
{
#pragma warning disable RCS1013
#pragma warning disable SA1649
    using System;
    using System.ComponentModel;
    using System.Reflection;

    public static class EnumOperations
    {
        public static T StringToEnum<T>(string name)
        {
            return (T)Enum.Parse(typeof(T), name);
        }
    }

    #pragma warning disable SA1402
    public static class EnumGetDescription
    {
        public static string GetDescription(this Enum enumObj)
        {
            FieldInfo fieldInfo = enumObj.GetType().GetField(enumObj.ToString());

            object[] attribArray = fieldInfo.GetCustomAttributes(false);

            if (attribArray.Length == 0)
            {
                return enumObj.ToString();
            }
            else
            {
                DescriptionAttribute attrib = attribArray[0] as DescriptionAttribute;
                return attrib.Description;
            }
        }
#pragma warning restore SA1402
#pragma warning restore RCS1013
#pragma warning restore SA1649
    }
}
