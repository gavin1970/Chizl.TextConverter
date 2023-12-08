using System;
using System.Text.RegularExpressions;

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

        public static string Name(this AuditTypes val)
        {
            return Enum.GetName(typeof(AuditTypes), val);
        }
        public static int Value(this AuditTypes val)
        {
            return (int)val;
        }

        public static string Name(this RegExFormats val)
        {
            return Enum.GetName(typeof(RegExFormats), val);
        }
        public static string Replace(this RegExFormats val, string fullString, string replaceWith)
        {
            string format;
            switch (val)
            {
                case RegExFormats.Alpha:
                    format = "[^a-zA-Z -]";
                    break;
                case RegExFormats.Numeric:
                    format = "[^0-9]";
                    break;
                case RegExFormats.AlphaNumeric:
                default:
                    format = "[^a-zA-Z0-9 -]";
                    break;
            }
            return new Regex(format).Replace(fullString, replaceWith);
        }

        /// <summary>
        /// Not supported by net47
        /// </summary>
        /// <example>
        /// bool isWindows = OSPlatform.Windows.Running();
        /// </example>
        //public static bool Running(this OSPlatform val)
        //{
        //    if (RuntimeInformation.IsOSPlatform(val))
        //        return true;
        //    else
        //        return false;
        //}
    }

    public enum FileTypes
    {
        Empty = -1,
        Fixed_Length_Columns = 0,
        Tab_Delimited,
        Comma_Delimited,
        Quote_Comma_Delimited,
        Semicolon_Delimited,
    }

    public enum AuditTypes
    {
        File = 0,
        Directory,
        Row,
        Column,
        ColumnDefinition,
        ColumnConversion
    }

    public enum MessageTypes
    {
        Error = 0,
        Information,
    }

    public enum DataTypes
    {
        Boolean = 0,
        ByteArray,
        DateTime,
        Decimal,
        Guid,
        Int64,
        String,
        TimeSpan
    }

    public enum RegExFormats
    {
        AlphaNumeric = 0,
        Alpha,
        Numeric
    }
}
