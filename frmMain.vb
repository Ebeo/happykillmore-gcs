Imports System.IO
Public Class frmMain
    Dim nHomeLat As String
    Dim nHomeLong As String
    Dim webDocument As Object
    Dim nLastGPS As Long
    Dim nLastMapUpdate As Long

    Dim sOutputFile As StreamWriter
    Dim rawData(0) As String
    Dim nDataIndex As Long

    'Public Event AttitudeChange(ByVal pitch As Single, ByVal roll As Single, ByVal yaw As Single)
    'Public Event GpsData(ByVal latitude As String, ByVal longitude As String, ByVal altitude As Single, ByVal speed As Single, ByVal heading As Single, ByVal satellites As Integer, ByVal fix As Integer, ByVal hdop As Single, ByVal verticalChange As Single)
    'Public Event Waypoints(ByVal waypointNumber As Integer, ByVal distance As Single)
    'Public Event RawPacket(ByVal messageString As String)

    Dim baudRates() As Long = {38400, 57600, 19200, 9600, 115200, 4800}
    Dim nBaudRateIndex As Integer
    Dim sBuffer As String
    Dim sCommandLineBuffer As String

    Dim nPitch As Single
    Dim nRoll As Single
    Dim nYaw As Single

    Dim nLatitude As String
    Dim nLongitude As String
    Dim nAltitude As Single
    Dim nGroundSpeed As Single
    Dim nAirSpeed As Single
    Dim nHeading As Single
    Dim nSats As Integer
    Dim nBattery As Single
    Dim nMode As Integer = -1
    Dim sMode As String
    Dim nFix As Integer
    Dim nHDOP As Single
    Dim nWaypointIndex As Integer
    Dim nWaypointTotal As Integer

    Dim nMaxListboxRecords As Integer = 1000

    Dim nWaypoint As Integer
    Dim nWaypointAlt As Single
    Dim nThrottle As Single
    Dim nDistance As Single

    Dim bNewGPS As Boolean
    Dim bNewAttitude As Boolean
    Dim bNewWaypoint As Boolean
    Dim dStartTime As Date

    Dim nAttitudeInterval As Integer
    Dim nWaypointInterval As Integer
    Dim nGPSInterval As Integer
    Dim nLastAttitude As Long
    Dim nLastWaypoint As Long
    'Dim nLastGPS As Long
    Dim nLastAlt As Single
    Dim nDataPoints As Long

    Dim eOutputDistance As e_DistanceFormat
    Dim eOutputSpeed As e_SpeedFormat
    Dim eOutputLatLongFormat As e_LatLongFormat = e_LatLongFormat.e_LatLongFormat_DD_DDDDDD
    Dim eMapSelection As e_MapSelection = e_MapSelection.e_MapSelection_GoogleEarth

    Dim nLastComPort As Integer
    Dim bFirstPaint As Boolean = False
    Dim bFirstVideoCapture As Boolean = False

    Dim nPlayerState As e_PlayerState

    Dim bFlightExtrude As Boolean = True
    Dim sFlightColor As String = "ff00ffff"
    Dim nFlightWidth As Integer = 1

    Dim bWaypointExtrude As Boolean = True
    Dim sWaypointColor As String = "00ffffff"
    Dim nWaypointWidth As Integer = 1

    Dim s3DModel As String
    Dim nCameraTracking As Integer
    Dim nCommandLineDelim As Integer

    Dim nInputStringLength As Long
    Dim nLastBandwidthCheck As Long

    Dim bPitchReverse As Boolean
    Dim bRollReverse As Boolean

    Private Sub SetPlayerState(ByVal newState As e_PlayerState)
        Dim bRecord As Boolean
        Dim bPlay As Boolean
        Dim bPause As Boolean
        Dim bReverse As Boolean
        Dim bFastForward As Boolean
        Dim bStepReverse As Boolean
        Dim bStepForward As Boolean
        Dim bLoaded As Boolean
        Dim bRecordReady As Boolean

        nPlayerState = newState
        Select Case nPlayerState
            Case e_PlayerState.e_PlayerState_None
                bRecord = False
                bPlay = False
                bPause = False
                bReverse = False
                bFastForward = False
                bStepReverse = False
                bStepForward = False
                bLoaded = False
                bRecordReady = False
            Case e_PlayerState.e_PlayerState_Loaded
                bRecord = False
                bPlay = False
                bPause = False
                bReverse = False
                bFastForward = False
                bStepReverse = False
                bStepForward = False
                bLoaded = True
                bRecordReady = True
            Case e_PlayerState.e_PlayerState_RecordReady
                bRecord = False
                bPlay = False
                bPause = False
                bReverse = False
                bFastForward = False
                bStepReverse = False
                bStepForward = False
                bLoaded = False
                bRecordReady = True
            Case e_PlayerState.e_PlayerState_Play
                bRecord = False
                bPlay = True
                bPause = False
                bReverse = False
                bFastForward = False
                bStepReverse = False
                bStepForward = False
                bLoaded = True
                bRecordReady = True
            Case e_PlayerState.e_PlayerState_Record
                bRecord = True
                bPlay = False
                bPause = False
                bReverse = False
                bFastForward = False
                bStepReverse = False
                bStepForward = False
                bLoaded = True
                bRecordReady = True
            Case e_PlayerState.e_PlayerState_Pause, e_PlayerState.e_PlayerState_StepBack, e_PlayerState.e_PlayerState_StepForward
                bRecord = False
                bPlay = False
                bPause = True
                bReverse = False
                bFastForward = False
                bStepReverse = False
                bStepForward = False
                bLoaded = True
                bRecordReady = True
        End Select

        chkRecord.Checked = bRecord
        chkPlay.Checked = bPlay
        chkPause.Checked = bPause
        chkReverse.Checked = bReverse
        chkFastForward.Checked = bFastForward
        chkStepBack.Checked = False
        chkStepForward.Checked = False

        chkRecord.Enabled = bRecordReady
        chkPlay.Enabled = bLoaded
        chkPause.Enabled = bLoaded
        chkReverse.Enabled = False
        chkFastForward.Enabled = False
        If bLoaded = True And nDataIndex > 1 Then
            chkStepBack.Enabled = bLoaded
        Else
            chkStepBack.Enabled = False
        End If
        If bLoaded = True And nDataIndex < UBound(rawData) Then
            chkStepForward.Enabled = bLoaded
        Else
            chkStepForward.Enabled = False
        End If
        cmdViewFile.Enabled = bLoaded
        TrackBar1.Enabled = bLoaded
    End Sub
    Private Sub FormClosing(ByVal sender As System.Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles Me.Closing
        Me.Dispose()
    End Sub

    Private Sub frmMain_Disposed(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Disposed
        Try
            tmrSearch.Enabled = False
            If serialPortIn.IsOpen = True Then
                serialPortIn.Close()
            End If
        Catch e2 As Exception
        End Try
    End Sub
    Public ReadOnly Property Version() As String
        Get
            Dim sTemp As String
            sTemp = Trim(System.Reflection.Assembly.GetExecutingAssembly.FullName.Split(",")(1).Replace("Version=", ""))
            Return "v" & Mid(sTemp, 1, InStrRev(sTemp, ".") - 1) & " - Build " & Mid(sTemp, InStrRev(sTemp, ".") + 1)


            'Return Application.ProductVersion.ToString()
            'Dim myBuildInfo As FileVersionInfo = FileVersionInfo.GetVersionInfo(Application.ExecutablePath)
            'Return myBuildInfo.ProductVersion
        End Get

    End Property
    Private Sub SetupWebBroswer()
        If eMapSelection = e_MapSelection.e_MapSelection_GoogleMaps Then
            WebBrowser1.Navigate(System.AppDomain.CurrentDomain.BaseDirectory & "Maps.html")
        Else
            WebBrowser1.DocumentText = My.Resources.AvionicsInstrumentsControlsRessources.pluginhost.ToString
            'WebBrowser1.Navigate(System.AppDomain.CurrentDomain.BaseDirectory & "pluginhost.html")
        End If

        While WebBrowser1.ReadyState <> 4
            Application.DoEvents()
        End While
        webDocument = WebBrowser1.Document

        If eMapSelection = e_MapSelection.e_MapSelection_GoogleMaps Then
            webDocument.GetElementById("lockDragDrop").SetAttribute("value", "Locked")
        End If

    End Sub

    Private Sub frmMain_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Dim sColor As String
        Dim nCount As Integer

        If Dir(System.AppDomain.CurrentDomain.BaseDirectory & "Maps.html") = "" Then
            Dim sFileContents As String = HK_GCS.My.Resources.AvionicsInstrumentsControlsRessources.Maps.ToString
            Dim fs As New FileStream(System.AppDomain.CurrentDomain.BaseDirectory & "Maps.html", FileMode.Create, FileAccess.Write)
            Dim sMapsFile As StreamWriter = New StreamWriter(fs)

            sMapsFile.WriteLine(sFileContents)
            sMapsFile.Close()
        End If

        If Dir(System.AppDomain.CurrentDomain.BaseDirectory & "Missions", FileAttribute.Directory) = "" Then
            MkDir(System.AppDomain.CurrentDomain.BaseDirectory & "Missions")
        End If

        Me.Text = Me.Text & " - " & Version

        txtOutputFolder.Text = GetRegSetting(sRootRegistry & "\Settings", "OutputFolder", System.AppDomain.CurrentDomain.BaseDirectory)
        LoadOutputFiles()

        Dim i As Integer

        Control.CheckForIllegalCrossThreadCalls = False

        cboAttitude.Items.Add("None")
        cboGPS.Items.Add("None")
        cboWaypoint.Items.Add("None")
        For i = 1 To 20
            cboAttitude.Items.Add(i & " Hz")
            If i <= 5 Then
                cboMapUpdateRate.Items.Add(i & " Hz")
            End If
            If i <= 10 Then
                cboGPS.Items.Add(i & " Hz")
                cboWaypoint.Items.Add(i & " Hz")
            End If
        Next
        cboAttitude.SelectedIndex = Convert.ToInt32(GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "Attitude Hz", "5"))
        cboGPS.SelectedIndex = Convert.ToInt32(GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "GPS Hz", "2"))
        cboWaypoint.SelectedIndex = Convert.ToInt32(GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "Waypoint Hz", "2"))
        cboMapUpdateRate.SelectedIndex = Convert.ToInt32(GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "Map Update Hz", "1") - 1)

        With cboCommandLineDelim
            .Items.Add("No line ending")
            .Items.Add("Line Feed")
            .Items.Add("Carriage return")
            .Items.Add("Line Feed+Carriage Return")
            .SelectedIndex = GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "Command Line Delim", "2")
            nCommandLineDelim = .SelectedIndex
        End With

        With cbo3DModel
            .Items.Add("EasyStar")
            .Items.Add("FunJet")
            '.Items.Add("-mi- Yellow Plane")
            '.Items.Add("Firecracker")
            '.Items.Add("T-Rex 450")
            '.Items.Add("AeroQuad")
            .Text = GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "3D Model", "EasyStar")
            s3DModel = .Text
        End With

        LoadComboEntries(cboCommandLineCommand)
        LoadMissions()

        With cboGoogleEarthCamera
            .Items.Add("No Tracking")
            .Items.Add("Lat and Long")
            .Items.Add("Lat, Long and Altitude")
            .Items.Add("Cockpit View")
            .SelectedIndex = GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "Camera Tracking", 0)
            nCameraTracking = .SelectedIndex
        End With

        With cboMaxSpeed
            For i = 40 To 200 Step 40
                .Items.Add(i)
            Next
            .SelectedIndex = GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "Max Speed", "2")
        End With

        With cboMapSelection
            .Items.Add("Google Earth")
            .Items.Add("Google Maps")
            .SelectedIndex = Convert.ToInt32(GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "Map Selection", "0"))
            eMapSelection = .SelectedIndex
        End With

        tbarModelScale.Value = GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "Model Scale", "10")

        chkCommandLineAutoScroll.Checked = GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "Command Line Auto Scroll", True)

        bPitchReverse = GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "Pitch Reverse", False)
        chkPitchReverse.Checked = bPitchReverse
        bRollReverse = GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "Roll Reverse", False)
        chkRollReverse.Checked = bRollReverse

        bFlightExtrude = GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "Flight Extrude", True)
        chkFlightExtrude.Checked = bFlightExtrude
        sColor = GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "Flight Color", "0000FFFF")
        'cmdFlightColor.BackColor = GetSystemColor(sColor)
        cmdFlightColor.BackColor = Color.FromArgb((Convert.ToInt32(Mid(sColor, 1, 2), 16)), (Convert.ToInt32(Mid(sColor, 3, 2), 16)), (Convert.ToInt32(Mid(sColor, 5, 2), 16)), (Convert.ToInt32(Mid(sColor, 7, 2), 16)))
        nFlightWidth = GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "Flight Width", 1)
        tbarFlightWidth.Value = nFlightWidth
        UpdateLineColor()

        'bWaypointExtrude = GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "WP Extrude", True)
        'chkWaypointExtrude.Checked = bWaypointExtrude

        SetupWebBroswer()

        'WebBrowser1.Document().Body.InnerHtml = HK_GCS.My.Resources.AvionicsInstrumentsControlsRessources.Maps.ToString
        'WebBrowser1.Navigate(System.AppDomain.CurrentDomain.BaseDirectory & "Maps.html")

        With cboDistanceUnits
            .Items.Add("Feet")
            .Items.Add("Meters")
            .SelectedIndex = Convert.ToInt32(GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "Distance Units", "0"))
            eOutputDistance = .SelectedIndex
            If eMapSelection = e_MapSelection.e_MapSelection_GoogleMaps Then
                webDocument.GetElementById("metersFeet").SetAttribute("value", eOutputDistance)
            End If
            AltimeterInstrumentControl1.SetAlimeterParameters(nAltitude, cboDistanceUnits.Text)
        End With

        With cboSpeedUnits
            .Items.Add("Knots")
            .Items.Add("KPH")
            .Items.Add("MPH")
            .Items.Add("M/S")
            .SelectedIndex = Convert.ToInt32(GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "Speed Units", "2"))
            eOutputSpeed = .SelectedIndex
            AirSpeedIndicatorInstrumentControl1.SetAirSpeedIndicatorParameters(nGroundSpeed, Convert.ToInt32(cboMaxSpeed.Text), "Speed", cboSpeedUnits.Text)
        End With

        With cboBaudRate
            For nCount = LBound(baudRates) To UBound(baudRates)
                .Items.Add(baudRates(nCount))
            Next
            .Text = GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "Baud Rate", "38400")
        End With

        LoadComPorts()
        SetPlayerState(e_PlayerState.e_PlayerState_None)

        lblGPSLock.Text = ""
        lblSatellites.Text = ""
        lblHDOP.Text = ""
        lblBattery.Text = ""
        lblWaypoint.Text = ""
        lblDistance.Text = ""
        lblMode.Text = ""
        lblThrottle.Text = ""
        lblLatitude.Text = ""
        lblLongitude.Text = ""
        lblTargetAlt.Text = ""
        lblDataPoints.Text = ""
        lblRunTime.Text = ""
        nDataPoints = 1
        nMode = -1

        'If cboComPort.Items.Count > 0 Then
        '    cboComPort.SelectedIndex = 0
        'End If

        'DirectShowControl1.StartCapture()

    End Sub
    Private Sub LoadComPorts(Optional ByVal defaultComPort As String = "")
        Dim i As Integer

        cboComPort.Items.Clear()
        Try
            For i = 0 To My.Computer.Ports.SerialPortNames.Count - 1
                If Strings.Left(My.Computer.Ports.SerialPortNames(i), 3) = "COM" Then
                    cboComPort.Items.Add(My.Computer.Ports.SerialPortNames(i))
                End If
            Next
            For i = 0 To cboComPort.Items.Count - 1
                If defaultComPort = "" Then
                    If My.Computer.Ports.SerialPortNames(i) = GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "COM Port", "") Then
                        cboComPort.Text = GetRegSetting(sRootRegistry & "\GPS Parser\Settings", "COM Port", "")
                    End If
                Else
                    If My.Computer.Ports.SerialPortNames(i) = defaultComPort Then
                        cboComPort.Text = defaultComPort
                    End If
                End If
            Next
            If cboComPort.Items.Count > 0 And cboComPort.SelectedIndex = -1 Then
                cboComPort.SelectedIndex = 0
                'Else
                '    cmdSearch_Click(sender, e)
            End If
        Catch e2 As Exception
        End Try
        nBaudRateIndex = 0

    End Sub
    Private Sub LoadMissions()
        Dim sMission As String
        With cboMission
            .Items.Clear()
            sMission = Dir(System.AppDomain.CurrentDomain.BaseDirectory & "Missions\*.txt", FileAttribute.Normal)
            Do While sMission <> ""
                .Items.Add(sMission)
                sMission = Dir()
            Loop
        End With
    End Sub

    Private Sub LoadOutputFiles()
        Dim sFilename As String
        With cboOutputFiles
            sFilename = Dir(txtOutputFolder.Text & "*.hko")
            Do While sFilename <> ""
                .Items.Add(Mid(sFilename, 1, InStr(sFilename, ".") - 1))
                sFilename = Dir()
            Loop
            .Text = GetRegSetting(sRootRegistry & "\Settings", "OutputFile", Now.ToString("mmddyyyy_hhmmss"))
        End With
    End Sub
    Private Sub cmdOutputFolder_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdOutputFolder.Click
        With FolderBrowserDialog1
            .SelectedPath = txtOutputFolder.Text
            .ShowNewFolderButton = True
            .ShowDialog()
            If .SelectedPath <> "" Then
                txtOutputFolder.Text = .SelectedPath
                Call SaveRegSetting(sRootRegistry & "\Settings", "OutputFolder", txtOutputFolder.Text)
            End If
        End With
    End Sub

    Private Sub txtOutputFilename_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles txtOutputFolder.TextChanged
        Call SaveRegSetting(sRootRegistry & "\Settings", "OutputFile", cboOutputFiles.Text)
    End Sub

    Private Sub chkSave_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkRecord.CheckedChanged
        If chkRecord.Checked = False Then
            If Not sOutputFile Is Nothing Then
                sOutputFile.Close()
                sOutputFile = Nothing
            End If
            LoadDataFile(txtOutputFolder.Text & cboOutputFiles.Text & ".hko")
            SetPlayerState(e_PlayerState.e_PlayerState_Loaded)
        Else
            cmdViewFile.Enabled = False
            Dim fs As New FileStream(txtOutputFolder.Text & cboOutputFiles.Text & ".hko", FileMode.Create, FileAccess.Write)
            sOutputFile = New StreamWriter(fs)

            SetPlayerState(e_PlayerState.e_PlayerState_Record)
        End If

    End Sub

    Private Sub cmdViewFile_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdViewFile.Click
        Shell("notepad.exe " & txtOutputFolder.Text & cboOutputFiles.Text & ".hko", AppWinStyle.NormalFocus)
    End Sub

    Private Sub cmdCenterOnPlane_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCenterOnPlane.Click
        Try
            If eMapSelection = e_MapSelection.e_MapSelection_GoogleMaps Then
                webDocument.GetElementById("centerTravelEndButton").InvokeMember("click")
            Else
                webDocument.InvokeScript("centerOnPlane", New Object() {})
            End If
        Catch e2 As Exception
        End Try
    End Sub

    Private Sub cmdClearMap_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdClearMap.Click
        Try
            If eMapSelection = e_MapSelection.e_MapSelection_GoogleMaps Then
                webDocument.GetElementById("ClearButton").InvokeMember("click")
            Else
                webDocument.InvokeScript("clearMap", New Object() {})
            End If
            nDataPoints = 1
        Catch e2 As Exception
        End Try
    End Sub
    Private Sub LoadDataFile(ByVal filename As String)

        If Dir(filename) <> "" Then
            Dim sFileName As String = filename
            Dim myFileStream As New System.IO.FileStream(sFileName, FileMode.Open, FileAccess.Read, FileShare.Read)

            'Create the StreamReader and associate the filestream with it
            Dim myReader As New System.IO.StreamReader(myFileStream)

            'Read the entire text, and set it to a string
            Dim sFileContents As String = myReader.ReadToEnd()

            'Close everything when you are finished
            myReader.Close()
            myFileStream.Close()

            rawData = Split(sFileContents, Chr(255) & vbCrLf)
            nDataIndex = 1

            TrackBar1.Maximum = UBound(rawData)
            If nDataIndex <= UBound(rawData) Then
                TrackBar1.Value = nDataIndex
            End If

            TrackBar1.TickFrequency = UBound(rawData) / 10

            dStartTime = New Date(Mid(rawData(0), 1, InStr(rawData(0), ":") - 1))

            TrackBar1.Enabled = True
            chkPlay.Enabled = True
            chkPause.Enabled = True
            chkStepBack.Enabled = True
            chkStepForward.Enabled = True
        Else
            TrackBar1.Enabled = False
            chkPlay.Enabled = False
            chkPause.Enabled = False
            chkStepBack.Enabled = False
            chkStepForward.Enabled = False
        End If

    End Sub
    Private Sub cboOutputFiles_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cboOutputFiles.SelectedIndexChanged
        LoadDataFile(txtOutputFolder.Text & cboOutputFiles.Text & ".hko")
        SetPlayerState(e_PlayerState.e_PlayerState_Loaded)
    End Sub

    Private Sub GpS_Parser1_AttitudeChange1(ByVal pitch As Single, ByVal roll As Single, ByVal yaw As Single)
        WebBrowser1.Invoke(New MyDelegate(AddressOf updateAttitude))
        AttitudeIndicatorInstrumentControl1.SetAttitudeIndicatorParameters(IIf(bPitchReverse = True, -1, 1) * -pitch, IIf(bRollReverse = True, -1, 1) * roll)
        'TurnCoordinatorInstrumentControl1.SetTurnCoordinatorParameters(-roll, 0, "TURN COORDINATOR", "L", "R")
        _3DMesh1.DrawMesh(IIf(bPitchReverse = True, -1, 1) * pitch, IIf(bRollReverse = True, -1, 1) * roll, yaw)
    End Sub
    Private Sub GPS_Parser1_Waypoints(ByVal waypointNumber As Integer, ByVal distance As Single, ByVal battery As Single, ByVal mode As Integer, ByVal throttle As Single)
        lblWaypoint.Text = "#" & waypointNumber
        lblDistance.Text = distance.ToString("0.0") & GetShortDistanceUnits()
        lblTargetAlt.Text = nWaypointAlt.ToString("0.0") & GetShortDistanceUnits()
        lblBattery.Text = battery & "v"
        If sMode <> "" Then
            lblMode.Text = sMode & " (#" & nMode & ")"
        ElseIf nMode = -1 Then
            lblMode.Text = ""
        Else
            lblMode.Text = mode
        End If
        lblThrottle.Text = throttle
    End Sub
    Private Function GetShortDistanceUnits() As String
        Select Case eOutputDistance
            Case e_DistanceFormat.e_DistanceFormat_Feet
                GetShortDistanceUnits = "ft"
            Case e_DistanceFormat.e_DistanceFormat_Meters
                GetShortDistanceUnits = "m"
        End Select
    End Function
    Private Sub GpS_Parser1_GpsData1(ByVal latitude As String, ByVal longitude As String, ByVal altitude As Single, ByVal groundSpeed As Single, ByVal heading As Single, ByVal satellites As Integer, ByVal fix As Integer, ByVal hdop As Single, ByVal verticalChange As Single, Optional ByVal airSpeed As Single = -1)
        Try

            AirSpeedIndicatorInstrumentControl1.SetAirSpeedIndicatorParameters(groundSpeed, cboMaxSpeed.Text, "Speed", cboSpeedUnits.Text, airSpeed)
            AltimeterInstrumentControl1.SetAlimeterParameters(altitude, cboDistanceUnits.Text)
            HeadingIndicatorInstrumentControl1.SetHeadingIndicatorParameters(heading)
            VerticalSpeedIndicatorInstrumentControl1.SetVerticalSpeedIndicatorParameters(verticalChange, "vertical speed", "up", "down", "100ft/min")

            Select Case fix
                Case 0
                    lblGPSLock.Text = "Not Locked"
                Case 1
                    lblGPSLock.Text = "Locked"
                Case 2
                    lblGPSLock.Text = "2D Lock"
                Case Else
                    lblGPSLock.Text = "3D Lock"
            End Select
            lblSatellites.Text = satellites
            lblHDOP.Text = hdop

            lblLatitude.Text = latitude
            lblLongitude.Text = longitude

            lblDataPoints.Text = nDataPoints.ToString("###,##0")
            nDataPoints = nDataPoints + 1

            If serialPortIn.IsOpen = True Then
                lblRunTime.Text = ConvertToRunTime(dStartTime, Now)
            Else
                lblRunTime.Text = ConvertToRunTime(dStartTime, New Date(Mid(rawData(nDataIndex), 1, InStr(rawData(nDataIndex), ":") - 1)))
            End If

            If Now.Ticks - nLastMapUpdate > (10000000 / (cboMapUpdateRate.SelectedIndex + 1)) Then
                nLastMapUpdate = Now.Ticks
                If latitude <> "" And longitude <> "" Then
                    If eMapSelection = e_MapSelection.e_MapSelection_GoogleMaps Then
                        If nHomeLat = "" Then
                            nHomeLat = latitude
                            nHomeLong = longitude
                            webDocument.GetElementById("homeLat").SetAttribute("value", latitude)
                            webDocument.GetElementById("homeLng").SetAttribute("value", longitude)
                            webDocument.GetElementById("setHomeLatLngButton").InvokeMember("click")
                            webDocument.GetElementById("centerMapHomeButton").InvokeMember("click")
                        End If

                        webDocument.GetElementById("heading").SetAttribute("value", heading)
                        webDocument.GetElementById("segmentInterval").SetAttribute("value", "1")
                        webDocument.GetElementById("segmentLat").SetAttribute("value", latitude)
                        webDocument.GetElementById("segmentLng").SetAttribute("value", longitude)
                        webDocument.GetElementById("addSegementButton").InvokeMember("click")
                    Else
                        'nAltitude = ConvertDistance(nAltitude, eOutputDistance, e_DistanceFormat.e_DistanceFormat_Meters)
                        If nHomeLat = "" Then
                            nHomeLat = latitude
                            nHomeLong = longitude
                            WebBrowser1.Invoke(New MyDelegate(AddressOf setHomeLatLng))
                            If cboMission.Text <> "" Then
                                cboMission_SelectedIndexChanged(Nothing, Nothing)
                            End If
                        End If
                        WebBrowser1.Invoke(New MyDelegate(AddressOf setPlaneLocation))
                    End If
                End If
            End If
        Catch e As Exception
            System.Diagnostics.Debug.Print("Error: " & e.Message)
        End Try

    End Sub
    Public Function ConvertToRunTime(ByVal startTime As Date, ByVal endTime As Date)
        Dim nSeconds As Long
        Dim nDays As Long
        Dim nHours As Long
        Dim nMinutes As Long

        nSeconds = DateDiff(DateInterval.Second, startTime, endTime)

        'ConvertToRunTime = ""
        'nDays = nSeconds \ 86400
        'If nDays > 0 Then
        '    ConvertToRunTime = ConvertToRunTime & nDays & " day" & IIf(nDays > 1, "s", "")
        'End If
        'nSeconds = nSeconds - (nDays * 86400)


        'nHours = nSeconds \ 3600
        'If nHours > 0 Then
        '    If ConvertToRunTime <> "" Then
        '        ConvertToRunTime = ConvertToRunTime & ","
        '    End If
        '    ConvertToRunTime = ConvertToRunTime & nHours & " hour" & IIf(nHours > 1, "s", "")
        'End If
        'nSeconds = nSeconds - (nHours * 3600)

        nMinutes = nSeconds \ 60
        If nMinutes > 0 Then
            If ConvertToRunTime <> "" Then
                ConvertToRunTime = ConvertToRunTime & ","
            End If
            ConvertToRunTime = ConvertToRunTime & nMinutes & " min" & IIf(nMinutes > 1, "s", "")
        End If
        nSeconds = nSeconds - (nMinutes * 60)

        If ConvertToRunTime <> "" Then
            ConvertToRunTime = ConvertToRunTime & ","
        End If
        ConvertToRunTime = ConvertToRunTime & nSeconds & " sec" & IIf(nSeconds > 1, "s", "")

    End Function
    Public Delegate Sub MyDelegate()
    Public Sub setHomeLatLng()
        Try
            webDocument.InvokeScript("setHomeLatLng", New Object() {nLatitude, nLongitude, ConvertDistance(nAltitude, eOutputDistance, e_DistanceFormat.e_DistanceFormat_Meters), cbo3DModel.Text})
            'lblHomeAltitude.Text = webDocument.GetElementById("homeGroundAltitude").ToString
        Catch e2 As Exception
        End Try
    End Sub
    Public Sub loadModel()
        Try
            webDocument.InvokeScript("loadModel", New Object() {cbo3DModel.Text})
            'lblHomeAltitude.Text = webDocument.GetElementById("homeGroundAltitude").ToString
        Catch e2 As Exception
        End Try
    End Sub
    Public Sub addWaypoint()
        Try
            webDocument.InvokeScript("addWaypoint", New Object() {nLatitude, nLongitude, ConvertDistance(nAltitude, eOutputDistance, e_DistanceFormat.e_DistanceFormat_Meters), nWaypoint.ToString.PadLeft(2, "0")})
        Catch e2 As Exception
        End Try
    End Sub
    Public Sub drawHomeLine()
        Try
            webDocument.InvokeScript("drawHomeLine", New Object() {})
        Catch e2 As Exception
        End Try
    End Sub
    Public Sub changeModelScale()
        Try
            webDocument.InvokeScript("changeModelScale", New Object() {tbarModelScale.Value})
        Catch e2 As Exception
        End Try
    End Sub
    Public Sub updateAttitude()
        Try
            webDocument.InvokeScript("updateAttitude", New Object() {nHeading, nPitch, nRoll})
        Catch e2 As Exception
        End Try
    End Sub
    Public Sub setPlaneLocation()
        Try
            webDocument.InvokeScript("drawAndCenter", New Object() {nLatitude, nLongitude, ConvertDistance(nAltitude, eOutputDistance, e_DistanceFormat.e_DistanceFormat_Meters), bFlightExtrude, sFlightColor, nFlightWidth, nCameraTracking, IIf(eOutputDistance = e_DistanceFormat.e_DistanceFormat_Feet, True, False), nHeading, nPitch, nRoll})
            'lblHomeAltitude.Text = webDocument.GetElementById("homeGroundAltitude").ToString
        Catch
        End Try
    End Sub

    Private Sub GpS_Parser1_RawPacket(ByVal messageString As String)
        If chkRecord.Checked = True Then
            If Not sOutputFile Is Nothing Then
                sOutputFile.WriteLine(Now.Ticks & ":" & messageString & Chr(255))
            End If
        End If
    End Sub

    Private Sub tmrPlayback_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tmrPlayback.Tick
        Dim oMessage As cMessage
        Dim sMessage As String

        Do While True
            If nDataIndex > UBound(rawData) Then
                tmrPlayback.Enabled = False
                SetPlayerState(e_PlayerState.e_PlayerState_Pause)
                nDataIndex = UBound(rawData)
                'chkPlay.Checked = False
                Exit Sub
            ElseIf Trim(rawData(nDataIndex)) = "" Then
                nDataIndex = nDataIndex + 1
            ElseIf Len(rawData(nDataIndex)) < 18 Then
                nDataIndex = nDataIndex + 1
            ElseIf Mid(rawData(nDataIndex), 19, 1) <> ":" And Mid(rawData(nDataIndex), 20, 1) <> ":" Then
                nDataIndex = nDataIndex + 1
            Else
                Exit Do
            End If
        Loop
        nDataPoints = nDataIndex

        'System.Diagnostics.Debug.Print(GetMessageTime(nDataIndex + 1) - GetMessageTime(nDataIndex))
        If GetMessageTime(nDataIndex + 1) > 0 And tmrPlayback.Enabled = True Then
            tmrPlayback.Interval = (GetMessageTime(nDataIndex + 1) - GetMessageTime(nDataIndex)) / 10000 + 1
        End If
        sMessage = rawData(nDataIndex)
        TrackBar1.Value = nDataIndex

        oMessage = GetNextSentence(sMessage)
        UpdateVariables(oMessage)

        nDataIndex = nDataIndex + 1

    End Sub
    Private Function GetMessageTime(ByVal index As Long) As Long
        If InStr(rawData(index), ":") = 0 Then
            GetMessageTime = 0
        Else
            GetMessageTime = Mid(rawData(index), 1, InStr(rawData(index), ":") - 1)
        End If
    End Function
    Private Sub serialPortIn_DataReceived(ByVal sender As Object, ByVal e As System.IO.Ports.SerialDataReceivedEventArgs) Handles serialPortIn.DataReceived
        Dim bWasSearching As Boolean
        Dim nReadCount As Integer
        Dim nReadResult As Integer
        Dim nSentenceCount As Integer
        Dim oMessage As cMessage
        Dim nCount As Integer
        Dim nExistingLen As Integer
        Dim nOldTop As Long
        Dim nBandwith As Single

        bWasSearching = tmrSearch.Enabled
        If bWasSearching Then
            tmrSearch.Enabled = False
        End If
        If serialPortIn.IsOpen = True Then
            If serialPortIn.BytesToRead > 0 Then
                nReadCount = serialPortIn.BytesToRead 'Number of Bytes to read
                Dim cData(nReadCount - 1) As Byte

                nInputStringLength = nInputStringLength + nReadCount
                serialPortIn.Encoding = System.Text.Encoding.GetEncoding(28591) ' System.Text.Encoding.GetEncoding(65001)
                nReadResult = serialPortIn.Read(cData, 0, nReadCount)  'Reading the Data

                For Each b As Byte In cData
                    If b <> 0 Then
                        'System.Diagnostics.Debug.Assert(False)
                    End If
                    sBuffer = sBuffer & Chr(b)        'Ascii String
                    sCommandLineBuffer = sCommandLineBuffer & Chr(b)
                    'nInputStringLength = nInputStringLength + 1
                Next

                nSentenceCount = 0
                oMessage = GetNextSentence(sBuffer)
                Do While oMessage.ValidMessage = True
                    If dStartTime.Ticks = 0 Then
                        dStartTime = Now
                    End If
                    tmrSearch.Enabled = False
                    cboBaudRate.Text = serialPortIn.BaudRate

                    bWasSearching = False

                    UpdateVariables(oMessage)
                    oMessage = GetNextSentence(sBuffer)
                Loop
                If bWasSearching Then
                    tmrSearch.Enabled = True
                End If

                nOldTop = lstCommandLineOutput.TopIndex

                Try
                    If nCommandLineDelim = 1 Then
                        Do While InStr(sCommandLineBuffer, vbLf) <> 0
                            lstCommandLineOutput.Items.Add(Mid(sCommandLineBuffer, 1, InStr(sCommandLineBuffer, vbLf) - 1))
                            sCommandLineBuffer = Mid(sCommandLineBuffer, InStr(sCommandLineBuffer, vbLf) + 1)
                        Loop
                    ElseIf nCommandLineDelim = 2 Then
                        Do While InStr(sCommandLineBuffer, vbCr) <> 0
                            lstCommandLineOutput.Items.Add(Mid(sCommandLineBuffer, 1, InStr(sCommandLineBuffer, vbCr) - 1))
                            sCommandLineBuffer = Mid(sCommandLineBuffer, InStr(sCommandLineBuffer, vbCr) + 1)
                        Loop
                    ElseIf nCommandLineDelim = 3 Then
                        Do While InStr(sCommandLineBuffer, vbCrLf) <> 0
                            lstCommandLineOutput.Items.Add(Mid(sCommandLineBuffer, 1, InStr(sCommandLineBuffer, vbCrLf) - 2))
                            sCommandLineBuffer = Mid(sCommandLineBuffer, InStr(sCommandLineBuffer, vbCrLf) + 2)
                        Loop
                    Else
                        If lstCommandLineOutput.Items.Count = 0 Then
                            lstCommandLineOutput.Items.Add(sCommandLineBuffer)
                        Else
                            nExistingLen = Len(lstCommandLineOutput.Items(lstCommandLineOutput.Items.Count - 1))
                            If nExistingLen > 80 Then
                                lstCommandLineOutput.Items.Add(sCommandLineBuffer)
                            Else
                                lstCommandLineOutput.Items(lstCommandLineOutput.Items.Count - 1) = lstCommandLineOutput.Items(lstCommandLineOutput.Items.Count - 1) & sCommandLineBuffer
                            End If
                        End If
                        sCommandLineBuffer = ""
                    End If
                    For nCount = nMaxListboxRecords To lstCommandLineOutput.Items.Count - 1
                        lstCommandLineOutput.Items.RemoveAt(nMaxListboxRecords)
                    Next
                Catch
                End Try

                If chkCommandLineAutoScroll.Checked = True Then
                    lstCommandLineOutput.TopIndex = lstCommandLineOutput.Items.Count - 1
                Else
                    lstCommandLineOutput.TopIndex = nOldTop
                End If

                If nLastBandwidthCheck = 0 Then
                    nLastBandwidthCheck = Now.Ticks
                    nInputStringLength = 0
                ElseIf Now.Ticks - nLastBandwidthCheck > 10000000 Then
                    nBandwith = nInputStringLength / ((Now.Ticks - nLastBandwidthCheck) / 10000000) / (serialPortIn.BaudRate / 11)
                    'System.Diagnostics.Debug.Print("InputLength = " & nInputStringLength & ",Elapsed Time = " & ((Now.Ticks - nLastBandwidthCheck) / 10000000) & ",Baud Rate = " & serialPortIn.BaudRate)
                    If nBandwith > 0.9 Then
                        lblBandwidth.ForeColor = Color.Red
                    Else
                        lblBandwidth.ForeColor = Color.Black
                    End If
                    lblBandwidth.Text = (nBandwith).ToString("0.00%")
                    nLastBandwidthCheck = Now.Ticks
                    nInputStringLength = 0
                End If
            End If
        End If

    End Sub
    Private Sub UpdateVariables(ByVal oMessage As cMessage)
        Dim sSplit() As String
        Dim sValues() As String
        Dim nCount As Integer
        Dim nAltChange As Single
        Dim sTemp As String

        GpS_Parser1_RawPacket(oMessage.RawMessage)
        If serialPortIn.IsOpen = False Then
            lstInbound.Items.Insert(0, "#" & nDataIndex & " - " & oMessage.VisibleSentence)
        Else
            lstInbound.Items.Insert(0, oMessage.VisibleSentence)
        End If
        Try
            For nCount = nMaxListboxRecords To lstInbound.Items.Count - 1
                lstInbound.Items.RemoveAt(nMaxListboxRecords)
            Next
        Catch
        End Try


        With oMessage
            Select Case .MessageType
                Case cMessage.e_MessageType.e_MessageType_NMEA
                    lblGPSType.Text = "NMEA"
                    sSplit = Split(.Packet, ",")
                    lblGPSMessage.Text = UCase(sSplit(0))
                    Select Case UCase(sSplit(0))
                        Case "GPRMC"
                            nLatitude = ConvertLatLongFormat(Convert.ToSingle(sSplit(3)), e_LatLongFormat.e_LatLongFormat_DDMM_MMMM, eOutputLatLongFormat, True)
                            If sSplit(4) = "S" Then
                                nLatitude = nLatitude * -1
                            End If
                            nLongitude = ConvertLatLongFormat(Convert.ToSingle(sSplit(5)), e_LatLongFormat.e_LatLongFormat_DDMM_MMMM, eOutputLatLongFormat, False)
                            If sSplit(6) = "W" Then
                                nLongitude = nLongitude * -1
                            End If
                            nGroundSpeed = ConvertSpeed(sSplit(7), e_SpeedFormat.e_SpeedFormat_Knots, eOutputSpeed)
                            nHeading = Convert.ToSingle(sSplit(8))
                            bNewGPS = True
                        Case "GPGGA"
                            nLatitude = ConvertLatLongFormat(Convert.ToSingle(sSplit(2)), e_LatLongFormat.e_LatLongFormat_DDMM_MMMM, eOutputLatLongFormat, True)
                            If sSplit(3) = "S" Then
                                nLatitude = nLatitude * -1
                            End If
                            nLongitude = ConvertLatLongFormat(Convert.ToSingle(sSplit(4)), e_LatLongFormat.e_LatLongFormat_DDMM_MMMM, eOutputLatLongFormat, False)
                            If sSplit(5) = "W" Then
                                nLongitude = nLongitude * -1
                            End If
                            nFix = Convert.ToInt32(sSplit(6))
                            nSats = Convert.ToSingle(sSplit(7))
                            nHDOP = Convert.ToSingle(sSplit(8))
                            nAltitude = ConvertDistance(sSplit(9), e_DistanceFormat.e_DistanceFormat_Meters, eOutputDistance)
                            bNewGPS = True
                        Case "GPGLL"
                            nLatitude = ConvertLatLongFormat(Convert.ToSingle(sSplit(1)), e_LatLongFormat.e_LatLongFormat_DDMM_MMMM, eOutputLatLongFormat, True)
                            If sSplit(2) = "S" Then
                                nLatitude = nLatitude * -1
                            End If
                            nLongitude = ConvertLatLongFormat(Convert.ToSingle(sSplit(3)), e_LatLongFormat.e_LatLongFormat_DDMM_MMMM, eOutputLatLongFormat, False)
                            If sSplit(4) = "W" Then
                                nLongitude = nLongitude * -1
                            End If
                            bNewGPS = True
                        Case "GPVTG"
                            nHeading = Convert.ToSingle(sSplit(1))
                            nGroundSpeed = ConvertSpeed(sSplit(7), e_SpeedFormat.e_SpeedFormat_KPH, eOutputSpeed)
                            bNewGPS = True
                    End Select

                Case cMessage.e_MessageType.e_MessageType_ArduPilot_WPCount
                    nWaypointTotal = Convert.ToInt32(Trim(Mid(.Packet, 10)))

                Case cMessage.e_MessageType.e_MessageType_ArduPilot_Home
                    sSplit = Split(.Packet, vbTab)
                    nLatitude = ConvertLatLongFormat((Convert.ToSingle(sSplit(1)) / 10000000), e_LatLongFormat.e_LatLongFormat_DD_DDDDDD, eOutputLatLongFormat, True)
                    nLongitude = ConvertLatLongFormat((Convert.ToSingle(sSplit(2)) / 10000000), e_LatLongFormat.e_LatLongFormat_DD_DDDDDD, eOutputLatLongFormat, False)
                    nAltitude = ConvertDistance(Convert.ToSingle(sSplit(3)) / 100, e_DistanceFormat.e_DistanceFormat_Meters, e_DistanceFormat.e_DistanceFormat_Feet)

                    WebBrowser1.Invoke(New MyDelegate(AddressOf setHomeLatLng))

                Case cMessage.e_MessageType.e_MessageType_ArduPilot_WP
                    sSplit = Split(.Packet, vbTab)
                    nWaypoint = Convert.ToUInt32(Trim(Mid(sSplit(0), 5, 2)))
                    nLatitude = ConvertLatLongFormat((Convert.ToSingle(sSplit(1)) / 10000000), e_LatLongFormat.e_LatLongFormat_DD_DDDDDD, eOutputLatLongFormat, True)
                    nLongitude = ConvertLatLongFormat((Convert.ToSingle(sSplit(2)) / 10000000), e_LatLongFormat.e_LatLongFormat_DD_DDDDDD, eOutputLatLongFormat, False)
                    nAltitude = ConvertDistance(Convert.ToSingle(sSplit(3)) / 100, e_DistanceFormat.e_DistanceFormat_Meters, e_DistanceFormat.e_DistanceFormat_Feet)
                    'nAltitude = Convert.ToSingle(sSplit(3)) / 100

                    WebBrowser1.Invoke(New MyDelegate(AddressOf addWaypoint))

                    If nWaypoint = nWaypointTotal Then
                        WebBrowser1.Invoke(New MyDelegate(AddressOf drawHomeLine))
                    End If

                Case cMessage.e_MessageType.e_MessageType_MediaTek
                    lblGPSType.Text = "MediaTek/MTK"
                    lblGPSMessage.Text = "Custom Binary"
                    nLatitude = ConvertLatLongFormat(ConvertHexToDec(Mid(.Packet, 3, 4), False) / 1000000, e_LatLongFormat.e_LatLongFormat_DD_DDDDDD, eOutputLatLongFormat, True)
                    nLongitude = ConvertLatLongFormat(ConvertHexToDec(Mid(.Packet, 7, 4), False) / 1000000, e_LatLongFormat.e_LatLongFormat_DD_DDDDDD, eOutputLatLongFormat, False)
                    nAltitude = ConvertDistance(ConvertHexToDec(Mid(.Packet, 11, 4), False) / 100, e_DistanceFormat.e_DistanceFormat_Meters, eOutputDistance)
                    nGroundSpeed = ConvertSpeed(ConvertHexToDec(Mid(.Packet, 15, 4), False) / 27.7777778, e_SpeedFormat.e_SpeedFormat_KPH, eOutputSpeed)
                    nHeading = ConvertHexToDec(Mid(.Packet, 19, 4), False) / 1000000
                    nSats = ConvertHexToDec(Mid(.Packet, 23, 1), False)
                    nFix = ConvertHexToDec(Mid(.Packet, 24, 1), False) - 1
                    bNewGPS = True

                Case cMessage.e_MessageType.e_MessageType_uBlox
                    lblGPSType.Text = "uBlox Binary"
                    Select Case Asc(Mid(.Packet, 2, 1))
                        Case 2 'NAV_POSLLH
                            lblGPSMessage.Text = "NAV_POSLLH"
                            nLongitude = ConvertLatLongFormat(ConvertHexToDec(Mid(.Packet, 9, 4)) / 10000000, e_LatLongFormat.e_LatLongFormat_DD_DDDDDD, eOutputLatLongFormat, False)
                            nLatitude = ConvertLatLongFormat(ConvertHexToDec(Mid(.Packet, 13, 4)) / 10000000, e_LatLongFormat.e_LatLongFormat_DD_DDDDDD, eOutputLatLongFormat, True)
                            nAltitude = ConvertDistance(ConvertHexToDec(Mid(.Packet, 17, 4)) / 1000, e_DistanceFormat.e_DistanceFormat_Meters, eOutputDistance)
                            bNewGPS = True
                        Case 18 'NAV_VELNED
                            lblGPSMessage.Text = "NAV_VELNED"
                            nGroundSpeed = (ConvertSpeed(ConvertHexToDec(Mid(.Packet, 25, 4)) / 100, e_SpeedFormat.e_SpeedFormat_MPerSec, eOutputSpeed)).ToString("#.0")
                            nHeading = (ConvertHexToDec(Mid(.Packet, 29, 4)) / 100000).ToString("#.0")
                            bNewGPS = True
                        Case 3 'NAV_STATUS
                            lblGPSMessage.Text = "NAV_STATUS"
                            nFix = ConvertHexToDec(Mid(.Packet, 9, 1))
                            bNewGPS = True
                        Case 6 'NAV_SOL
                            lblGPSMessage.Text = "NAV_SOL"
                            nSats = ConvertHexToDec(Mid(.Packet, 52, 1))
                            bNewGPS = True
                        Case 4 'NAV_DOP
                            lblGPSMessage.Text = "NAV_DOP"
                            nHDOP = ConvertHexToDec(Mid(.Packet, 17, 2)) / 100
                            bNewGPS = True
                    End Select

                Case cMessage.e_MessageType.e_MessageType_ArduPilot_ModeChange
                    lblGPSType.Text = "ArduPilot ASCII"
                    lblGPSMessage.Text = "Mode Change"
                    sSplit = Split(.Packet, vbTab)
                    sMode = sSplit(0)
                    nMode = Convert.ToInt32(sSplit(1))
                    bNewWaypoint = True

                Case cMessage.e_MessageType.e_MessageType_ArduPilot_Attitude, cMessage.e_MessageType.e_MessageType_ArduPilot_GPS
                    lblGPSType.Text = "ArduPilot ASCII"
                    lblGPSMessage.Text = "ArduPilot Message"
                    sSplit = Split(.Packet, ",")
                    Try
                        For nCount = 0 To UBound(sSplit)
                            sValues = Split(sSplit(nCount), ":")
                            Select Case UCase(sValues(0))
                                Case "RLL"
                                    nRoll = -Convert.ToSingle(sValues(1))
                                    bNewAttitude = True
                                Case "PCH"
                                    nPitch = -Convert.ToSingle(sValues(1))
                                    bNewAttitude = True
                                Case "CRS", "COG"
                                    nHeading = Convert.ToSingle(sValues(1))
                                    nYaw = nHeading
                                    bNewAttitude = True

                                Case "LAT"
                                    nLatitude = ConvertLatLongFormat(Convert.ToSingle(sValues(1)) / 1000000, e_LatLongFormat.e_LatLongFormat_DD_DDDDDD, eOutputLatLongFormat, True)
                                    bNewGPS = True
                                Case "LON"
                                    nLongitude = ConvertLatLongFormat(Convert.ToSingle(sValues(1)) / 1000000, e_LatLongFormat.e_LatLongFormat_DD_DDDDDD, eOutputLatLongFormat, False)
                                    bNewGPS = True
                                Case "SOG", "SPD"
                                    nGroundSpeed = ConvertSpeed(sValues(1), e_SpeedFormat.e_SpeedFormat_MPerSec, eOutputSpeed)
                                    bNewGPS = True
                                Case "ASP"
                                    nAirSpeed = ConvertSpeed(sValues(1), e_SpeedFormat.e_SpeedFormat_MPerSec, eOutputSpeed)
                                    bNewGPS = True
                                Case "FIX", "LOC"
                                    nFix = System.Math.Abs(Convert.ToInt32(sValues(1)) - 1)
                                    bNewGPS = True
                                Case "ALT"
                                    nAltitude = ConvertDistance(sValues(1), e_DistanceFormat.e_DistanceFormat_Meters, eOutputDistance)
                                    bNewGPS = True
                                Case "SAT"
                                    nSats = Convert.ToInt32(sValues(1))
                                    bNewGPS = True
                                Case "BTV"
                                    nBattery = (Convert.ToSingle(sValues(1)) / 1000).ToString("#.00")
                                    bNewWaypoint = True

                                Case "TXS", "STT"
                                    nMode = Convert.ToInt32(sValues(1))
                                    sMode = ""
                                    bNewWaypoint = True
                                Case "WPN"
                                    nWaypoint = Convert.ToInt32(sValues(1))
                                    bNewWaypoint = True
                                Case "THH"
                                    nThrottle = Convert.ToString(sValues(1))
                                    bNewWaypoint = True
                                Case "DST"
                                    nDistance = ConvertDistance(sValues(1), e_DistanceFormat.e_DistanceFormat_Meters, eOutputDistance)
                                    bNewWaypoint = True
                                Case "ALH"
                                    nWaypointAlt = ConvertDistance(sValues(1), e_DistanceFormat.e_DistanceFormat_Meters, eOutputDistance)
                                    bNewWaypoint = True
                            End Select
                        Next
                    Catch e2 As Exception
                    End Try

                Case cMessage.e_MessageType.e_MessageType_SiRF
                    lblGPSType.Text = "SiRF"
                    Select Case .ID
                        Case 91
                            lblGPSMessage.Text = "Geonavigation Data"
                            nFix = ConvertHexToDec(Mid(.Packet, 4, 1)) And 3
                            nLatitude = ConvertLatLongFormat(ConvertHexToDec(Mid(.Packet, 24, 4), False) / 10000000, e_LatLongFormat.e_LatLongFormat_DD_DDDDDD, eOutputLatLongFormat, True)
                            nLongitude = ConvertLatLongFormat(ConvertHexToDec(Mid(.Packet, 28, 4), False) / 10000000, e_LatLongFormat.e_LatLongFormat_DD_DDDDDD, eOutputLatLongFormat, False)
                            nAltitude = ConvertDistance(ConvertHexToDec(Mid(.Packet, 36, 4), False) / 100, e_DistanceFormat.e_DistanceFormat_Meters, eOutputDistance)
                            nGroundSpeed = ConvertSpeed(ConvertHexToDec(Mid(.Packet, 41, 2), False) / 100, e_SpeedFormat.e_SpeedFormat_MPerSec, eOutputSpeed)
                            nHeading = ConvertHexToDec(Mid(.Packet, 43, 2), False, False) / 100
                            nSats = ConvertHexToDec(Mid(.Packet, 89, 1))
                            nHDOP = ConvertHexToDec(Mid(.Packet, 90, 1)) / 5
                            bNewGPS = True
                    End Select

                Case cMessage.e_MessageType.e_MessageType_ArduIMU_Binary
                    lblGPSType.Text = "ArduIMU Binary"
                    Select Case Asc(Mid(.Packet, 2, 1))
                        Case 2
                            lblGPSMessage.Text = "Attitude Data"
                            nRoll = ConvertHexToDec(Mid(.Packet, 3, 2)) / 100
                            nPitch = ConvertHexToDec(Mid(.Packet, 5, 2)) / 100
                            nYaw = ConvertHexToDec(Mid(.Packet, 7, 2)) / 100
                            bNewAttitude = True
                        Case 3
                            lblGPSMessage.Text = "GPS Data"
                            nLongitude = ConvertLatLongFormat(ConvertHexToDec(Mid(.Packet, 3, 4)) / 10000000, e_LatLongFormat.e_LatLongFormat_DD_DDDDDD, eOutputLatLongFormat, False)
                            nLatitude = ConvertLatLongFormat(ConvertHexToDec(Mid(.Packet, 7, 4)) / 10000000, e_LatLongFormat.e_LatLongFormat_DD_DDDDDD, eOutputLatLongFormat, True)
                            nAltitude = ConvertDistance(ConvertHexToDec(Mid(.Packet, 11, 2)), e_DistanceFormat.e_DistanceFormat_Meters, eOutputDistance)
                            'System.Diagnostics.Debug.Print("Speed=" & ConvertHexToDec(Mid(.Packet, 13, 2)) / 100)
                            nGroundSpeed = ConvertSpeed(ConvertHexToDec(Mid(.Packet, 13, 2), , False) / 100, e_SpeedFormat.e_SpeedFormat_MPerSec, eOutputSpeed)
                            nHeading = ConvertHexToDec(Mid(.Packet, 15, 2), , False) / 100
                            If .PacketLength >= 31 Then
                                nSats = Asc(Mid(.Packet, 30))
                                nFix = Asc(Mid(.Packet, 31))
                            End If
                            bNewGPS = True

                            'System.Diagnostics.Debug.Print("Lat=" & sLatitude & ",Long=" & sLongitude)
                    End Select
            End Select
        End With

        If bNewAttitude = True Then
            If Now.Ticks - nLastAttitude > (1000 / (cboAttitude.SelectedIndex)) * 10000 And cboAttitude.SelectedIndex <> 0 Then
                nLastAttitude = Now.Ticks
                bNewAttitude = False
                GpS_Parser1_AttitudeChange1(nPitch, nRoll, nHeading)
                lstEvents.Items.Insert(0, "Pitch=" & nPitch & ",Roll=" & nRoll & ",Yaw=" & nYaw)
            End If
        End If

        If bNewGPS = True Then
            If Now.Ticks - nLastGPS > (1000 / (cboGPS.SelectedIndex)) * 10000 And cboGPS.SelectedIndex <> 0 Then
                'System.Diagnostics.Debug.Print("Diff=" & Now.Ticks - nLastGPS)
                If nAltitude <> 0 And nLastAlt <> 0 Then
                    nAltChange = -(nLastAlt - nAltitude) / ((Now.Ticks - nLastGPS) / 600000000)
                End If
                nLastAlt = nAltitude
                nLastGPS = Now.Ticks
                bNewGPS = False
                GpS_Parser1_GpsData1(nLatitude, nLongitude, nAltitude, nGroundSpeed, nHeading, nSats, nFix, nHDOP, nAltChange, nAirSpeed)
                lstEvents.Items.Insert(0, "Lat=" & nLatitude & ",Long=" & nLongitude & ",Alt=" & nAltitude & ",Spd=" & nGroundSpeed & ",Hdg=" & nHeading & ",Sats=" & nSats & ",Fix=" & nFix & ",HDOP=" & nHDOP)
            End If
        End If

        If bNewWaypoint = True Then
            If Now.Ticks - nLastWaypoint > (1000 / (cboWaypoint.SelectedIndex)) * 10000 And cboWaypoint.SelectedIndex <> 0 Then
                nLastWaypoint = Now.Ticks
                bNewWaypoint = False
                GPS_Parser1_Waypoints(nWaypoint, nDistance, nBattery, nMode, nThrottle)
                lstEvents.Items.Insert(0, "WP#=" & nWaypoint & ",Dist=" & nDistance & ",Battery=" & nBattery & ",Mode=" & nMode & ",Throttle=" & nThrottle)
            End If
        End If
        Try
            For nCount = nMaxListboxRecords To lstEvents.Items.Count - 1
                lstEvents.Items.RemoveAt(nMaxListboxRecords)
            Next
        Catch
        End Try
    End Sub

    Private Sub serialPortIn_Disposed(ByVal sender As Object, ByVal e As System.EventArgs) Handles serialPortIn.Disposed
        System.Diagnostics.Debug.Print("Serial Port Disposed")
        EnableComButtons(True)
    End Sub

    Private Sub serialPortIn_ErrorReceived(ByVal sender As Object, ByVal e As System.IO.Ports.SerialErrorReceivedEventArgs) Handles serialPortIn.ErrorReceived
        'System.Diagnostics.Debug.Assert(False)
        System.Diagnostics.Debug.Print("serialPortIn_ErrorReceived - " & e.ToString)
    End Sub

    Private Sub cboComPort_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cboComPort.SelectedIndexChanged
        Dim bOpenState As Boolean

        bOpenState = serialPortIn.IsOpen
        If bOpenState Then
            serialPortIn.Close()
        End If
        serialPortIn.PortName = cboComPort.Text
        If bOpenState Then
            serialPortIn.Open()
        End If
    End Sub

    Private Sub cmdConnect_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdConnect.Click

        tmrSearch.Enabled = False
        If serialPortIn.IsOpen = True Then
            cmdConnect.Text = "Connect"
            Try
                serialPortIn.Close()
            Catch e2 As Exception
            End Try
            lblComPortStatus.Text = "Disconnected from " & serialPortIn.PortName
            EnableComButtons(True)
            lblGPSType.Text = ""
            lblGPSMessage.Text = ""
        Else
            nLastComPort = -1
            nBaudRateIndex = -1
            lblGPSType.Text = ""
            lblGPSMessage.Text = ""
            tmrSearch_Tick(sender, e)
        End If
        lblBandwidth.Text = ""
    End Sub

    Private Sub EnableComButtons(ByVal enable As Boolean)
        cboComPort.Enabled = enable
        cboBaudRate.Enabled = enable
        cmdSearch.Enabled = enable
        cmdSearchCOM.Enabled = enable
        cmdReloadComPorts.Enabled = enable
    End Sub
    Private Sub cmdSearchCOM_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdSearchCOM.Click
        nLastComPort = 0
        nBaudRateIndex = cboBaudRate.SelectedIndex
        lblGPSType.Text = ""
        lblGPSMessage.Text = ""
        tmrSearch.Enabled = True
    End Sub

    Private Sub cboAttitude_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cboAttitude.SelectedIndexChanged
        Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "Attitude Hz", cboAttitude.SelectedIndex)
    End Sub

    Private Sub cboGPS_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cboGPS.SelectedIndexChanged
        Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "GPS Hz", cboGPS.SelectedIndex)
    End Sub

    Private Sub cboWaypoint_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cboWaypoint.SelectedIndexChanged
        Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "Waypoint Hz", cboWaypoint.SelectedIndex)
    End Sub

    Private Sub cmdSearch_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdSearch.Click
        nLastComPort = -1
        'nLastComPort = cboBaudRate.SelectedIndex
        nBaudRateIndex = cboBaudRate.SelectedIndex
        lblGPSType.Text = ""
        lblGPSMessage.Text = ""
        tmrSearch.Enabled = True
    End Sub

    Private Sub tmrSearch_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tmrSearch.Tick
        Try
            nAirSpeed = -1
            tmrSearch.Interval = 200
            If serialPortIn.IsOpen = True Then
                serialPortIn.Close()
            End If
            If nLastComPort = -1 Then
                serialPortIn.PortName = cboComPort.Text
            Else
                serialPortIn.PortName = cboComPort.Items(nLastComPort).ToString
            End If
            cboComPort.Text = serialPortIn.PortName
            lstInbound.Items.Insert(0, "Checking " & cboBaudRate.Text & " baud")
            serialPortIn.BaudRate = cboBaudRate.Text
            Try
                With serialPortIn
                    '.BaseStream.Dispose()
                    '.DataBits = 8
                    '.StopBits = 1
                    '.Parity = Ports.Parity.None
                    '.ReadTimeout = 20
                    sBuffer = ""
                    sCommandLineBuffer = ""
                    lstCommandLineOutput.Items.Clear()
                    dStartTime = Nothing
                    .Open()
                End With
                'serialPortIn.BytesToRead()
                Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "COM Port", cboComPort.Text)
                Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "Baud Rate", cboBaudRate.Text)
                cmdConnect.Text = "Disconnect"
                lblComPortStatus.Text = "Connected to " & serialPortIn.PortName & " at " & serialPortIn.BaudRate
                EnableComButtons(False)
            Catch e3 As Exception
                lblComPortStatus.Text = e3.Message
                nBaudRateIndex = UBound(baudRates)
                cmdConnect.Text = "Connect"
                lblGPSType.Text = ""
                lblGPSMessage.Text = ""
                EnableComButtons(True)
            End Try
        Catch e2 As Exception
        End Try
        If nBaudRateIndex > -1 Then
            nBaudRateIndex = nBaudRateIndex + 1
            If nBaudRateIndex > UBound(baudRates) Then
                nBaudRateIndex = 0
                If nLastComPort > -1 Then
                    nLastComPort = nLastComPort + 1
                    If nLastComPort > cboComPort.Items.Count - 1 Then
                        nLastComPort = 0
                    End If
                End If
            End If
            cboBaudRate.Text = baudRates(nBaudRateIndex)
        End If
    End Sub

    Private Sub cboDistanceUnits_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cboDistanceUnits.SelectedIndexChanged
        Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "Distance Units", cboDistanceUnits.SelectedIndex)
        eOutputDistance = cboDistanceUnits.SelectedIndex
        If eMapSelection = e_MapSelection.e_MapSelection_GoogleMaps Then
            webDocument.GetElementById("metersFeet").SetAttribute("value", cboDistanceUnits.SelectedIndex.ToString)
            cmdClearMap_Click(sender, e)
        End If
        AltimeterInstrumentControl1.SetAlimeterParameters(nAltitude, cboDistanceUnits.Text)
    End Sub

    Private Sub cboSpeedUnits_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cboSpeedUnits.SelectedIndexChanged
        Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "Speed Units", cboSpeedUnits.SelectedIndex)
        eOutputSpeed = cboSpeedUnits.SelectedIndex
        AirSpeedIndicatorInstrumentControl1.SetAirSpeedIndicatorParameters(nGroundSpeed, Convert.ToInt32(cboMaxSpeed.Text), "Speed", cboSpeedUnits.Text)
    End Sub

    Private Sub cboMaxSpeed_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cboMaxSpeed.SelectedIndexChanged
        Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "Max Speed", cboMaxSpeed.SelectedIndex)
        AirSpeedIndicatorInstrumentControl1.SetAirSpeedIndicatorParameters(nGroundSpeed, Convert.ToInt32(cboMaxSpeed.Text), "Speed", cboSpeedUnits.Text)
    End Sub

    Private Sub cmdExit_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdExit.Click
        Me.Dispose()
    End Sub

    Private Sub cboBaudRate_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cboBaudRate.SelectedIndexChanged
        nBaudRateIndex = cboBaudRate.SelectedIndex
        serialPortIn.BaudRate = cboBaudRate.Text
    End Sub

    Private Sub chkPlay_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkPlay.CheckedChanged
        If chkPlay.Checked = True Then
            If serialPortIn.IsOpen = True Then
                cmdConnect_Click(sender, e)
            End If
            If nDataIndex >= UBound(rawData) Then
                nDataIndex = 0
            End If
            nHomeLat = ""
            nHomeLong = ""
            'If eMapSelection = e_MapSelection.e_MapSelection_GoogleMaps Then
            cmdClearMap_Click(sender, e)
            'End If

            SetPlayerState(e_PlayerState.e_PlayerState_Play)

            tmrPlayback.Enabled = True
            tmrPlayback_Tick(sender, e)

        Else
            tmrPlayback.Enabled = False
            SetPlayerState(e_PlayerState.e_PlayerState_Pause)
        End If
    End Sub

    Private Sub TrackBar1_Scroll(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TrackBar1.Scroll
        nDataIndex = TrackBar1.Value
        SetPlayerState(e_PlayerState.e_PlayerState_Pause)
        tmrPlayback.Enabled = False
        tmrPlayback_Tick(sender, e)
    End Sub

    Private Sub chkPause_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkPause.CheckedChanged
        tmrPlayback.Enabled = False
        SetPlayerState(e_PlayerState.e_PlayerState_Pause)
    End Sub

    Private Sub chkStepForward_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkStepForward.CheckedChanged
        If chkStepForward.Checked = True Then
            tmrPlayback.Enabled = False

            tmrPlayback_Tick(sender, e)
            SetPlayerState(e_PlayerState.e_PlayerState_Pause)
        End If
    End Sub

    Private Sub chkStepBack_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkStepBack.CheckedChanged
        If chkStepBack.Checked = True Then
            SetPlayerState(e_PlayerState.e_PlayerState_Pause)
            tmrPlayback.Enabled = False

            nDataIndex = nDataIndex - 2
            tmrPlayback_Tick(sender, e)
        End If
    End Sub

    Private Sub cboOutputFiles_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles cboOutputFiles.TextChanged
        If Trim(cboOutputFiles.Text) <> "" Then
            If Dir(txtOutputFolder.Text & cboOutputFiles.Text & ".hko") <> "" Then
                LoadDataFile(txtOutputFolder.Text & cboOutputFiles.Text & ".hko")
                SetPlayerState(e_PlayerState.e_PlayerState_Loaded)
            Else
                SetPlayerState(e_PlayerState.e_PlayerState_RecordReady)
            End If
        End If
    End Sub

    Private Sub frmMain_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Me.Paint
        _3DMesh1.DrawMesh(IIf(bPitchReverse = True, -1, 1) * nPitch, IIf(bRollReverse = True, -1, 1) * nRoll, nHeading, False, s3DModel, System.AppDomain.CurrentDomain.BaseDirectory & "3D Models\")
        'System.Diagnostics.Debug.Print("Paint " & Now)
    End Sub

    Private Sub tabMapView_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles tabMapView.SelectedIndexChanged
        Select Case tabMapView.SelectedIndex
            Case 0
            Case 1
                If bFirstVideoCapture = False Then
                    DirectShowControl1.StartCapture()
                End If
                bFirstVideoCapture = True
        End Select

    End Sub

    'Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

    '    webDocument.InvokeScript("setHomeLatLng", New Object() {"41.254732", "-81.814360", "350"})

    '    webDocument.InvokeScript("addWaypoint", New Object() {"41.257678", "-81.81922", "390", "01"})
    '    webDocument.InvokeScript("addWaypoint", New Object() {"41.259582", "-81.815658", "600", "02"})
    '    webDocument.InvokeScript("addWaypoint", New Object() {"41.259646", "-81.80892", "420", "03"})
    '    webDocument.InvokeScript("addWaypoint", New Object() {"41.257775", "-81.805916", "500", "04"})
    '    webDocument.InvokeScript("addWaypoint", New Object() {"41.254732", "-81.814360", "350", "-1"})

    'End Sub

    Private Sub chkFlightExtrude_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkFlightExtrude.CheckedChanged
        bFlightExtrude = chkFlightExtrude.Checked
        Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "Flight Extrude", bFlightExtrude)
    End Sub

    Private Sub cmdFlightColor_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdFlightColor.Click
        If Me.ColorDialog1.ShowDialog = DialogResult.OK Then
            cmdFlightColor.BackColor = ColorDialog1.Color
            UpdateLineColor()
            Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "Flight Color", GetColor(cmdFlightColor.BackColor))
        End If
    End Sub
    Private Function GetColor(ByVal inputColor As System.Drawing.Color) As String
        GetColor = Hex(inputColor.A).PadLeft(2, "0")
        GetColor = GetColor & Hex(inputColor.R).PadLeft(2, "0")
        GetColor = GetColor & Hex(inputColor.G).PadLeft(2, "0")
        GetColor = GetColor & Hex(inputColor.B).PadLeft(2, "0")
    End Function
    Private Function GetSystemColor(ByVal inputString As String) As System.Drawing.Color
        GetSystemColor = New System.Drawing.Color
        GetSystemColor.FromArgb((Convert.ToInt32(Mid(inputString, 1, 2), 16)), (Convert.ToInt32(Mid(inputString, 3, 2), 16)), (Convert.ToInt32(Mid(inputString, 5, 2), 16)), (Convert.ToInt32(Mid(inputString, 7, 2), 16)))
    End Function
    Private Sub UpdateLineColor()
        Dim sTemp As String

        sTemp = Hex(cmdFlightColor.BackColor.ToArgb).PadLeft(8, "0")
        sFlightColor = Hex("255").PadLeft(2, "0") & Mid(sTemp, 7, 2) & Mid(sTemp, 5, 2) & Mid(sTemp, 3, 2)

        'sTemp = Hex(cmdWaypointColor.BackColor.ToArgb).PadLeft(8, "0")
        'sWaypointColor = Hex("255").PadLeft(2, "0") & Mid(sTemp, 7, 2) & Mid(sTemp, 5, 2) & Mid(sTemp, 3, 2)

    End Sub

    Private Sub tbarFlightWidth_Scroll(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tbarFlightWidth.Scroll
        nFlightWidth = tbarFlightWidth.Value
        Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "Flight Width", nFlightWidth)
    End Sub

    'Private Sub cmdWaypointColor_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
    '    If Me.ColorDialog1.ShowDialog = DialogResult.OK Then
    '        cmdWaypointColor.BackColor = ColorDialog1.Color
    '        UpdateLineColor()
    '    End If

    'End Sub

    'Private Sub chkWaypointExtrude_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs)
    '    bWaypointExtrude = chkWaypointExtrude.Checked
    '    Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "WP Extrude", bWaypointExtrude)
    'End Sub

    'Private Sub tbarWaypointWidth_Scroll(ByVal sender As System.Object, ByVal e As System.EventArgs)
    '    nWaypointWidth = tbarWaypointWidth.Value
    'End Sub

    Private Sub cbo3DModel_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cbo3DModel.SelectedIndexChanged
        Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "3D Model", cbo3DModel.Text)
        s3DModel = cbo3DModel.Text
        _3DMesh1.DrawMesh(IIf(bPitchReverse = True, -1, 1) * nPitch, IIf(bRollReverse = True, -1, 1) * nRoll, nYaw, True, s3DModel, System.AppDomain.CurrentDomain.BaseDirectory & "3D Models\")
        WebBrowser1.Invoke(New MyDelegate(AddressOf loadModel))
    End Sub

    Private Sub cboGoogleEarthCamera_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cboGoogleEarthCamera.SelectedIndexChanged
        Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "Camera Tracking", cboGoogleEarthCamera.SelectedIndex)
        nCameraTracking = cboGoogleEarthCamera.SelectedIndex
    End Sub

    Private Sub cboMission_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cboMission.SelectedIndexChanged
        ReadMission(cboMission.Text)
        If tabMapView.SelectedIndex = 0 Then
            WebBrowser1.Focus()
        End If
    End Sub
    Private Sub ReadMission(ByVal inputMission As String)
        Dim sMission As String
        Dim sSplit() As String
        Dim sSplit2() As String
        Dim nCount As Integer
        Dim nWPNumber As Integer
        sMission = GetFileContents(System.AppDomain.CurrentDomain.BaseDirectory & "Missions\" & inputMission)

        sSplit = Split(sMission, vbCrLf)
        If Mid(sSplit(0), 1, 5) <> "OPTIO" And Mid(sSplit(0), 1, 5) <> "HOME:" Then
            Call MsgBox("Invalid Mission File Format", MsgBoxStyle.Critical + MsgBoxStyle.OkOnly, "Invalid File")
            Exit Sub
        End If

        nWPNumber = 0
        For nCount = 0 To UBound(sSplit)
            If Trim(sSplit(nCount)) <> "" Then
                If Mid(sSplit(nCount), 1, 5) = "HOME:" Then
                    sSplit2 = Split(sSplit(nCount), ",")
                    Try
                        If eMapSelection = e_MapSelection.e_MapSelection_GoogleEarth Then
                            webDocument.InvokeScript("setHomeLatLng", New Object() {Mid(sSplit2(0), 6), sSplit2(1), ConvertDistance(sSplit2(2), e_DistanceFormat.e_DistanceFormat_Meters, e_DistanceFormat.e_DistanceFormat_Feet)})
                        End If
                    Catch e2 As Exception
                    End Try
                    nHomeLat = Mid(sSplit2(0), 6)
                    nHomeLong = sSplit2(1)
                ElseIf Mid(sSplit(nCount), 1, 5) = "OPTIO" Then
                Else
                    nWPNumber = nWPNumber + 1
                    sSplit2 = Split(sSplit(nCount), ",")
                    Try
                        If eMapSelection = e_MapSelection.e_MapSelection_GoogleEarth Then
                            webDocument.InvokeScript("addWaypoint", New Object() {sSplit2(0), sSplit2(1), ConvertDistance(sSplit2(2), e_DistanceFormat.e_DistanceFormat_Meters, e_DistanceFormat.e_DistanceFormat_Feet), nWPNumber.ToString.PadLeft(2, "0")})
                        End If
                    Catch e2 As Exception
                    End Try
                End If
            End If
        Next

        If nWPNumber > 0 Then
            Try
                If eMapSelection = e_MapSelection.e_MapSelection_GoogleEarth Then
                    webDocument.InvokeScript("drawHomeLine")
                End If
            Catch e2 As Exception
            End Try
        End If


    End Sub

    Private Sub cmdReloadMissions_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdReloadMissions.Click
        LoadMissions()
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        cboMission_SelectedIndexChanged(sender, e)
    End Sub

    Private Sub cboMapSelection_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cboMapSelection.SelectedIndexChanged
        Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "Map Selection", cboMapSelection.SelectedIndex)
        nHomeLat = ""
        nHomeLong = ""
        eMapSelection = cboMapSelection.SelectedIndex
        SetupWebBroswer()
    End Sub

    Private Sub chkPitchReverse_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkPitchReverse.CheckedChanged
        bPitchReverse = chkPitchReverse.Checked
        If chkPitchReverse.Checked = True Then
            chkPitchReverse.Text = "Reversed"
        Else
            chkPitchReverse.Text = "Normal"
        End If
        Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "Pitch Reverse", bPitchReverse)
    End Sub

    Private Sub chkRollReverse_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkRollReverse.CheckedChanged
        bRollReverse = chkRollReverse.Checked
        If chkRollReverse.Checked = True Then
            chkRollReverse.Text = "Reversed"
        Else
            chkRollReverse.Text = "Normal"
        End If
        Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "Roll Reverse", bRollReverse)
    End Sub

    Private Sub cmdCommandLineSend_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdCommandLineSend.Click
        If serialPortIn.IsOpen = True Then
            serialPortIn.Write(cboCommandLineCommand.Text)
            SaveComboEntries(cboCommandLineCommand)
        End If
    End Sub
    Private Sub SaveComboEntries(ByVal inputCombo As ComboBox)
        Dim nCount As Integer
        Dim nFoundIndex As Integer
        Dim sText As String

        With inputCombo
            sText = .Text
            nFoundIndex = -1
            For nCount = 0 To .Items.Count - 1
                If UCase(Trim(sText)) = UCase(Trim(.Items(nCount).ToString)) Then
                    nFoundIndex = nCount
                End If
            Next
            If nFoundIndex > -1 Then
                .Items.RemoveAt(nFoundIndex)
            End If
            .Items.Insert(0, Trim(sText))

            For nCount = 0 To .Items.Count - 1
                Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings\ComboSaves\" & .Name, "Index " & nCount, .Items(nCount).ToString)
            Next
            Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings\ComboSaves\" & .Name, "Total Count", .Items.Count)
            .Text = sText
        End With
    End Sub
    Private Sub LoadComboEntries(ByVal inputCombo As ComboBox)
        Dim nCount As Integer
        Dim nMax As Integer
        Dim sText As String
        With inputCombo
            nMax = GetRegSetting(sRootRegistry & "\GPS Parser\Settings\ComboSaves\" & .Name, "Total Count", 0)

            For nCount = 0 To nMax - 1
                sText = GetRegSetting(sRootRegistry & "\GPS Parser\Settings\ComboSaves\" & .Name, "Index " & nCount, "")
                If Trim(sText) <> "" Then
                    .Items.Add(Trim(sText))
                End If
            Next
        End With
    End Sub
    Private Sub cboCommandLineCommand_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles cboCommandLineCommand.GotFocus
        Me.AcceptButton = cmdCommandLineSend
    End Sub

    Private Sub cboCommandLineDelim_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles cboCommandLineDelim.SelectedIndexChanged
        SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "Command Line Delim", cboCommandLineDelim.SelectedIndex)
        nCommandLineDelim = cboCommandLineDelim.SelectedIndex
    End Sub

    Private Sub cmdReloadComPorts_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmdReloadComPorts.Click
        LoadComPorts(cboComPort.Text)
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        If serialPortIn.IsOpen = True Then
            dStartTime = Now
        Else
            dStartTime = New Date(Mid(rawData(nDataIndex), 1, InStr(rawData(nDataIndex), ":") - 1))
        End If
        lblRunTime.Text = ConvertToRunTime(dStartTime, dStartTime)
    End Sub

    Private Sub chkCommandLineAutoScroll_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles chkCommandLineAutoScroll.CheckedChanged
        SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "Command Line Auto Scroll", chkCommandLineAutoScroll.Checked)
    End Sub

    Private Sub cboMapUpdateRate_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cboMapUpdateRate.SelectedIndexChanged
        Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "Map Update Hz", cboMapUpdateRate.SelectedIndex + 1)
    End Sub

    Private Sub tbarModelScale_Scroll(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tbarModelScale.Scroll
        WebBrowser1.Invoke(New MyDelegate(AddressOf changeModelScale))
        Call SaveRegSetting(sRootRegistry & "\GPS Parser\Settings", "Model Scale", tbarModelScale.Value)
    End Sub
End Class