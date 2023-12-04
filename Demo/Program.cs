using Chizl.TextConverter;
using System.Data;
using System.Runtime.CompilerServices;
using System.Text;

const string KeyPressOptions = "By pressing 'Esc', your requesting to skip the rest of this file.  " +
    "Press 'Esc' again to confirm, press 'End' to display until end of file without pause, " +
    "or any other key to continue with pause.";

Console.OutputEncoding = Encoding.UTF8;
Console.TreatControlCAsInput = false;   //allows control-c to close window

// may want this the first time, but throws warnings during builds.  After the first time, it caches your console buffer.
// Console.WindowWidth = Console.LargestWindowWidth;

Validate("./test_comma_delimited.txt", FileTypes.Comma_Delimited, TestDelimited());
Validate("./test_quote_comma_delimited.txt", FileTypes.Quote_Comma_Delimited, TestDelimited());
Validate("./test_tab_delimited.txt", FileTypes.Tab_Delimited, TestDelimited());
Validate("./test_Semicolon_delimited.txt", FileTypes.Semicolon_Delimited, TestDelimited());
Validate("./test_fixed_length_columns.txt", FileTypes.Fixed_Length_Columns, TestFixLengthColumns(), 20);
Validate("./test_fixed_length_columns_full.txt", FileTypes.Fixed_Length_Columns, TestFixLengthColumns(), 40);

List<ColumnDefinition> TestFixLengthColumns()
{
    /*
     * Data comes from:
     * https://apps.hud.gov/pub/chums/cy2024-gse-limits.txt
     * Definition comes from:
     * https://apps.hud.gov/pub/chums/#Limits2010
        Field Name                   Position     Format and Size
        MSA Code                          001-005           9(5)
        Metropolitan Division Code        006-010           9(5)
        MSA Name                          011-060           X(50)
        SOA Code                          061-065           X(5)
        Limit Type                        066-066           X(1)
            Values: S - Standard Limits - Indicates limits are at national floor.
                    H - High Cost Limits - Indicates limits are above national floor.
        Median Price                      067-073           9(7)
        Limit for 1 Living Unit           074-080           9(7)
        Limit for 2 Living Units          081-087           9(7)
        Limit for 3 Living Units          088-094           9(7)
        Limit for 4 Living Units          095-101           9(7)
        State Abbreviation                102-103           X(2)
        County Code (FIPS)                104-106           9(3)
        State Name                        107-132           X(26)
        County Name                       133-147           X(15)
        County Transaction Date           148-155           9(8)   - This is supposed to be an int, however, as seen in file, it's empty. So have to make it a String.
        Limit Transaction Date            156-163           9(8)
        Median Price Determining Limit    164-170           9(7)
        Year For Median Determining Limit 171-175           9(4) 
    */

    return new()
    {
        new ColumnDefinition("MSACode", DataTypes.Int64, 5),
        new ColumnDefinition("MetroDivCode", DataTypes.Int64, 5),
        new ColumnDefinition("MSAName", DataTypes.String, 50),
        new ColumnDefinition("SOACode", DataTypes.String, 5),
        new ColumnDefinition("LimitType", DataTypes.String, 1){ AllowedValues=new List<object> { "S", "H" } },
        new ColumnDefinition("MedianPrice", DataTypes.Int64, 7),
        new ColumnDefinition("LimitFor1LivUnit", DataTypes.Int64, 7),
        new ColumnDefinition("LimitFor2LivUnits", DataTypes.Int64, 7),
        new ColumnDefinition("LimitFor3LivUnits", DataTypes.Int64, 7),
        new ColumnDefinition("LimitFor4LivUnits", DataTypes.Int64, 7),
        new ColumnDefinition("StateAbbreviation", DataTypes.String, 2),
        new ColumnDefinition("CountyCodeFIPS", DataTypes.Int64, 3),
        new ColumnDefinition("StateName", DataTypes.String, 26),
        new ColumnDefinition("CountyName", DataTypes.String, 15),
        new ColumnDefinition("CountyTransDate", DataTypes.Int64, 8) { AllowDBNull=true },  //int with possible empty value
        new ColumnDefinition("LimitTransDate", DataTypes.Int64, 8),
        new ColumnDefinition("MedPriceDetLimit", DataTypes.Int64, 7),
        new ColumnDefinition("YearForMedDetLimit", DataTypes.Int64, 4)
    };
}

List<ColumnDefinition> TestDelimited()
{
    //TimeSpan ts = TimeSpan.MinValue;
    //byte[] bytes = Encoding.ASCII.GetBytes(ts.ToString());
    //string enc = Convert.ToBase64String(bytes);

    //NOTE: Because this is CSV format, these sizes that are NOT for string, are ignored, but can be used for display.
    return new()
    {
        new ColumnDefinition("Column1", DataTypes.String, 20),
        new ColumnDefinition("Column2", DataTypes.String, 1){ AllowedValues=new List<object>{ "A", "B" } },
        new ColumnDefinition("Column3", DataTypes.DateTime),
        new ColumnDefinition("Column4", DataTypes.Decimal, decimalSize:2),
        new ColumnDefinition("Column5", DataTypes.Boolean),
        new ColumnDefinition("Column6", DataTypes.Int64),
        new ColumnDefinition("Column7", DataTypes.TimeSpan, 25),
        new ColumnDefinition("Column8", DataTypes.ByteArray, 35)
    };
}

void Validate(string fileName, FileTypes ft, List<ColumnDefinition> columns, int maxRecordsToDisplay = 10)
{
    if (maxRecordsToDisplay <= 0) maxRecordsToDisplay = 1;
    bool skipPause = false;
    DrawTitle($"Running test on: {fileName}");

    var validationLog = new List<ValidationLog>();
    LoadFile fileLoad = new(fileName, ft)
    {
        ColumnDefinitions = columns
    };

    if (fileLoad.Validate(out validationLog))
    {
        Console.WriteLine("Validation Success!");
        skipPause = DisplayTable(fileLoad, maxRecordsToDisplay);
    }
    else
    {
        Console.WriteLine("Validation Failed");
        skipPause = DisplayLog(validationLog, maxRecordsToDisplay);
    }

    if(!skipPause)
        Console.ReadKey(true);
}

bool DisplayLog(List<ValidationLog> validationLog, int maxRecordsToDisplay = 10, bool errorsOnly = true) 
{
    bool skipPause = false;
    if (maxRecordsToDisplay <= 0) maxRecordsToDisplay = 1;
    int orgMaxRecToDisplay = maxRecordsToDisplay;

    DrawTitle("Displaying validation log");
    if (File.Exists(".\\validation.log"))
        File.Delete(".\\validation.log");

    foreach (ValidationLog vl in validationLog)
    {
        var locType = vl.ValidationType == ValidationTypes.LineImport ? "Line" : "Column";
        string msg = $"{vl.ValidationType} - {locType}#: {vl.Location} - {vl.Message}";

        if (!errorsOnly || vl.MessageType == MessageTypes.Error) 
        {
            Console.WriteLine(msg);
            if (maxRecordsToDisplay-- <= 0)
            {
                skipPause = PromptForInput(out bool setMaxRow);
                if (skipPause)
                    break;

                if (setMaxRow)
                    maxRecordsToDisplay = validationLog.Count;
                else
                    maxRecordsToDisplay = orgMaxRecToDisplay;
            }
        }

        File.AppendAllText(".\\validation.log", $"{msg}\n");
    }

    return skipPause;
}

bool DisplayTable(LoadFile fileLoad, int maxRecordsToDisplay = 10)
{
    bool skipPause = false;
    if (maxRecordsToDisplay <= 0) maxRecordsToDisplay = 1;
    int orgMaxRecToDisplay = maxRecordsToDisplay;

    DataTable dt = fileLoad.AsDataTable.Copy();
    if (dt == null)
    {
        Console.WriteLine("Failed to pull table.");
        return skipPause;
    }

    DrawTitle($"Displaying DataTable: {dt.TableName} with {dt.Rows.Count} record{(dt.Rows.Count == 1 ? "" : "s")}.");

    List<int> colSizes = new();
    StringBuilder sb = new();

    string colRow = "Row #";
    int colSize = colRow.Length + 3;
    int addLengh = colSize - colRow.Length;

    colSizes.Add(colSize);
    sb.Append($"{colRow}{(new string(' ', addLengh))}| ");

    foreach (DataColumn dc in dt.Columns)
    {
        var cd = fileLoad.ColumnDefinitions.FindLast(f => f.Name == dc.ColumnName);
        if (cd != null)
        {
            //default 20 if no size specified, add 3 for padding.
            colSize = (cd.Size == 0 ? 20 : cd.Size) + 3;
            if (dc.ColumnName.Length > colSize)
                colSize = dc.ColumnName.Length + 3;

            colSizes.Add(colSize);

            addLengh = colSize - dc.ColumnName.Length;
            sb.Append($"{dc.ColumnName}{(new string(' ', addLengh))}| ");
        }
        else
            break;
    }

    Console.WriteLine(sb.ToString());
    Console.WriteLine($"{(new string('-', sb.ToString().Length))}");

    int row = 0;
    foreach (DataRow dr in dt.Rows)
    {
        int col = 0;
        sb = new($"{++row}{(new string(' ', Math.Abs(colSizes[col++] - row.ToString().Length)))}| ");

        foreach (DataColumn dc in dt.Columns)
        {
            //get stored column title size
            colSize = colSizes[col++];
            //create empty string for two different type of conversions.
            var data = string.Empty;
            //should never be, but lets check.
            if (dr[dc.ColumnName] != null)
            {
                if (dr[dc.ColumnName].GetType() == typeof(DBNull))
                    data = "DBNull";
                else if (dc.DataType == typeof(byte[]))
                    data = Convert.ToBase64String((byte[])dr[dc.ColumnName]);
                else
                    data = dr[dc.ColumnName].ToString();
            }

            if (!string.IsNullOrWhiteSpace(data))
                sb.Append($"{data}{(new string(' ', Math.Abs(colSize - data.Length)))}| ");
            else
                sb.Append($"null{(new string(' ', colSize-4))}| ");
        }
        //write row
        Console.WriteLine(sb.ToString());

        if (maxRecordsToDisplay-- <= 0) 
        {
            skipPause = PromptForInput(out bool setMaxRow);
            if (skipPause)
                break;

            if(setMaxRow)
                maxRecordsToDisplay = dt.Rows.Count;
            else
                maxRecordsToDisplay = orgMaxRecToDisplay;
        }
    }
    
    //ending
    Console.WriteLine($"{(new string('-', sb.ToString().Length))}");
    return skipPause;
}

bool PromptForInput(out bool setMaxRow)
{
    bool skipPause = false;
    setMaxRow = false;

    ConsoleKeyInfo key = Console.ReadKey(true);

    if (key.Key == ConsoleKey.Escape)
    {
        Console.WriteLine(KeyPressOptions);
        key = Console.ReadKey(true);

        if (key.Key == ConsoleKey.Escape)
        {
            skipPause = true;
        }
        else if (key.Key == ConsoleKey.End)
        {
            setMaxRow = true;
            Console.SetCursorPosition(0, Console.CursorTop - 1);    //deleting the Console.WriteLine above.
        }
        else
            Console.SetCursorPosition(0, Console.CursorTop - 1);    //deleting the Console.WriteLine above.
    }

    return skipPause;
}

void DrawTitle(string title)
{
    Console.Clear();

    int barSizer = 20;
    title = $"=[ {title} ]=";
    int len = (barSizer * 2) + title.Length;

    Console.WriteLine($"\n{(new string('-', len))}");
    Console.Write($"{(new string('-', barSizer))}");
    Console.Write(title);
    Console.WriteLine($"{(new string('-', barSizer))}");
    Console.WriteLine($"{(new string('-', len))}\n"); //double newline
}