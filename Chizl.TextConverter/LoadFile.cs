using System;
using System.IO;
using System.Data;
using System.Collections.Generic;

namespace Chizl.TextConverter
{
    /// TODO: Add more summaries to this class and it's methods/properties.
    /// <summary>
    /// Takes a text file and the file type and allows a caller to load all line data into a DataTable.<br/>
    /// <br/>
    /// <a href="https://github.com/gavin1970/Chizl.TextConverter/blob/master/Chizl.TextConverter/FileLoad.cs">View on Github</a>
    /// </summary>
    public class LoadFile : UserProperties
    {
        /// <summary>
        /// Takes a text file and the file type and allows a caller to load all line data into a DataTable.<br/>
        /// <br/>
        /// <a href="https://github.com/gavin1970/Chizl.TextConverter/blob/master/Chizl.TextConverter/FileLoad.cs">View on Github</a>
        /// </summary>
        /// <param name="srcFile"></param>
        /// <param name="srcFileTypes"></param>
        /// <exception cref="ArgumentException"></exception>
        public LoadFile(string srcFile, FileTypes srcFileTypes)
        {
            if (string.IsNullOrWhiteSpace(srcFile))
                throw new ArgumentException($"'{nameof(srcFile)}' is a required parameter.");
            else if (!File.Exists(srcFile))
                throw new ArgumentException($"'{srcFile}' does not exists.");
            else if (srcFileTypes == FileTypes.Empty)
                throw new ArgumentException($"'{nameof(srcFileTypes)}' can not be set to Empty.  This is for internal use only.");

            FilePath = srcFile;
            FileType = srcFileTypes;
            AsDataTable = Common.CreateTable(Path.GetFileName(FilePath));
        }
        #region Public Methods
        /// <summary>
        /// Loads and validates all data in file and stores them into a DataTable.
        /// </summary>
        /// <param name="validationLog">Will return with all information and errors that may occure.</param>
        /// <returns>bool true = success, false = check AuditLogs</returns>
        public bool Validate()
        {
            bool retVal = false;
            int lineNumber = 0;
            FileInfo fi = new FileInfo(FilePath);

            //if not columns setup.
            if (ColumnDefinitions.Count == 0)
            {
                AuditLogs.Add(
                    new AuditLog(
                        AuditTypes.Import_ColumnDefinition,
                        Common.NO_LOCATION, 
                        $"No column definitions have been set: Use {nameof(ColumnDefinitions)}", 
                        MessageTypes.Error, 
                        ColumnDefinition.Empty));
                return retVal;
            }

            //if file exists
            if (fi.Exists)
            {
                AuditLogs.Add(
                    new AuditLog(
                        AuditTypes.FileLoad,
                        Common.NO_LOCATION, 
                        $"file: '{FilePath}' exists and table name will be called: '{AsDataTable.TableName}", 
                        MessageTypes.Information, 
                        ColumnDefinition.Empty));

                //add all columns to DataTable
                if (!AddColumns())
                    return retVal;

                try
                {
                    //open file for streaming.
                    using (StreamReader sr = new StreamReader(FilePath))
                    {
                        bool hadIssue = false;
                        string line;

                        //read through file, line by line
                        while ((line = sr.ReadLine()) != null)
                        {
                            lineNumber++;

                            AuditLogs.Add(
                                new AuditLog(
                                    AuditTypes.Import_Line,
                                    lineNumber,
                                    $"Loading line with {line.Length}b in size, without trim.  {line.Trim().Length}b trimmed.",
                                    MessageTypes.Information,
                                    ColumnDefinition.Empty));

                            //parse out the line based on file type.
                            List<string> parsedCols = Common.ParseLine(line, this.FileType, this.TrimValues, this.ColumnDefinitions);

                            //if the parse cound is not the same as expected column definition
                            //count, the row might be malformed, log and move to the next line..
                            if (parsedCols.Count != ColumnDefinitions.Count)
                            {
                                hadIssue = true;
                                AuditLogs.Add(
                                    new AuditLog(
                                        AuditTypes.Import_Line,
                                        lineNumber,
                                        $"Error during parseing line# {lineNumber}.\n" +
                                        $"Source Columns: {parsedCols.Count}\n" +
                                        $"Definition Columns: {ColumnDefinitions.Count}",
                                        MessageTypes.Error,
                                        ColumnDefinition.Empty));
                                continue;
                            }

                            //take the array data with column definition data and create a record.
                            hadIssue = CreateRow(lineNumber, parsedCols, out DataRow dataRow);
                            if (hadIssue)
                                continue;

                            //add the row to DataTable.
                            AsDataTable.Rows.Add(dataRow);
                        }

                        //see if there were any issues.
                        if (!hadIssue)
                            retVal = true;
                    }
                }
                catch (Exception ex)
                {
                    AuditLogs.Add(
                        new AuditLog(
                            AuditTypes.FileLoad, 
                            lineNumber, 
                            ex.Message, 
                            MessageTypes.Error, 
                            ColumnDefinition.Empty));
                }
            }
            else
                AuditLogs.Add(
                    new AuditLog(
                        AuditTypes.FileLoad,
                        Common.NO_LOCATION, 
                        $"File '{FilePath}' Missing", 
                        MessageTypes.Error, 
                        ColumnDefinition.Empty));

            return retVal;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Add columns to DataTable.
        /// </summary>
        /// <param name="validationLog">Will return with all information and errors that may occure.</param>
        /// <returns>bool true = success, false = check AuditLogs</returns>
        private bool AddColumns()
        {
            bool retVal = false;
            int colLoc = 0;

            try
            {
                //loop through all column definitions and create a column for eeach within our DataTable.
                foreach (ColumnDefinition col in ColumnDefinitions)
                {
                    //TextConverter.DataType to standard Type.
                    var dType = Common.GetDataType(col.DataType);

                    //start the build for this column.
                    var dc = new DataColumn(col.Name, dType)
                    {
                        AllowDBNull = col.AllowDBNull
                    };

                    //only setting column size based on being a string.
                    if (col.Size > 0 && dType == typeof(String))
                        dc.MaxLength = col.Size;

                    //add column to DataTable
                    AsDataTable.Columns.Add(dc);

                    AuditLogs.Add(
                        new AuditLog(
                            AuditTypes.Import_ColumnDefinition, 
                            (colLoc + 1),   //zero based.
                            $"Added '{col.Name}' to table successfully.", 
                            MessageTypes.Information, 
                            ColumnDefinitions[colLoc++]));
                }
                retVal = true;
            }
            catch (Exception ex)
            {
                AuditLogs.Add(
                    new AuditLog(
                        AuditTypes.Import_ColumnDefinition,
                        (colLoc + 1),   //zero based.
                        ex.Message, 
                        MessageTypes.Error, 
                        ColumnDefinitions[colLoc]));
            }

            return retVal;
        }
        /// <summary>
        /// Create a DataRow
        /// </summary>
        /// <param name="lineNumber">Row number</param>
        /// <param name="parsedCols">List parsed out columns for 1 row.</param>
        /// <param name="dataRow">Return of DataRow data</param>
        /// <param name="validationLog">Will return with all information and errors that may occure.</param>
        /// <returns>bool true = success, false = check AuditLogs</returns>
        private bool CreateRow(int lineNumber, List<string> parsedCols, out DataRow dataRow)
        {
            bool hadIssue = false;

            //initialize a blank row.
            dataRow = AsDataTable.NewRow();
            //loop through all defind columns.
            for (int colLoc = 0; colLoc < parsedCols.Count; colLoc++)
            {
                var errMessage = "";
                //get real type from defined column data type.
                var dataType = Common.GetDataType(ColumnDefinitions[colLoc].DataType);

                AuditLogs.Add(
                    new AuditLog(
                        AuditTypes.Import_ColumnDefinition,
                        Common.NO_LOCATION,   //handle this within the message.
                        $"Processing Line# {lineNumber}\nColumn #{(colLoc + 1)} Name: {ColumnDefinitions[colLoc].Name}\nColumn Type: {dataType.FullName}",
                        MessageTypes.Error,
                        ColumnDefinitions[colLoc]));

                //convert this one column into what it supposed to be.   
                //HasValidData() validates, including if column is AllowDBNull=true
                if (ConvertColumn(parsedCols[colLoc], ColumnDefinitions[colLoc], out object retData)
                    && Common.HasValidData(retData, ColumnDefinitions[colLoc], out errMessage))
                {
                    try
                    {
                        //if it data is DBNull, then we will skip updating this column.
                        if (retData != DBNull.Value)
                            dataRow[ColumnDefinitions[colLoc].Name] = Convert.ChangeType(retData, dataType);
                    } 
                    catch(Exception ex)
                    {
                        errMessage = $"Exception while converting and adding '{retData}' " +
                                     $"({dataType}) to column: {ColumnDefinitions[colLoc].Name}\n{ex.Message}";
                    }
                }

                //if there is a 
                if (!string.IsNullOrWhiteSpace(errMessage))
                {
                    AuditLogs.Add(
                        new AuditLog(
                            AuditTypes.DataValidation,
                            lineNumber,
                            errMessage,
                            MessageTypes.Error,
                            ColumnDefinition.Empty));

                    hadIssue = true;
                    break;
                }
            }

            return hadIssue;
        }
        /// <summary>
        /// Convert string data to required data type.
        /// </summary>
        /// <param name="data">Original string data.</param>
        /// <param name="column">Column definition like Type and size.</param>
        /// <param name="newValue">New value after conversion.</param>
        /// <param name="validationLog">Will return with all information and errors that may occure.</param>
        /// <returns>bool true = success, false = check AuditLogs</returns>
        private bool ConvertColumn(string data,  ColumnDefinition column, out object newValue)
        {
            bool retVal = true;
            //convert local DataType to System.DataType
            var dataType = Common.GetDataType(column.DataType);

            //pulls default for System.DataType
            newValue = Common.GetDefault(dataType);

            //if it's string, just return what it is, else, we have some checking to do.
            if (column.DataType != DataTypes.String)
            {
                //non-string needs no spaces before conversions.
                data = data.Trim();

                //if empty and no AllowDBNull, no matter the DataType, we are going to skip adding data to the column.
                if (string.IsNullOrWhiteSpace(data) && column.AllowDBNull)
                    //return DBNull.Value, telling caller to skip column data
                    newValue = DBNull.Value;
                else
                {
                    switch (column.DataType)
                    {
                        case DataTypes.Boolean:
                            if (Boolean.TryParse(data, out Boolean bVal))
                                newValue = bVal;
                            else
                                retVal = false;
                            break;
                        case DataTypes.DateTime:
                            if (DateTime.TryParse(data, out DateTime dtVal))
                                newValue = dtVal;
                            else
                                retVal = false;
                            break;
                        case DataTypes.TimeSpan:
                            if (TimeSpan.TryParse(data, out TimeSpan tsVal))
                                newValue = tsVal;
                            else
                                retVal = false;
                            break;
                        case DataTypes.Decimal:
                            //convert and if successul, check decimal size expected.
                            if (Decimal.TryParse(data, out Decimal decVal))
                                newValue = Math.Round(decVal, column.DecimalSize);
                            else
                                retVal = false;
                            break;
                        case DataTypes.Int64:
                            if (Int64.TryParse(data, out Int64 iVal))
                                newValue = iVal;
                            else
                                retVal = false;
                            break;
                        case DataTypes.Guid:
                            if (Guid.TryParse(data, out Guid gVal))
                                newValue = gVal;
                            else
                                retVal = false;
                            break;
                        case DataTypes.ByteArray:
                            //special case
                            newValue = Convert.FromBase64String(data.ToString());
                            break;
                        default:
                            //should never occur, if setup correctly.
                            newValue = data;
                            break;
                    }
                }
            }
            else
                newValue = data;

            if(!retVal)
                AuditLogs.Add(
                    new AuditLog(
                        AuditTypes.Import_ColumnDefinition, 
                        Common.NO_LOCATION, 
                        $"Failed to convert '{data}' to a {column.DataType.Name()}.", 
                        MessageTypes.Error, 
                        column));

            return retVal;
        }
        #endregion
    }
}
