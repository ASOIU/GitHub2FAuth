// Win32 Authentication Functions: http://msdn.microsoft.com/en-us/library/aa374731.aspx
//  CredWrite: http://msdn.microsoft.com/en-us/library/aa375187.aspx
//  CredDelete: http://msdn.microsoft.com/en-us/library/aa374787.aspx
//  CredProtect: http://msdn.microsoft.com/en-us/library/aa374803.aspx


using System;
using System.Runtime.InteropServices;

namespace GitHub2FAuth
{
	internal static class NativeMethods
	{
		[DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CredRead(string target, CRED_TYPE type, uint flags/*=0*/, out IntPtr credentialPtr);

		[DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CredWrite(ref CREDENTIAL userCredential, uint flags = 0);

		[DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode)]
		public static extern bool CredDelete(string target, CRED_TYPE type, uint flags = 0);

		[DllImport("advapi32.dll")]
		public static extern void CredFree(ref IntPtr credentialPtr);

		public enum CRED_TYPE:uint
		{
			GENERIC = 1,
			DOMAIN_PASSWORD = 2,
			DOMAIN_CERTIFICATE = 3,
			DOMAIN_VISIBLE_PASSWORD = 4,
			MAXIMUM = 5
		}

		public enum CRED_PERSIST:uint
		{
			SESSION = 1,
			LOCAL_MACHINE = 2,
			ENTERPRISE = 3
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct CREDENTIAL
		{
			public uint flags;
			public CRED_TYPE type;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string targetName;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string comment;
			public System.Runtime.InteropServices.ComTypes.FILETIME lastWritten;
			public uint credentialBlobSize;
			[MarshalAs(UnmanagedType.BStr)]
			public string credentialBlob;
			public CRED_PERSIST persist;
			public uint attributeCount;
			public IntPtr credAttribute;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string targetAlias;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string userName;
		}
	}
}
