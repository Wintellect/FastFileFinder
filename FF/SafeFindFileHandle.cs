using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics.CodeAnalysis;

namespace FastFind
{
    /// <summary>
    /// Wraps up the FindFirstFileEx and FindNextFile handle.
    /// </summary>
    internal sealed class SafeFindFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeFindFileHandle() : base(true)
        {
        }

        [SuppressMessage("Microsoft.Performance",
                         "CA1811:AvoidUncalledPrivateCode", 
                         Justification = "Not called directly, but implicitly by FindFirstFileEx ")]
        public SafeFindFileHandle(IntPtr handle, Boolean ownsHandle = true) : base(ownsHandle)
        {
            SetHandle(handle);
        }

        protected override Boolean ReleaseHandle()
        {
            Boolean retValue = true;
            if (!IsClosed)
            {
                retValue = NativeMethods.FindClose(handle);
                SetHandleAsInvalid();
            }
            return retValue;
        }
    }
}
