Private Function IsAppRunningAs64Bit()
    Return System.IntPtr.Size = 8
End Function

Private Function GetProcessUserSID(chosenProcess As Process)
    Dim processHandle As IntPtr = IntPtr.Zero

    Try
        OpenProcessToken(chosenProcess.Handle, 8, processHandle)
        Dim wi As New WindowsIdentity(processHandle)
        'MsgBox(wi.Owner.ToString & delimiter & wi.User.ToString)

        Return wi.Owner.ToString()

    Catch ex As Exception
        'MsgBox(ex.Message)
        Return Nothing
    Finally
        If processHandle <> IntPtr.Zero Then
            CloseHandle(processHandle)
        End If
    End Try

End Function

Private Declare Function OpenProcessToken Lib "advapi32.dll" (ByVal ProcessHandle As IntPtr, ByVal DesiredAccess As UInteger, ByRef TokenHandle As IntPtr) As Boolean
Private Declare Function CloseHandle Lib "kernel32.dll" (ByVal hObject As IntPtr) As Boolean
