namespace Chizl.TextConverter
{
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
