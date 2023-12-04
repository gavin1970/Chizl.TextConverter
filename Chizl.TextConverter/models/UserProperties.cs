using System.Collections.Generic;
using System.Data;

namespace Chizl.TextConverter
{
    public abstract class UserProperties
    {
        public string FilePath { get; internal set; } = string.Empty;
        public FileTypes FileType { get; internal set; } = FileTypes.Empty;
        public DataTable AsDataTable { get; internal set; }
        public List<ColumnDefinition> ColumnDefinitions { get; set; } = new List<ColumnDefinition>();
        public bool TrimValues { get; set; } = false;
    }
}
