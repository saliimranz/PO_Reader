#!/usr/bin/env python3
import sys
import os
sys.path.append('/workspace/PO_Reader/bin')

try:
    from UglyToad.PdfPig import PdfDocument
    from UglyToad.PdfPig.Content import Page
    import re
except ImportError as e:
    print(f"Import error: {e}")
    print("Available files in bin directory:")
    for root, dirs, files in os.walk('/workspace/PO_Reader/bin'):
        for file in files:
            print(f"  {os.path.join(root, file)}")
    sys.exit(1)

def analyze_pdf(pdf_path):
    print(f"\n=== Analyzing: {os.path.basename(pdf_path)} ===")
    print("=" * 60)
    
    try:
        with PdfDocument.Open(pdf_path) as doc:
            pages = list(doc.GetPages())
            print(f"Number of pages: {len(pages)}")
            
            for i, page in enumerate(pages):
                print(f"\n--- Page {i+1} ---")
                text = page.Text
                lines = text.split('\n')
                
                # Look for line item patterns
                line_item_section = False
                for j, line in enumerate(lines):
                    line = line.strip()
                    if not line:
                        continue
                        
                    # Check if we're in the line items section
                    if re.search(r'(line\s+item|item\s+code|description|qty|unit\s+price|amount)', line, re.IGNORECASE):
                        line_item_section = True
                        print(f"Line {j+1}: {line}")
                        continue
                    
                    # Check for potential line items (rows with numbers and data)
                    if line_item_section:
                        # Look for patterns that might be line items
                        if re.search(r'^\d+', line) or re.search(r'[A-Z0-9\-]{3,}', line):
                            print(f"Potential line item {j+1}: {line}")
                            
                    # Stop at certain sections
                    if re.search(r'(terms\s+and\s+conditions|grand\s+total|page\s+\d+)', line, re.IGNORECASE):
                        line_item_section = False
                        break
                        
    except Exception as e:
        print(f"Error analyzing {pdf_path}: {e}")

def main():
    uploads_dir = "/workspace/PO_Reader/App_Data/uploads"
    pdf_files = [f for f in os.listdir(uploads_dir) if f.endswith('.pdf')]
    
    print(f"Found {len(pdf_files)} PDF files to analyze")
    
    for pdf_file in pdf_files:
        pdf_path = os.path.join(uploads_dir, pdf_file)
        analyze_pdf(pdf_path)

if __name__ == "__main__":
    main()