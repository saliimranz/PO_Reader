Imports System
Imports System.IO
Imports PO_Reader

Module TestParser
    Sub Main()
        ' Test the two PDF files
        Dim pdfFiles = {"7b0eac07-6cbf-4dfd-b812-fabf16e67e7e.pdf", "a522f578-b62a-4caa-be91-f300c4f1e19d.pdf"}
        
        For Each pdfFile In pdfFiles
            Console.WriteLine("=== Testing: " & pdfFile & " ===")
            Dim pdfPath = Path.Combine("PO_Reader", "App_Data", "uploads", pdfFile)
            
            If File.Exists(pdfPath) Then
                Try
                    Dim parser As New PdfPoParser()
                    Dim result = parser.Parse(pdfPath)
                    
                    Console.WriteLine("PO Number: " & If(result.Master.PONumber, "N/A"))
                    Console.WriteLine("Supplier: " & If(result.Master.SupplierName, "N/A"))
                    Console.WriteLine("Total: " & If(result.Master.Total, "N/A"))
                    Console.WriteLine("SubTotal: " & If(result.Master.SubTotal, "N/A"))
                    Console.WriteLine("VAT: " & If(result.Master.VAT, "N/A"))
                    Console.WriteLine()
                    Console.WriteLine("Line Items (" & result.Details.Count & "):")
                    
                    For Each item In result.Details
                        Console.WriteLine("  Line " & item.LineNumber & ": " & If(item.ItemCode, "N/A") & " - " & If(item.Description, "N/A") & " - Qty: " & If(item.Qty, "N/A") & " - Amount: " & If(item.Amount, "N/A"))
                    Next
                    
                Catch ex As Exception
                    Console.WriteLine("Error parsing PDF: " & ex.Message)
                End Try
            Else
                Console.WriteLine("File not found: " & pdfPath)
            End If
            Console.WriteLine()
        Next
    End Sub
End Module