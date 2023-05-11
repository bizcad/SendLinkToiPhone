/* A program which is intended to sit on the Desktop
 *   and copy the contents of the clipboard to a file
 *   and save it to my iCloudDrive folder.
 *   
 *   The files in the iCloudDrive folder are automagically synced
 *   with the files app on my iPhone.  For short files the syncing is
 *   almost instantaneous.
 *   
 *   I could then use the iPhone hard push and copy the link and
 *   paste it into an iMessage.
 *   
 *   https://stackoverflow.com/questions/69690135/how-do-i-properly-use-clipboard-in-a-console-application
 *   https://github.com/MrM40/WitWinClipboard/tree/main
 *   
 */
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
namespace InstagramLink
{
    internal class Program
    {
        static void Main()
        {
            var link = WindowsClipboard.GetText();
            if (link != null && link.Length > 0)
            {
                FileInfo info = new (@"E:\iCouldDrive\iCloudDrive\Link.txt");
                if (info.Exists)
                {
                    info.Delete();
                }
                using StreamWriter writer = new(info.FullName);
                writer.WriteLine(link);
                writer.Flush();
                writer.Close();
            }
        }
    }
    static class WindowsClipboard
    {
        const uint cfUnicodeText = 13;

        public static string? GetText()
        {
            if (!IsClipboardFormatAvailable(cfUnicodeText))
            {
                return null;
            }

            TryOpenClipboard();

            return InnerGet();
        }

        public static void SetText(string text)
        {
            TryOpenClipboard();

            InnerSet(text);
        }

        static void InnerSet(string text)
        {
            EmptyClipboard();
            IntPtr hGlobal = default;
            try
            {
                var bytes = (text.Length + 1) * 2;
                hGlobal = Marshal.AllocHGlobal(bytes);

                if (hGlobal == default)
                {
                    ThrowWin32();
                }

                var target = GlobalLock(hGlobal);

                if (target == default)
                {
                    ThrowWin32();
                }

                try
                {
                    Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                }
                finally
                {
                    GlobalUnlock(target);
                }

                if (SetClipboardData(cfUnicodeText, hGlobal) == default)
                {
                    ThrowWin32();
                }

                hGlobal = default;
            }
            finally
            {
                if (hGlobal != default)
                {
                    Marshal.FreeHGlobal(hGlobal);
                }

                CloseClipboard();
            }
        }

        static string? InnerGet()
        {
            IntPtr handle = default;

            IntPtr pointer = default;
            try
            {
                handle = GetClipboardData(cfUnicodeText);
                if (handle == default)
                {
                    return null;
                }

                pointer = GlobalLock(handle);
                if (pointer == default)
                {
                    return null;
                }

                var size = GlobalSize(handle);
                var buff = new byte[size];

                Marshal.Copy(pointer, buff, 0, size);

                return Encoding.Unicode.GetString(buff).TrimEnd('\0');
            }
            finally
            {
                if (pointer != default)
                {
                    GlobalUnlock(handle);
                }

                CloseClipboard();
            }
        }

        static void TryOpenClipboard()
        {
            var num = 10;
            while (true)
            {
                if (OpenClipboard(default))
                {
                    break;
                }

                if (--num == 0)
                {
                    ThrowWin32();
                }

                Thread.Sleep(100);
            }
        }

        static void ThrowWin32()
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("User32.dll", SetLastError = true)]
        static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetClipboardData(uint uFormat, IntPtr data);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        static extern bool EmptyClipboard();

        [DllImport("Kernel32.dll", SetLastError = true)]
        static extern int GlobalSize(IntPtr hMem);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    }
}