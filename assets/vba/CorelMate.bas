Attribute VB_Name = "CorelMate"
Option Explicit

' =========================================================================
' CORELMATE PRODUCTIVITY SUITE - NATIVE DIRECT WORKFLOWS (NO DOCKER)
' 1. Curves: Current Page, All Pages, Selected Objects
' 2. Export: ID Card Template (JSON + Editable PDF), Print PDF (Curves + CMYK)
' 3. Color: Convert to CMYK, Convert to RGB
' =========================================================================

' -------------------------------------------------------------------------
' FEATURE 1: CORELMATE CURVES
' -------------------------------------------------------------------------
Public Sub CurveCurrentPage()
    If ActiveDocument Is Nothing Then
        MsgBox "No document is open in CorelDRAW.", vbExclamation, "CorelMate Curves"
        Exit Sub
    End If
    
    If MsgBox("Convert all text on Current Page to curves?", vbYesNo + vbQuestion, "CorelMate Curves") <> vbYes Then Exit Sub
    
    Dim count As Long
    ActiveDocument.BeginCommandGroup "CorelMate: Curve Current Page"
    On Error Resume Next
    ActivePage.UnlockAllShapes
    count = ProcessCurvesInRange(ActivePage.Shapes.All)
    ActiveDocument.EndCommandGroup
    Application.CorelScript.RedrawScreen
    
    MsgBox "Curve conversion complete!" & vbCrLf & "Converted: " & count & " text object(s) on current page.", vbInformation, "CorelMate Curves"
End Sub

Public Sub CurveAllPages()
    If ActiveDocument Is Nothing Then
        MsgBox "No document is open in CorelDRAW.", vbExclamation, "CorelMate Curves"
        Exit Sub
    End If
    
    If MsgBox("Convert all text across ALL pages in the document to curves?", vbYesNo + vbQuestion, "CorelMate Curves") <> vbYes Then Exit Sub
    
    Dim count As Long, p As Page
    ActiveDocument.BeginCommandGroup "CorelMate: Curve All Pages"
    On Error Resume Next
    For Each p In ActiveDocument.Pages
        p.UnlockAllShapes
        count = count + ProcessCurvesInRange(p.Shapes.All)
    Next p
    ActiveDocument.EndCommandGroup
    Application.CorelScript.RedrawScreen
    
    MsgBox "Curve conversion complete!" & vbCrLf & "Converted: " & count & " text object(s) across " & ActiveDocument.Pages.Count & " page(s).", vbInformation, "CorelMate Curves"
End Sub

Public Sub CurveSelection()
    If ActiveDocument Is Nothing Then
        MsgBox "No document is open in CorelDRAW.", vbExclamation, "CorelMate Curves"
        Exit Sub
    End If
    
    If ActiveSelectionRange.Count = 0 Then
        MsgBox "Please select one or more text objects first.", vbExclamation, "CorelMate Curves"
        Exit Sub
    End If
    
    If MsgBox("Convert selected text objects to curves?", vbYesNo + vbQuestion, "CorelMate Curves") <> vbYes Then Exit Sub
    
    Dim count As Long
    ActiveDocument.BeginCommandGroup "CorelMate: Curve Selection"
    On Error Resume Next
    ActiveDocument.Selection.Unlock
    count = ProcessCurvesInRange(ActiveSelectionRange)
    ActiveDocument.EndCommandGroup
    Application.CorelScript.RedrawScreen
    
    MsgBox "Curve conversion complete!" & vbCrLf & "Converted: " & count & " selected text object(s).", vbInformation, "CorelMate Curves"
End Sub

Private Function ProcessCurvesInRange(sr As ShapeRange) As Long
    Dim col As New Collection, s As Shape, c As Long, i As Long
    CollectTextShapes sr, col
    c = 0
    On Error Resume Next
    For i = col.Count To 1 Step -1
        Set s = col(i)
        If s.Locked Then s.Locked = False
        s.ConvertToCurves
        c = c + 1
    Next i
    ProcessCurvesInRange = c
End Function

Private Sub CollectTextShapes(sr As ShapeRange, ByRef col As Collection)
    Dim s As Shape
    On Error Resume Next
    For Each s In sr
        If s.Type = cdrGroupShape Then
            CollectTextShapes s.Shapes.All, col
        ElseIf s.Type = cdrTextShape Then
            col.Add s
        End If
        If Not s.PowerClip Is Nothing Then
            CollectTextShapes s.PowerClip.Shapes.All, col
        End If
    Next s
End Sub

' -------------------------------------------------------------------------
' FEATURE 2: CORELMATE EXPORT
' -------------------------------------------------------------------------
Public Sub ExportAsIdCard()
    If ActiveDocument Is Nothing Then
        MsgBox "No document is open in CorelDRAW.", vbExclamation, "CorelMate Export"
        Exit Sub
    End If
    
    If MsgBox("Export ID Card template (metadata JSON + editable PDF)?", vbYesNo + vbQuestion, "CorelMate Export") <> vbYes Then Exit Sub
    
    On Error GoTo ExportIdCard_Err
    Dim docPath As String, baseName As String, outDir As String
    docPath = ActiveDocument.FullFileName
    If Len(docPath) > 0 Then
        outDir = ActiveDocument.FilePath
        baseName = Left(ActiveDocument.FileName, InStrRev(ActiveDocument.FileName, ".") - 1)
    Else
        outDir = CreateObject("WScript.Shell").SpecialFolders("MyDocuments")
        baseName = "Untitled_IDCard"
    End If
    If Right(outDir, 1) <> "\" Then outDir = outDir & "\"
    
    Dim pdfPath As String, jsonPath As String
    pdfPath = outDir & baseName & "_Template.pdf"
    jsonPath = outDir & baseName & "_Fields.json"
    
    Dim json As String, s As Shape, fCount As Long
    json = "{" & vbCrLf & "  ""document"": """ & baseName & """," & vbCrLf
    json = json & "  ""pageWidthMm"": " & FormatNumber(ActivePage.SizeWidth, 2) & "," & vbCrLf
    json = json & "  ""pageHeightMm"": " & FormatNumber(ActivePage.SizeHeight, 2) & "," & vbCrLf
    json = json & "  ""fields"": [" & vbCrLf
    
    Dim first As Boolean: first = True
    fCount = 0
    Dim col As New Collection, i As Long
    CollectTextShapes ActivePage.Shapes.All, col
    For i = 1 To col.Count
        Set s = col(i)
        If Not first Then json = json & "," & vbCrLf
        first = False
        fCount = fCount + 1
        
        Dim clrHex As String: clrHex = "#000000"
        If s.Fill.Type = cdrUniformFill Then
            Dim c As Color: Set c = s.Fill.UniformColor
            clrHex = Right("000000" & Hex(RGB(c.RGBRed, c.RGBGreen, c.RGBBlue)), 6)
        End If
        
        Dim cleanText As String
        cleanText = Replace(Replace(s.Text.Story.Text, vbCrLf, " "), vbLf, " ")
        cleanText = Replace(Replace(cleanText, Chr(34), "\" & Chr(34)), vbCr, "")
        
        json = json & "    {" & vbCrLf
        json = json & "      ""name"": """ & s.Name & """," & vbCrLf
        json = json & "      ""text"": """ & cleanText & """," & vbCrLf
        json = json & "      ""font"": """ & s.Text.Story.Font & """," & vbCrLf
        json = json & "      ""sizePt"": " & FormatNumber(s.Text.Story.Size, 1) & "," & vbCrLf
        json = json & "      ""xMm"": " & FormatNumber(s.PositionX, 2) & "," & vbCrLf
        json = json & "      ""yMm"": " & FormatNumber(s.PositionY, 2) & "," & vbCrLf
        json = json & "      ""widthMm"": " & FormatNumber(s.SizeWidth, 2) & "," & vbCrLf
        json = json & "      ""heightMm"": " & FormatNumber(s.SizeHeight, 2) & "," & vbCrLf
        json = json & "      ""color"": """ & clrHex & """" & vbCrLf
        json = json & "    }"
    Next i
    json = json & vbCrLf & "  ]" & vbCrLf & "}"
    
    Dim fso As Object, ts As Object
    Set fso = CreateObject("Scripting.FileSystemObject")
    Set ts = fso.CreateTextFile(jsonPath, True, False)
    ts.Write json
    ts.Close
    
    With ActiveDocument.PDFSettings
        .Reset
        .PublishRange = cdrCurrentPage
        .PageRange = CStr(ActivePage.Index)
        .ColorMode = cdrPDFColorModeNative
        .TextAsCurves = False
        .EmbedBaseFonts = True
        .EmbedFonts = True
        .Linearize = False
    End With
    ActiveDocument.PublishToPDF pdfPath
    
    MsgBox "ID Card template successfully exported!" & vbCrLf & vbCrLf & "PDF: " & pdfPath & vbCrLf & "Data: " & jsonPath & vbCrLf & "Fields Captured: " & fCount, vbInformation, "CorelMate Export"
    Exit Sub
    
ExportIdCard_Err:
    MsgBox "ID Card export failed: " & Err.Description, vbCritical, "CorelMate Export"
End Sub

Public Sub ExportAsPrint()
    If ActiveDocument Is Nothing Then
        MsgBox "No document is open in CorelDRAW.", vbExclamation, "CorelMate Export"
        Exit Sub
    End If
    
    If MsgBox("Export high-quality Print PDF (curving all text & converting all colors to CMYK)?", vbYesNo + vbQuestion, "CorelMate Export") <> vbYes Then Exit Sub
    
    On Error GoTo ExportPrint_Err
    Dim docPath As String, baseName As String, outDir As String
    docPath = ActiveDocument.FullFileName
    If Len(docPath) > 0 Then
        outDir = ActiveDocument.FilePath
        baseName = Left(ActiveDocument.FileName, InStrRev(ActiveDocument.FileName, ".") - 1)
    Else
        outDir = CreateObject("WScript.Shell").SpecialFolders("MyDocuments")
        baseName = "Untitled_Print"
    End If
    If Right(outDir, 1) <> "\" Then outDir = outDir & "\"
    
    Dim pdfPath As String
    pdfPath = outDir & baseName & "_Print_CMYK.pdf"
    
    With ActiveDocument.PDFSettings
        .Reset
        .PublishRange = cdrCurrentPage
        .PageRange = CStr(ActivePage.Index)
        .ColorMode = cdrPDFColorModeCMYK
        .TextAsCurves = True
        .BitmapCompression = cdrPDFCompressionNone
        .DownsampleColor = False
        .DownsampleGray = False
        .DownsampleMono = False
        .CompressColor = False
        .Linearize = False
        .VectorResolution = 600
    End With
    ActiveDocument.PublishToPDF pdfPath
    
    MsgBox "Print-ready CMYK PDF exported successfully!" & vbCrLf & vbCrLf & "Destination: " & pdfPath, vbInformation, "CorelMate Export"
    Exit Sub
    
ExportPrint_Err:
    MsgBox "Print PDF export failed: " & Err.Description, vbCritical, "CorelMate Export"
End Sub

' -------------------------------------------------------------------------
' FEATURE 3: CORELMATE COLOR
' -------------------------------------------------------------------------
Public Sub ConvertToCmyk()
    If ActiveDocument Is Nothing Then
        MsgBox "No document is open in CorelDRAW.", vbExclamation, "CorelMate Color"
        Exit Sub
    End If
    
    If MsgBox("Convert all vector colors and bitmap images to CMYK?", vbYesNo + vbQuestion, "CorelMate Color") <> vbYes Then Exit Sub
    
    Dim sr As ShapeRange
    If ActiveSelectionRange.Count > 0 Then
        Set sr = ActiveSelectionRange
    Else
        Set sr = ActivePage.Shapes.All
    End If
    
    If sr.Count = 0 Then
        MsgBox "No shapes found to convert.", vbInformation, "CorelMate Color"
        Exit Sub
    End If
    
    ActiveDocument.BeginCommandGroup "CorelMate: Convert to CMYK"
    On Error Resume Next
    ProcessColorRange sr, cdrColorCMYK, cdrCMYKColorImage
    ActiveDocument.EndCommandGroup
    Application.CorelScript.RedrawScreen
    
    MsgBox "Color conversion to CMYK complete!", vbInformation, "CorelMate Color"
End Sub

Public Sub ConvertToRgb()
    If ActiveDocument Is Nothing Then
        MsgBox "No document is open in CorelDRAW.", vbExclamation, "CorelMate Color"
        Exit Sub
    End If
    
    If MsgBox("Convert all vector colors and bitmap images to RGB?", vbYesNo + vbQuestion, "CorelMate Color") <> vbYes Then Exit Sub
    
    Dim sr As ShapeRange
    If ActiveSelectionRange.Count > 0 Then
        Set sr = ActiveSelectionRange
    Else
        Set sr = ActivePage.Shapes.All
    End If
    
    If sr.Count = 0 Then
        MsgBox "No shapes found to convert.", vbInformation, "CorelMate Color"
        Exit Sub
    End If
    
    ActiveDocument.BeginCommandGroup "CorelMate: Convert to RGB"
    On Error Resume Next
    ProcessColorRange sr, cdrColorRGB, cdrRGBColorImage
    ActiveDocument.EndCommandGroup
    Application.CorelScript.RedrawScreen
    
    MsgBox "Color conversion to RGB complete!", vbInformation, "CorelMate Color"
End Sub

Private Sub ProcessColorRange(sr As ShapeRange, targetColorType As cdrColorType, targetImgType As cdrImageType)
    Dim s As Shape, fc As FountainColor
    On Error Resume Next
    For Each s In sr
        If s.Type = cdrGroupShape Then
            ProcessColorRange s.Shapes.All, targetColorType, targetImgType
        ElseIf s.Type = cdrBitmapShape Then
            If s.Bitmap.Mode <> targetImgType Then s.Bitmap.ConvertTo targetImgType
        Else
            If s.CanHaveFill Then
                Select Case s.Fill.Type
                    Case cdrUniformFill
                        ConvertColorVal s.Fill.UniformColor, targetColorType
                    Case cdrFountainFill
                        For Each fc In s.Fill.Fountain.Colors
                            ConvertColorVal fc.Color, targetColorType
                        Next fc
                End Select
            End If
            If s.CanHaveOutline Then
                If s.Outline.Type <> cdrNoOutline Then
                    ConvertColorVal s.Outline.Color, targetColorType
                End If
            End If
        End If
        If Not s.PowerClip Is Nothing Then
            ProcessColorRange s.PowerClip.Shapes.All, targetColorType, targetImgType
        End If
    Next s
End Sub

Private Sub ConvertColorVal(c As Color, targetType As cdrColorType)
    On Error Resume Next
    If c.Type <> targetType Then
        If targetType = cdrColorRGB Then
            c.ConvertToRGB
        ElseIf targetType = cdrColorCMYK Then
            If c.Type = cdrColorPantone Or c.Type = cdrColorSpot Then
                c.ConvertToRGB
                c.ConvertToCMYK
            Else
                c.ConvertToCMYK
            End If
        End If
    End If
End Sub
