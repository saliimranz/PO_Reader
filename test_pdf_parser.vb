Imports System
Imports System.IO
Imports UglyToad.PdfPig
Imports UglyToad.PdfPig.Content

Module TestPdfParser
    Sub Main()
        ' Test the two PDF files
        Dim pdfFiles = {"7b0eac07-6cbf-4dfd-b812-fabf16e67e7e.pdf", "a522f578-b62a-4caa-be91-f300c4f1e19d.pdf"}
        
        For Each pdfFile In pdfFiles
            Console.WriteLine("=== Testing: " & pdfFile & " ===")
            Dim pdfPath = Path.Combine("PO_Reader", "App_Data", "uploads", pdfFile)
            
            If File.Exists(pdfPath) Then
                Using doc = PdfDocument.Open(pdfPath)
                    For Each page In doc.GetPages()
                        Console.WriteLine("Page " & page.Number & ":")
                        Console.WriteLine("Text content:")
                        Console.WriteLine(page.Text)
                        Console.WriteLine("--- End of page ---")
                        Console.WriteLine()
                    Next
                End Using
            Else
                Console.WriteLine("File not found: " & pdfPath)
            End If
            Console.WriteLine()
        Next
    End Sub
End Module