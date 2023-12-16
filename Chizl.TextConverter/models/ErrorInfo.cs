using System;
using System.Diagnostics;

namespace Chizl
{
    internal class ErrorInfo
    {
        internal ErrorInfo(Exception ex) => this.Exception = ex;
        internal Exception Exception { get; }
        internal StackTrace StackTrace { get { return new StackTrace(this.Exception, true); } }
        internal StackFrame StackFrame { get { return this.StackTrace.GetFrame(0); } }
        internal string FileName { get { return this.StackFrame.GetFileName(); } }
        internal int LineNumber { get { return this.StackFrame.GetFileLineNumber(); } }
    }
}
