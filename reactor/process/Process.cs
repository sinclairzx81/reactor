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
    /// A operating system process with event streams for stdin, stdout and stderr.
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

        #region Process

        public int BasePriority  { 
            get { return this.process.BasePriority; }
        }

        public bool EnableRaisingEvents {
            get { return this.process.EnableRaisingEvents; }
            set { this.process.EnableRaisingEvents = value; }
        }

        public int ExitCode {
            get { return this.process.ExitCode; }
        }

        public DateTime ExitTime {
            get { return this.process.ExitTime; }
        }

        public IntPtr Handle {
            get { return this.process.Handle; }
        }

        public int HandleCount {
            get { return this.process.HandleCount; }
        }

        public bool HasExited {
            get { return this.process.HasExited; }
        }

        public int Id {
            get { return this.process.Id; }
        }

        public string MachineName {
            get { return this.process.MachineName; }
        }

        public ProcessModule MainModule {
            get { return this.process.MainModule; }
        }

        public IntPtr MainWindowHandle {
            get { return this.process.MainWindowHandle; }
        }

        public string MainWindowTitle {
            get { return this.process.MainWindowTitle; }
        }

        public IntPtr MaxWorkingSet {
            get { return this.process.MaxWorkingSet; }
            set { this.process.MaxWorkingSet = value; }
        }

        public IntPtr MinWorkingSet {
            get { return this.process.MinWorkingSet; }
            set { this.process.MinWorkingSet = value; }
        }

        public ProcessModuleCollection Modules {
            get { return this.process.Modules; }
        }

        public long NonpagedSystemMemorySize {
            get { return this.process.NonpagedSystemMemorySize64; }
        }

        public long PagedMemorySize {
            get { return this.process.PagedMemorySize64; }
        }

        public long PagedSystemMemorySize {
            get { return this.process.PagedSystemMemorySize64; }
        }

        public long PeakPagedMemorySize {
            get { return this.process.PeakPagedMemorySize64; }
        }

        public long PeakVirtualMemorySize {
            get { return this.process.PeakVirtualMemorySize64; }
        }

        public long PeakWorkingSet {
            get { return this.process.PeakWorkingSet64; }
        }

        public bool PriorityBoostEnabled {
            get { return this.process.PriorityBoostEnabled; }
            set { this.process.PriorityBoostEnabled = value; }
        }

        public ProcessPriorityClass PriorityClass {
            get { return this.process.PriorityClass; }
            set { this.process.PriorityClass = value; }
        }

        public long PrivateMemorySize {
            get { return this.process.PrivateMemorySize64; }
        }

        public TimeSpan PrivilegedProcessorTime {
            get { return this.process.PrivilegedProcessorTime; }
        }

        public string ProcessName {
            get { return this.process.ProcessName; }
        }

        public IntPtr ProcessorAffinity {
            get { return this.process.ProcessorAffinity; }
            set { this.process.ProcessorAffinity = value; }
        }

        public bool Responding {
            get { return this.process.Responding; }
        }
   
        public int SessionId {
            get { return this.process.SessionId; }
        }

        public DateTime StartTime {
            get { return this.process.StartTime; }
        }

        public ProcessThreadCollection Threads {
            get { return this.process.Threads; }
        }

        public TimeSpan TotalProcessorTime {
            get { return this.process.TotalProcessorTime; }
        }

        public TimeSpan UserProcessorTime {
            get { return this.process.UserProcessorTime; }
        }

        public long VirtualMemorySize {
            get { return this.process.VirtualMemorySize64; }
        }

        public long WorkingSet {
            get { return this.process.WorkingSet64; }
        }

        public void Refresh() {
            this.process.Refresh();
        }

        public void Close() {
            this.process.Close();
        }

        public bool CloseMainWindow() {
            return this.process.CloseMainWindow();
        }
 
        public void Kill() {
            this.process.Kill();
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
