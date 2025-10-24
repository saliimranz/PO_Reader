Imports System
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Globalization

Module TestVatFix
    Sub Main()
        Console.WriteLine("Testing VAT extraction fix...")
        
        ' Test with sample data that represents both scenarios
        Dim scenario1 = "VAT 5% 25,000.00"
        Dim scenario2 = "VAT % 0.00"
        Dim scenario3 = "VAT 15% 1,500.00"
        Dim scenario4 = "VAT % 500.00"
        
        Console.WriteLine("Scenario 1: " & scenario1)
        TestVatExtraction(scenario1)
        
        Console.WriteLine("Scenario 2: " & scenario2)
        TestVatExtraction(scenario2)
        
        Console.WriteLine("Scenario 3: " & scenario3)
        TestVatExtraction(scenario3)
        
        Console.WriteLine("Scenario 4: " & scenario4)
        TestVatExtraction(scenario4)
    End Sub
    
    Sub TestVatExtraction(text As String)
        Dim vatValue = ExtractVATValue(text, New List(Of String) From {text})
        Console.WriteLine($"  Extracted VAT: {vatValue}")
        Console.WriteLine()
    End Sub
    
    Private Function ExtractVATValue(normalized As String, lines As List(Of String)) As Decimal?
        ' Try multiple patterns for VAT extraction
        Dim vatPatterns = {
            "VAT\s*\d+%\s*(\d{1,3}(?:,\d{3})*(?:\.\d{2})?)",  ' VAT 5% 25,000.00
            "VAT\s*%\s*(\d{1,3}(?:,\d{3})*(?:\.\d{2})?)",     ' VAT % 0.00
            "VAT\s*\d+%\s*(\d+(?:,\d{3})*(?:\.\d{2})?)",      ' VAT 5% 25000.00 (without commas)
            "VAT\s*%\s*(\d+(?:,\d{3})*(?:\.\d{2})?)"          ' VAT % 0.00 (without commas)
        }
        
        ' Try direct pattern matching on each line
        For Each line In lines
            If line.Contains("VAT") AndAlso line.Contains("%") Then
                For Each pattern In vatPatterns
                    Dim match = Regex.Match(line, pattern, RegexOptions.IgnoreCase)
                    If match.Success AndAlso match.Groups.Count > 1 Then
                        Dim valueStr = match.Groups(1).Value
                        If Not String.IsNullOrEmpty(valueStr) Then
                            Dim parsedValue = ParseDec(valueStr)
                            If parsedValue <> 0 Then
                                Return parsedValue
                            End If
                        End If
                    End If
                Next
            End If
        Next
        
        Return Nothing
    End Function
    
    Private Function ParseDec(s As String) As Decimal
        s = s.Replace(",", "")
        Dim d As Decimal
        If Decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, d) Then Return d
        Return 0D
    End Function
End Module