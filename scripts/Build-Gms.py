import io
import os
import struct
import olefile

VBA_SOURCE_CODE = b'''Attribute VB_Name = "Converter"
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
    If Right(outDir, 1) <> "\\" Then outDir = outDir & "\\"
    
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
        cleanText = Replace(Replace(cleanText, Chr(34), "\\" & Chr(34)), vbCr, "")
        
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
    If Right(outDir, 1) <> "\\" Then outDir = outDir & "\\"
    
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
'''

def compress_chunk_lz77(chunk_bytes):
    decomp_len = len(chunk_bytes)
    out = bytearray()
    pos = 0
    while pos < decomp_len:
        flag_byte_pos = len(out)
        out.append(0)
        flag_byte = 0
        for bit in range(8):
            if pos >= decomp_len:
                break
            bits = 4
            while (1 << bits) < pos:
                bits += 1
            length_mask = 0xFFFF >> bits
            offset_mask = ~length_mask
            max_len = length_mask + 3
            
            best_len = 0
            best_offset = 0
            max_lookback = min(pos, 4096)
            for offset in range(1, max_lookback + 1):
                m_len = 0
                while pos + m_len < decomp_len and chunk_bytes[pos - offset + m_len] == chunk_bytes[pos + m_len]:
                    m_len += 1
                    if m_len == max_len:
                        break
                if m_len > best_len:
                    best_len = m_len
                    best_offset = offset
                    
            if best_len >= 3:
                flag_byte |= (1 << bit)
                token = ((best_len - 3) & length_mask) | (((best_offset - 1) << (16 - bits)) & offset_mask)
                out.extend(struct.pack('<H', token))
                pos += best_len
            else:
                out.append(chunk_bytes[pos])
                pos += 1
        out[flag_byte_pos] = flag_byte
    return bytes(out)

def encode_stream_lz77(code_bytes):
    out = bytearray([0x01])
    offset = 0
    while offset < len(code_bytes):
        chunk = code_bytes[offset:offset+4096]
        offset += len(chunk)
        comp_chunk = compress_chunk_lz77(chunk)
        chunk_size = len(comp_chunk) + 2
        header = 0xB000 | (chunk_size - 3)
        out.extend(struct.pack('<H', header))
        out.extend(comp_chunk)
    return bytes(out)

def build_gms():
    src_path = r"C:\Program Files\Corel\CorelDRAW Graphics Suite\27\Draw\GMS\ConvertAllToCurves.gms"
    if not os.path.exists(src_path):
        # Fallback to local asset if available
        src_path = os.path.abspath(r"assets\gms\ConvertAllToCurves.gms")
        if not os.path.exists(src_path):
            raise FileNotFoundError(f"Source template not found: {src_path}")
        
    data = open(src_path, "rb").read()
    header_18 = data[:18]
    ole_bytes = bytearray(data[18:])
    
    ole = olefile.OleFileIO(io.BytesIO(ole_bytes))
    stream_name = "apc/The VBA Project/_VBA_Project/VBA/Converter"
    sid = ole._find(stream_name)
    entry = ole.direntries[sid]
    orig_stream = ole.openstream(stream_name).read()
    
    textoffset = 5157
    prefix = orig_stream[:textoffset]
    comp = encode_stream_lz77(VBA_SOURCE_CODE)
    new_stream = prefix + comp
    new_size = len(new_stream)
    print(f"Building GMS: Original size={entry.size}, New size={new_size}")
    
    # Update size in direntry at pos 7936
    pos = 7936
    ole_bytes[pos + 120 : pos + 124] = struct.pack('<I', new_size)
    
    # Calculate sectors needed
    sec_size = ole.sectorsize
    sectors_needed = (new_size + sec_size - 1) // sec_size
    
    # Existing sector chain
    sectors = []
    s = entry.isectStart
    while s != olefile.ENDOFCHAIN and s < len(ole.fat):
        sectors.append(s)
        s = ole.fat[s]
        
    total_sectors = (len(ole_bytes) - 512) // sec_size
    fat_sector_idx = struct.unpack('<I', ole_bytes[76:80])[0]
    fat_sector_pos = 512 + fat_sector_idx * sec_size
    
    while len(sectors) < sectors_needed:
        new_s = total_sectors
        total_sectors += 1
        last_s = sectors[-1]
        ole_bytes[fat_sector_pos + last_s * 4 : fat_sector_pos + last_s * 4 + 4] = struct.pack('<I', new_s)
        ole_bytes[fat_sector_pos + new_s * 4 : fat_sector_pos + new_s * 4 + 4] = struct.pack('<I', 0xFFFFFFFE)
        ole_bytes.extend(b'\x00' * sec_size)
        sectors.append(new_s)
        
    header_offset = 512
    for i, s_idx in enumerate(sectors):
        sec_data = new_stream[i*sec_size : (i+1)*sec_size]
        if len(sec_data) < sec_size:
            sec_data += b'\x00' * (sec_size - len(sec_data))
        p_offset = header_offset + s_idx * sec_size
        ole_bytes[p_offset : p_offset + sec_size] = sec_data
        
    final_gms_data = header_18 + bytes(ole_bytes)
    
    # Output targets
    app_data = os.environ.get("APPDATA", "")
    targets = [
        r"C:\Program Files\Corel\CorelDRAW Graphics Suite\27\Draw\GMS\ConvertAllToCurves.gms",
        os.path.join(app_data, r"Corel\CorelDRAW Graphics Suite 2026\Draw\GMS\ConvertAllToCurves.gms"),
        os.path.join(app_data, r"Corel\CorelDRAW Graphics Suite 2026\Draw\GMS\CorelMate.gms"),
        os.path.abspath(r"assets\gms\ConvertAllToCurves.gms"),
        os.path.abspath(r"assets\gms\CorelMate.gms")
    ]
    
    for t in targets:
        try:
            os.makedirs(os.path.dirname(t), exist_ok=True)
            with open(t, "wb") as f:
                f.write(final_gms_data)
            print(f"  ✓ Deployed GMS to: {t}")
        except Exception as ex:
            print(f"  ! Skipped ({ex}): {t}")

if __name__ == "__main__":
    build_gms()
