Imports System
Imports System.IO
Imports System.Globalization
Imports System.Linq
Imports System.Text.RegularExpressions
Imports UglyToad.PdfPig
Imports UglyToad.PdfPig.Content

Public Class PdfPoParser
    Private ReadOnly CI As CultureInfo = CultureInfo.InvariantCulture
    Private Shared ReadOnly KnownLabelPrefixes As String() = {
        "PO. Number", "PO Number", "Purchase Order", "Supplier Name", "Supplier Number", "Date",
        "Currency", "Payment Terms", "Incoterms", "Shipping Address", "Sub Total",
        "Subtotal", "Total Discount", "Total Before VAT", "VAT", "Grand Total",
        "Grand Total Amount in Words", "Purchase Order Description", "Line Item",
        "Line Item Code", "Line", "Item Code", "Description", "Delivery Date", "Deliver to",
        "UOM", "Qty", "Qty.", "Unit Price", "Discount", "Net Price", "Amount",
        "Supplier Details", "TERMS", "Invoice Address", "Supplier Address"
    }

    Public Function Parse(pdfPath As String) As ParsedPo
        Using fs As FileStream = File.OpenRead(pdfPath)
            Return Parse(fs)
        End Using
    End Function

    Public Function Parse(stream As Stream) As ParsedPo
        Dim pages As New List(Of PageData)
        Using doc = PdfDocument.Open(stream)
            For Each p In doc.GetPages()
                pages.Add(New PageData(p))
            Next
        End Using

        Dim full = String.Join(Environment.NewLine & Environment.NewLine, pages.ConvertAll(Function(p) p.Text))

        Dim master As New ParsedMaster()
        FillHeader(master, full)
        master.PODescription = ExtractPoDescription(full)
        
        ' If PODescription is still empty, try to extract it from the full text
        If String.IsNullOrEmpty(master.PODescription) Then
            Dim descMatch = Regex.Match(full, "Purchase Order Description:\s*(.+?)(?=\s*TERMS|$)", RegexOptions.IgnoreCase)
            If descMatch.Success Then
                master.PODescription = descMatch.Groups(1).Value.Trim()
            End If
        End If

        Dim details As New List(Of ParsedDetail)
        Dim stopProcessing As Boolean = False
        
        For Each p In pages
            ' If we've already found the end marker, stop processing all subsequent pages
            If stopProcessing Then
                Exit For
            End If
            
            ' Check if this page contains the end marker
            If p.Text.Contains("Grand Total Amount in Words") Then
                stopProcessing = True
            End If
            
            details.AddRange(ParseItemsOnPage(p))
        Next
        details = CoalesceWrapped(details)
        AssignLineNumbers(details)

        Return New ParsedPo With {.Master = master, .Details = details}
    End Function

    ' ---------- page model ----------
    Private Class PageData
        Public ReadOnly Words As List(Of Word)
        Public ReadOnly Text As String
        Public Sub New(pg As UglyToad.PdfPig.Content.Page)
            Words = pg.GetWords().ToList()
            Text = pg.Text
        End Sub
    End Class

    ' ---------- header parsing ----------
    Private Sub FillHeader(ByRef h As ParsedMaster, tAll As String)
        Dim normalized = tAll.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf).Replace(ChrW(160), " "c)
        Dim lines = normalized.Split(New String() {vbLf}, StringSplitOptions.RemoveEmptyEntries) _
                                .Select(Function(l) l.Trim()) _
                                .Where(Function(l) l.Length > 0) _
                                .ToList()


        h.PONumber = ExtractLabelValue(lines, "PO. Number", 1)
        If String.IsNullOrEmpty(h.PONumber) Then
            h.PONumber = ExtractLabelValue(lines, "Purchase Order", 1)
        End If
        If String.IsNullOrEmpty(h.PONumber) Then
            h.PONumber = ExtractLabelValue(lines, "PO Number", 1)
        End If

        Dim sDate = ExtractLabelValue(lines, "Date", 1)
        If Not String.IsNullOrEmpty(sDate) Then
            h.PODate = ParseDdMmmYyyy(sDate)
        End If
        
        ' If date extraction failed, try a more direct approach
        If Not h.PODate.HasValue Then
            For Each line In lines
                If line.Contains("Date:") Then
                    Dim dateMatch = Regex.Match(line, "Date:\s*(\d{1,2}-[A-Z]{3}-\d{4})")
                    If dateMatch.Success Then
                        h.PODate = ParseDdMmmYyyy(dateMatch.Groups(1).Value)
                        Exit For
                    End If
                End If
            Next
        End If

        h.SupplierNumber = ExtractLabelValue(lines, "Supplier Number", 1)
        h.SupplierName = ExtractLabelValue(lines, "Supplier Name", 1)
        
        ' If we didn't get supplier details, try to extract from the concatenated line
        If String.IsNullOrEmpty(h.SupplierNumber) OrElse String.IsNullOrEmpty(h.SupplierName) Then
            For Each line In lines
                If line.Contains("Supplier Number:") AndAlso line.Contains("PO. Number:") AndAlso line.Contains("Supplier Name:") Then
                    ' Extract Supplier Number
                    Dim supplierNumberMatch = Regex.Match(line, "Supplier Number:\s*(\d+)")
                    If supplierNumberMatch.Success Then
                        h.SupplierNumber = supplierNumberMatch.Groups(1).Value
                    End If
                    
                    ' Extract PO Number
                    Dim poNumberMatch = Regex.Match(line, "PO\.\s*Number:\s*([A-Z0-9\-]+?)(?=Supplier|$)")
                    If poNumberMatch.Success Then
                        h.PONumber = poNumberMatch.Groups(1).Value
                    End If
                    
                    ' Extract Supplier Name
                    Dim supplierNameMatch = Regex.Match(line, "Supplier Name:\s*([A-Z\s]+?)(?=\s*$|Supplier VAT|VAT#)")
                    If supplierNameMatch.Success Then
                        h.SupplierName = supplierNameMatch.Groups(1).Value.Trim()
                    End If
                    Exit For
                End If
            Next
        End If

        Dim currencyText = ExtractLabelValue(lines, "Currency", 2)
        If Not String.IsNullOrEmpty(currencyText) Then
            Dim mCur = Regex.Match(currencyText, "\b([A-Z]{3})\b")
            If mCur.Success Then 
                h.Currency = mCur.Groups(1).Value
            Else
                ' Try to extract from patterns like "UAE Dirham -  AED"
                mCur = Regex.Match(currencyText, "-\s*([A-Z]{3})\s*$")
                If mCur.Success Then h.Currency = mCur.Groups(1).Value
            End If
        End If
        
        ' If currency extraction failed, try a more direct approach
        If String.IsNullOrEmpty(h.Currency) Then
            For Each line In lines
                If line.Contains("Currency:") Then
                    Dim currencyMatch = Regex.Match(line, "Currency:\s*[^-]+-\s*([A-Z]{3})")
                    If currencyMatch.Success Then
                        h.Currency = currencyMatch.Groups(1).Value
                        Exit For
                    End If
                End If
            Next
        End If

        ' Extract Payment Terms and IncoTerms from the same line
        For Each line In lines
            If line.Contains("Payment Terms:") AndAlso line.Contains("Incoterms:") Then
                ' Extract Payment Terms - get everything between Payment Terms: and Incoterms:
                Dim paymentMatch = Regex.Match(line, "Payment Terms:\s*([^:]+?)\s*Incoterms:")
                If paymentMatch.Success Then
                    h.PaymentTerms = paymentMatch.Groups(1).Value.Trim()
                End If
                
                ' Extract IncoTerms with improved pattern to handle "None All" and similar cases
                ' This pattern stops at common shipping address indicators or capitalized words
                Dim incoMatch = Regex.Match(line, "Incoterms:\s*([A-Za-z0-9]+(?:\s+[A-Za-z0-9]+)*?)(?=\s+(?:Shipping|Address|All|[A-Z][a-z]{2,})\s|$)")
                If incoMatch.Success Then
                    h.IncoTerms = incoMatch.Groups(1).Value.Trim()
                Else
                    ' Fallback pattern for cases where the above doesn't match
                    incoMatch = Regex.Match(line, "Incoterms:\s*([A-Za-z0-9\s]+?)(?:\s+[A-Z][a-z]+\s|$)")
                    If incoMatch.Success Then
                        h.IncoTerms = incoMatch.Groups(1).Value.Trim()
                    End If
                End If
                Exit For
            End If
        Next

        ' Extract shipping address from the line containing Payment Terms and IncoTerms
        For Each line In lines
            If line.Contains("Payment Terms:") AndAlso line.Contains("Incoterms:") Then
                ' Extract shipping address after the Incoterms value
                ' Look for common shipping address indicators after the Incoterms value
                ' Stop reading when "line" or "lineitem" is encountered (even within words like "DubaiLineItem")
                ' This will stop at "Dubai" if the text is "DubaiLineItem"
                Dim addressMatch = Regex.Match(line, "Incoterms:\s*[A-Za-z0-9]+(?:\s+[A-Za-z0-9]+)*?\s+(?:Shipping|Address|All\s+Shipping|All\s+Address)?\s*(.+?)(?=\s*line|lineitem|$)", RegexOptions.IgnoreCase)
                If addressMatch.Success Then
                    Dim potentialAddress = addressMatch.Groups(1).Value.Trim()
                    ' Only use it if it's not empty and looks like an address
                    If Not String.IsNullOrEmpty(potentialAddress) AndAlso 
                       Not potentialAddress.StartsWith("Incoterms:") AndAlso
                       potentialAddress.Length > 3 Then
                        h.Shipping_Address = potentialAddress
                        Exit For
                    End If
                End If
            End If
        Next

        ' Try multiple patterns for SubTotal
        h.SubTotal = MoneyAfterLabelLine(normalized, "Sub\.?\s*Total\s*Before\s*VAT")
        If Not h.SubTotal.HasValue Then
            h.SubTotal = MoneyAfterLabelLine(normalized, "Sub\s*Total\s*Before\s*VAT")
        End If
        If Not h.SubTotal.HasValue Then
            h.SubTotal = MoneyAfterLabelLine(normalized, "Sub\.?\s*Total\s*Before\s*VAT")
        End If
        
        ' Extract VAT using dedicated function
        h.VAT = ExtractVATValue(normalized, lines)

        ' Try multiple patterns for Total
        h.Total = MoneyAfterLabelLine(normalized, "Grand\s*Total")
        If Not h.Total.HasValue Then
            h.Total = MoneyAfterLabelLine(normalized, "Grand\s*Total")
        End If

        ' If totals extraction failed, try a more direct approach
        If Not h.SubTotal.HasValue OrElse Not h.VAT.HasValue OrElse Not h.Total.HasValue Then
            For Each line In lines
                ' Look for SubTotal
                If Not h.SubTotal.HasValue AndAlso line.Contains("Sub. Total Before VAT") Then
                    Dim subtotalMatch = Regex.Match(line, "Sub\.?\s*Total\s*Before\s*VAT\s*(\d{1,3}(?:,\d{3})*(?:\.\d{2})?)")
                    If subtotalMatch.Success Then
                        h.SubTotal = ParseDec(subtotalMatch.Groups(1).Value)
                    End If
                End If

                ' VAT extraction is now handled by the dedicated ExtractVATValue function

                ' Look for Grand Total
                If Not h.Total.HasValue AndAlso line.Contains("Grand Total") Then
                    Dim totalMatch = Regex.Match(line, "Grand\s*Total\s*(\d{1,3}(?:,\d{3})*(?:\.\d{2})?)")
                    If totalMatch.Success Then
                        h.Total = ParseDec(totalMatch.Groups(1).Value)
                    End If
                End If
            Next
        End If
    End Sub

    Private Function ExtractLabelValue(lines As IList(Of String), label As String, Optional maxNextLines As Integer = 1) As String
        If lines Is Nothing OrElse lines.Count = 0 Then Return Nothing

        Dim pattern = BuildLabelPattern(label)
        Const MaxLabelLines As Integer = 3
        For i = 0 To lines.Count - 1
            Dim combined = lines(i)
            For span = 0 To MaxLabelLines - 1
                Dim m = Regex.Match(combined, pattern, RegexOptions.IgnoreCase)
                If m.Success Then
                    Dim remainder = m.Groups("val").Value
                    If Not String.IsNullOrWhiteSpace(remainder) Then
                        Dim result = Clean(remainder)
                        Return result
                    End If

                    If maxNextLines <= 0 Then Return Nothing

                    Dim parts As New List(Of String)
                    Dim j = i + span + 1
                    While j < lines.Count AndAlso parts.Count < maxNextLines
                        Dim candidate = lines(j)
                        j += 1
                        Dim normalizedCandidate = Regex.Replace(candidate.Replace(ChrW(160), " "c), "\s+", " ").Trim()
                        If normalizedCandidate.Length = 0 Then Continue While
                        If IsLikelyNewLabel(normalizedCandidate) Then Exit While

                        Dim valuePart = Clean(candidate)
                        If valuePart.Length = 0 Then Continue While
                        parts.Add(valuePart)
                    End While

                    If parts.Count > 0 Then
                        Dim result = Clean(String.Join(" ", parts))
                        Return result
                    End If
                    Return Nothing
                End If

                Dim nextIndex = i + span + 1
                If nextIndex >= lines.Count Then Exit For
                combined &= " " & lines(nextIndex)
            Next
        Next

        ' Try a more flexible pattern for concatenated text
        Dim flexiblePattern = BuildFlexibleLabelPattern(label)
        For i = 0 To lines.Count - 1
            Dim line = lines(i)
            Dim m = Regex.Match(line, flexiblePattern, RegexOptions.IgnoreCase)
            If m.Success Then
                Dim remainder = m.Groups("val").Value
                If Not String.IsNullOrWhiteSpace(remainder) Then
                    Dim result = Clean(remainder)
                    Return result
                End If
            End If
        Next

        Return ExtractLabelValueLegacy(lines, label, maxNextLines)
    End Function

    Private Function ExtractLabelValueLegacy(lines As IList(Of String), label As String, maxNextLines As Integer) As String
        If lines Is Nothing OrElse lines.Count = 0 Then Return Nothing

        Dim trimmed = label.Trim()
        If trimmed.EndsWith(":"c) Then
            trimmed = trimmed.Substring(0, trimmed.Length - 1)
        End If

        For i = 0 To lines.Count - 1
            Dim line = lines(i)
            If line.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase) Then
                Dim remainder = line.Substring(Math.Min(trimmed.Length, line.Length)).Trim()
                If remainder.StartsWith(":"c) Then remainder = remainder.Substring(1).Trim()

                If remainder.Length > 0 Then
                    Dim result = Clean(remainder)
                    Return result
                End If

                If maxNextLines <= 0 Then Return Nothing

                Dim parts As New List(Of String)
                Dim j = i + 1
                While j < lines.Count AndAlso parts.Count < maxNextLines
                    Dim candidate = lines(j)
                    j += 1
                    Dim normalizedCandidate = Regex.Replace(candidate.Replace(ChrW(160), " "c), "\s+", " ").Trim()
                    If normalizedCandidate.Length = 0 Then Continue While
                    If IsLikelyNewLabel(normalizedCandidate) Then Exit While

                    Dim valuePart = Clean(candidate)
                    If valuePart.Length = 0 Then Continue While
                    parts.Add(valuePart)
                End While

                If parts.Count > 0 Then
                    Dim result = Clean(String.Join(" ", parts))
                    Return result
                End If
                Return Nothing
            End If
        Next

        Return Nothing
    End Function

    Private Function ExtractVATValue(normalized As String, lines As List(Of String)) As Decimal?
        ' Try multiple patterns for VAT extraction
        Dim vatPatterns = {
            "VAT\s*\d+%\s*(\d{1,3}(?:,\d{3})*(?:\.\d{2})?)",  ' VAT 5% 25,000.00
            "VAT\s*%\s*(\d{1,3}(?:,\d{3})*(?:\.\d{2})?)",     ' VAT % 0.00
            "VAT\s*\d+%\s*(\d+(?:,\d{3})*(?:\.\d{2})?)",      ' VAT 5% 25000.00 (without commas)
            "VAT\s*%\s*(\d+(?:,\d{3})*(?:\.\d{2})?)"          ' VAT % 0.00 (without commas)
        }
        
        ' First try the MoneyAfterLabelLine approach
        Dim vatValue = MoneyAfterLabelLine(normalized, "VAT\d+%")
        If vatValue.HasValue Then Return vatValue
        
        vatValue = MoneyAfterLabelLine(normalized, "VAT\s*\d+%")
        If vatValue.HasValue Then Return vatValue
        
        vatValue = MoneyAfterLabelLine(normalized, "VAT\s*%")
        If vatValue.HasValue Then Return vatValue
        
        ' Try direct pattern matching on each line
        For Each line In lines
            If line.Contains("VAT") AndAlso line.Contains("%") Then
                For Each pattern In vatPatterns
                    Dim match = Regex.Match(line, pattern, RegexOptions.IgnoreCase)
                    If match.Success AndAlso match.Groups.Count > 1 Then
                        Dim valueStr = match.Groups(1).Value
                        If Not String.IsNullOrEmpty(valueStr) Then
                            Dim parsedValue = ParseDec(valueStr)
                            If parsedValue <> 0 Then
                                Return parsedValue
                            End If
                        End If
                    End If
                Next
            End If
        Next
        
        ' Try looking for VAT value on the next line after "VAT %"
        For i = 0 To lines.Count - 1
            Dim line = lines(i)
            If line.Contains("VAT") AndAlso line.Contains("%") Then
                ' Check if the next line contains a number
                If i + 1 < lines.Count Then
                    Dim nextLine = lines(i + 1).Trim()
                    Dim nextLineMatch = Regex.Match(nextLine, "^(\d{1,3}(?:,\d{3})*(?:\.\d{2})?)$")
                    If nextLineMatch.Success Then
                        Return ParseDec(nextLineMatch.Groups(1).Value)
                    End If
                End If
            End If
        Next
        
        Return Nothing
    End Function

    Private Function MoneyAfterLabelLine(t As String, labelPattern As String) As Decimal?
        Dim normalized = t.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf).Replace(ChrW(160), " "c)
        Dim lines = normalized.Split(New String() {vbLf}, StringSplitOptions.None) _
                              .Select(Function(l) l.Trim()) _
                              .ToList()

        Dim pat = New Regex("^\s*" & labelPattern & "\s*:?\s*(?<n>\-?\d{1,3}(?:,\d{3})*(?:\.\d{2})?)\b", RegexOptions.IgnoreCase)
        ' Also try pattern without the ^ anchor for cases where the label might not be at start of line
        Dim pat2 = New Regex("\b" & labelPattern & "\s*:?\s*(?<n>\-?\d{1,3}(?:,\d{3})*(?:\.\d{2})?)\b", RegexOptions.IgnoreCase)
        ' Try pattern that matches the exact format in the PDF
        Dim pat3 = New Regex(labelPattern & "\s*(?<n>\d{1,3}(?:,\d{3})*(?:\.\d{2})?)", RegexOptions.IgnoreCase)

        For i = 0 To lines.Count - 1
            Dim line = lines(i)
            Dim m = pat.Match(line)
            If m.Success Then
                Dim grp = m.Groups("n")
                If grp.Success AndAlso grp.Value.Length > 0 Then
                    Dim result = ParseDec(grp.Value)
                    Return result
                End If

                Dim j = i + 1
                While j < lines.Count
                    Dim candidateRaw = Regex.Replace(lines(j).Replace(ChrW(160), " "c), "\s+", " ").Trim()
                    Dim candidateValue = Clean(lines(j))
                    j += 1
                    If candidateValue.Length = 0 Then Continue While
                    Dim numMatch = Regex.Match(candidateValue, "^\-?\d{1,3}(?:,\d{3})*(?:\.\d{2})?$")
                    If numMatch.Success Then
                        Dim result = ParseDec(numMatch.Value)
                        Return result
                    End If
                    If IsLikelyNewLabel(candidateRaw) Then Exit While
                End While

                Exit For
            End If

            ' Try the second pattern (without ^ anchor)
            m = pat2.Match(line)
            If m.Success Then
                Dim grp = m.Groups("n")
                If grp.Success AndAlso grp.Value.Length > 0 Then
                    Dim result = ParseDec(grp.Value)
                    Return result
                End If
            End If

            ' Try the third pattern (exact format)
            m = pat3.Match(line)
            If m.Success Then
                Dim grp = m.Groups("n")
                If grp.Success AndAlso grp.Value.Length > 0 Then
                    Dim result = ParseDec(grp.Value)
                    Return result
                End If
            End If
        Next

        Return Nothing
    End Function

    Private Function ExtractPoDescription(tAll As String) As String
        Dim normalized = tAll.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf).Replace(ChrW(160), " "c)
        Dim lines = normalized.Split(New String() {vbLf}, StringSplitOptions.None) _
                              .Select(Function(l) l.Trim()) _
                              .ToList()

        Dim pattern = BuildLabelPattern("Purchase Order Description")

        For i = 0 To lines.Count - 1
            Dim line = lines(i)
            Dim m = Regex.Match(line, pattern, RegexOptions.IgnoreCase)
            If m.Success Then
                Dim parts As New List(Of String)
                Dim inline = Clean(m.Groups("val").Value)
                If Not String.IsNullOrEmpty(inline) Then parts.Add(inline)

                Dim j = i + 1
                While j < lines.Count
                    Dim candidate = lines(j).Trim()
                    j += 1
                    If candidate.Length = 0 Then Continue While
                    If IsLikelyNewLabel(candidate) Then Exit While
                    parts.Add(candidate)
                End While

                Dim desc = Clean(String.Join(" ", parts))
                If String.IsNullOrEmpty(desc) Then Return Nothing

                desc = Regex.Replace(desc, "(?i)\bTERMS\s*&?\s*CONDITIONS\b.*$", String.Empty).Trim()
                If desc.Length = 0 Then Return Nothing
                Return desc
            End If
        Next

        Return Nothing
    End Function

    Private Function ParseDdMmmYyyy(s As String) As DateTime?
        Dim dt As DateTime
        ' Try dd-MMM-yyyy format first
        If DateTime.TryParseExact(s, "dd-MMM-yyyy", Globalization.CultureInfo.GetCultureInfo("en-GB"), DateTimeStyles.None, dt) Then
            Return dt
        End If
        ' Try dd-MMM-yyyy format with InvariantCulture
        If DateTime.TryParseExact(s, "dd-MMM-yyyy", Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, dt) Then
            Return dt
        End If
        ' Try other common formats
        If DateTime.TryParseExact(s, "dd-MMM-yyyy", Globalization.CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, dt) Then
            Return dt
        End If
        Return Nothing
    End Function

    Private Function ParseDec(s As String) As Decimal
        s = s.Replace(",", "")
        Dim d As Decimal
        If Decimal.TryParse(s, NumberStyles.Any, CI, d) Then Return d
        Return 0D
    End Function

    Private Function Clean(s As String) As String
        If String.IsNullOrEmpty(s) Then Return String.Empty

        Dim normalized = s.Replace(ChrW(160), " "c)
        normalized = Regex.Replace(normalized, "\s+", " ").Trim()
        normalized = normalized.Trim(":"c).Trim()
        Return normalized
    End Function

    Private Function BuildLabelPattern(label As String) As String
        Dim trimmed = label.Trim()
        If trimmed.EndsWith(":"c) Then trimmed = trimmed.Substring(0, trimmed.Length - 1)

        Dim segments = Regex.Split(trimmed, "\s+") _
                             .Where(Function(seg) seg.Length > 0) _
                             .Select(Function(seg) Regex.Escape(seg))

        Dim body = String.Join("\s*", segments)
        If body.Length = 0 Then body = Regex.Escape(trimmed)

        Return "^\s*" & body & "\s*:?\s*(?<val>.+)?$"
    End Function

    Private Function BuildFlexibleLabelPattern(label As String) As String
        Dim trimmed = label.Trim()
        If trimmed.EndsWith(":"c) Then trimmed = trimmed.Substring(0, trimmed.Length - 1)

        Dim segments = Regex.Split(trimmed, "\s+") _
                             .Where(Function(seg) seg.Length > 0) _
                             .Select(Function(seg) Regex.Escape(seg))

        Dim body = String.Join("\s*", segments)
        If body.Length = 0 Then body = Regex.Escape(trimmed)

        ' More flexible pattern that doesn't require start of line and handles concatenated text
        Return "\b" & body & "\s*:?\s*(?<val>[^:]+?)(?=\s*[A-Z][a-z]*\s*:|$)"
    End Function

    Private Function IsLikelyNewLabel(candidate As String) As Boolean
        Dim normalized = Regex.Replace(candidate.Replace(ChrW(160), " "c), "\s+", " ").Trim()
        If normalized.Length = 0 Then Return False

        For Each prefix In KnownLabelPrefixes
            If normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
        Next

        Return False
    End Function

    ' ---------- items parsing ----------
    Private Function ParseItemsOnPage(p As PageData) As List(Of ParsedDetail)
        Dim words = p.Words.OrderBy(Function(w) -w.BoundingBox.Top).ThenBy(Function(w) w.BoundingBox.Left).ToList()
        Dim rows = RowsFromLineAnchors(words, 12.0)
        If rows Is Nothing Then
            rows = GroupByY(words, 6.0)
        End If

        Dim cuts = New List(Of Double) From {0, 40, 110, 190, 240, 290, 330, 360, 400, 450, 500}

        Dim list As New List(Of ParsedDetail)
        For Each r In rows
            r.Sort(Function(a, b) a.BoundingBox.Left.CompareTo(b.BoundingBox.Left))
            Dim line = String.Join(" ", r.Select(Function(w) w.Text))
            If Regex.IsMatch(line, "\b(Line|Code|Description|Delivery|UOM|Qty|Unit|Discount|Net|Amount)\b", RegexOptions.IgnoreCase) Then Continue For
            
            ' Check for end marker - if found, stop processing this page and return what we have so far
            If Regex.IsMatch(line, "Grand\s+Total\s+Amount\s+in\s+Words", RegexOptions.IgnoreCase) Then
                Return list
            End If

            Dim it As New ParsedDetail
            it.ItemCode = Slice(r, cuts, 1)
            it.Description = Slice(r, cuts, 2)
            it.DeliveryDate = TryDate(FirstDateLike(Slice(r, cuts, 3)))
            it.UOM = Slice(r, cuts, 5)
            it.Qty = TryInt(Slice(r, cuts, 6))
            it.UnitPrice = TryDec(Slice(r, cuts, 7))
            it.NetPrice = TryDec(Slice(r, cuts, 9))
            it.Amount = TryDec(Slice(r, cuts, 10))

            Dim rowText = line.ToUpperInvariant()
            If rowText.Contains("SUPPLIER DETAILS") OrElse rowText.StartsWith("LINE ITEM CODE") Then Continue For

            If Regex.IsMatch(it.Description, "^(SUPPLIER|DETAILS|TERMS|PAYMENT|INVOICE|DELIVERY|ADDRESS)\b", RegexOptions.IgnoreCase) Then
                Continue For
            End If

            If Not String.IsNullOrWhiteSpace(it.ItemCode) OrElse (it.Amount.HasValue AndAlso it.Amount.Value > 0) Then
                list.Add(it)
            End If
        Next
        Return list
    End Function

    Private Function RowsFromLineAnchors(words As List(Of Word), tol As Double) As List(Of List(Of Word))
        Dim anchors = words _
            .Where(Function(w) w.BoundingBox.Left < 70 AndAlso Regex.IsMatch(w.Text.Trim(), "^\d+(?:\.\d+)?$")) _
            .ToList()
        If anchors.Count = 0 Then Return Nothing

        Dim map As New Dictionary(Of Word, List(Of Word))()
        For Each a In anchors
            map(a) = New List(Of Word)()
        Next

        For Each w In words
            Dim best = anchors _
                .Select(Function(a) New With {.Anchor = a, .Dist = Math.Abs(a.BoundingBox.Top - w.BoundingBox.Top)}) _
                .Where(Function(x) x.Dist <= tol) _
                .OrderBy(Function(x) x.Dist) _
                .ThenBy(Function(x) Math.Abs(x.Anchor.BoundingBox.Left - w.BoundingBox.Left)) _
                .FirstOrDefault()
            If best IsNot Nothing Then
                map(best.Anchor).Add(w)
            End If
        Next

        Dim ordered = map.Where(Function(kvp) kvp.Value.Count > 0) _
                          .OrderByDescending(Function(kvp) kvp.Key.BoundingBox.Top) _
                          .ToList()

        Dim result As New List(Of List(Of Word))()
        For Each kvp In ordered
            kvp.Value.Sort(Function(a, b) a.BoundingBox.Left.CompareTo(b.BoundingBox.Left))
            result.Add(kvp.Value)
        Next

        Return result
    End Function

    Private Function GroupByY(words As List(Of Word), tol As Double) As List(Of List(Of Word))
        Dim rows As New List(Of List(Of Word))
        For Each w In words
            Dim g = rows.FirstOrDefault(Function(r) Math.Abs(r(0).BoundingBox.Top - w.BoundingBox.Top) <= tol)
            If g Is Nothing Then rows.Add(New List(Of Word) From {w}) Else g.Add(w)
        Next
        For Each r In rows
            r.Sort(Function(a, b) a.BoundingBox.Left.CompareTo(b.BoundingBox.Left))
        Next
        Return rows
    End Function

    Private Function Slice(row As List(Of Word), cuts As List(Of Double), idx As Integer) As String
        Dim xs = cuts(idx)
        Dim xe = If(idx = cuts.Count - 1, Double.MaxValue, cuts(idx + 1))
        Dim s = String.Join(" ", row.Where(Function(w) w.BoundingBox.Left >= xs AndAlso w.BoundingBox.Left < xe) _
                                 .OrderBy(Function(w) w.BoundingBox.Left) _
                                 .Select(Function(w) w.Text))
        Return s.Trim()
    End Function

    Private Function FirstDateLike(s As String) As String
        Dim m = Regex.Match(s, "\d{1,2}\-[A-Z]{3}\-\d{4}")
        Return If(m.Success, m.Value, Nothing)
    End Function

    Private Function TryDate(s As String) As DateTime?
        If String.IsNullOrWhiteSpace(s) Then Return Nothing
        Return ParseDdMmmYyyy(s)
    End Function

    Private Function TryInt(s As String) As Integer?
        If String.IsNullOrWhiteSpace(s) Then Return Nothing
        Dim n As Integer
        If Integer.TryParse(s.Replace(",", ""), n) Then Return n
        Dim d As Decimal
        If Decimal.TryParse(s.Replace(",", ""), NumberStyles.Any, CI, d) Then Return CInt(Math.Round(d))
        Return Nothing
    End Function

    Private Function TryDec(s As String) As Decimal?
        If String.IsNullOrWhiteSpace(s) Then Return Nothing
        Dim d As Decimal
        If Decimal.TryParse(s.Replace(",", ""), NumberStyles.Any, CI, d) Then Return d
        Return Nothing
    End Function

    Private Function CoalesceWrapped(items As List(Of ParsedDetail)) As List(Of ParsedDetail)
        Dim res As New List(Of ParsedDetail)
        For Each it In items
            If res.Count > 0 Then
                Dim last = res(res.Count - 1)
                Dim cont = String.IsNullOrWhiteSpace(it.ItemCode) AndAlso Not String.IsNullOrWhiteSpace(it.Description) _
                           AndAlso Not it.Qty.HasValue AndAlso Not it.UnitPrice.HasValue AndAlso Not it.Amount.HasValue
                If cont Then
                    last.Description = (last.Description & " " & it.Description).Trim()
                    Continue For
                End If
            End If
            res.Add(it)
        Next
        Return res
    End Function

    Private Sub AssignLineNumbers(items As List(Of ParsedDetail))
        Dim index = 1
        For Each item In items
            item.LineNumber = index
            index += 1
        Next
    End Sub
End Class
