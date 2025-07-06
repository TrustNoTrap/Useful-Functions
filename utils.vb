Private Function IsAppRunningAs64Bit()
    Return System.IntPtr.Size = 8
End Function