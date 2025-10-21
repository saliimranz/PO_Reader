Imports System
Imports System.IO

Module TestParser
    Sub Main()
        Try
            Dim pdfPath = "/workspace/PO_Reader/App_Data/uploads/59971afc-f79c-4af5-8ddd-6f489a597597.pdf"
            Dim parser As New PdfPoParser()
            Dim parsed = parser.Parse(pdfPath)
            
            Console.WriteLine("=== HEADER PARSING TEST ===")
            Console.WriteLine($"PO Number: {parsed.Master.PONumber}")
            Console.WriteLine($"Supplier Name: {parsed.Master.SupplierName}")
            Console.WriteLine($"Supplier Number: {parsed.Master.SupplierNumber}")
            Console.WriteLine($"PO Date: {parsed.Master.PODate}")
            Console.WriteLine($"Currency: {parsed.Master.Currency}")
            Console.WriteLine($"Payment Terms: {parsed.Master.PaymentTerms}")
            Console.WriteLine($"IncoTerms: {parsed.Master.IncoTerms}")
            Console.WriteLine($"Shipping Address: {parsed.Master.Shipping_Address}")
            Console.WriteLine($"SubTotal: {parsed.Master.SubTotal}")
            Console.WriteLine($"VAT: {parsed.Master.VAT}")
            Console.WriteLine($"Total: {parsed.Master.Total}")
            Console.WriteLine()
            
            Console.WriteLine("=== ITEM PARSING TEST ===")
            Console.WriteLine($"Total items found: {parsed.Details.Count}")
            Console.WriteLine()
            
            For i = 0 To Math.Min(5, parsed.Details.Count - 1)
                Dim item = parsed.Details(i)
                Console.WriteLine($"Item {i + 1}:")
                Console.WriteLine($"  ItemCode: '{item.ItemCode}'")
                Console.WriteLine($"  Description: '{item.Description}'")
                Console.WriteLine($"  DeliveryDate: {item.DeliveryDate}")
                Console.WriteLine($"  UOM: '{item.UOM}'")
                Console.WriteLine($"  Qty: {item.Qty}")
                Console.WriteLine($"  UnitPrice: {item.UnitPrice}")
                Console.WriteLine($"  NetPrice: {item.NetPrice}")
                Console.WriteLine($"  Amount: {item.Amount}")
                Console.WriteLine()
            Next
            
        Catch ex As Exception
            Console.WriteLine($"Error: {ex.Message}")
            Console.WriteLine($"Stack Trace: {ex.StackTrace}")
        End Try
    End Sub
End Module