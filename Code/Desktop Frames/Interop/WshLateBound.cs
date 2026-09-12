using System;
using System.Globalization;
using System.Reflection;

// Late-bound replacement for the IWshRuntimeLibrary COM interop assembly.
//
// Why: the project used <COMReference> items, which require TlbImp.exe/AxImp.exe from
// the .NET Framework SDK on the build machine. That made the project buildable only from
// a full Visual Studio install: `dotnet build` fails with MSB4803 ("ResolveComReference is
// not supported on the .NET Core version of MSBuild") and plain MSBuild fails with MSB3091
// when the Windows SDK tools are missing. WScript.Shell is a pure IDispatch automation
// server, so reflection reaches exactly the same members with no compile-time COM
// reference - and the project then builds with nothing but the .NET SDK.
//
// The public surface mirrors the two types the source actually uses (WshShell and
// IWshShortcut) and only the members it touches: CreateShortcut, TargetPath, Arguments,
// WorkingDirectory, IconLocation, FullName and Save.
namespace IWshRuntimeLibrary
{
    /// <summary>Result of <see cref="WshShell.CreateShortcut"/>; all members are late-bound.</summary>
    public sealed class IWshShortcut
    {
        private const BindingFlags GET = BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags SET = BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags CALL = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance;

        private readonly object _com;

        internal IWshShortcut(object com)
        {
            _com = com ?? throw new ArgumentNullException(nameof(com));
        }

        public string TargetPath
        {
            get => Read("TargetPath");
            set => Write("TargetPath", value);
        }

        public string Arguments
        {
            get => Read("Arguments");
            set => Write("Arguments", value);
        }

        public string WorkingDirectory
        {
            get => Read("WorkingDirectory");
            set => Write("WorkingDirectory", value);
        }

        public string IconLocation
        {
            get => Read("IconLocation");
            set => Write("IconLocation", value);
        }

        public string FullName => Read("FullName");

        public void Save() => _com.GetType().InvokeMember("Save", CALL, null, _com, null);

        private string Read(string member)
        {
            object value = _com.GetType().InvokeMember(member, GET, null, _com, null);
            if (value == null) return null;
            return value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private void Write(string member, string value)
        {
            _com.GetType().InvokeMember(member, SET, null, _com, new object[] { value ?? string.Empty });
        }
    }

    /// <summary>Late-bound WScript.Shell. A fresh instance is created per call so the
    /// automation object lives in the calling thread's apartment.</summary>
    public sealed class WshShell
    {
        private const BindingFlags CALL = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance;

        public IWshShortcut CreateShortcut(string path)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
                throw new InvalidOperationException("WScript.Shell is not available on this system (Windows Script Host missing).");

            object shell = Activator.CreateInstance(shellType);
            object shortcut = shellType.InvokeMember("CreateShortcut", CALL, null, shell, new object[] { path });
            return new IWshShortcut(shortcut);
        }
    }
}
