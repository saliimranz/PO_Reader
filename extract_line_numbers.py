#!/usr/bin/env python3
import PyPDF2
import re

def extract_line_numbers_from_pdf(pdf_path):
    """Extract all line numbers from PDF"""
    try:
        with open(pdf_path, 'rb') as file:
            reader = PyPDF2.PdfReader(file)
            text = ""
            for page in reader.pages:
                text += page.extract_text() + "\n"
            
            print(f"=== Analyzing: {pdf_path} ===")
            print(f"Text length: {len(text)} characters")
            
            # Look for line numbers like 1.1, 2.1, 3.1, etc.
            # Pattern: number followed by dot followed by 1, then space or end
            line_number_pattern = r'\b(\d+\.1)\b'
            line_numbers = re.findall(line_number_pattern, text)
            
            print(f"\nLine numbers found: {line_numbers}")
            print(f"Total line numbers: {len(line_numbers)}")
            
            # Show the context around each line number
            print(f"\nContext around line numbers:")
            lines = text.split('\n')
            for i, line in enumerate(lines):
                if re.search(line_number_pattern, line):
                    print(f"Line {i+1}: {line.strip()}")
            
            return line_numbers
            
    except Exception as e:
        print(f"Error reading {pdf_path}: {e}")
        return []

def main():
    pdf_files = [
        "PO_Reader/App_Data/uploads/7b0eac07-6cbf-4dfd-b812-fabf16e67e7e.pdf",
        "PO_Reader/App_Data/uploads/a522f578-b62a-4caa-be91-f300c4f1e19d.pdf"
    ]
    
    for pdf_file in pdf_files:
        line_numbers = extract_line_numbers_from_pdf(pdf_file)
        print("\n" + "="*60 + "\n")

if __name__ == "__main__":
    main()