Imports System.Data
Imports System.Data.SqlClient
Imports PO_Reader
Imports WebApp

Public Class PoRepository

    Private ReadOnly _cs As String
    Public Sub New(connectionString As String)
        _cs = connectionString
    End Sub


    Public Function InsertMaster(m As ParsedMaster) As Integer
        Using con As New SqlConnection(_cs)
            con.Open()
            Using cmd As New SqlCommand("" &
            "INSERT INTO dbo.IBL_PO_Master(" &
            "PONumber, SupplierName, SupplierNumber, PODate, Currency, SubTotal, VAT, Total, PaymentTerms, Shipping_Address, IncoTerms, PODescription)" &
            " OUTPUT INSERTED.POMasterID" &
            " VALUES (@PONumber,@SupplierName,@SupplierNumber,@PODate,@Currency,@SubTotal,@VAT,@Total,@PaymentTerms,@Shipping_Address,@IncoTerms,@PODescription);", con)


                cmd.Parameters.AddWithValue("@PONumber", NullIf(m.PONumber))
                cmd.Parameters.AddWithValue("@SupplierName", NullIf(m.SupplierName))
                cmd.Parameters.AddWithValue("@SupplierNumber", NullIf(m.SupplierNumber))
                cmd.Parameters.AddWithValue("@PODate", If(m.PODate.HasValue, CType(m.PODate, Object), DBNull.Value))
                cmd.Parameters.AddWithValue("@Currency", NullIf(m.Currency))
                cmd.Parameters.Add("@SubTotal", SqlDbType.Decimal).Value = If(m.SubTotal.HasValue, m.SubTotal.Value, 0D)
                cmd.Parameters("@SubTotal").Precision = 18 : cmd.Parameters("@SubTotal").Scale = 2
                cmd.Parameters.Add("@VAT", SqlDbType.Decimal).Value = If(m.VAT.HasValue, m.VAT.Value, 0D)
                cmd.Parameters("@VAT").Precision = 18 : cmd.Parameters("@VAT").Scale = 2
                cmd.Parameters.Add("@Total", SqlDbType.Decimal).Value = If(m.Total.HasValue, m.Total.Value, 0D)
                cmd.Parameters("@Total").Precision = 18 : cmd.Parameters("@Total").Scale = 2
                cmd.Parameters.AddWithValue("@PaymentTerms", NullIf(m.PaymentTerms))
                cmd.Parameters.AddWithValue("@Shipping_Address", NullIf(m.Shipping_Address))
                cmd.Parameters.AddWithValue("@IncoTerms", NullIf(m.IncoTerms))
                cmd.Parameters.AddWithValue("@PODescription", NullIf(m.PODescription))


                Return CInt(cmd.ExecuteScalar())
            End Using
        End Using
    End Function

    Public Sub InsertDetails(masterId As Integer, items As List(Of ParsedDetail))
        Using con As New SqlConnection(_cs)
            con.Open()
            Using tx = con.BeginTransaction()
                Try
                    Using cmd As New SqlCommand("" &
                    "INSERT INTO dbo.IBL_PO_Detail(" &
                    "POMasterID, ItemCode, Description, DeliveryDate, UOM, Qty, UnitPrice, NetPrice, Amount)" &
                    " VALUES (@POMasterID,@ItemCode,@Description,@DeliveryDate,@UOM,@Qty,@UnitPrice,@NetPrice,@Amount);", con, tx)


                        cmd.Parameters.Add("@POMasterID", SqlDbType.Int)
                        cmd.Parameters.Add("@ItemCode", SqlDbType.NVarChar, 50)
                        cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 500)
                        cmd.Parameters.Add("@DeliveryDate", SqlDbType.Date)
                        cmd.Parameters.Add("@UOM", SqlDbType.NVarChar, 10)
                        cmd.Parameters.Add("@Qty", SqlDbType.Int)
                        cmd.Parameters.Add("@UnitPrice", SqlDbType.Decimal).Precision = 18 : cmd.Parameters("@UnitPrice").Scale = 2
                        cmd.Parameters.Add("@NetPrice", SqlDbType.Decimal).Precision = 18 : cmd.Parameters("@NetPrice").Scale = 2
                        cmd.Parameters.Add("@Amount", SqlDbType.Decimal).Precision = 18 : cmd.Parameters("@Amount").Scale = 2


                        For Each it In items
                            cmd.Parameters("@POMasterID").Value = masterId
                            cmd.Parameters("@ItemCode").Value = NullIf(it.ItemCode)
                            cmd.Parameters("@Description").Value = NullIf(If(it.Description, String.Empty).Trim())
                            cmd.Parameters("@DeliveryDate").Value = If(it.DeliveryDate.HasValue, CType(it.DeliveryDate, Object), DBNull.Value)
                            cmd.Parameters("@UOM").Value = NullIf(it.UOM)
                            cmd.Parameters("@Qty").Value = If(it.Qty.HasValue, it.Qty.Value, 0)
                            cmd.Parameters("@UnitPrice").Value = If(it.UnitPrice.HasValue, it.UnitPrice.Value, 0D)
                            cmd.Parameters("@NetPrice").Value = If(it.NetPrice.HasValue, it.NetPrice.Value, 0D)
                            cmd.Parameters("@Amount").Value = If(it.Amount.HasValue, it.Amount.Value, 0D)
                            cmd.ExecuteNonQuery()
                        Next
                    End Using
                    tx.Commit()
                Catch
                    tx.Rollback()
                    Throw
                End Try
            End Using
        End Using
    End Sub

    Public Function GetAllPOs() As List(Of POMasterSummary)
        Dim poList As New List(Of POMasterSummary)()

        Using con As New SqlConnection(_cs)
            con.Open()
            Using cmd As New SqlCommand("SELECT POMasterID, PONumber, SupplierName, PODate, Total FROM dbo.IBL_PO_Master ORDER BY PODate DESC, PONumber", con)
                Using reader = cmd.ExecuteReader()
                    ' get ordinals once (faster & avoids mistakes)
                    Dim ordPOMasterID = reader.GetOrdinal("POMasterID")
                    Dim ordPONumber = reader.GetOrdinal("PONumber")
                    Dim ordSupplierName = reader.GetOrdinal("SupplierName")
                    Dim ordPODate = reader.GetOrdinal("PODate")
                    Dim ordTotal = reader.GetOrdinal("Total")

                    While reader.Read()
                        ' POMasterID
                        Dim poMasterID As Integer = 0
                        If Not reader.IsDBNull(ordPOMasterID) Then
                            ' use TryCast pattern or Convert.ToInt32 to be robust
                            Dim val = reader.GetValue(ordPOMasterID)
                            Integer.TryParse(Convert.ToString(val), poMasterID)
                        End If

                        ' PONumber & SupplierName
                        Dim poNumber As String = If(reader.IsDBNull(ordPONumber), String.Empty, reader.GetString(ordPONumber))
                        Dim supplierName As String = If(reader.IsDBNull(ordSupplierName), String.Empty, reader.GetString(ordSupplierName))

                        ' PODate (nullable)
                        Dim poDate As DateTime? = Nothing
                        If Not reader.IsDBNull(ordPODate) Then
                            poDate = reader.GetDateTime(ordPODate)
                        End If

                        ' Total (decimal)
                        Dim total As Decimal = 0D
                        If Not reader.IsDBNull(ordTotal) Then
                            total = reader.GetDecimal(ordTotal)
                        End If

                        poList.Add(New POMasterSummary With {
                        .POMasterID = poMasterID,
                        .PONumber = poNumber,
                        .SupplierName = supplierName,
                        .PODate = poDate,
                        .Total = total
                    })
                    End While
                End Using
            End Using
        End Using

        Return poList
    End Function


    Public Function GetPOByID(poMasterID As Integer) As ParsedPo
        Dim parsedPo As New ParsedPo()
        
        ' Get Master data
        Using con As New SqlConnection(_cs)
            con.Open()
            Using cmd As New SqlCommand("SELECT * FROM dbo.IBL_PO_Master WHERE POMasterID = @POMasterID", con)
                cmd.Parameters.AddWithValue("@POMasterID", poMasterID)
                Using reader = cmd.ExecuteReader()
                    If reader.Read() Then
                        parsedPo.Master = New ParsedMaster With {
                            .PONumber = If(reader.IsDBNull("PONumber"), "", reader.GetString("PONumber")),
                            .SupplierName = If(reader.IsDBNull("SupplierName"), "", reader.GetString("SupplierName")),
                            .SupplierNumber = If(reader.IsDBNull("SupplierNumber"), "", reader.GetString("SupplierNumber")),
                            .PODate = If(reader.IsDBNull("PODate"), Nothing, reader.GetDateTime("PODate")),
                            .Currency = If(reader.IsDBNull("Currency"), "", reader.GetString("Currency")),
                            .SubTotal = If(reader.IsDBNull("SubTotal"), Nothing, reader.GetDecimal("SubTotal")),
                            .VAT = If(reader.IsDBNull("VAT"), Nothing, reader.GetDecimal("VAT")),
                            .Total = If(reader.IsDBNull("Total"), Nothing, reader.GetDecimal("Total")),
                            .PaymentTerms = If(reader.IsDBNull("PaymentTerms"), "", reader.GetString("PaymentTerms")),
                            .Shipping_Address = If(reader.IsDBNull("Shipping_Address"), "", reader.GetString("Shipping_Address")),
                            .IncoTerms = If(reader.IsDBNull("IncoTerms"), "", reader.GetString("IncoTerms")),
                            .PODescription = If(reader.IsDBNull("PODescription"), "", reader.GetString("PODescription"))
                        }
                    End If
                End Using
            End Using
        End Using

        ' Get Details data
        parsedPo.Details = New List(Of ParsedDetail)
        Using con As New SqlConnection(_cs)
            con.Open()
            Using cmd As New SqlCommand("SELECT * FROM dbo.IBL_PO_Detail WHERE POMasterID = @POMasterID ORDER BY LineNumber", con)
                cmd.Parameters.AddWithValue("@POMasterID", poMasterID)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        ' Handle LineNumber conversion safely
                        Dim lineNumber As Integer = 0
                        If Not reader.IsDBNull("LineNumber") Then
                            Integer.TryParse(reader("LineNumber").ToString(), lineNumber)
                        End If
                        
                        ' Handle Qty conversion safely
                        Dim qty As Integer? = Nothing
                        If Not reader.IsDBNull("Qty") Then
                            Dim qtyValue As Integer = 0
                            If Integer.TryParse(reader("Qty").ToString(), qtyValue) Then
                                qty = qtyValue
                            End If
                        End If
                        
                        parsedPo.Details.Add(New ParsedDetail With {
                            .LineNumber = lineNumber,
                            .ItemCode = If(reader.IsDBNull("ItemCode"), "", reader.GetString("ItemCode")),
                            .Description = If(reader.IsDBNull("Description"), "", reader.GetString("Description")),
                            .DeliveryDate = If(reader.IsDBNull("DeliveryDate"), Nothing, reader.GetDateTime("DeliveryDate")),
                            .UOM = If(reader.IsDBNull("UOM"), "", reader.GetString("UOM")),
                            .Qty = qty,
                            .UnitPrice = If(reader.IsDBNull("UnitPrice"), Nothing, reader.GetDecimal("UnitPrice")),
                            .NetPrice = If(reader.IsDBNull("NetPrice"), Nothing, reader.GetDecimal("NetPrice")),
                            .Amount = If(reader.IsDBNull("Amount"), Nothing, reader.GetDecimal("Amount"))
                        })
                    End While
                End Using
            End Using
        End Using

        Return parsedPo
    End Function

    Public Sub UpdateMaster(poMasterID As Integer, m As ParsedMaster)
        Using con As New SqlConnection(_cs)
            con.Open()
            Using cmd As New SqlCommand("" &
                "UPDATE dbo.IBL_PO_Master SET " &
                "PONumber=@PONumber, SupplierName=@SupplierName, SupplierNumber=@SupplierNumber, " &
                "PODate=@PODate, Currency=@Currency, SubTotal=@SubTotal, VAT=@VAT, Total=@Total, " &
                "PaymentTerms=@PaymentTerms, Shipping_Address=@Shipping_Address, IncoTerms=@IncoTerms, " &
                "PODescription=@PODescription " &
                "WHERE POMasterID=@POMasterID", con)

                cmd.Parameters.AddWithValue("@POMasterID", poMasterID)
                cmd.Parameters.AddWithValue("@PONumber", NullIf(m.PONumber))
                cmd.Parameters.AddWithValue("@SupplierName", NullIf(m.SupplierName))
                cmd.Parameters.AddWithValue("@SupplierNumber", NullIf(m.SupplierNumber))
                cmd.Parameters.AddWithValue("@PODate", If(m.PODate.HasValue, CType(m.PODate, Object), DBNull.Value))
                cmd.Parameters.AddWithValue("@Currency", NullIf(m.Currency))
                cmd.Parameters.Add("@SubTotal", SqlDbType.Decimal).Value = If(m.SubTotal.HasValue, m.SubTotal.Value, 0D)
                cmd.Parameters("@SubTotal").Precision = 18 : cmd.Parameters("@SubTotal").Scale = 2
                cmd.Parameters.Add("@VAT", SqlDbType.Decimal).Value = If(m.VAT.HasValue, m.VAT.Value, 0D)
                cmd.Parameters("@VAT").Precision = 18 : cmd.Parameters("@VAT").Scale = 2
                cmd.Parameters.Add("@Total", SqlDbType.Decimal).Value = If(m.Total.HasValue, m.Total.Value, 0D)
                cmd.Parameters("@Total").Precision = 18 : cmd.Parameters("@Total").Scale = 2
                cmd.Parameters.AddWithValue("@PaymentTerms", NullIf(m.PaymentTerms))
                cmd.Parameters.AddWithValue("@Shipping_Address", NullIf(m.Shipping_Address))
                cmd.Parameters.AddWithValue("@IncoTerms", NullIf(m.IncoTerms))
                cmd.Parameters.AddWithValue("@PODescription", NullIf(m.PODescription))

                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Public Sub UpdateDetails(poMasterID As Integer, items As List(Of ParsedDetail))
        Using con As New SqlConnection(_cs)
            con.Open()
            Using tx = con.BeginTransaction()
                Try
                    ' Delete existing details
                    Using cmd As New SqlCommand("DELETE FROM dbo.IBL_PO_Detail WHERE POMasterID = @POMasterID", con, tx)
                        cmd.Parameters.AddWithValue("@POMasterID", poMasterID)
                        cmd.ExecuteNonQuery()
                    End Using

                    ' Insert updated details
                    Using cmd As New SqlCommand("" &
                        "INSERT INTO dbo.IBL_PO_Detail(" &
                        "POMasterID, LineNumber, ItemCode, Description, DeliveryDate, UOM, Qty, UnitPrice, NetPrice, Amount)" &
                        " VALUES (@POMasterID,@LineNumber,@ItemCode,@Description,@DeliveryDate,@UOM,@Qty,@UnitPrice,@NetPrice,@Amount);", con, tx)

                        cmd.Parameters.Add("@POMasterID", SqlDbType.Int)
                        cmd.Parameters.Add("@LineNumber", SqlDbType.Int)
                        cmd.Parameters.Add("@ItemCode", SqlDbType.NVarChar, 50)
                        cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 500)
                        cmd.Parameters.Add("@DeliveryDate", SqlDbType.Date)
                        cmd.Parameters.Add("@UOM", SqlDbType.NVarChar, 10)
                        cmd.Parameters.Add("@Qty", SqlDbType.Int)
                        cmd.Parameters.Add("@UnitPrice", SqlDbType.Decimal).Precision = 18 : cmd.Parameters("@UnitPrice").Scale = 2
                        cmd.Parameters.Add("@NetPrice", SqlDbType.Decimal).Precision = 18 : cmd.Parameters("@NetPrice").Scale = 2
                        cmd.Parameters.Add("@Amount", SqlDbType.Decimal).Precision = 18 : cmd.Parameters("@Amount").Scale = 2

                        For Each it In items
                            cmd.Parameters("@POMasterID").Value = poMasterID
                            cmd.Parameters("@LineNumber").Value = it.LineNumber
                            cmd.Parameters("@ItemCode").Value = NullIf(it.ItemCode)
                            cmd.Parameters("@Description").Value = NullIf(If(it.Description, String.Empty).Trim())
                            cmd.Parameters("@DeliveryDate").Value = If(it.DeliveryDate.HasValue, CType(it.DeliveryDate, Object), DBNull.Value)
                            cmd.Parameters("@UOM").Value = NullIf(it.UOM)
                            cmd.Parameters("@Qty").Value = If(it.Qty.HasValue, it.Qty.Value, 0)
                            cmd.Parameters("@UnitPrice").Value = If(it.UnitPrice.HasValue, it.UnitPrice.Value, 0D)
                            cmd.Parameters("@NetPrice").Value = If(it.NetPrice.HasValue, it.NetPrice.Value, 0D)
                            cmd.Parameters("@Amount").Value = If(it.Amount.HasValue, it.Amount.Value, 0D)
                            cmd.ExecuteNonQuery()
                        Next
                    End Using
                    tx.Commit()
                Catch
                    tx.Rollback()
                    Throw
                End Try
            End Using
        End Using
    End Sub

    Private Function NullIf(v As String) As Object
        If String.IsNullOrWhiteSpace(v) Then Return DBNull.Value
        Return v
    End Function

End Class

Public Class POMasterSummary
    Public Property POMasterID As Integer
    Public Property PONumber As String
    Public Property SupplierName As String
    Public Property PODate As DateTime?
    Public Property Total As Decimal
End Class
