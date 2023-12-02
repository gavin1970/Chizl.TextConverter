using System;
using System.IO;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Collections;
using System.Data.Common;

namespace Chizl.TextConverter
{
    public class FileLoad
    {
        private DataTable _dataTable = new DataTable();
        private readonly Regex _rgx = new Regex("[^a-zA-Z0-9 -]");

        public FileLoad(string filePath, FileType fileType)
        {
            if(string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException($"'{nameof(filePath)}' is a required parameter.");

            if (!File.Exists(filePath))
                throw new ArgumentException($"'{filePath}' does not exists.");

            FilePath = filePath;
            InputFileType = fileType;
        }

        public string FilePath { get; } = string.Empty;
        public FileType InputFileType { get; }
        public List<ColumnDefinition> ColumnDefinitions { get; set; } = new List<ColumnDefinition>();
        public DataTable ToDataTable { get { return _dataTable; } }

        public bool TrimValues { get; set; } = false;

        private Type GetDataType(DataTypes dataType)
        {
            var dt = dataType == DataTypes.ByteArray ? "Byte[]" : dataType.Name();
            return Type.GetType($"System.{dt}");
        }
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
                validationLog.Add(new ValidationLog(ValidationTypes.Prep, 0, $"file: '{FilePath}' exists.", MessageTypes.Information, ColumnDefinition.Empty));
                _dataTable = CreateTable(Path.GetFileName(FilePath));
                validationLog.Add(new ValidationLog(ValidationTypes.Prep, 0, $"Table name '{_dataTable.TableName}' created.", MessageTypes.Information, ColumnDefinition.Empty));

                int colLoc = 0;
                try
                {
                    foreach (ColumnDefinition col in ColumnDefinitions)
                    {
                        var dType = GetDataType(col.DataType);

                        DataColumn dc = new DataColumn(col.Name, dType);
                        dc.AllowDBNull = col.AllowDBNull;

                        if (col.Size > 0 && dType==typeof(String))
                            dc.MaxLength = col.Size;
                        _dataTable.Columns.Add(dc);

                        validationLog.Add(new ValidationLog(ValidationTypes.ColumnDefinition, (colLoc + 1), $"Added '{col.Name}' to table successfully.", MessageTypes.Information, ColumnDefinitions[colLoc]));
                        colLoc++;
                    }
                } 
                catch(Exception ex)
                {
                    validationLog.Add(new ValidationLog(ValidationTypes.ColumnDefinition, colLoc, ex.Message, MessageTypes.Error, ColumnDefinitions[colLoc]));
                    return retVal;
                }

                try
                {
                    using (StreamReader sr = new StreamReader(FilePath))
                    {
                        bool hadIssue = false;
                        string line;

                        while ((line = sr.ReadLine()) != null)
                        {
                            //bool recIssue = false;
                            DataRow dataRow = _dataTable.NewRow();
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

                            for (colLoc = 0; colLoc < columns.Count; colLoc++)
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
                                    if(retData != DBNull.Value)
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
                                    continue;
                                }
                            }

                            _dataTable.Rows.Add(dataRow);
                        }

                        if (!hadIssue)
                            retVal = true;
                    }
                } 
                catch(Exception ex)
                {
                    validationLog.Add(new ValidationLog(ValidationTypes.FileLoad, lineNumber, ex.Message, MessageTypes.Error, ColumnDefinition.Empty));
                }
            }
            else
                validationLog.Add(new ValidationLog(ValidationTypes.Prep, 0, $"File '{FilePath}' Missing", MessageTypes.Error, ColumnDefinition.Empty));

            return retVal; 
        }

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
        public bool ConvertColumn(string data,  ColumnDefinition column, out object newValue, ref List<ValidationLog> validationLog)
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
                        case DataTypes.Int32:
                            if (Int32.TryParse(data, out Int32 iVal))
                                newValue = iVal;
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
        private List<string> ParseLine(string line)
        {
            var retVal = new List<string>();

            switch(this.InputFileType)
            {
                case FileType.Comma_Delimited:
                    retVal = line.Split(',').ToList();
                    break;
                case FileType.Fixed_Length_Columns:
                    retVal = FixedLengthParse(line);
                    break;
                case FileType.Semicolon_Delimited:
                    retVal = line.Split(';').ToList();
                    break;
                case FileType.Tab_Delimited:
                    retVal = line.Split('\t').ToList();
                    break;
                default: break;
            }

            if (this.TrimValues)
                retVal = TrimListValues(retVal);

            return retVal;
        }
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
                    string col = line.Substring(0, len);
                    retVal.Add(col);
                    line = line.Substring(len);
                }
            }

            return retVal;
        }
        private List<string> TrimListValues(List<string> retVal)
        {
            for(int i=0; i< retVal.Count; i++)
                retVal[i] = retVal[i].Trim();

            return retVal;
        }
        private DataTable CreateTable(string tableName)
        {
            return new DataTable(_rgx.Replace(tableName, ""));
        }
        public object GetDefault(Type t)
        {
            return this.GetType().GetMethod("GetDefaultValue").MakeGenericMethod(t).Invoke(this, null);
        }
        public T GetDefaultValue<T>()
        {
            return default(T);
        }
    }
}
