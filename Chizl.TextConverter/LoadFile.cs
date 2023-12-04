using System;
using System.IO;
using System.Data;
using System.Linq;
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
                throw new ArgumentException($"'{srcFileTypes}' can not be set to Empty.  This is for internal use only.");

            FilePath = srcFile;
            FileType = srcFileTypes;
            AsDataTable = CreateTable(Path.GetFileName(FilePath));
        }
        /// <summary>
        /// Loads and validates all data in file and stores them into a DataTable.
        /// </summary>
        /// <param name="validationLog">Will return with all information and errors that may occure.</param>
        /// <returns>bool - success or error</returns>
        public bool Validate(out List<ValidationLog> validationLog)
        {
            bool retVal = false;
            int lineNumber = 0;
            FileInfo fi = new FileInfo(FilePath);
            validationLog = new List<ValidationLog>();

            if (ColumnDefinitions.Count == 0)
            {
                validationLog.Add(new ValidationLog(ValidationTypes.Prep, 0, $"No column definitions have been set: Use {nameof(ColumnDefinitions)}", MessageTypes.Error, ColumnDefinition.Empty));
                return retVal;
            }

            if (fi.Exists)
            {
                validationLog.Add(new ValidationLog(ValidationTypes.Prep, 0, $"file: '{FilePath}' exists and table name will be called: '{AsDataTable.TableName}", MessageTypes.Information, ColumnDefinition.Empty));

                if (!AddColumns(ref validationLog))
                    return retVal;

                try
                {
                    using (StreamReader sr = new StreamReader(FilePath))
                    {
                        bool hadIssue = false;
                        string line;

                        while ((line = sr.ReadLine()) != null)
                        {
                            lineNumber++;

                            List<string> columns = ParseLine(line);
                            if (columns.Count != ColumnDefinitions.Count)
                            {
                                hadIssue = true;
                                validationLog.Add(
                                    new ValidationLog(
                                        ValidationTypes.LineImport,
                                        lineNumber,
                                        $"Error during parseing line# {lineNumber}.\nSource Columns: {columns.Count}\nDefinition Columns: {ColumnDefinitions.Count}",
                                        MessageTypes.Error,
                                        ColumnDefinition.Empty));
                                continue;
                            }

                            hadIssue = CreateRow(lineNumber, columns, out DataRow dataRow, ref validationLog);
                            if (hadIssue)
                                continue;

                            AsDataTable.Rows.Add(dataRow);
                        }

                        if (!hadIssue)
                            retVal = true;
                    }
                }
                catch (Exception ex)
                {
                    validationLog.Add(new ValidationLog(ValidationTypes.FileLoad, lineNumber, ex.Message, MessageTypes.Error, ColumnDefinition.Empty));
                }
            }
            else
                validationLog.Add(new ValidationLog(ValidationTypes.Prep, 0, $"File '{FilePath}' Missing", MessageTypes.Error, ColumnDefinition.Empty));

            return retVal;
        }
        /// <summary>
        /// Internal use, but left as public.<br/>
        /// Since we can't do this: var test = default(Type.GetType($"System.Int32"));<br/>
        /// To use it, call this instead: GetDefault(Type t) instead.<br/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetDefaultValue<T>()
        {
            return default(T);
        }
        /// <summary>
        /// Internal use, but left as public.<br/>
        /// Calls GetDefaultValue() to generate default value;
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public object GetDefault(Type t)
        {
            return this.GetType().GetMethod("GetDefaultValue").MakeGenericMethod(t).Invoke(this, null);
        }

        /// <summary>
        /// Converts Internal DataTypes to real System Types.
        /// </summary>
        /// <param name="dataType">Chizl.TextConverter DataType</param>
        /// <returns></returns>
        private Type GetDataType(DataTypes dataType)
        {
            var dt = dataType == DataTypes.ByteArray ? "Byte[]" : dataType.Name();
            return Type.GetType($"System.{dt}");
        }
        /// <summary>
        /// Add columns to DataTable.
        /// </summary>
        /// <param name="validationLog">Will return with all information and errors that may occure.</param>
        /// <returns></returns>
        private bool AddColumns(ref List<ValidationLog> validationLog)
        {
            bool retVal = false;
            int colLoc = 0;

            try
            {
                foreach (ColumnDefinition col in ColumnDefinitions)
                {
                    var dType = GetDataType(col.DataType);

                    var dc = new DataColumn(col.Name, dType)
                    {
                        AllowDBNull = col.AllowDBNull
                    };

                    if (col.Size > 0 && dType == typeof(String))
                        dc.MaxLength = col.Size;

                    AsDataTable.Columns.Add(dc);

                    validationLog.Add(new ValidationLog(ValidationTypes.ColumnDefinition, (colLoc + 1), $"Added '{col.Name}' to table successfully.", MessageTypes.Information, ColumnDefinitions[colLoc]));
                    colLoc++;
                }
                retVal = true;
            }
            catch (Exception ex)
            {
                validationLog.Add(new ValidationLog(ValidationTypes.ColumnDefinition, colLoc, ex.Message, MessageTypes.Error, ColumnDefinitions[colLoc]));
            }

            return retVal;
        }
        /// <summary>
        /// Create a DataRow
        /// </summary>
        /// <param name="lineNumber">Row number</param>
        /// <param name="columns">List of all columns.</param>
        /// <param name="dataRow">Return of DataRow data</param>
        /// <param name="validationLog">Will return with all information and errors that may occure.</param>
        /// <returns></returns>
        private bool CreateRow(int lineNumber, List<string> columns, out DataRow dataRow, ref List<ValidationLog> validationLog)
        {
            bool hadIssue = false;

            dataRow = AsDataTable.NewRow();
            for (int colLoc = 0; colLoc < columns.Count; colLoc++)
            {
                var retMessage = "";
                var dataType = GetDataType(ColumnDefinitions[colLoc].DataType);

                validationLog.Add(
                    new ValidationLog(
                        ValidationTypes.ColumnDefinition,
                        colLoc,
                        $"Processing Line# {lineNumber}\nColumn Name: {ColumnDefinitions[colLoc].Name}\nColumn Type: {dataType.FullName}",
                        MessageTypes.Error,
                        ColumnDefinitions[colLoc]));

                if (ConvertColumn(columns[colLoc], ColumnDefinitions[colLoc], out object retData, ref validationLog) && HasValidData(retData, ColumnDefinitions[colLoc], out retMessage))
                {
                    if (retData != DBNull.Value)
                        dataRow[ColumnDefinitions[colLoc].Name] = Convert.ChangeType(retData, dataType);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(retMessage))
                    {
                        validationLog.Add(
                            new ValidationLog(
                                ValidationTypes.DataValidation,
                                lineNumber,
                                retMessage,
                                MessageTypes.Error,
                                ColumnDefinition.Empty));
                    }

                    hadIssue = true;
                    break;
                }
            }

            return hadIssue;
        }
        /// <summary>
        /// Validating against AllowedValues list passed in for DataColumn.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="colDef"></param>
        /// <param name="retMessage"></param>
        /// <returns></returns>
        private bool HasValidData(object data, ColumnDefinition colDef, out string retMessage)
        {
            retMessage = string.Empty;
            if (colDef.AllowedValues.Count == 0 || colDef.AllowedValues.Contains(data))
                return true;
            else
            {
                retMessage = $"Invalid Data in '{colDef.Name}'.  Value found was: '{data}' and allowed values must be within: ({String.Join(",", colDef.AllowedValues)})";
                return false;
            }
        }
        /// <summary>
        /// Convert string data to required data type.
        /// </summary>
        /// <param name="data">Original string data.</param>
        /// <param name="column">Column definition like Type and size.</param>
        /// <param name="newValue">New value after conversion.</param>
        /// <param name="validationLog">Will return with all information and errors that may occure.</param>
        /// <returns></returns>
        private bool ConvertColumn(string data,  ColumnDefinition column, out object newValue, ref List<ValidationLog> validationLog)
        {
            bool retVal = true;
            var dataType = GetDataType(column.DataType);

            newValue = GetDefault(dataType);

            if (column.DataType != DataTypes.String)
            {
                //non-string needs no spaces before conversions.
                data = data.Trim();

                //if empty and no AllowDBNull, no matter the DataType, we are going to skip adding data to the column.
                if (string.IsNullOrWhiteSpace(data) && column.AllowDBNull)
                    //return a DBNull.Value, telling caller to skip column data
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
                            newValue = Convert.FromBase64String(data.ToString());
                            break;
                        default:
                            newValue = data;
                            break;
                    }
                }
            }
            else
                newValue = data;

            if(!retVal)
                validationLog.Add(new ValidationLog(ValidationTypes.ColumnDefinition, 0, $"Failed to convert '{data}' to a {column.DataType.Name()}.", MessageTypes.Error, column));

            return retVal;
        }
        /// <summary>
        /// Parses the line into a list of strings for each columns.
        /// </summary>
        /// <param name="line">String to parse.</param>
        /// <returns></returns>
        private List<string> ParseLine(string line)
        {
            var retVal = new List<string>();

            switch(this.FileType)
            {
                case FileTypes.Comma_Delimited:
                    retVal = line.Split(',').ToList();
                    break;
                case FileTypes.Quote_Comma_Delimited:
                    retVal = QuoteCommaParse(line);
                    break;
                case FileTypes.Fixed_Length_Columns:
                    retVal = FixedLengthParse(line);
                    break;
                case FileTypes.Semicolon_Delimited:
                    retVal = line.Split(';').ToList();
                    break;
                case FileTypes.Tab_Delimited:
                    retVal = line.Split('\t').ToList();
                    break;
                default: break;
            }

            if (this.TrimValues)
                retVal = TrimListValues(retVal);

            return retVal;
        }
        /// <summary>
        /// Special parsing for quote comma delimited.
        /// </summary>
        /// <param name="line">String to parse.</param>
        /// <returns></returns>
        private List<string> QuoteCommaParse(string line)
        {
            var retVal = new List<string>();
            int iStart = line.IndexOf("\"");

            while(iStart >= 0) 
            {
                int iEnd = line.IndexOf("\"", iStart + 1);
                if (iEnd == -1)
                    break;      //file not formatted correctly

                //removing quotes
                var value = line.Substring(iStart + 1, iEnd - (iStart + 1));
                retVal.Add(value);

                iStart = line.IndexOf("\"", iEnd + 1);
            }

            return retVal;
        }
        /// <summary>
        /// Special parsing for fixed length columns, usually created by Main Frame machines.
        /// </summary>
        /// <param name="line">String to parse.</param>
        /// <returns></returns>
        private List<string> FixedLengthParse(string line)
        {
            var retVal = new List<string>();

            foreach (ColumnDefinition cd in ColumnDefinitions)
            {
                int len = (line.Length >= cd.Size ? cd.Size : line.Length);
                if (len <= 0)
                    retVal.Add("");
                else
                {
                    var col = line.Substring(0, len);
                    retVal.Add(col);
                    line = line.Substring(len);
                }
            }

            return retVal;
        }
        /// <summary>
        /// Will trim each value.before storing into DataRow.
        /// </summary>
        /// <param name="retVal">Data to trim up.</param>
        /// <returns></returns>
        private List<string> TrimListValues(List<string> retVal)
        {
            for(int i=0; i< retVal.Count; i++)
                retVal[i] = retVal[i].Trim();

            return retVal;
        }
        /// <summary>
        /// Create a DataTable and name it based on the filename
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private DataTable CreateTable(string fileName)
        {
            string name = RegExFormats.AlphaNumeric.Replace(fileName, "");
            return new DataTable(name);
        }
    }
}
