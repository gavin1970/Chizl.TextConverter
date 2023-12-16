using System.Collections.Generic;
using System.Data;

namespace Chizl.TextConverter
{
    public abstract class UserProperties
    {
        private List<ColumnDefinition> _columnDefinitions = new List<ColumnDefinition>();
        /// <summary>
        /// Validation Logs is an audit log with all successful and failed messages.
        /// </summary>
        public List<AuditLog> AuditLogs { get; } = new List<AuditLog>();
        /// <summary>
        /// Get full path and file name.
        /// </summary>
        public string FilePath { get; internal set; } = string.Empty;
        /// <summary>
        /// Directory of FileName
        /// </summary>
        public string FileName { get; internal set; } = string.Empty;
        /// <summary>
        /// Directory of FilePath
        /// </summary>
        public string FileDirectory { get; internal set; } = string.Empty;
        /// <summary>
        /// Get the set FileType for the file.
        /// </summary>
        public FileTypes FileType { get; internal set; } = FileTypes.Empty;
        /// <summary>
        /// Get DataTable that was pulled from file or the existing DataTable used to save to disc.
        /// </summary>
        public DataTable AsDataTable { get; internal set; }
        /// <summary>
        /// Default: new List<ColumnDefinition>()</br>
        /// Each Column Definition and all their properties.<br/>
        /// null will be treated as ColumnDefinitions.Clear().
        /// </summary>
        public List<ColumnDefinition> ColumnDefinitions
        { 
            get { return _columnDefinitions; } 
            set { _columnDefinitions = value ?? (_columnDefinitions = new List<ColumnDefinition>()); } 
        }
        /// <summary>
        /// Default: false<br/>
        /// --------------------------<br/>
        /// Set to true to trim on Load or set to true if trim on Save.<br/>
        /// Trim for Load and Save are 2 completely separate settings.<br/>
        /// Be sure to set for each unless the data you pulled was trimmed<br/>
        /// and that is what is being passed to Save.
        /// </summary>
        public bool TrimValues { get; set; } = false;
        /// <summary>
        /// Default: false</br>
        /// --------------------------<br/>
        /// When loading a file or saving to file, the first row will be the column names.
        /// </summary>
        public bool FirstRowIsHeader { get; set; } = false;
        /// <summary>
        /// Default: false<br/>
        /// Use by SaveFile class only, setting this to true will ignore the order<br/>
        /// of the DataTable and only create a file based on columns set in ColumnDefinitions.</br>
        /// When set to false, the file will be saved based on the DataTable, but will use any</br>
        /// ColumnDefinitions set.  And all columns are not required to have a ColumnDefinition.
        /// </summary>
        public bool FileByColDefOnly { get; internal set; } = false;
    }
}
