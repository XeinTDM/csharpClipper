namespace csharpClipper;

public static class ClipboardListener
{
    private static IntPtr _windowHandle;
    private static readonly ManualResetEvent _threadReady = new(false);
    private static Thread _messageThread;
    private static readonly object _lock = new();

    public static event EventHandler ClipboardUpdate;

    public static void Start()
    {
        lock (_lock)
        {
            if (_messageThread != null && _messageThread.IsAlive)
                return;

            _messageThread = new Thread(MessageLoop)
            {
                IsBackground = true
            };

            _messageThread.SetApartmentState(ApartmentState.STA);
            _messageThread.Start();
            _threadReady.WaitOne();
        }
    }

    public static void Stop()
    {
        if (_windowHandle != IntPtr.Zero)
        {
            NativeMethods.RemoveClipboardFormatListener(_windowHandle);
            NativeMethods.PostMessage(_windowHandle, NativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            _windowHandle = IntPtr.Zero;
        }

        _messageThread?.Join();
        _messageThread = null;
    }

    private static void MessageLoop()
    {
        _windowHandle = NativeMethods.CreateMessageOnlyWindow();
        if (_windowHandle == IntPtr.Zero)
        {
            Console.WriteLine("Failed to create message window.");
            _threadReady.Set();
            return;
        }

        NativeMethods.AddClipboardFormatListener(_windowHandle);

        _threadReady.Set();

        while (NativeMethods.GetMessage(out MSG msg, IntPtr.Zero, 0, 0))
        {
            if (msg.message == NativeMethods.WM_CLIPBOARDUPDATE)
            {
                ClipboardUpdate?.Invoke(null, EventArgs.Empty);
            }

            NativeMethods.TranslateMessage(ref msg);
            NativeMethods.DispatchMessage(ref msg);
        }
    }
}
