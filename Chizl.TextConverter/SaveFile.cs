using System;
using System.Data;
using System.IO;

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
        public SaveFile(DataTable dataTable, string dstFile, FileTypes dstFileType, bool overwriteDstFile = false, bool createFolder = true)
        {
            if (dataTable == null || dataTable.Columns.Count == 0 || dataTable.Rows.Count == 0)
                throw new ArgumentException($"'{nameof(dataTable)}' can not be null, no columns, or no rows.");
            else if (string.IsNullOrWhiteSpace(dstFile))
                throw new ArgumentException($"'{nameof(dstFile)}' can not be blank or null.");
            else if (dstFileType == FileTypes.Empty)
                throw new ArgumentException($"'{dstFileType}' can not be set to Empty.  This is for internal use only.");

            FilePath = dstFile.Contains("/") ? dstFile.Replace("/", "\\") : dstFile;    //set before IO validation

            if (!overwriteDstFile && File.Exists(FilePath))
                throw new ArgumentException($"'{FilePath}' file exists and the overwriteDstFile is set to False.");

            FileDirectory = Path.GetDirectoryName(FilePath);
            FileName = Path.GetFileName(FilePath);

            if (!createFolder && !Directory.Exists(FileDirectory))
                throw new ArgumentException($"'{FileDirectory}' directory doesn't exist and '{nameof(createFolder)}' is set to False.");

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
            if(!CheckDestination())
                return false;
            else
                throw new NotImplementedException("This is not implemented yet.");
        }
        #endregion

        #region Private Methods
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
                    AuditLogs.Add(new AuditLog(AuditTypes.File, Common.NO_LOCATION, $"File '{FilePath}' exists.  attempting to delete.", MessageTypes.Information, ColumnDefinition.Empty));
                    File.Delete(FilePath);
                }
            }
            catch (Exception ex)
            {
                retVal = false;
                AuditLogs.Add(new AuditLog(AuditTypes.File, Common.NO_LOCATION, ex.Message, MessageTypes.Error, ColumnDefinition.Empty));
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
                            buildPath += "\\";

                        buildPath += spltPath[i];

                        if (!Directory.Exists(buildPath))
                            Directory.CreateDirectory(buildPath);
                    }
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
