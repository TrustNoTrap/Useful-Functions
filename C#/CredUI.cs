using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RunAsDomainUser
{
    public class Creds
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Domain { get; set; }
    }
    public static class CredUI
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct CREDUI_INFO
        {
            public int cbSize;
            public IntPtr hwndParent;
            public string pszMessageText;
            public string pszCaptionText;
            public IntPtr hbmBanner;
        }

        [Flags]
        private enum dwFlags
        {
            CREDUIWIN_GENERIC   = 0x1,
            CREDUIWIN_CHECKBOX  = 0x2,
            CREDUIWIN_AUTHPACKAGE_ONLY = 0x10,
            CREDUIWIN_IN_CRED_ONLY = 0x20,
            CREDUIWIN_ENUMERATE_ADMINS = 0x100,
            CREDUIWIN_ENUMERATE_CURRENT_USER = 0x200,
            CREDUIWIN_SECURE_PROMPT = 0x1000,
            CREDUIWIN_PREPROMPTING = 0x2000,
            CREDUIWIN_PACK_32_WOW = 0x10000000,
            //CREDUIWIN_WINDOWS_HELLO = 0x80000000
        }

        [DllImport("credui.dll", CharSet = CharSet.Auto)]
        private static extern bool CredUnPackAuthenticationBuffer(int dwFlags, IntPtr pAuthBuffer, uint cbAuthBuffer, StringBuilder pszUserName, ref int pcchMaxUserName, StringBuilder pszDomainName, ref int pcchMaxDomainame, StringBuilder pszPassword, ref int pcchMaxPassword);
        [DllImport("credui.dll", CharSet = CharSet.Auto)]
        private static extern int CredUIPromptForWindowsCredentials(ref CREDUI_INFO notUsedHere, int authError, ref uint authPackage, IntPtr InAuthBuffer, uint InAuthBufferSize, out IntPtr refOutAuthBuffer, out uint refOutAuthBufferSize, ref bool fSave, int flags);
        [DllImport("ole32.dll", CharSet = CharSet.Auto)]
        private static extern int CoTaskMemFree(IntPtr pv);
        [DllImport("KERNEL32.DLL", EntryPoint = "RtlZeroMemory")]
        public static extern bool SecureZeroMemory2(IntPtr destination, uint length);
        //public static Creds Prompt()
        public static NetworkCredential Prompt()
        {
            CREDUI_INFO credui = new CREDUI_INFO();
            credui.pszCaptionText = "Domain Credentials Needed";
            credui.pszMessageText = "";
            credui.cbSize = Marshal.SizeOf(credui);
            uint authPackage = 0;
            IntPtr outCredBuffer = new IntPtr();
            uint outCredSize;
            bool save = false;
            int result = CredUIPromptForWindowsCredentials(ref credui, 0, ref authPackage, IntPtr.Zero, 0, out outCredBuffer, out outCredSize, ref save, (int)dwFlags.CREDUIWIN_PREPROMPTING /* Generic */);
            var usernameBuf = new StringBuilder(100);
            var passwordBuf = new StringBuilder(100);
            var domainBuf = new StringBuilder(100);
            int maxUserName = 100;
            int maxDomain = 100;
            int maxPassword = 100;
            //Creds toReturn = new Creds();
            NetworkCredential toReturn = new NetworkCredential();
            if (result == 0)
            {
                if (CredUnPackAuthenticationBuffer(0, outCredBuffer, outCredSize, usernameBuf, ref maxUserName, domainBuf, ref maxDomain, passwordBuf, ref maxPassword))
                {
                    //toReturn = new Creds { UserName = usernameBuf.ToString(), Password = passwordBuf.ToString(), Domain = domainBuf.ToString() };
                    toReturn = new NetworkCredential(usernameBuf.ToString(), passwordBuf.ToString());
                }
            }
            SecureZeroMemory2(outCredBuffer, outCredSize); // Fill memory with zeros
            CoTaskMemFree(outCredBuffer); // Cleanup memory allocation by CredUIPromptForWindowsCredentials

            return toReturn;
        }
    }
}
