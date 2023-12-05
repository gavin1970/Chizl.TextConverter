using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Chizl.TextConverter
{
    internal class Common
    {
        public const int NO_LOCATION = -1;

        private struct Type_Default
        {
            public Type dataType;
            public object defaultValue;
        }

        //search and add to TypeDefaults, we need to lock, just in case someone is multi-threading.
        private static readonly object _lockObj = new object();
        private static List<Type_Default> TypeDefaults { get; } = new List<Type_Default>();

        #region Internal Methods
        /// <summary>
        /// Converts Internal DataTypes to real System Types.
        /// </summary>
        /// <param name="dataType">Chizl.TextConverter DataType</param>
        /// <returns></returns>
        internal static Type GetDataType(DataTypes dataType)
        {
            var dt = dataType == DataTypes.ByteArray ? "Byte[]" : dataType.Name();
            return Type.GetType($"System.{dt}");
        }
        /// <summary>
        /// Create a DataTable and name it based on the filename
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>New DataTable names the same as the FileName.</returns>
        internal static DataTable CreateTable(string fileName)
        {
            string name = RegExFormats.AlphaNumeric.Replace(fileName, "");
            return new DataTable(name);
        }
        /// <summary>
        /// Internal use, but left as public.<br/>
        /// Calls GetDefaultValue() to generate default value;
        /// </summary>
        /// <param name="t"></param>
        /// <returns>Default value based on Type</returns>
        internal static object GetDefault(Type t)
        {
            object retVal;

            lock (_lockObj)
            {
                //don't want to constantly create generics for every column, if we already have type and it's default data in memory.
                var found = TypeDefaults.Find(f => f.dataType.Equals(t));

                if (found.dataType == t)
                    return found.defaultValue;

                var cmn = new Common();
                var metInfo = cmn.GetType().GetMethod("GetDefaultValue", BindingFlags.Static | 
                                                                         BindingFlags.NonPublic | 
                                                                         BindingFlags.Instance);
                
                //something has gone wrong, we can't access our own private method.
                if (metInfo == null)
                    return null;

                //setup generic to pass type
                var genericFooMethod = metInfo.MakeGenericMethod(t);
                //call and get the Type's default data.
                retVal = genericFooMethod.Invoke(cmn, null);

                //add to static for later use.
                TypeDefaults.Add(
                    new Type_Default()
                    {
                        dataType = t,
                        defaultValue = retVal
                    });
            }

            return retVal;
        }
        /// <summary>
        /// Validating against AllowedValues list passed in for DataColumn.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="colDef"></param>
        /// <param name="errMessage"></param>
        /// <returns>bool true = success, false = check AuditLogs</returns>
        internal static bool HasValidData(object data, ColumnDefinition colDef, out string errMessage)
        {
            errMessage = string.Empty;
            if (colDef.AllowedValues.Count == 0 || colDef.AllowedValues.Contains(data))
                return true;
            else
            {
                errMessage = $"Invalid Data in '{colDef.Name}'.  Value found was: '{data}' and allowed values must be within: ({String.Join(",", colDef.AllowedValues)})";
                return false;
            }
        }
        /// <summary>
        /// Parses the line into a list of strings for each columns.
        /// </summary>
        /// <param name="line">String to parse.</param>
        /// <returns>ListArray of strings of each column after parsed for 1 line</returns>
        internal static List<string> ParseLine(string line, FileTypes fileType, bool trimValues, List<ColumnDefinition> columnDefinitions)
        {
            var retVal = new List<string>();

            switch (fileType)
            {
                case FileTypes.Comma_Delimited:
                    //easy split
                    retVal = line.Split(',').ToList();
                    break;
                case FileTypes.Quote_Comma_Delimited:
                    //special case parsing
                    retVal = QuoteCommaParse(line);
                    break;
                case FileTypes.Fixed_Length_Columns:
                    //special case parsing
                    retVal = FixedLengthParse(line, columnDefinitions);
                    break;
                case FileTypes.Semicolon_Delimited:
                    //easy split
                    retVal = line.Split(';').ToList();
                    break;
                case FileTypes.Tab_Delimited:
                    //easy split
                    retVal = line.Split('\t').ToList();
                    break;
                default: break;
            }

            //if configured to trim values, do it while it's a string.
            if (trimValues)
                retVal = TrimListValues(retVal);

            return retVal;
        }
        /// <summary>
        /// Will trim each value.before storing into DataRow.
        /// </summary>
        /// <param name="retVal">Data to trim up.</param>
        /// <returns>ListArray of strings of each column after removing spaces on the front and end of the line.</returns>
        internal static List<string> TrimListValues(List<string> retVal)
        {
            for (int i = 0; i < retVal.Count; i++)
                retVal[i] = retVal[i].Trim();

            return retVal;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Special parsing for quote comma delimited.<br/>
        /// Only concern here if there are \" quotes setup inside of the double quotes.
        /// </summary>
        /// <param name="line">String to parse.</param>
        /// <returns>ListArray of strings of each column after parsed for 1 line</returns>
        private static List<string> QuoteCommaParse(string line)
        {
            var retVal = new List<string>();
            int iStart = line.IndexOf("\"");

            while (iStart >= 0)
            {
                int iEnd = line.IndexOf("\"", iStart + 1);
                //if file not formatted correctly, lets exit
                if (iEnd == -1)
                    break;

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
        /// <returns>ListArray of strings of each column after parsed for 1 line</returns>
        private static List<string> FixedLengthParse(string line, List<ColumnDefinition> columnDefinitions)
        {
            var retVal = new List<string>();

            //loop through columns based on Size set.
            foreach (ColumnDefinition cd in columnDefinitions)
            {
                //possible that the end of the line stops before sizing, so empty string is set.
                int len = (line.Length >= cd.Size ? cd.Size : line.Length);
                if (len <= 0)
                    retVal.Add("");
                else
                {
                    //start at the beginning and grab specified length
                    var col = line.Substring(0, len);
                    //add column
                    retVal.Add(col);
                    //delete column size from beginning of string
                    line = line.Substring(len);
                }
            }

            return retVal;
        }
        /// <summary>
        /// Internal use<br/>
        /// Since we can't do this: var test = default(Type.GetType($"System.Int32"));<br/>
        /// To use it, call this instead: GetDefault(Type t) instead.<br/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>Default value based on Type</returns>
        private static T GetDefaultValue<T>()
        {
            return default(T);
        }
        #endregion
    }
}
