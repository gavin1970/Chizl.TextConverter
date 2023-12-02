namespace Chizl.TextConverter
{
    /// <summary>
    /// Represents log entries for internal processing that will be passed back to the caller.<br/>
    /// <br/>
    /// <a href="https://github.com/gavin1970/Chizl.TextConverter/blob/master/Chizl.TextConverter/models/ValidationLog.cs">View on Github</a>
    /// </summary>
    public class ValidationLog
    {
        /// <summary>
        /// Represents log entries for internal processing that will be passed back to the caller.<br/>
        /// <br/>
        /// <a href="https://github.com/gavin1970/Chizl.TextConverter/blob/master/Chizl.TextConverter/models/ValidationLog.cs">View on Github</a>
        /// </summary>
        /// <param name="type">Type of task</param>
        /// <param name="location">Column or Row Number</param>
        /// <param name="message">Information or Error that might occur.</param>
        /// <param name="msgType">Type of message, Information or Error</param>
        /// <param name="columnDefinition">The column that it's currently processing. Check ColumnDefinition.IsEmpty before using it.</param>
        public ValidationLog(ValidationTypes type, int location, string message, MessageTypes msgType, ColumnDefinition columnDefinition)
        {
            ValidationType = type;
            Column = columnDefinition;
            Location = location;
            Message = message;
            MessageType = msgType;
        }
        /// <summary>
        /// Define what type of issue is it.
        /// </summary>
        public ValidationTypes ValidationType { get; }
        /// <summary>
        /// Line in source file that had an issue or Column number that had an issue.
        /// </summary>
        public int Location { get; }
        /// <summary>
        /// Column definition required for source file.
        /// </summary>
        public ColumnDefinition Column { get; }
        /// <summary>
        /// Error of Information
        /// </summary>
        public MessageTypes MessageType { get; }
        /// <summary>
        /// Description of issue that occured.
        /// </summary>
        public string Message { get; }
    }
}
