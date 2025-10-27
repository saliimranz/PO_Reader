Imports System
Imports System.IO
Imports PO_Reader

Module TestPdfParser
    Sub Main()
        Console.WriteLine("=== PDF Line Item Analysis ===")
        Console.WriteLine()
        
        Dim uploadsPath = "/workspace/PO_Reader/App_Data/uploads/"
        Dim pdfFiles = Directory.GetFiles(uploadsPath, "*.pdf")
        
        For Each pdfFile In pdfFiles
            Console.WriteLine($"Analyzing: {Path.GetFileName(pdfFile)}")
            Console.WriteLine("=" & New String("=", 50))
            
            Try
                Dim parser As New PdfPoParser()
                Dim parsed = parser.Parse(pdfFile)
                
                Console.WriteLine($"PO Number: {parsed.Master.PONumber}")
                Console.WriteLine($"Supplier: {parsed.Master.SupplierName}")
                Console.WriteLine($"Total Line Items: {parsed.Details.Count}")
                Console.WriteLine()
                
                Console.WriteLine("LINE ITEMS:")
                Console.WriteLine("-" & New String("-", 80))
                Console.WriteLine($"{"#",-3} {"ItemCode",-15} {"Description",-30} {"Qty",-8} {"UnitPrice",-12} {"Amount",-12}")
                Console.WriteLine("-" & New String("-", 80))
                
                For Each item In parsed.Details
                    Console.WriteLine($"{item.LineNumber,-3} {item.ItemCode,-15} {item.Description.Substring(0, Math.Min(30, item.Description.Length)),-30} {item.Qty,-8} {item.UnitPrice,-12} {item.Amount,-12}")
                Next
                
                Console.WriteLine()
                Console.WriteLine("DETAILED ANALYSIS:")
                Console.WriteLine("-" & New String("-", 50))
                
                For i = 0 To parsed.Details.Count - 1
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
                Console.WriteLine($"Error parsing {Path.GetFileName(pdfFile)}: {ex.Message}")
            End Try
            
            Console.WriteLine()
            Console.WriteLine(New String("=", 60))
            Console.WriteLine()
        Next
        
        Console.WriteLine("Analysis complete. Press any key to exit.")
        Console.ReadKey()
    End Sub
End Module