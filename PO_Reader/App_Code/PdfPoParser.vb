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
        Dim t = Regex.Replace(tAll, "\s+", " ").Trim()

        Dim mPO = Regex.Match(t, "Purchase\s+Order:\s*\((?<po>[A-Z0-9\-]+)\)", RegexOptions.IgnoreCase)
        If mPO.Success Then h.PONumber = mPO.Groups("po").Value

        Dim mDate = Regex.Match(t, "\bDate:\s*(?<d>\d{1,2}\-[A-Z]{3}\-\d{4})", RegexOptions.IgnoreCase)
        If mDate.Success Then h.PODate = ParseDdMmmYyyy(mDate.Groups("d").Value)

        Dim mSupp = Regex.Match(t, "Supplier\s+Name:\s*(?<name>.+?)\s+Supplier\s+(No\.?|Number)?:\s*(?<num>[A-Z0-9\-]+)?", RegexOptions.IgnoreCase)
        If mSupp.Success Then
            h.SupplierName = mSupp.Groups("name").Value.Trim()
            h.SupplierNumber = mSupp.Groups("num").Value.Trim()
        Else
            Dim mSupp2 = Regex.Match(t, "Supplier\s+Name:\s*(?<name>.+?)\s+Supplier\s+VAT#:", RegexOptions.IgnoreCase)
            If mSupp2.Success Then h.SupplierName = mSupp2.Groups("name").Value.Trim()
        End If

        Dim mCur = Regex.Match(t, "Currency:\s*[A-Za-z ]+\-\s*(?<cur>[A-Z]{3})", RegexOptions.IgnoreCase)
        If mCur.Success Then h.Currency = mCur.Groups("cur").Value.ToUpperInvariant()

        h.SubTotal = MoneyAfterLabel(t, "Sub\.?\s*Total\s*Before\s*VAT")
        h.VAT = MoneyAfterLabel(t, "VAT\s*\d+%")
        h.Total = MoneyAfterLabel(t, "Grand\s*Total")

        Dim mTerms = Regex.Match(t, "Payment\s*Terms:\s*(?<x>[^:]+?)(?:\s+[A-Z][a-z]+:|\s+Grand\s*Total|$)", RegexOptions.IgnoreCase)
        If mTerms.Success Then h.PaymentTerms = Clean(mTerms.Groups("x").Value)

        Dim mShip = Regex.Match(t, "Invoice\s*Address:\s*(?<x>.+?)\s+Delivery\s*Address:\s*(?<y>.+?)\s+(?:Currency|Page)\b", RegexOptions.IgnoreCase)
        If mShip.Success Then h.Shipping_Address = Clean(mShip.Groups("y").Value)

        Dim mInco = Regex.Match(t, "Incoterms?:\s*(?<x>[^:]+?)(?:\s+[A-Z][a-z]+:|\s+Grand\s*Total|$)", RegexOptions.IgnoreCase)
        If mInco.Success Then h.IncoTerms = Clean(mInco.Groups("x").Value)
    End Sub

    Private Function ExtractPoDescription(tAll As String) As String
        Dim m = Regex.Match(tAll, "Purchase\s*Order\s*Description:\s*(?<d>[\s\S]+)$", RegexOptions.IgnoreCase)
        If Not m.Success Then Return Nothing
        Return m.Groups("d").Value.Trim()
    End Function

    Private Function MoneyAfterLabel(t As String, label As String) As Decimal?
        Dim m = Regex.Match(t, label & "\s*(?<n>\-?\d{1,3}(?:,\d{3})*(?:\.\d{2})?)", RegexOptions.IgnoreCase)
        If m.Success Then Return ParseDec(m.Groups("n").Value)
        Return Nothing
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
        Dim rows = GroupByY(words, 2.0)

        ' cuts tuned for your layout
        Dim cuts = New List(Of Double) From {0, 75, 155, 355, 470, 520, 565, 615, 675, 725, 780}

        Dim list As New List(Of ParsedDetail)
        For Each r In rows
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
