Imports System
Imports System.IO
Imports UglyToad.PdfPig
Imports UglyToad.PdfPig.Content

Module DebugPdfParser
    Sub Main()
        Dim pdfPath = "/workspace/PO_Reader/App_Data/uploads/59971afc-f79c-4af5-8ddd-6f489a597597.pdf"
        
        Console.WriteLine("=== PDF Debug Analysis ===")
        Console.WriteLine()
        
        Using doc = PdfDocument.Open(pdfPath)
            Console.WriteLine($"Total pages: {doc.NumberOfPages}")
            Console.WriteLine()
            
            For pageNum = 1 To doc.NumberOfPages
                Dim page = doc.GetPage(pageNum)
                Console.WriteLine($"=== PAGE {pageNum} ===")
                Console.WriteLine("Raw text:")
                Console.WriteLine(page.Text)
                Console.WriteLine()
                Console.WriteLine("Words with positions:")
                For Each word In page.GetWords().OrderBy(Function(w) -w.BoundingBox.Top).ThenBy(Function(w) w.BoundingBox.Left)
                    Console.WriteLine($"'{word.Text}' at ({word.BoundingBox.Left:F1}, {word.BoundingBox.Top:F1})")
                Next
                Console.WriteLine()
                Console.WriteLine("=" & New String("="c, 50))
                Console.WriteLine()
            Next
        End Using
    End Sub
End Module