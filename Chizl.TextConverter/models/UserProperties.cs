using System.Collections.Generic;
using System.Data;

namespace Chizl.TextConverter
{
    public abstract class UserProperties
    {
        /// <summary>
        /// Validation Logs is an audit log with all successful and failed messages.
        /// </summary>
        public List<AuditLog> AuditLogs { get; } = new List<AuditLog>();
        /// <summary>
        /// Get full path and file name.
        /// </summary>
        public string FilePath { get; internal set; } = string.Empty;
        /// <summary>
        /// Get the set FileType for the file.
        /// </summary>
        public FileTypes FileType { get; internal set; } = FileTypes.Empty;
        /// <summary>
        /// Get DataTable that was pulled from file or the existing DataTable used to save to disc.
        /// </summary>
        public DataTable AsDataTable { get; internal set; }
        /// <summary>
        /// Each Column Definition and all their properties.
        /// </summary>
        public List<ColumnDefinition> ColumnDefinitions { get; set; } = new List<ColumnDefinition>();
        /// <summary>
        /// Default: false<br/>
        /// Set to true to trim on Load or set to true if trim on Save.<br/>
        /// Trim for Load and Save are 2 completely separate settings.<br/>
        /// Be sure to set for each unless the data you pulled was trimmed<br/>
        /// and that is what is being passed to Save.
        /// </summary>
        public bool TrimValues { get; set; } = false;
    }
}
