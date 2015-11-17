/*--------------------------------------------------------------------------

Reactor

The MIT License (MIT)

Copyright (c) 2015 Haydn Paterson (sinclair) <haydn.developer@gmail.com>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

---------------------------------------------------------------------------*/

using System;
using System.Diagnostics;
using System.IO;

namespace Reactor.Process
{
    /// <summary>
    /// Provides functionality to child OS processes and stream data via stdin/stdout/stderr.
    /// </summary>
    public class Process {

        private System.Diagnostics.Process process;

        #region Properties

        /// <summary>
        /// Standard input stream.
        /// </summary>
        public Reactor.Process.Writer  In     { get; private set; }

        /// <summary>
        /// Standard output stream.
        /// </summary>
        public Reactor.Process.Reader  Out    { get; private set; }
        
        /// <summary>
        /// Standard error stream.
        /// </summary>
        public Reactor.Process.Reader  Error  { get; private set; }

        #endregion

        #region Constructor

        public Process(string filename, string arguments, string workingdirectory) {
            this.process                                  = new System.Diagnostics.Process();
            this.process.StartInfo.UseShellExecute        = false;
            this.process.StartInfo.RedirectStandardInput  = true;
            this.process.StartInfo.RedirectStandardOutput = true;
            this.process.StartInfo.RedirectStandardError  = true;
            this.process.StartInfo.CreateNoWindow         = true;
            this.process.StartInfo.WindowStyle            = ProcessWindowStyle.Hidden;
            this.process.StartInfo.FileName               = filename;
            this.process.StartInfo.WorkingDirectory       = workingdirectory;
            if (arguments != null) {
                this.process.StartInfo.Arguments = arguments;
            }
            this.process.Start();
            this.In     = Reactor.Process.Writer.Create (this.process.StandardInput.BaseStream);
            this.Out    = Reactor.Process.Reader.Create (this.process.StandardOutput.BaseStream);
            this.Error  = Reactor.Process.Reader.Create (this.process.StandardError.BaseStream);
        }

        public Process(string filename, string arguments) : this(filename, arguments, Directory.GetCurrentDirectory()) { }

        public Process(string filename) : this(filename, string.Empty, Directory.GetCurrentDirectory()) { }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the base priority of the associated process.
        /// </summary>
        public int BasePriority  { 
            get { return this.process.BasePriority; }
        }

        /// <summary>
        /// Gets or sets whether the Exited event should be raised when the process terminates.
        /// </summary>
        public bool EnableRaisingEvents {
            get { return this.process.EnableRaisingEvents; }
            set { this.process.EnableRaisingEvents = value; }
        }

        /// <summary>
        /// Gets the value that the associated process specified when it terminated.
        /// </summary>
        public int ExitCode {
            get { return this.process.ExitCode; }
        }

        /// <summary>
        /// Gets the time that the associated process exited.
        /// </summary>
        public DateTime ExitTime {
            get { return this.process.ExitTime; }
        }

        /// <summary>
        /// Gets the native handle of the associated process.
        /// </summary>
        public IntPtr Handle {
            get { return this.process.Handle; }
        }

        /// <summary>
        /// Gets the number of handles opened by the process.
        /// </summary>
        public int HandleCount {
            get { return this.process.HandleCount; }
        }

        /// <summary>
        /// Gets a value indicating whether the associated process has been terminated.
        /// </summary>
        public bool HasExited {
            get { return this.process.HasExited; }
        }

        /// <summary>
        /// Gets the unique identifier for the associated process.
        /// </summary>
        public int Id {
            get { return this.process.Id; }
        }

        /// <summary>
        /// Gets the name of the computer the associated process is running on.
        /// </summary>
        public string MachineName {
            get { return this.process.MachineName; }
        }

        /// <summary>
        /// Gets the main module for the associated process.
        /// </summary>
        public ProcessModule MainModule {
            get { return this.process.MainModule; }
        }
        
        /// <summary>
        /// Gets the window handle of the main window of the associated process.
        /// </summary>
        public IntPtr MainWindowHandle {
            get { return this.process.MainWindowHandle; }
        }

        /// <summary>
        /// Gets the caption of the main window of the process.
        /// </summary>
        public string MainWindowTitle {
            get { return this.process.MainWindowTitle; }
        }

        /// <summary>
        /// Gets or sets the maximum allowable working set size for the associated process.
        /// </summary>
        public IntPtr MaxWorkingSet {
            get { return this.process.MaxWorkingSet; }
            set { this.process.MaxWorkingSet = value; }
        }

        /// <summary>
        /// Gets or sets the minimum allowable working set size for the associated process.
        /// </summary>
        public IntPtr MinWorkingSet {
            get { return this.process.MinWorkingSet; }
            set { this.process.MinWorkingSet = value; }
        }

        /// <summary>
        /// Gets the modules that have been loaded by the associated process.
        /// </summary>
        public ProcessModuleCollection Modules {
            get { return this.process.Modules; }
        }

        /// <summary>
        /// Gets the amount of nonpaged system memory allocated for the associated process.
        /// </summary>
        public long NonpagedSystemMemorySize {
            get { return this.process.NonpagedSystemMemorySize64; }
        }

        /// <summary>
        /// Gets the amount of paged memory allocated for the associated process.
        /// </summary>
        public long PagedMemorySize {
            get { return this.process.PagedMemorySize64; }
        }

        /// <summary>
        /// Gets the amount of pageable system memory allocated for the associated 
        /// </summary>
        public long PagedSystemMemorySize {
            get { return this.process.PagedSystemMemorySize64; }
        }

        /// <summary>
        /// Gets the maximum amount of memory in the virtual memory paging file used by the associated process.
        /// </summary>
        public long PeakPagedMemorySize {
            get { return this.process.PeakPagedMemorySize64; }
        }

        /// <summary>
        /// Gets the maximum amount of virtual memory used by the associated process.
        /// </summary>
        public long PeakVirtualMemorySize {
            get { return this.process.PeakVirtualMemorySize64; }
        }

        /// <summary>
        /// Gets the maximum amount of physical memory used by the associated process.
        /// </summary>
        public long PeakWorkingSet {
            get { return this.process.PeakWorkingSet64; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the associated process priority should temporarily be boosted by the operating system when the main window has the focus.
        /// </summary>
        public bool PriorityBoostEnabled {
            get { return this.process.PriorityBoostEnabled; }
            set { this.process.PriorityBoostEnabled = value; }
        }

        /// <summary>
        /// Gets or sets the overall priority category for the associated process.
        /// </summary>
        public ProcessPriorityClass PriorityClass {
            get { return this.process.PriorityClass; }
            set { this.process.PriorityClass = value; }
        }

        /// <summary>
        /// Gets the amount of private memory allocated for the associated process.
        /// </summary>
        public long PrivateMemorySize {
            get { return this.process.PrivateMemorySize64; }
        }

        /// <summary>
        /// Gets the privileged processor time for this process.
        /// </summary>
        public TimeSpan PrivilegedProcessorTime {
            get { return this.process.PrivilegedProcessorTime; }
        }

        /// <summary>
        /// Gets the name of the process.
        /// </summary>
        public string ProcessName {
            get { return this.process.ProcessName; }
        }

        /// <summary>
        /// Gets or sets the processors on which the threads in this process can be scheduled to run.
        /// </summary>
        public IntPtr ProcessorAffinity {
            get { return this.process.ProcessorAffinity; }
            set { this.process.ProcessorAffinity = value; }
        }

        /// <summary>
        /// Gets a value indicating whether the user interface of the process is responding.
        /// </summary>
        public bool Responding {
            get { return this.process.Responding; }
        }
   
        /// <summary>
        /// Gets the Terminal Services session identifier for the associated process.
        /// </summary>
        public int SessionId {
            get { return this.process.SessionId; }
        }

        /// <summary>
        /// Gets the properties used to initialize this process.
        /// </summary>
        public ProcessStartInfo StartInfo {
            get {  return this.process.StartInfo; }
        }

        /// <summary>
        /// Gets the time that the associated process was started.
        /// </summary>
        public DateTime StartTime {
            get { return this.process.StartTime; }
        }

        /// <summary>
        /// Gets the set of threads that are running in the associated process.
        /// </summary>
        public ProcessThreadCollection Threads {
            get { return this.process.Threads; }
        }

        /// <summary>
        /// Gets the total processor time for this process.
        /// </summary>
        public TimeSpan TotalProcessorTime {
            get { return this.process.TotalProcessorTime; }
        }

        /// <summary>
        /// Gets the user processor time for this process.
        /// </summary>
        public TimeSpan UserProcessorTime {
            get { return this.process.UserProcessorTime; }
        }

        /// <summary>
        /// Gets the amount of the virtual memory allocated for the associated process.
        /// </summary>
        public long VirtualMemorySize {
            get { return this.process.VirtualMemorySize64; }
        }

        /// <summary>
        /// Gets the amount of physical memory allocated for the associated process.
        /// </summary>
        public long WorkingSet {
            get { return this.process.WorkingSet64; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Discards any information about the associated process that has been cached inside the process component.
        /// </summary>
        public void Refresh() {
            this.process.Refresh();
        }

        /// <summary>
        /// Frees all the resources that are associated with this component.
        /// </summary>
        public void Close() {
            this.process.Close();
        }

        /// <summary>
        /// Closes a process that has a user interface by sending a close message to its main window.
        /// </summary>
        /// <returns></returns>
        public bool CloseMainWindow() {
            return this.process.CloseMainWindow();
        }
 
        /// <summary>
        /// Immediately stops the associated process.
        /// </summary>
        public void Kill() {
            try {
                this.process.Kill();
            }
            catch { }
        }

        #endregion

        #region Statics

        public static Reactor.Process.Process Create(string filename, string arguments, string workingdirectory) {
            return new Reactor.Process.Process(filename, arguments, workingdirectory);
        }

        public static Reactor.Process.Process Create(string filename, string arguments) {
            return new Reactor.Process.Process(filename, arguments);
        }
        
        public static Reactor.Process.Process Create(string filename) {
            return new Reactor.Process.Process(filename);
        }

        #endregion
    }
}
