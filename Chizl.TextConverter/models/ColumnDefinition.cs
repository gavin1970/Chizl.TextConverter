using System.Collections.Generic;

namespace Chizl.TextConverter
{
    /// <summary>
    /// Setup a column definition.<br/>
    /// The following properties can be setup post construction: (AllowedValues, AllowDBNull)
    /// <br/>
    /// <a href="https://github.com/gavin1970/Chizl.TextConverter/blob/master/Chizl.TextConverter/models/ColumnDefinition.cs">View on Github</a>
    /// </summary>
    public class ColumnDefinition
    {
        #region Constructors
        /// <summary>
        /// Only the property Empty can call this Constructor.
        /// </summary>
        private ColumnDefinition() 
        {
            this.IsEmpty = true;
        }

        /// <summary>
        /// Setup a column's definition.<br/>
        /// The following properties can be setup post construction: (AllowedValues, AllowDBNull)
        /// <br/>
        /// <a href="https://github.com/gavin1970/Chizl.TextConverter/blob/master/Chizl.TextConverter/models/ColumnDefinition.cs">View on Github</a>
        /// </summary>
        /// <param name="name">Column Name</param>
        /// <param name="dataType">Data Type required for column.</param>
        /// <param name="size">Size of column, based on Data Type<br/>
        /// String max length of string or Fixed length size of field.
        /// </param>
        /// <param name="decimals">If double or float, what is floating value.</param>
        public ColumnDefinition(string name, DataTypes dataType, int size = 0, int decimalSize = -1)
        {
            this.Name = name;
            this.Size = size;
            this.DataType = dataType;
            this.DecimalSize = decimalSize;
        }
        #endregion

        #region Handle empty class to be passed.
        /// <summary>
        /// Return if this is a empty class or not.
        /// </summary>
        public bool IsEmpty { get; } = false;
        /// <summary>
        /// Create an empty class to pass in, when not required, but can't be null.
        /// </summary>
        public static ColumnDefinition Empty { get { return new ColumnDefinition(); } }
        #endregion

        #region Set Property Definitions
        /// <summary>
        /// Column name.
        /// </summary>
        public string Name { get; } = string.Empty;
        /// <summary>
        /// Only required for String DataTypes or Fixed Lenght imports.
        /// </summary>
        public int Size { get; } = int.MinValue;
        /// <summary>
        /// Data type of column.
        /// </summary>
        public DataTypes DataType { get; } = DataTypes.String;
        /// <summary>
        /// Default: -1 = No Rounding, leave as is.<br/>
        /// Floating point value for Double or Float DataTypes
        /// </summary>
        public int DecimalSize { get; } = -1;
        /// <summary>
        /// If only specific values are allowed, they can be added here.
        /// </summary>
        public List<object> AllowedValues { get; set; } = new List<object>();
        /// <summary>
        /// Allows a column to have null values, including Int64 for examlpe.
        /// </summary>
        public bool AllowDBNull { get; set; } = false;
        #endregion
    }
}
