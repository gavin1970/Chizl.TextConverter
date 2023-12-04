using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace Chizl.TextConverter
{
    public class SaveFile : UserProperties
    {
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

            AsDataTable = dataTable;
            FilePath = dstFile;
            FileType = dstFileType;
        }

        public bool Save(out List<ValidationLog> validationLog)
        {
            validationLog = new List<ValidationLog>();
            throw new NotImplementedException("This is not ready yet.");
        }
    }
}
