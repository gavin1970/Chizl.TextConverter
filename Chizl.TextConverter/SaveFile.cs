using System;
using System.Data;
using System.IO;

namespace Chizl.TextConverter
{
    public class SaveFile : UserProperties
    {
        private bool OverwriteDstFile { get; }
        public SaveFile(DataTable dataTable, string dstFile, FileTypes dstFileType, bool overwriteDstFile = false)
        {
            if (dataTable == null || dataTable.Columns.Count == 0 || dataTable.Rows.Count == 0)
                throw new ArgumentException($"'{nameof(dataTable)}' can not be null, no columns, or no rows.");
            else if (string.IsNullOrWhiteSpace(dstFile))
                throw new ArgumentException($"'{nameof(dstFile)}' can not be blank or null.");
            else if (!overwriteDstFile && File.Exists(dstFile))
                throw new ArgumentException($"'{dstFile}' file exists and the overwriteDstFile is set to False.");
            else if (dstFileType == FileTypes.Empty)
                throw new ArgumentException($"'{dstFileType}' can not be set to Empty.  This is for internal use only.");

            //should never really need this at this point, since it's
            //already validated, but doesn't hurt to double check later.
            OverwriteDstFile = overwriteDstFile;    
            AsDataTable = dataTable;
            FilePath = dstFile;
            FileType = dstFileType;
        }

        public bool Save()
        {
            bool success = false;

            if(!CheckDestination())
                return success;


            throw new NotImplementedException("This is not ready yet.");
        }

        /// <summary>
        /// Check if file exists and delete it.
        /// </summary>
        /// <returns>bool true = success, false = check AuditLogs</returns>
        private bool CheckDestination()
        {
            bool retVal = false;
            try
            {
                AuditLogs.Add(new AuditLog(AuditTypes.DeleteFile, Common.NO_LOCATION, $"Checking if file '{FilePath}' exists.", MessageTypes.Information, ColumnDefinition.Empty));

                if (OverwriteDstFile && File.Exists(FilePath))
                    File.Delete(FilePath);

                retVal = true;
            }
            catch (Exception ex)
            {
                AuditLogs.Add(new AuditLog(AuditTypes.DeleteFile, Common.NO_LOCATION, ex.Message, MessageTypes.Error, ColumnDefinition.Empty));
            }

            return retVal;
        }
    }
}
