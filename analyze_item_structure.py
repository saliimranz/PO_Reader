#!/usr/bin/env python3
import PyPDF2
import re

def analyze_item_structure(pdf_path):
    """Analyze the structure of line items in PDF"""
    try:
        with open(pdf_path, 'rb') as file:
            reader = PyPDF2.PdfReader(file)
            text = ""
            for page in reader.pages:
                text += page.extract_text() + "\n"
            
            print(f"=== Analyzing: {pdf_path} ===")
            
            # Look for lines that contain both line numbers and item codes
            lines = text.split('\n')
            
            print(f"\nLooking for line item patterns:")
            print("="*60)
            
            for i, line in enumerate(lines):
                line = line.strip()
                if line and ('1.1' in line or '2.1' in line or '3.1' in line):
                    print(f"Line {i+1}: {line}")
                    
                    # Try to extract different parts
                    parts = line.split()
                    if len(parts) >= 2:
                        print(f"  -> Parts: {parts}")
                        if len(parts) >= 2:
                            print(f"  -> Part 1 (line number): '{parts[0]}'")
                            print(f"  -> Part 2 (item code): '{parts[1]}'")
                        if len(parts) >= 3:
                            print(f"  -> Part 3 (description start): '{parts[2]}'")
                    print()
            
            # Look for patterns like "xxxxx-xxxxx" (item codes)
            item_code_pattern = r'\b\w{5}-\w{4,}\b'
            item_codes = re.findall(item_code_pattern, text)
            print(f"\nItem codes found (xxxxx-xxxxx pattern): {item_codes}")
            
            # Look for patterns like "x.1" (line numbers)
            line_number_pattern = r'\b(\d+\.1)\b'
            line_numbers = re.findall(line_number_pattern, text)
            print(f"Line numbers found (x.1 pattern): {line_numbers}")
            
            return text
            
    except Exception as e:
        print(f"Error reading {pdf_path}: {e}")
        return ""

def main():
    pdf_files = [
        "PO_Reader/App_Data/uploads/7b0eac07-6cbf-4dfd-b812-fabf16e67e7e.pdf",
        "PO_Reader/App_Data/uploads/a522f578-b62a-4caa-be91-f300c4f1e19d.pdf"
    ]
    
    for pdf_file in pdf_files:
        text = analyze_item_structure(pdf_file)
        print("\n" + "="*80 + "\n")

if __name__ == "__main__":
    main()