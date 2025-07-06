using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace RunAsDomainUser
{
    class ImpersonatedProcess
    {

        //private const string CommandLine = @"C:\createjobobject.exe";
        private NativeMethods.ProcessInformation _processInfo;
        private readonly ManualResetEvent _exited = new ManualResetEvent(false);


        public IntPtr Handle { get; private set; }
        public event EventHandler? Exited;
        public TextReader StandardOutput { get; private set; }
        public TextReader StandardError { get; private set; }
        public TextWriter StandardInput { get; private set; }

        public void WaitForExit()
        {
            WaitForExit(-1);
        }

        public bool WaitForExit(int milliseconds)
        {
            return _exited.WaitOne(milliseconds);
        }

        public bool Start(string username, string password, string? domain, string executablePath)
        {
            //Console.WriteLine("Starting...");
            _processInfo = new NativeMethods.ProcessInformation();
            var startInfo = new NativeMethods.StartupInfo();
            bool success;

            //Console.WriteLine("Safe file handle...");
            SafeFileHandle hToken, hReadOut, hWriteOut, hReadErr, hWriteErr, hReadIn, hWriteIn;

            //Console.WriteLine("Security attributes...");
            var securityAttributes = new NativeMethods.SecurityAttributes();
            securityAttributes.bInheritHandle = true;

            //Console.WriteLine("Pipes...");
            success = NativeMethods.CreatePipe(out hReadOut, out hWriteOut, securityAttributes, 0);
            if (!success)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            success = NativeMethods.CreatePipe(out hReadErr, out hWriteErr, securityAttributes, 0);
            if (!success)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            success = NativeMethods.CreatePipe(out hReadIn, out hWriteIn, securityAttributes, 0);
            if (!success)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            //Console.WriteLine("Handle information...");
            success = NativeMethods.SetHandleInformation(hReadOut, NativeMethods.Constants.HANDLE_FLAG_INHERIT, 0);
            if (!success)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            // Logon user
            success = NativeMethods.LogonUser(
                username,
                domain,
                password,
                NativeMethods.LogonType.LOGON32_LOGON_NEW_CREDENTIALS,
                NativeMethods.LogonProvider.LOGON32_PROVIDER_DEFAULT,
                out hToken
            );
            if (!success)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            if (!NativeMethods.CreateEnvironmentBlock(out IntPtr unmanagedEnv, hToken.DangerousGetHandle(), false))
            {
                int lastError = Marshal.GetLastWin32Error();
                throw new Win32Exception(lastError, "Error calling CreateEnvironmentBlock: " + lastError);
            }

            //Console.WriteLine("Create process...");

            // Create process
            startInfo.cb = Marshal.SizeOf(startInfo);
            startInfo.dwFlags = NativeMethods.Constants.STARTF_USESTDHANDLES;
            startInfo.hStdOutput = hWriteOut;
            startInfo.hStdError = hWriteErr;
            startInfo.hStdInput = hReadIn;

            //success = NativeMethods.CreateProcessWithLogonW(
            //    username,
            //    domain,
            //    password,
            //    NativeMethods.LogonFlags.LOGON_NETCREDENTIALS_ONLY,
            //    null,
            //    executablePath,
            //    NativeMethods.CreateProcessFlags.CREATE_UNICODE_ENVIRONMENT,
            //    IntPtr.Zero,//unmanagedEnv,
            //    null,
            //    ref startInfo,
            //    out _processInfo
            //    );

            success = NativeMethods.CreateProcessAsUser(
                hToken,
                null,
                executablePath,
                IntPtr.Zero,
                IntPtr.Zero,
                true,
                NativeMethods.CreateProcessFlags.CREATE_UNICODE_ENVIRONMENT,
                unmanagedEnv,
                null,
                ref startInfo,
                out _processInfo
            );

            if (!success)
                throw new Win32Exception(Marshal.GetLastWin32Error());


            //Console.WriteLine("Post process creation...");

            if (!success)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            //Console.WriteLine("Handle...");
            Handle = _processInfo.hProcess;

            //Console.WriteLine("Close handles...");
            startInfo.hStdOutput.Close();
            startInfo.hStdError.Close();
            startInfo.hStdInput.Close();
            StandardOutput = new StreamReader(new FileStream(hReadOut, FileAccess.Read), Console.OutputEncoding);
            StandardError = new StreamReader(new FileStream(hReadErr, FileAccess.Read), Console.OutputEncoding);
            StandardInput = new StreamWriter(new FileStream(hWriteIn, FileAccess.Write), Console.InputEncoding);

            //Console.WriteLine("Wait for exit...");
            WaitForExitAsync();

            return success;
        }

        private void WaitForExitAsync()
        {
            var thr = new Thread(() =>
            {
                _ = NativeMethods.WaitForSingleObject(_processInfo.hProcess, NativeMethods.Constants.INFINITE);
                Exited?.Invoke(this, EventArgs.Empty);
                _exited.Set();
            });
            thr.Start();
        }
    }
}
