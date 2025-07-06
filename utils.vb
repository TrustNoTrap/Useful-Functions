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

Private Function StartLogFile(FileName As String) As Boolean
    Dim StringStartIndex As Integer = FileName.LastIndexOf("\") + 1
    Dim DestinationDirectory As String = FileName.Substring(0, StringStartIndex)
    If Not System.IO.Directory.Exists(DestinationDirectory) Then
        System.IO.Directory.CreateDirectory(DestinationDirectory)
    End If
End Function

Private Function DeployFile(FileName As String) As Boolean
    Try
        Dim StringStartIndex As Integer = FileName.LastIndexOf("\") + 1
        Dim StringEndIndex As Integer = FileName.LastIndexOf(".")
        Dim ResourceName As String = FileName.Substring(StringStartIndex, StringEndIndex - StringStartIndex).Replace(" ", "_")
        Dim DestinationDirectory As String = FileName.Substring(0, StringStartIndex)

        If Not System.IO.Directory.Exists(DestinationDirectory) Then
            System.IO.Directory.CreateDirectory(DestinationDirectory)
        End If

        If System.IO.File.Exists(FileName) = True Then
            System.IO.File.Delete(FileName)

        End If

        Using BufferFileStream As New IO.FileStream(FileName, IO.FileMode.Create, IO.FileAccess.Write)

            Dim ResourceToUse As Object = My.Resources.ResourceManager.GetObject(ResourceName)


            If FileName.Substring(StringEndIndex) = ".reg" Then
                BufferFileStream.Write(System.Text.Encoding.Default.GetBytes(ResourceToUse), 0, ResourceToUse.Length)
            ElseIf FileName.Substring(StringEndIndex) <> ".reg" Then
                BufferFileStream.Write(ResourceToUse, 0, ResourceToUse.Length)
            End If

            BufferFileStream.Close()
        End Using

        Return True
    Catch ex As Exception
        Return False
    End Try
End Function

Private Function DeleteFile(filePath As String, Optional maxRetries As Integer = 5)
    Try
        My.Computer.FileSystem.DeleteFile(filePath, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.DeletePermanently)
    Catch ex As Exception
        Return False
    End Try

    If File.Exists(filePath) Then
        If maxRetries > 0 Then
            DeleteFile(filePath, maxRetries - 1)
        Else
            Return False
        End If
    End If
    Return True
End Function

Private Function InstallCertificate(certPath As String, sN As StoreName, location As StoreLocation)
    Try
        Dim cert As New X509Certificate2(certPath)
        Dim store As New X509Store(sN, location)

        store.Open(OpenFlags.ReadWrite)
        store.Add(cert)
    Catch ex As Exception
        Return False
    End Try

    Return True
End Function

Private Function InstallPFX(certPath As String, passphrase As String, sN As StoreName, location As StoreLocation)
    Try
        Dim collection As New X509Certificate2Collection()
        collection.Import(certPath, passphrase, X509KeyStorageFlags.PersistKeySet)
        Dim selectedStore As New X509Store(sN, location)
        Dim rootStore As New X509Store(StoreName.Root, location)
        Dim caStore As New X509Store(StoreName.CertificateAuthority, location)
        For Each certificate As X509Certificate2 In collection
            If certificate.HasPrivateKey Then
                selectedStore.Open(OpenFlags.ReadWrite)
                selectedStore.Add(certificate)
            ElseIf certificate.GetIssuerName() = certificate.GetName() Then
                rootStore.Open(OpenFlags.ReadWrite)
                rootStore.Add(certificate)
            Else
                caStore.Open(OpenFlags.ReadWrite)
                caStore.Add(certificate)
            End If
        Next

    Catch ex As Exception

        Return False
    End Try

    Return True
End Function

Private Function CreateURLShortcut(shortcutName As String, shortcutTargetPath As String, Optional shortcutIcon As String = Nothing, Optional tempPath As String = "C:\Temp\") As Boolean
    Try
        Dim ResourceToUse As Object
        If shortcutIcon IsNot Nothing Then
            ResourceToUse = My.Resources.ResourceManager.GetObject(shortcutIcon)
            Dim fileName As String = tempPath & shortcutIcon & ".ico"

            If System.IO.File.Exists(fileName) = True Then
                System.IO.File.Delete(fileName)
            End If
            Using stream As New IO.FileStream(fileName, IO.FileMode.Create, IO.FileAccess.Write)
                Dim ico = TryCast(ResourceToUse, Icon)
                ico?.Save(stream)
            End Using

            Dim shortcutPath As String = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) & "\" & shortcutName & ".url"

            Using writer As New StreamWriter(shortcutPath)
                writer.WriteLine("[InternetShortcut]")
                writer.WriteLine("URL=" + shortcutTargetPath)
                writer.WriteLine("IconIndex=0")
                writer.WriteLine("IconFile=" & fileName)
            End Using


        End If


        Return True
    Catch ex As Exception
        Return False
    End Try
End Function

Private Function CreateRegKey(regPath As String, regKey As String, Optional stringVal As String = Nothing, Optional numVal As Int32 = Nothing)
    Try
        Dim reg = Registry.GetValue(regPath, regKey, Nothing)

        If reg And (stringVal And reg = stringVal Or numVal And reg = numVal) Then
            Return False
        End If

        If stringVal <> Nothing Then
            Registry.SetValue(regPath, regKey, stringVal)
        ElseIf numVal.ToString().Length > 0 Then
            Registry.SetValue(regPath, regKey, numVal)
        Else
            Return False
        End If

    Catch ex As Exception
    End Try

    Return True
End Function

Private Sub SelfDestruct()
    Dim proc = New ProcessStartInfo()
    proc.Arguments = "/C choice /C Y /N /D Y /T 3 & Del """ + Application.ExecutablePath + """"
    proc.WindowStyle = ProcessWindowStyle.Hidden
    proc.CreateNoWindow = True
    proc.FileName = "cmd.exe"
    Process.Start(proc)
End Sub

Private Function GetFileVersionInfo(ByVal filename As String) As Version
    Return System.Version.Parse(FileVersionInfo.GetVersionInfo(filename).FileVersion)
End Function

Private Function ShowMessageBox(ByVal content As String, ByVal buttons As MessageBoxButtons, ByVal title As String)
    Me.Invoke((Function() MsgBox(content, buttons, title)))
End Function

Private Function SetRegistry(ByVal path As String, ByVal valueName As String, ByVal value As Object) As Boolean
    'Registry.SetValue("HKEY_LOCAL_MACHINE\Software\test", "works", "yes") ' Will create the value if it does not exist

    Dim toPrint As String = path & "\" & valueName & " with value of " & value.ToString()
    Try
        Registry.SetValue(path, valueName, value)
        AppendColoredLine("Successfully set " & toPrint, Color.Green)
    Catch ex As Exception
        AppendColoredLine("Error happend when trying to set " & toPrint & " | ERROR: " & ex.Message, Color.Red)
        Return False
    End Try

    Return True
End Function
