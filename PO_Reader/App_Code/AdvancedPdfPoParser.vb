Imports System
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class AdvancedPdfPoParser
    Private ReadOnly _pythonPath As String = "python3"
    Private ReadOnly _scriptPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Code", "ProductionPdfParser.py")

    Public Function Parse(pdfPath As String) As ParsedPo
        Try
            ' Call Python script to parse PDF
            Dim result = CallPythonParser(pdfPath)

            ' Convert JSON result to ParsedPo object
            Return ConvertJsonToParsedPo(result)

        Catch ex As Exception
            Throw New Exception($"Failed to parse PDF with advanced OCR: {ex.Message}", ex)
        End Try
    End Function

    Private Function CallPythonParser(pdfPath As String) As String
        Dim processInfo As New ProcessStartInfo() With {
            .FileName = _pythonPath,
            .Arguments = $"""{_scriptPath}"" ""{pdfPath}""",
            .UseShellExecute = False,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .CreateNoWindow = True
        }

        Using process As New Process()
            process.StartInfo = processInfo

            Dim output As New StringBuilder()
            Dim error_e As New StringBuilder()

            AddHandler process.OutputDataReceived, Sub(sender, e)
                                                       If Not String.IsNullOrEmpty(e.Data) Then
                                                           output.AppendLine(e.Data)
                                                       End If
                                                   End Sub

            AddHandler process.ErrorDataReceived, Sub(sender, e)
                                                      If Not String.IsNullOrEmpty(e.Data) Then
                                                          error_e.AppendLine(e.Data)
                                                      End If
                                                  End Sub

            process.Start()
            process.BeginOutputReadLine()
            process.BeginErrorReadLine()

            process.WaitForExit(30000) ' 30 second timeout

            If process.ExitCode <> 0 Then
                Throw New Exception($"Python script failed with exit code {process.ExitCode}. Error: {error_e.ToString()}")
            End If

            Return output.ToString()
        End Using
    End Function

    Private Function ConvertJsonToParsedPo(jsonResult As String) As ParsedPo
        Try
            Dim jsonObject = JObject.Parse(jsonResult)

            ' Parse master data
            Dim master = New ParsedMaster()
            Dim masterData = jsonObject("master")
            If masterData IsNot Nothing Then
                master.PONumber = GetStringValue(masterData, "PONumber")
                master.SupplierName = GetStringValue(masterData, "SupplierName")
                master.SupplierNumber = GetStringValue(masterData, "SupplierNumber")
                master.Currency = GetStringValue(masterData, "Currency")
                master.PODescription = GetStringValue(masterData, "PODescription")

                ' Parse dates
                Dim poDateStr = GetStringValue(masterData, "PODate")
                If Not String.IsNullOrEmpty(poDateStr) Then
                    Dim poDate As DateTime
                    If DateTime.TryParse(poDateStr, poDate) Then
                        master.PODate = poDate
                    End If
                End If

                ' Parse numeric values
                master.SubTotal = GetDecimalValue(masterData, "SubTotal")
                master.VAT = GetDecimalValue(masterData, "VAT")
                master.Total = GetDecimalValue(masterData, "Total")
                master.PaymentTerms = GetStringValue(masterData, "PaymentTerms")
                master.Shipping_Address = GetStringValue(masterData, "Shipping_Address")
                master.IncoTerms = GetStringValue(masterData, "IncoTerms")
            End If

            ' Parse details data
            Dim details As New List(Of ParsedDetail)()
            Dim detailsArray = jsonObject("details")
            If detailsArray IsNot Nothing AndAlso detailsArray.Type = JTokenType.Array Then
                For Each detailJson In detailsArray
                    Dim detail = New ParsedDetail()
                    detail.LineNumber = GetIntegerValue(detailJson, "LineNumber")
                    detail.ItemCode = GetStringValue(detailJson, "ItemCode")
                    detail.Description = GetStringValue(detailJson, "Description")
                    detail.UOM = GetStringValue(detailJson, "UOM")
                    detail.Qty = GetIntegerValue(detailJson, "Qty")
                    detail.UnitPrice = GetDecimalValue(detailJson, "UnitPrice")
                    detail.NetPrice = GetDecimalValue(detailJson, "NetPrice")
                    detail.Amount = GetDecimalValue(detailJson, "Amount")

                    ' Parse delivery date
                    Dim deliveryDateStr = GetStringValue(detailJson, "DeliveryDate")
                    If Not String.IsNullOrEmpty(deliveryDateStr) Then
                        Dim deliveryDate As DateTime
                        If DateTime.TryParse(deliveryDateStr, deliveryDate) Then
                            detail.DeliveryDate = deliveryDate
                        End If
                    End If

                    details.Add(detail)
                Next
            End If

            Return New ParsedPo With {
                .Master = master,
                .Details = details
            }

        Catch ex As Exception
            Throw New Exception($"Failed to convert JSON result to ParsedPo: {ex.Message}", ex)
        End Try
    End Function

    Private Function GetStringValue(token As JToken, propertyName As String) As String
        If token Is Nothing Then Return String.Empty

        Dim value = token(propertyName)
        If value Is Nothing OrElse value.Type = JTokenType.Null Then
            Return String.Empty
        End If

        Return value.ToString().Trim()
    End Function

    Private Function GetIntegerValue(token As JToken, propertyName As String) As Integer?
        If token Is Nothing Then Return Nothing

        Dim value = token(propertyName)
        If value Is Nothing OrElse value.Type = JTokenType.Null Then
            Return Nothing
        End If

        Dim intValue As Integer
        If Integer.TryParse(value.ToString(), intValue) Then
            Return intValue
        End If

        Return Nothing
    End Function

    Private Function GetDecimalValue(token As JToken, propertyName As String) As Decimal?
        If token Is Nothing Then Return Nothing

        Dim value = token(propertyName)
        If value Is Nothing OrElse value.Type = JTokenType.Null Then
            Return Nothing
        End If

        Dim decimalValue As Decimal
        If Decimal.TryParse(value.ToString(), decimalValue) Then
            Return decimalValue
        End If

        Return Nothing
    End Function
End Class