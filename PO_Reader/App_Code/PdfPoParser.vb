Imports System
Imports System.Globalization
Imports System.Linq
Imports System.Text.RegularExpressions
Imports UglyToad.PdfPig
Imports UglyToad.PdfPig.Content

Public Class PdfPoParser
    Private ReadOnly CI As CultureInfo = CultureInfo.InvariantCulture

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

        Dim details As New List(Of ParsedDetail)
        For Each p In pages
            details.AddRange(ParseItemsOnPage(p))
        Next
        details = CoalesceWrapped(details)
        AssignLineNumbers(details)

        Return New ParsedPo With {.master = master, .details = details}
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
        Dim normalized = tAll.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf)
        Dim lines = normalized.Split(New String() {vbLf}, StringSplitOptions.RemoveEmptyEntries) _
                                .Select(Function(l) l.Trim()) _
                                .Where(Function(l) l.Length > 0) _
                                .ToList()

        h.PONumber = ExtractLabelValue(lines, "PO. Number:", 1)
        If String.IsNullOrEmpty(h.PONumber) Then
            h.PONumber = ExtractLabelValue(lines, "Purchase Order:", 1)
        End If

        Dim sDate = ExtractLabelValue(lines, "Date:", 1)
        If Not String.IsNullOrEmpty(sDate) Then
            h.PODate = ParseDdMmmYyyy(sDate)
        End If

        h.SupplierNumber = ExtractLabelValue(lines, "Supplier Number:", 1)
        h.SupplierName = ExtractLabelValue(lines, "Supplier Name:", 1)

        Dim currencyText = ExtractLabelValue(lines, "Currency:", 2)
        If Not String.IsNullOrEmpty(currencyText) Then
            Dim mCur = Regex.Match(currencyText, "\b([A-Z]{3})\b")
            If mCur.Success Then h.Currency = mCur.Groups(1).Value
        End If

        h.PaymentTerms = ExtractLabelValue(lines, "Payment Terms:", 0)
        h.IncoTerms = ExtractLabelValue(lines, "Incoterms:", 0)

        Dim ship = ExtractLabelValue(lines, "Shipping Address", 1)
        If Not String.IsNullOrEmpty(ship) Then
            h.Shipping_Address = ship
        End If

        h.SubTotal = MoneyAfterLabelLine(normalized, "Sub\.?\s*Total\s*Before\s*VAT")
        h.VAT = MoneyAfterLabelLine(normalized, "VAT(?:\s*\d+%)*")
        h.Total = MoneyAfterLabelLine(normalized, "Grand\s*Total")
    End Sub

    Private Function ExtractLabelValue(lines As IList(Of String), label As String, Optional maxNextLines As Integer = 1) As String
        For i = 0 To lines.Count - 1
            Dim line = lines(i)
            If line.StartsWith(label, StringComparison.OrdinalIgnoreCase) Then
                Dim remainder = line.Substring(Math.Min(label.Length, line.Length)).Trim()
                If remainder.Length > 0 Then Return Clean(remainder)

                Dim parts As New List(Of String)
                Dim j = i + 1
                While j < lines.Count AndAlso parts.Count < maxNextLines
                    Dim candidate = lines(j).Trim()
                    j += 1
                    If candidate.Length = 0 Then Continue While
                    If candidate.Contains(":"c) Then Continue While
                    parts.Add(candidate)
                End While

                If parts.Count > 0 Then Return Clean(String.Join(" ", parts))
            End If
        Next
        Return Nothing
    End Function

    Private Function MoneyAfterLabelLine(t As String, labelPattern As String) As Decimal?
        Dim pat = labelPattern & "\s*:?(?:\s|\r|\n)*(?<n>\-?\d{1,3}(?:,\d{3})*(?:\.\d{2})?)"
        Dim m = Regex.Match(t, pat, RegexOptions.IgnoreCase)
        If m.Success Then Return ParseDec(m.Groups("n").Value)
        Return Nothing
    End Function

    Private Function ExtractPoDescription(tAll As String) As String
        Dim normalized = tAll.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf)
        Dim m = Regex.Match(normalized, "Purchase\s*Order\s*Description:\s*(?<d>[\s\S]+?)(?:\r?\n\s*(?:TERMS|Shipping Address|Line)\b|$)", RegexOptions.IgnoreCase)
        If Not m.Success Then Return Nothing
        Return Clean(m.Groups("d").Value)
    End Function

    Private Function ParseDdMmmYyyy(s As String) As DateTime?
        Dim dt As DateTime
        If DateTime.TryParseExact(s, "dd-MMM-yyyy", Globalization.CultureInfo.GetCultureInfo("en-GB"), DateTimeStyles.None, dt) Then
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
        Return Regex.Replace(s, "\s+", " ").Trim().TrimEnd(":"c)
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
