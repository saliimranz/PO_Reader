Imports System
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
        Dim pages As New List(Of PageData)
        Using doc = PdfDocument.Open(pdfPath)
            For Each p In doc.GetPages()
                pages.Add(New PageData(p))
            Next
        End Using

        Dim full = String.Join(Environment.NewLine & Environment.NewLine, pages.ConvertAll(Function(p) p.Text))

        Dim master As New ParsedMaster()
        FillHeader(master, full)
        master.PODescription = ExtractPoDescription(full)
        If String.IsNullOrEmpty(master.PODescription) Then
            master.PODescription = ExtractLabelValue(lines, "Purchase Order Description", 2)
        End If
        If String.IsNullOrEmpty(master.PODescription) Then
            ' Try to find the description in the lines
            For Each line In lines
                If line.Contains("Purchase Order Description:") Then
                    Dim descMatch = Regex.Match(line, "Purchase Order Description:\s*(.+)")
                    If descMatch.Success Then
                        master.PODescription = descMatch.Groups(1).Value.Trim()
                        Exit For
                    End If
                End If
            Next
        End If

        Dim details As New List(Of ParsedDetail)
        For Each p In pages
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
                    Dim supplierNameMatch = Regex.Match(line, "Supplier Name:\s*([A-Z\s]+?)(?=\s*$|Supplier|PO|VAT)")
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

        h.PaymentTerms = ExtractLabelValue(lines, "Payment Terms", 3)
        h.IncoTerms = ExtractLabelValue(lines, "Incoterms", 2)

        Dim ship = ExtractLabelValue(lines, "Shipping Address", 5)
        If Not String.IsNullOrEmpty(ship) Then
            h.Shipping_Address = ship
        End If
        
        ' Also try to extract shipping address from the terms section
        If String.IsNullOrEmpty(h.Shipping_Address) Then
            For i = 0 To lines.Count - 1
                If lines(i).Contains("Terms") AndAlso i + 1 < lines.Count Then
                    Dim nextLine = lines(i + 1)
                    If nextLine.Contains("All Makes Auto Parts") Then
                        h.Shipping_Address = nextLine.Trim()
                        Exit For
                    End If
                End If
            Next
        End If

        h.SubTotal = MoneyAfterLabelLine(normalized, "Sub\.?\s*Total\s*Before\s*VAT")
        If Not h.SubTotal.HasValue Then
            h.SubTotal = MoneyAfterLabelLine(normalized, "Sub\s*Total\s*Before\s*VAT")
        End If
        If Not h.SubTotal.HasValue Then
            h.SubTotal = MoneyAfterLabelLine(normalized, "Sub\.?\s*Total\s*Before\s*VAT")
        End If
        
        h.VAT = MoneyAfterLabelLine(normalized, "VAT(?:\s*\d+%)*")
        If Not h.VAT.HasValue Then
            h.VAT = MoneyAfterLabelLine(normalized, "VAT\d+%")
        End If
        If Not h.VAT.HasValue Then
            h.VAT = MoneyAfterLabelLine(normalized, "VAT\s*\d+%")
        End If
        
        h.Total = MoneyAfterLabelLine(normalized, "Grand\s*Total")
        If Not h.Total.HasValue Then
            h.Total = MoneyAfterLabelLine(normalized, "Grand\s*Total")
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

    Private Function MoneyAfterLabelLine(t As String, labelPattern As String) As Decimal?
        Dim normalized = t.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf).Replace(ChrW(160), " "c)
        Dim lines = normalized.Split(New String() {vbLf}, StringSplitOptions.None) _
                              .Select(Function(l) l.Trim()) _
                              .ToList()

        Dim pat = New Regex("^\s*" & labelPattern & "\s*:?\s*(?<n>\-?\d{1,3}(?:,\d{3})*(?:\.\d{2})?)\b", RegexOptions.IgnoreCase)
        ' Also try pattern without the ^ anchor for cases where the label might not be at start of line
        Dim pat2 = New Regex("\b" & labelPattern & "\s*:?\s*(?<n>\-?\d{1,3}(?:,\d{3})*(?:\.\d{2})?)\b", RegexOptions.IgnoreCase)

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
            If Regex.IsMatch(line, "Grand\s*Total|TERMS\s+AND\s+CONDITIONS|Page\s+\d+\s+of\s+\d+", RegexOptions.IgnoreCase) Then Continue For

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
