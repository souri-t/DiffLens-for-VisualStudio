using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace DiffLens.VisualStudio.ToolWindows
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// 
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// 
    /// This class derives from the ToolWindowPane class provided by the Managed Package Framework
    /// and uses the IVsUIElementPane interface for the implementation.
    /// </summary>
    [Guid("13e7f8c3-1234-4567-8901-1234567890ab")]
    public class DiffLensWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiffLensWindow"/> class.
        /// </summary>
        public DiffLensWindow() : base(null)
        {
            this.Caption = "DiffLens";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new DiffLensWindowControl();
        }
    }
}
