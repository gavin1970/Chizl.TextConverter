# Chizl.TextConverter
Just starting on this project, but currently loads (csv, tab, semicolon, fixed length column) text files into a DataTable.

.NETCore Demo console included.

## Future
- Add save to csv, json, tab-delimited, semicolon-delimited

## Written in
Visual Studio 2022 Professional

## Supports
- netstandard2.0
- netstandard2.1
- net47
- net48
- net6.0
- net7.0
 
  ## Example of use
 ```csharp
    var validationLog = new List<ValidationLog>();
    FileLoad fileLoad = new(".\\myfile.csv", FileType.Comma_Delimited);
    fileLoad.ColumnDefinitions = new()
    {
        new ColumnDefinition("Column1", DataTypes.String, 20),
        new ColumnDefinition("Column2", DataTypes.String, 1) { AllowedValues=new List<object>{ "A", "B" } },
        new ColumnDefinition("Column3", DataTypes.DateTime),
        new ColumnDefinition("Column4", DataTypes.Decimal, decimalSize:2),
        new ColumnDefinition("Column5", DataTypes.Boolean),
        new ColumnDefinition("Column6", DataTypes.Int32) { AllowDBNull=true },
        new ColumnDefinition("Column7", DataTypes.TimeSpan, 25),
        new ColumnDefinition("Column8", DataTypes.ByteArray, 35)
    };

    if (fileLoad.Validate(out validationLog)) 
    {
        Console.WriteLine("Validation Success!");
        DataTable dt = fileLoad.ToDataTable.Copy();
    }
    else
    {
        foreach (ValidationLog vl in validationLog)
        {
            Console.WriteLine($"{vl.MessageType.Name()} {vl.ValidationType.Name()}: {vl.Location} - {vl.Message}");
        }
    }
```