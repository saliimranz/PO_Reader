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

    Private Function NullIf(v As String) As Object
        If String.IsNullOrWhiteSpace(v) Then Return DBNull.Value
        Return v
    End Function

End Class
