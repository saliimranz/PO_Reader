Public Class ParsedMaster
    Public Property PONumber As String
    Public Property SupplierName As String
    Public Property SupplierNumber As String
    Public Property PODate As DateTime?
    Public Property Currency As String
    Public Property SubTotal As Decimal?
    Public Property VAT As Decimal?
    Public Property Total As Decimal?
    Public Property PaymentTerms As String
    Public Property Shipping_Address As String
    Public Property IncoTerms As String
    Public Property PODescription As String
End Class

Public Class ParsedDetail
    Public Property ItemCode As String
    Public Property Description As String
    Public Property DeliveryDate As DateTime?
    Public Property UOM As String
    Public Property Qty As Integer?
    Public Property UnitPrice As Decimal?
    Public Property NetPrice As Decimal?
    Public Property Amount As Decimal?
End Class

Public Class ParsedPo
    Public Property Master As ParsedMaster
    Public Property Details As List(Of ParsedDetail)
End Class
