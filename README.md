# Chizl.TextConverter
Just starting on this project, but currently supports the following text files and stores them into a DataTable.
- Boolean
- Byte[]
- DateTime
- Decimal
- Guid
- Int64
- String
- TimeSpan

A .NET6 Core console Demo included.

## Future
- Add save DataTable to csv, json, tab-delimited, semicolon-delimited, and more to come....

## Written in
- Visual Studio 2022 Professional on Win11

## Supports
- netstandard2.0
- netstandard2.1
- net47
- net48
- net6.0
- net7.0
 
## Dependencies
- None

## Example with CSV
 ```csharp
    var validationLog = new List<ValidationLog>();
    LoadFile loadFile = new(".\\myfile.csv", FileTypes.Comma_Delimited);
    loadFile.ColumnDefinitions = new()
    {
        new ColumnDefinition("Column1", DataTypes.String, 20),
        new ColumnDefinition("Column2", DataTypes.String, 1) { AllowedValues=new List<object>{ "A", "B" } },
        new ColumnDefinition("Column3", DataTypes.DateTime),
        new ColumnDefinition("Column4", DataTypes.Decimal, decimalSize:2),
        new ColumnDefinition("Column5", DataTypes.Boolean),
        new ColumnDefinition("Column6", DataTypes.Int64) { AllowDBNull=true },
        new ColumnDefinition("Column7", DataTypes.TimeSpan, 25),
        new ColumnDefinition("Column8", DataTypes.ByteArray, 35)
    };

    if (loadFile.Validate(out validationLog)) 
    {
        Console.WriteLine("Validation Success!");
        DataTable dt = loadFile.AsDataTable.Copy();
    }
    else
    {
        foreach (ValidationLog vl in validationLog)
        {
            Console.WriteLine($"{vl.MessageType.Name()} {vl.ValidationType.Name()}: {vl.Location} - {vl.Message}");
        }
    }
```

## Example with Fixed Length Columns
```csharp
    var validationLog = new List<ValidationLog>();
    LoadFile loadFile = new(".\\myfile_FixedLengthColumns.txt", FileTypes.Fixed_Length_Columns);
    loadFile.ColumnDefinitions = new()
    {
        new ColumnDefinition("MSACode", DataTypes.Int64, 5),
        new ColumnDefinition("MetroDivCode", DataTypes.Int64, 5),
        new ColumnDefinition("MSAName", DataTypes.String, 50),
        new ColumnDefinition("SOACode", DataTypes.String, 5),
        new ColumnDefinition("LimitType", DataTypes.String, 1){ AllowedValues=new List<object> { "S", "H" } },
        new ColumnDefinition("MedianPrice", DataTypes.Int64, 7),
        new ColumnDefinition("LimitFor1LivUnit", DataTypes.Int64, 7),
        new ColumnDefinition("LimitFor2LivUnits", DataTypes.Int64, 7)
    };

    if (loadFile.Validate(out validationLog)) 
    {
        Console.WriteLine("Validation Success!");
        DataTable dt = loadFile.AsDataTable.Copy();
    }
    else
    {
        foreach (ValidationLog vl in validationLog)
        {
            Console.WriteLine($"{vl.MessageType.Name()} {vl.ValidationType.Name()}: {vl.Location} - {vl.Message}");
        }
    }
```


## Questions and Answers
**Q:** Why are there only Int64 and not Int16 or Int32?<br/>
**A:** Since this is a conversion tool, it's all temporary storage. Int64 being the largest it covers all Ints or Longs. This was done for String instead of Char, Decimal instead of Double, etc. You can set length and floating-point length as part of **ColumnDefinition**.<br/>

**Q:** Until this library has a way to save to disc in a different format, how can I use this now?<br/>
**A:** The [Demo](https://github.com/gavin1970/Chizl.TextConverter/blob/master/Demo/Program.cs#L142) has a method named, **DisplayTable**. It dumps all columns and data to the console window. This can be changed to save to a file since all rows and columns are already separated and validated in the DataTable. It also shows what needs special conversions (Byte[] and DBNull data). Something to Note DBNull data only comes from column set to AllowDBNull being set, no matter what DataType the column is.<br/>

**Example:**<br/>
```csharp
Row #  | Column1             | Column2   | Column3                | Column4     | Column5     | Column6    | Column7                     | Column8                               |
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
1      | test1 text data     | A         | 12/15/1970 5:39:35 AM  | 50.05       | True        | 545151     | 10675199.02:48:05.4775807   | LTEwNjc1MTk5LjAyOjQ4OjA1LjQ3NzU4MDg=  |
2      | test2 data          | B         | 10/10/2000 4:39:35 PM  | 100.56      | False       | 545151     | 10675199.02:48:05.4775807   | LTEwNjc1MTk5LjAyOjQ4OjA1LjQ3NzU4MDg=  |
3      | test3 text          | B         | 11/11/2020 3:39:35 AM  | 1000.5      | True        | 545151     | 10675199.02:48:05.4775807   | LTEwNjc1MTk5LjAyOjQ4OjA1LjQ3NzU4MDg=  |
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
```

## Links
[Home Page](http://www.chizl.com/)<br />
[License](https://github.com/gavin1970/Chizl.TextConverter/blob/master/LICENSE.md)
