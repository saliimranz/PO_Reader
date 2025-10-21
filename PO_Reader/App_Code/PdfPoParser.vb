Imports System
Imports System.Globalization
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
        ' Normalize only CRLF/CR, NOT all spaces
        Dim t = tAll.Replace(vbCr, "").Replace(vbLf, vbLf) ' keep line breaks

        ' PO number - Updated pattern to match the actual format
        h.PONumber = RxVal(t, "Purchase\s+Order:\s*\((?<v>[A-Z0-9\-]+)\)")

        ' Date (line contains "Date: 18-OCT-2025")
        Dim sDate = RxVal(t, "Date:\s*(?<v>\d{1,2}\-[A-Z]{3}\-\d{4})", RegexOptions.IgnoreCase)
        If Not String.IsNullOrEmpty(sDate) Then h.PODate = ParseDdMmmYyyy(sDate)

        ' Supplier Number - Updated pattern
        h.SupplierNumber = RxVal(t, "Supplier\s+Number:\s*(?<v>[A-Za-z0-9\-]+)", RegexOptions.IgnoreCase)

        ' Supplier Name - Updated pattern
        h.SupplierName = RxVal(t, "Supplier\s+Name:\s*(?<v>[^\r\n]+)", RegexOptions.IgnoreCase)

        ' Currency: "UAE Dirham - AED" - Updated pattern
        h.Currency = RxVal(t, "Currency:\s*[A-Za-z ]+\-\s*(?<v>[A-Z]{3})", RegexOptions.IgnoreCase)

        ' Payment Terms / Incoterms - Updated patterns
        h.PaymentTerms = RxVal(t, "Payment\s*Terms:\s*(?<v>[^\r\n]+)", RegexOptions.IgnoreCase)
        h.IncoTerms = RxVal(t, "Incoterms?:\s*(?<v>[^\r\n]+)", RegexOptions.IgnoreCase)

        ' Shipping address - Updated pattern
        h.Shipping_Address = RxVal(t, "Shipping\s+Address:\s*(?<v>[^\r\n]+)", RegexOptions.IgnoreCase)

        ' Totals (exact labels from the PO) - These might not be present in this PDF format
        h.SubTotal = MoneyAfterLabelLine(t, "Sub\.?\s*Total\s*Before\s*VAT\s+(?<n>\-?\d{1,3}(?:,\d{3})*(?:\.\d{2})?)")
        h.VAT = MoneyAfterLabelLine(t, "VAT\s*\d+%\s+(?<n>\-?\d{1,3}(?:,\d{3})*(?:\.\d{2})?)")
        h.Total = MoneyAfterLabelLine(t, "Grand\s*Total\s+(?<n>\-?\d{1,3}(?:,\d{3})*(?:\.\d{2})?)")
    End Sub

    Private Function RxVal(t As String, pat As String, Optional opt As RegexOptions = RegexOptions.IgnoreCase) As String
        Dim m = Regex.Match(t, pat, opt)
        If m.Success Then Return m.Groups("v").Value.Trim()
        Return Nothing
    End Function

    Private Function MoneyAfterLabelLine(t As String, pat As String) As Decimal?
        Dim m = Regex.Match(t, pat, RegexOptions.IgnoreCase)
        If m.Success Then Return ParseDec(m.Groups("n").Value)
        Return Nothing
    End Function

    Private Function ExtractPoDescription(tAll As String) As String
        Dim m = Regex.Match(tAll, "Purchase\s*Order\s*Description:\s*(?<d>[\s\S]+)$", RegexOptions.IgnoreCase)
        If Not m.Success Then Return Nothing
        Return m.Groups("d").Value.Trim()
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
        Dim rows = GroupByY(words, 3.5)

        ' cuts tuned for the actual PDF layout based on analysis
        Dim cuts = New List(Of Double) From {0, 35, 100, 200, 250, 300, 325, 360, 415, 450, 515, 570}

        Dim list As New List(Of ParsedDetail)
        For Each r In rows
            Dim line = String.Join(" ", r.Select(Function(w) w.Text))
            ' Skip header rows
            If Regex.IsMatch(line, "\b(Line|Item\s+Code|Description|Delivery\s+Date|Deliver\s+to|UOM|Qty|Unit\s+Price|Discount|Net\s+Price|Amount)\b", RegexOptions.IgnoreCase) Then Continue For
            ' Skip footer and page info
            If Regex.IsMatch(line, "Grand\s*Total|TERMS\s+AND\s+CONDITIONS|Page\s+\d+\s+of\s+\d+", RegexOptions.IgnoreCase) Then Continue For
            ' Skip lines that are just "Trading FZE" or similar
            If Regex.IsMatch(line, "^\s*Trading\s+FZE\s*$", RegexOptions.IgnoreCase) Then Continue For

            Dim it As New ParsedDetail
            it.ItemCode = Slice(r, cuts, 1)  ' Column 1: Item Code
            it.Description = Slice(r, cuts, 2)  ' Column 2: Description
            it.DeliveryDate = TryDate(FirstDateLike(Slice(r, cuts, 3)))  ' Column 3: Delivery Date
            it.UOM = Slice(r, cuts, 5)  ' Column 5: UOM
            it.Qty = TryInt(Slice(r, cuts, 6))  ' Column 6: Qty
            it.UnitPrice = TryDec(Slice(r, cuts, 7))  ' Column 7: Unit Price
            it.NetPrice = TryDec(Slice(r, cuts, 9))  ' Column 9: Net Price
            it.Amount = TryDec(Slice(r, cuts, 10))  ' Column 10: Amount

            ' --- CLEANUP / FILTER LOGIC ---
            ' Drop lines that are obviously headers or section text
            Dim rowText = String.Join(" ", r.Select(Function(w) w.Text)).ToUpperInvariant()
            If rowText.Contains("SUPPLIER DETAILS") OrElse rowText.StartsWith("LINE ITEM CODE") Then Continue For

            ' Ignore “Deliver to” and other non-item blocks
            If Regex.IsMatch(it.Description, "^(SUPPLIER|DETAILS|TERMS|PAYMENT|INVOICE|DELIVERY|ADDRESS|ALL\s+MAKES|AUTO\s+PARTS|GENERAL|TRADING\s+FZE)\b", RegexOptions.IgnoreCase) Then
                Continue For
            End If
            
            ' Skip rows that don't have meaningful data
            If String.IsNullOrWhiteSpace(it.ItemCode) AndAlso String.IsNullOrWhiteSpace(it.Description) AndAlso Not it.Qty.HasValue AndAlso Not it.Amount.HasValue Then
                Continue For
            End If
            ' --- END CLEANUP ---

            If Not String.IsNullOrWhiteSpace(it.ItemCode) OrElse (it.Amount.HasValue AndAlso it.Amount.Value > 0) Then
                list.Add(it)
            End If
        Next
        Return list
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
End Class
