namespace Chizl.TextConverter
{
    public class ValidationLog
    {
        public ValidationLog(ValidationTypes type, int location, string message, MessageTypes msgType, ColumnDefinition columnDefinition)
        {
            ValidationType = type;
            Column = columnDefinition;
            Location = location;
            Message = message;
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
