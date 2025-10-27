Imports System
Imports System.IO
Imports UglyToad.PdfPig
Imports UglyToad.PdfPig.Content

Module TestPdfParser
    Sub Main()
        Console.WriteLine("Testing PDF Parser with uploaded files...")
        Console.WriteLine()
        
        ' Test the two PDF files
        Dim pdfFiles = {"7b0eac07-6cbf-4dfd-b812-fabf16e67e7e.pdf", "a522f578-b62a-4caa-be91-f300c4f1e19d.pdf"}
        
        For Each pdfFile In pdfFiles
            Console.WriteLine("=== Testing: " & pdfFile & " ===")
            Dim pdfPath = Path.Combine("PO_Reader", "App_Data", "uploads", pdfFile)
            
            If File.Exists(pdfPath) Then
                Try
                    ' Create a simple parser instance to test
                    Dim parser As New SimplePdfParser()
                    Dim result = parser.Parse(pdfPath)
                    
                    Console.WriteLine("PO Number: " & If(result.PONumber, "N/A"))
                    Console.WriteLine("Supplier: " & If(result.SupplierName, "N/A"))
                    Console.WriteLine("Total: " & If(result.Total, "N/A"))
                    Console.WriteLine("SubTotal: " & If(result.SubTotal, "N/A"))
                    Console.WriteLine("VAT: " & If(result.VAT, "N/A"))
                    Console.WriteLine()
                    Console.WriteLine("Line Items (" & result.Details.Count & "):")
                    
                    For Each item In result.Details
                        Console.WriteLine("  Line " & item.LineNumber & ": " & If(item.ItemCode, "N/A") & " - " & If(item.Description, "N/A") & " - Qty: " & If(item.Qty, "N/A") & " - Amount: " & If(item.Amount, "N/A"))
                    Next
                    
                Catch ex As Exception
                    Console.WriteLine("Error parsing PDF: " & ex.Message)
                    Console.WriteLine("Stack trace: " & ex.StackTrace)
                End Try
            Else
                Console.WriteLine("File not found: " & pdfPath)
            End If
            Console.WriteLine()
        Next
        
        Console.WriteLine("Test completed. Press any key to exit...")
        Console.ReadKey()
    End Sub
End Module

' Simple test classes to mimic the main parser
Public Class SimplePdfParser
    Public Function Parse(pdfPath As String) As SimpleParsedPo
        Dim result As New SimpleParsedPo()
        
        Using doc = PdfDocument.Open(pdfPath)
            For Each page In doc.GetPages()
                ' Simple text extraction for testing
                Dim pageText = page.Text
                Console.WriteLine("Page " & page.Number & " text length: " & pageText.Length)
                
                ' Look for basic patterns
                If pageText.Contains("Grand Total") Then
                    Console.WriteLine("Found Grand Total on page " & page.Number)
                End If
                
                If pageText.Contains("Sub Total") Then
                    Console.WriteLine("Found Sub Total on page " & page.Number)
                End If
                
                ' Look for item codes in format xxxxx-xxxxx
                Dim itemCodePattern = "\b\w{5}-\w{5}\b"
                Dim matches = System.Text.RegularExpressions.Regex.Matches(pageText, itemCodePattern)
                Console.WriteLine("Found " & matches.Count & " potential item codes on page " & page.Number)
                
                For Each match In matches
                    Console.WriteLine("  Item code: " & match.Value)
                Next
            Next
        End Using
        
        Return result
    End Function
End Class

Public Class SimpleParsedPo
    Public Property PONumber As String
    Public Property SupplierName As String
    Public Property Total As String
    Public Property SubTotal As String
    Public Property VAT As String
    Public Property Details As New List(Of SimpleParsedDetail)
End Class

Public Class SimpleParsedDetail
    Public Property LineNumber As Integer
    Public Property ItemCode As String
    Public Property Description As String
    Public Property Qty As String
    Public Property Amount As String
End Class