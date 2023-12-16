using Chizl.TextConverter;
using System.Data;
using System.Text;

namespace Demo
{
    internal class Program
    {
        const string auditFile = ".\\auditFile.log";
        static LoadFile fileLoad = LoadFile.Empty;
        static SaveFile saveFile = SaveFile.Empty;

        const string KeyPressOptions = "By pressing 'Esc', your requesting to skip the rest of this file.  " +
                                        "Press 'Esc' again to confirm, press 'End' to display until end of file without pause, " +
                                        "or any other key to continue with pause.";

        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.TreatControlCAsInput = false;   //allows control-c to close window

            // may want this the first time, but throws warnings during builds.  After the first time, it caches your console buffer.
            // Console.WindowWidth = Console.LargestWindowWidth;

            // NOTE: SaveData isn't ready, which is why it's commented.

            RunValidateTest(FileTypes.Comma_Delimited, FileTypes.Fixed_Length_Columns, TestDelimited(), trimData: false);
            RunValidateTest(FileTypes.Quote_Comma_Delimited, FileTypes.Semicolon_Delimited, TestDelimited());
            RunValidateTest(FileTypes.Tab_Delimited, FileTypes.Quote_Comma_Delimited, TestDelimited());
            RunValidateTest(FileTypes.Semicolon_Delimited, FileTypes.Comma_Delimited, TestDelimited());
            RunValidateTest(FileTypes.Fixed_Length_Columns, FileTypes.Semicolon_Delimited, TestFixLengthColumns());
            RunValidateTest(FileTypes.Fixed_Length_Columns, FileTypes.Quote_Comma_Delimited, TestFixLengthColumns(), isFull: true);
        }

        static void RunValidateTest(FileTypes fromType, FileTypes toType, List<ColumnDefinition> colDefs, bool isFull = false, bool trimData = true)
        {
            Validate(fromType, colDefs, isFull);
            SaveData(toType, colDefs, trimData);
        }

        /// <summary>
        /// * Data comes from:
        /// * https://apps.hud.gov/pub/chums/cy2024-gse-limits.txt
        /// * Definition comes from:
        /// * https://apps.hud.gov/pub/chums/#Limits2010
        /// Field Name                   Position     Format and Size
        /// MSA Code                          001-005           9(5)
        /// Metropolitan Division Code        006-010           9(5)
        /// MSA Name                          011-060           X(50)
        /// SOA Code                          061-065           X(5)
        /// Limit Type                        066-066           X(1)
        ///     Values: S - Standard Limits - Indicates limits are at national floor.
        ///             H - High Cost Limits - Indicates limits are above national floor.
        /// Median Price                      067-073           9(7)
        /// Limit for 1 Living Unit           074-080           9(7)
        /// Limit for 2 Living Units          081-087           9(7)
        /// Limit for 3 Living Units          088-094           9(7)
        /// Limit for 4 Living Units          095-101           9(7)
        /// State Abbreviation                102-103           X(2)
        /// County Code (FIPS)                104-106           9(3)
        /// State Name                        107-132           X(26)
        /// County Name                       133-147           X(15)
        /// County Transaction Date           148-155           9(8)
        ///     - This is supposed to be an int. As seen in demo file it's empty. AllowDBNull 
        ///         property needs to be set. Value DataType response for AllowDBNull columns, 
        ///         if empty, is DBNull for all DataTypes.
        /// Limit Transaction Date            156-163           9(8)
        /// Median Price Determining Limit    164-170           9(7)
        /// Year For Median Determining Limit 171-175           9(4) 
        /// </summary>
        static List<ColumnDefinition> TestFixLengthColumns()
        {
            return new()
            {
                new ColumnDefinition("MSACode", DataTypes.Int64, size:5),
                new ColumnDefinition("MetroDivCode", DataTypes.Int64, size:5),
                new ColumnDefinition("MSAName", DataTypes.String, size:50),
                new ColumnDefinition("SOACode", DataTypes.String, size:5),
                new ColumnDefinition("LimitType", DataTypes.String, size:1){ AllowedValues=new List<object> { "S", "H" } },
                new ColumnDefinition("MedianPrice", DataTypes.Int64, size:7),
                new ColumnDefinition("LimitFor1LivUnit", DataTypes.Int64, size:7),
                new ColumnDefinition("LimitFor2LivUnits", DataTypes.Int64, size:7),
                new ColumnDefinition("LimitFor3LivUnits", DataTypes.Int64, size:7),
                new ColumnDefinition("LimitFor4LivUnits", DataTypes.Int64, size:7),
                new ColumnDefinition("StateAbbreviation", DataTypes.String, size:2),
                new ColumnDefinition("CountyCodeFIPS", DataTypes.Int64, size:3),
                new ColumnDefinition("StateName", DataTypes.String, size:26),
                new ColumnDefinition("CountyName", DataTypes.String, size:15),
                new ColumnDefinition("CountyTransDate", DataTypes.Int64, size:8) { AllowDBNull=true },  //int with possible empty value
                new ColumnDefinition("LimitTransDate", DataTypes.Int64, size:8),
                new ColumnDefinition("MedPriceDetLimit", DataTypes.Int64, size:7),
                new ColumnDefinition("YearForMedDetLimit", DataTypes.Int64, size:4)
            };
        }

        /// <summary>
        /// TimeSpan ts = TimeSpan.MinValue;
        /// byte[] bytes = Encoding.ASCII.GetBytes(ts.ToString());
        /// string enc = Convert.ToBase64String(bytes);
        /// NOTE: Because this is CSV format, these sizes that are NOT string, are ignored for LoadFile 
        /// unless Fixed Column Sized. They can also be used for SaveFile on Fixed Column Sizes.
        /// </summary>
        static List<ColumnDefinition> TestDelimited()
        {
            return new()
            {
                new ColumnDefinition("Column1", DataTypes.String, size:20),
                new ColumnDefinition("Column2", DataTypes.String, size:1) { AllowedValues=new List<object>{ "A", "B" } },
                new ColumnDefinition("Column3", DataTypes.DateTime, size:25),
                new ColumnDefinition("Column4", DataTypes.Decimal, size:10, decimalSize:2),
                new ColumnDefinition("Column5", DataTypes.Boolean, size:10),
                new ColumnDefinition("Column6", DataTypes.Int64, size:10),
                new ColumnDefinition("Column7", DataTypes.TimeSpan, size:30),
                new ColumnDefinition("Column8", DataTypes.ByteArray, size:48)
            };
        }

        static void SaveData(FileTypes ft, List<ColumnDefinition> colDefs, bool trimData = true)
        {
            if (fileLoad.IsEmpty)
                return;

            var skipPause = false;

            var fileName = $".\\Export\\{Path.GetFileNameWithoutExtension(fileLoad.FileName)}_to_{ft.Name()}.txt";

            DrawTitle($"Saving file: {fileName}, Type: {ft}");

            saveFile = new SaveFile(fileLoad.AsDataTable, fileName, ft, fileByColDefOnly: false, overwriteFile: true, createFolder: true) 
                                    { TrimValues = trimData, FirstRowIsHeader = true, ColumnDefinitions = colDefs };

            if (saveFile.Save())
                Console.WriteLine("Save Success!");
            else
            {
                Console.WriteLine("Save Failed");
                skipPause = DisplayLog(30, true, false);
            }

            if (!skipPause)
                Console.ReadKey(true);
        }

        static void Validate(FileTypes ft, List<ColumnDefinition> columns, bool isFull = false)
        {
            bool skipPause;
            int maxRecordsToDisplay = ft == FileTypes.Fixed_Length_Columns ? (isFull ? 40 : 20) : 10;

            var ext = ".txt";
            var full = isFull ? "_full" : "";
            var fileName = $@".\data\{ft.Name()}{full}{ext}";

            DrawTitle($"Running test on: {fileName}");

            fileLoad = new(fileName, ft)
            {
                ColumnDefinitions = columns
            };

            if (fileLoad.LoadToDataTable())
            {
                Console.WriteLine("Validation Success!");
                skipPause = DisplayTable(fileLoad, maxRecordsToDisplay);
            }
            else
            {
                Console.WriteLine("Validation Failed");
                skipPause = DisplayLog(maxRecordsToDisplay);
            }

            if (!skipPause)
                Console.ReadKey(true);
        }

        static bool DisplayLog(int maxRecordsToDisplay, bool errorsOnly = true, bool isLoad = true)
        {
            bool skipPause = false;
            int orgMaxRecToDisplay = maxRecordsToDisplay;

            DrawTitle("Displaying validation log");
            if (File.Exists(auditFile))
                File.Delete(auditFile);

            List<AuditLog> allLogs = isLoad ? fileLoad.AuditLogs : saveFile.AuditLogs;

            foreach (AuditLog vl in allLogs)
            {
                var locType = vl.ValidationType.Name().Contains("_Line") ? "Line" : "Column";
                string msg = $"{vl.ValidationType} - {(vl.Location > 0 ? $"{locType}#: {vl.Location} - " : "")} {vl.Message}";

                if (!errorsOnly || vl.MessageType == MessageTypes.Error)
                {
                    Console.WriteLine(msg);
                    if (maxRecordsToDisplay-- <= 0)
                    {
                        skipPause = PromptForInput(out bool setMaxRow);
                        if (skipPause)
                            break;

                        if (setMaxRow)
                            maxRecordsToDisplay = allLogs.Count;
                        else
                            maxRecordsToDisplay = orgMaxRecToDisplay;
                    }
                }

                File.AppendAllText(auditFile, $"{msg}\n");
            }

            return skipPause;
        }

        static bool DisplayTable(LoadFile fileLoad, int maxRecordsToDisplay = 10)
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
                        //check if data inside is a DBNull Type.
                        if (dr[dc.ColumnName].GetType() == typeof(DBNull))
                            data = "DBNull";
                        else if (dc.DataType == typeof(byte[]))
                            //if byte array, convert to Base64 string for display
                            data = Convert.ToBase64String((byte[])dr[dc.ColumnName]);
                        else
                            //convert to string
                            data = dr[dc.ColumnName].ToString();
                    }

                    //if data exist
                    if (!string.IsNullOrWhiteSpace(data))
                        sb.Append($"{data}{(new string(' ', Math.Abs(colSize - data.Length)))}| ");
                    else
                        sb.Append($"null{(new string(' ', colSize - 4))}| ");
                }
                //write row
                Console.WriteLine(sb.ToString());

                if (--maxRecordsToDisplay <= 0)
                {
                    skipPause = PromptForInput(out bool setMaxRow);
                    if (skipPause)
                        break;

                    if (setMaxRow)
                        maxRecordsToDisplay = dt.Rows.Count;
                    else
                        maxRecordsToDisplay = orgMaxRecToDisplay;
                }
            }

            //ending
            Console.WriteLine($"{(new string('-', sb.ToString().Length))}");
            return skipPause;
        }

        static bool PromptForInput(out bool setMaxRow)
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

        static void DrawTitle(string title)
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
    }
}