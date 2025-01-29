using System.Runtime.InteropServices;

namespace csharpClipper;

public static class ClipboardHelper
{
    public static string GetClipboardText()
    {
        for (int i = 0; i < 5; i++)
        {
            if (NativeMethods.OpenClipboard(IntPtr.Zero))
            {
                try
                {
                    var handle = NativeMethods.GetClipboardData(NativeMethods.CF_UNICODETEXT);
                    if (handle == IntPtr.Zero)
                    {
                        Logger.Log("No Unicode text found in clipboard.");
                        return null;
                    }

                    var ptr = NativeMethods.GlobalLock(handle);
                    if (ptr == IntPtr.Zero)
                    {
                        Logger.Log("GlobalLock failed.");
                        return null;
                    }

                    string text = Marshal.PtrToStringUni(ptr);
                    NativeMethods.GlobalUnlock(handle);
                    return text;
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "GetClipboardText");
                    return null;
                }
                finally
                {
                    NativeMethods.CloseClipboard();
                }
            }
            else
            {
                Thread.Sleep(10);
            }
        }
        Logger.Log("Failed to open clipboard after 5 attempts.");
        return null;
    }

    public static void SetClipboardText(string text)
    {
        if (text == null)
        {
            Logger.Log("SetClipboardText called with null. Aborting.");
            return;
        }

        bool clipboardOpened = false;
        for (int i = 0; i < 5; i++)
        {
            if (NativeMethods.OpenClipboard(IntPtr.Zero))
            {
                clipboardOpened = true;
                break;
            }
            else
            {
                Thread.Sleep(10);
            }
        }

        if (!clipboardOpened)
        {
            Logger.Log("Failed to open clipboard after 5 attempts.");
            return;
        }

        try
        {
            if (!NativeMethods.EmptyClipboard())
            {
                int error = Marshal.GetLastWin32Error();
                Logger.Log($"Failed to empty clipboard. Error: 0x{error:X8}");
                return;
            }

            int bytes = (text.Length + 1) * 2;
            IntPtr hGlobal = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE, (UIntPtr)bytes);
            if (hGlobal == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                Logger.Log($"GlobalAlloc failed. Error: 0x{error:X8}");
                return;
            }

            IntPtr target = NativeMethods.GlobalLock(hGlobal);
            if (target == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                Logger.Log($"GlobalLock failed. Error: 0x{error:X8}");
                NativeMethods.GlobalFree(hGlobal);
                return;
            }

            try
            {
                Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                Marshal.WriteInt16(target, text.Length * 2, 0);
            }
            finally
            {
                NativeMethods.GlobalUnlock(hGlobal);
            }

            if (NativeMethods.SetClipboardData(NativeMethods.CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                Logger.Log($"Failed to set clipboard data. Error: 0x{error:X8}");
                NativeMethods.GlobalFree(hGlobal);
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "SetClipboardText");
        }
        finally
        {
            NativeMethods.CloseClipboard();
        }
    }
}
