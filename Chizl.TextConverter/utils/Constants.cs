using System;

namespace Chizl.TextConverter
{
    public static class EnumExtensions
    {
        public static int Value(this DataTypes val)
        {
            return (int)val;
        }
        public static string Name(this DataTypes val)
        {
            return Enum.GetName(typeof(DataTypes), val);
        }

        public static string Name(this MessageTypes val)
        {
            return Enum.GetName(typeof(MessageTypes), val);
        }
        public static int Value(this MessageTypes val)
        {
            return (int)val;
        }

        public static string Name(this ValidationTypes val)
        {
            return Enum.GetName(typeof(ValidationTypes), val);
        }
        public static int Value(this ValidationTypes val)
        {
            return (int)val;
        }
    }

    public enum FileType
    {
        Fixed_Length_Columns = 0,
        Tab_Delimited,
        Comma_Delimited,
        Semicolon_Delimited,
    }

    public enum ValidationTypes
    {
        FileLoad = 0,
        LineImport,
        RowDefinition,
        ColumnDefinition,
        DataValidation,
        Prep,
    }

    public enum MessageTypes
    {
        Error = 0,
        Information,
    }

    public enum DataTypes
    {
        String = 0,
        Int32,
        Boolean,
        TimeSpan,
        DateTime,
        Decimal,
        ByteArray,
    }

    internal class Constants
    {
    }
}
