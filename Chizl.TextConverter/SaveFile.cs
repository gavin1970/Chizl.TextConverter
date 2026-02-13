using System;
using System.Data;
using System.IO;
using System.Text;

namespace Chizl.TextConverter
{
    /// <summary>
    /// Take a DataTable and Save to specified File Type file.<br/>
    /// <br/>
    /// <a href="https://github.com/gavin1970/Chizl.TextConverter/blob/master/Chizl.TextConverter/SaveFile.cs">View on Github</a>
    /// </summary>
    public class SaveFile : UserProperties
    {
        /// <summary>
        /// Setup a new Class without setup.<br/>
        /// var saveFile = SaveFile.Empty
        /// </summary>
        public static SaveFile Empty { get { return new SaveFile(); } }
        /// <summary>
        /// Is fullly initialzed?
        /// </summary>
        public bool IsEmpty { get; private set; } = true;

        #region Constructors
        /// <summary>
        /// Only available via Empty property.
        /// </summary>
        private SaveFile() { }
        /// <summary>
        /// Take a DataTable and Save to specified File Type file.<br/>
        /// <br/>
        /// <a href="https://github.com/gavin1970/Chizl.TextConverter/blob/master/Chizl.TextConverter/SaveFile.cs">View on Github</a>
        /// </summary>
        /// <param name="dataTable">DataTable to save as file.</param>
        /// <param name="dstFile">Full file path and name of the file to write to.</param>
        /// <param name="dstFileType">What file type to save.</param>
        /// <param name="overwriteDstFile">Default false:<br/>
        /// If file exists, delete it, before creating it.</param>
        /// <param name="createFolder">Default: true<br/>
        /// If folder/directory doesn't exist, create it.</param>
        /// <exception cref="ArgumentException">Any parameters with error.</exception>
        public SaveFile(DataTable dataTable, string dstFile, FileTypes dstFileType, bool fileByColDefOnly = false, bool overwriteFile = false, bool createFolder = true)
        {
            if (dataTable == null || dataTable.Columns.Count == 0 || dataTable.Rows.Count == 0)
                throw new ArgumentException($"'{nameof(dataTable)}' can not be null, no columns, or no rows.");
            else if (string.IsNullOrWhiteSpace(dstFile))
                throw new ArgumentException($"'{nameof(dstFile)}' can not be blank or null.");
            else if (dstFileType == FileTypes.Empty)
                throw new ArgumentException($"'{dstFileType}' can not be set to Empty.  This is for internal use only.");

            FilePath = dstFile.Contains("/") ? dstFile.Replace("/", "\\") : dstFile;    //set before IO validation

            if (!overwriteFile && File.Exists(FilePath))
                throw new ArgumentException($"'{FilePath}' file exists and the overwriteDstFile is set to False.");

            FileDirectory = Path.GetDirectoryName(FilePath);
            FileName = Path.GetFileName(FilePath);

            if (!createFolder && !Directory.Exists(FileDirectory))
                throw new ArgumentException($"'{FileDirectory}' directory doesn't exist and '{nameof(createFolder)}' is set to False.");

            FileByColDefOnly = fileByColDefOnly;
            AsDataTable = dataTable;
            FileType = dstFileType;
            IsEmpty = false;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Save the set DataTable to File based on FileType.
        /// </summary>
        /// <returns>If successfully saved.</returns>
        public bool Save()
        {
            bool retVal = false;

            if (IsEmpty)
                AuditLogs.Add(new AuditLog(AuditTypes.Initialize, Common.NO_LOCATION, $"SaveFile class has not initialize.", MessageTypes.Error, ColumnDefinition.Empty));
            else if (CheckDestination())
                retVal = GenerateFile();

            return retVal;
        }
        #endregion

        #region Private Methods
        private bool GenerateFile()
        {
            bool retVal = false;
            switch(FileType)
            {
                case FileTypes.Tab_Delimited:
                    retVal = CreateFileByDelimiter("\t");
                    break;
                case FileTypes.Semicolon_Delimited:
                    retVal = CreateFileByDelimiter(";");
                    break;
                case FileTypes.Comma_Delimited:
                    retVal = CreateFileByDelimiter(",");
                    break;
                case FileTypes.Quote_Comma_Delimited:
                    retVal = CreateFileByDelimiter("\",\"", true);
                    break;
                case FileTypes.Fixed_Length_Columns:
                    retVal = CreateFileByFixedLength();
                    break;
            }

            if(!retVal)
            {
                try
                {
                    if (File.Exists(FilePath))
                        File.Delete(FilePath);
                }
                catch (IOException ex)
                {
                    AuditLogs.Add(
                        new AuditLog(
                            AuditTypes.File,
                            Common.NO_LOCATION,
                            $"Failed to delete file '{FilePath}'.\n" +
                            $"Exception: {ex.Message}",
                            MessageTypes.Error,
                            ColumnDefinition.Empty));
                }
            }

            return retVal;
        }
        private bool CreateFileByFixedLength()
        {
            var retVal = false;

            if(ColumnDefinitions.Count == 0)
            {
                AuditLogs.Add(
                    new AuditLog(
                        AuditTypes.Initialize,
                        Common.NO_LOCATION,
                        "For Fixed Length Column saves, column definitions are required.",
                        MessageTypes.Error,
                        ColumnDefinition.Empty));
                return retVal;
            }

            var dt = AsDataTable.Copy();

            if (dt == null)
            {
                AuditLogs.Add(
                    new AuditLog(
                        AuditTypes.Initialize,
                        Common.NO_LOCATION,
                        "Failed to pull table.",
                        MessageTypes.Error,
                        ColumnDefinition.Empty));
                return retVal;
            }

            AuditLogs.Add(
                new AuditLog(
                    AuditTypes.File,
                    Common.NO_LOCATION,
                    $"Create '{FileName}' from DataTable: {dt.TableName} " +
                    $"with {dt.Rows.Count} row{(dt.Rows.Count == 1 ? "" : "s")}",
                    MessageTypes.Information,
                    ColumnDefinition.Empty));

            try
            {
                using (StreamWriter sw = new StreamWriter(FilePath))
                {
                    if (FirstRowIsHeader)
                    {
                        var sb = new StringBuilder();

                        foreach (var cd in ColumnDefinitions)
                        {
                            var colName = cd.Name.Trim();
                            var maxLegth = cd.Size;

                            if (colName.Length > maxLegth)
                                colName = colName.Substring(0, maxLegth);
                            else 
                            {
                                var diff = maxLegth - colName.Length;
                                colName = $"{colName}{(new string(' ', diff))}";
                            }
                            sb.Append(colName);
                        }

                        var line = sb.ToString();
                        sw.WriteLine(line);
                    }

                    foreach (DataRow dr in dt.Rows)
                    {
                        var sb = new StringBuilder();

                        foreach (var cd in ColumnDefinitions)
                        {
                            var maxLegth = cd.Size;
                            //create empty string for two different type of conversions.
                            var data = "";
                            //should never be, but lets check.
                            if (dr[cd.Name] != null)
                            {
                                {
                                    //check if data inside is a DBNull Type.
                                    if (dr[cd.Name].GetType() == typeof(DBNull))
                                        data = "";
                                    else if (cd.DataType == DataTypes.ByteArray)
                                        //if byte array, convert to Base64 string for display
                                        data = Convert.ToBase64String((byte[])dr[cd.Name]);
                                    else
                                        data = dr[cd.Name].ToString();
                                }
                            }

                            //Every column is required to have a ColumnDefinition.
                            if (!ValidateData(data, cd.Name, out string newData, true))
                                throw new Exception($"Validation of column '{cd.Name}' " +
                                                    $"with data '{data}' from DataTable '{dt.TableName}' " +
                                                    $"to File has failed.  View previous errors.");


                            if (newData.Length > maxLegth)
                                newData = newData.Substring(0, maxLegth);
                            else
                            {
                                var diff = maxLegth - newData.Length;
                                newData = $"{newData}{(new string(' ', diff))}";
                            }

                            sb.Append(newData);
                        }

                        var line = sb.ToString();

                        //write row
                        sw.WriteLine(line);
                    }
                }
                retVal = true;
            }
            catch (Exception ex)
            {
                AuditLogs.Add(
                    new AuditLog(
                        AuditTypes.File,
                        Common.NO_LOCATION,
                        $"Failed to create '{FileName}' from DataTable: " +
                        $"{dt.TableName}.\nException: {ex.Message}",
                        MessageTypes.Error,
                        ColumnDefinition.Empty));
            }

            return retVal;
        }
        private bool CreateFileByDelimiter(string delimiter, bool quoteWrap = false)
        {
            var retVal = false;
            var dt = AsDataTable.Copy();
            var quote = quoteWrap ? "\"" : "";

            if (dt == null)
            {
                AuditLogs.Add(
                    new AuditLog(
                        AuditTypes.Initialize, 
                        Common.NO_LOCATION, 
                        "Failed to pull table.", 
                        MessageTypes.Error, 
                        ColumnDefinition.Empty));
                return retVal;
            }

            AuditLogs.Add(
                new AuditLog(
                    AuditTypes.File, 
                    Common.NO_LOCATION, 
                    $"Create '{FileName}' from DataTable: {dt.TableName} " +
                    $"with {dt.Rows.Count} row{(dt.Rows.Count == 1 ? "" : "s")}", 
                    MessageTypes.Information, 
                    ColumnDefinition.Empty));

            try
            {
                using (StreamWriter sw = new StreamWriter(FilePath))
                {
                    if (FirstRowIsHeader)
                    {
                        var sb = new StringBuilder();
                        var col = 0;

                        foreach (DataColumn dc in dt.Columns)
                            sb.Append($"{dc.ColumnName}{(++col >= dt.Columns.Count ? "" : delimiter)}");

                        var line = $"{quote}{sb}{quote}";
                        sw.WriteLine(line);
                    }

                    foreach (DataRow dr in dt.Rows)
                    {
                        var sb = new StringBuilder();
                        var col = 0;

                        if (FileByColDefOnly)
                        {
                            foreach(var cd in ColumnDefinitions)
                            {
                                //create empty string for two different type of conversions.
                                var data = "";
                                //should never be, but lets check.
                                if (dr[cd.Name] != null)
                                {
                                    {
                                        //check if data inside is a DBNull Type.
                                        if (dr[cd.Name].GetType() == typeof(DBNull))
                                            data = "";
                                        else if (cd.DataType == DataTypes.ByteArray)
                                            //if byte array, convert to Base64 string for display
                                            data = Convert.ToBase64String((byte[])dr[cd.Name]);
                                        else
                                            data = dr[cd.Name].ToString();
                                    }
                                }

                                //Every column is required to have a ColumnDefinition.
                                if (!ValidateData(data, cd.Name, out string newData, true))
                                    throw new Exception($"Validation of column '{cd.Name}' " +
                                                        $"with data '{data}' from DataTable '{dt.TableName}' " +
                                                        $"to File has failed.  View previous errors.");

                                sb.Append($"{newData}{(++col >= dt.Columns.Count ? "" : delimiter)}");
                            }
                        }
                        else
                        {
                            foreach (DataColumn dc in dt.Columns)
                            {
                                //create empty string for two different type of conversions.
                                var data = "";
                                //should never be, but lets check.
                                if (dr[dc.ColumnName] != null)
                                {
                                    //check if data inside is a DBNull Type.
                                    if (dr[dc.ColumnName].GetType() == typeof(DBNull))
                                        data = "";
                                    else if (dc.DataType == typeof(byte[]))
                                        //if byte array, convert to Base64 string for display
                                        data = Convert.ToBase64String((byte[])dr[dc.ColumnName]);
                                    else
                                        data = dr[dc.ColumnName].ToString();
                                }

                                //Every column not required to have a ColumnDefinition.
                                //Lets see if this is one does.
                                if (!ValidateData(data, dc.ColumnName, out string newData))
                                    throw new Exception($"Validation of column '{dc.ColumnName}' " +
                                                        $"with data '{data}' from DataTable '{dt.TableName}' " +
                                                        $"to File has failed.  View previous errors.");

                                sb.Append($"{newData}{(++col >= dt.Columns.Count ? "" : delimiter)}");
                            }
                        }

                        var line = $"{quote}{sb}{quote}";

                        //write row
                        sw.WriteLine(line);
                    }
                }
                retVal = true;
            } 
            catch(Exception ex)
            {
                AuditLogs.Add(
                    new AuditLog(
                        AuditTypes.File, 
                        Common.NO_LOCATION, 
                        $"Failed to create '{FileName}' from DataTable: " +
                        $"{dt.TableName}.\nException: {ex.Message}", 
                        MessageTypes.Error, 
                        ColumnDefinition.Empty));
            }

            return retVal;
        }
        private bool ValidateData(string raw, string columnName, out string newVal, bool required = false)
        {
            var retVal = true;

            if (TrimValues)
                newVal = raw.Trim();
            else
                newVal = raw;

            var colDef = ColumnDefinitions.Find(f => f.Name == columnName);
            var colLoc = ColumnDefinitions.FindIndex(f => f.Name == columnName) + (colDef != null ? 1 : 0);

            if (colDef != null)
            {
                AuditLogs.Add(
                    new AuditLog(
                        AuditTypes.ColumnConversion,
                        colLoc,
                        $"Column Definition was found for column '{columnName}'.",
                        MessageTypes.Information,
                        colDef));

                var allowedValues = colDef.AllowedValues;
                int decimalSize = colDef.DecimalSize;

                if (decimalSize > -1)
                {
                    if (decimal.TryParse(newVal.ToString(), out decimal dblVal))
                        newVal = Math.Round(dblVal, decimalSize).ToString();
                    else
                    {
                        retVal = false;
                        AuditLogs.Add(
                            new AuditLog(
                                AuditTypes.ColumnConversion,
                                colLoc,
                                $"DecimalSize is set to '{decimalSize}' for column '{columnName}', but during " +
                                $"convertsion '{newVal}' could not be convert to a decimal to set floating pointer size.",
                                MessageTypes.Error,
                                colDef));
                    }
                }

                if (allowedValues != null && allowedValues.Count > 0)
                {
                    if (!allowedValues.Contains(newVal))
                    {
                        retVal = false;
                        AuditLogs.Add(
                            new AuditLog(
                                AuditTypes.ColumnConversion,
                                colLoc,
                                $"AllowedValues is set to '{String.Join(",", allowedValues)}' for " +
                                $"column '{columnName}', but the value of '{newVal}' was found.",
                                MessageTypes.Error,
                                colDef));
                    }
                }
            } 
            else if(required)
            {
                retVal = false;
                AuditLogs.Add(
                    new AuditLog(
                        AuditTypes.ColumnConversion,
                        colLoc,
                        $"Column '{columnName}' is required to have a ColumnDefinition, but it's missing.",
                        MessageTypes.Error,
                        colDef));
            }

            return retVal;
        }
        /// <summary>
        /// Create folder if it doesn't exist.<br/>
        /// Check if file exists and delete's it.
        /// </summary>
        /// <returns>bool true = success, false = check AuditLogs</returns>
        private bool CheckDestination()
        {
            var retVal = true;

            try
            {
                if (CheckDirectory(Path.GetDirectoryName(FilePath)) && File.Exists(FilePath))
                {
                    AuditLogs.Add(
                        new AuditLog(
                            AuditTypes.File, 
                            Common.NO_LOCATION, 
                            $"File '{FilePath}' exists.  " +
                            $"Attempting to delete.", 
                            MessageTypes.Information, 
                            ColumnDefinition.Empty));

                    File.Delete(FilePath);
                }
            }
            catch (IOException ex)
            {
                retVal = false;
                AuditLogs.Add(
                    new AuditLog(
                        AuditTypes.File,
                        Common.NO_LOCATION,
                        $"Failed to delete file '{FilePath}'.\n" +
                        $"Exception: {ex.Message}",
                        MessageTypes.Error,
                        ColumnDefinition.Empty));
            }

            return retVal;
        }
        /// <summary>
        /// Checks and creates folder structure from the ground up.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool CheckDirectory(string path)
        {
            var seps = new char[2] { '\\', '/' };
            var retVal = true;

            try
            {
                if (!Directory.Exists(path))
                {
                    AuditLogs.Add(new AuditLog(AuditTypes.Directory, Common.NO_LOCATION, $"Attempting to create: '{path}'.", MessageTypes.Information, ColumnDefinition.Empty));

                    var spltPath = path.Split(seps);
                    var buildPath = "";

                    for (int i = 0; i < spltPath.Length; i++)
                    {
                        if(buildPath.Length > 0)
                            buildPath += "/";   // "/" works in windows an linux

                        buildPath += spltPath[i];

                        if (!Directory.Exists(buildPath))
                            Directory.CreateDirectory(buildPath);
                    }

                    AuditLogs.Add(new AuditLog(AuditTypes.Directory, Common.NO_LOCATION, $"Directory '{path}' successfully created.", MessageTypes.Information, ColumnDefinition.Empty));
                }
            }
            catch (Exception ex) 
            {
                retVal = false;
                AuditLogs.Add(new AuditLog(AuditTypes.Directory, Common.NO_LOCATION, $"Exception creating: '{path}' - {ex.Message}", MessageTypes.Error, ColumnDefinition.Empty));
            }

            return retVal;
        }
        #endregion
    }
}