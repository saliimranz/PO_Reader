#!/usr/bin/env python3
import sys
import os
import re

def extract_text_from_pdf(pdf_path):
    """Extract text from PDF using basic text extraction"""
    try:
        import PyPDF2
        with open(pdf_path, 'rb') as file:
            reader = PyPDF2.PdfReader(file)
            text = ""
            for page in reader.pages:
                text += page.extract_text() + "\n"
            return text
    except ImportError:
        print("PyPDF2 not available, trying alternative method...")
        return None

def analyze_pdf_content(pdf_path):
    """Analyze PDF content to understand the structure"""
    print(f"=== Analyzing: {pdf_path} ===")
    
    if not os.path.exists(pdf_path):
        print(f"File not found: {pdf_path}")
        return
    
    # Try to extract text
    text = extract_text_from_pdf(pdf_path)
    if text is None:
        print("Could not extract text from PDF")
        return
    
    print(f"Text length: {len(text)} characters")
    print("\nFirst 1000 characters:")
    print(text[:1000])
    print("\n" + "="*50)
    
    # Look for patterns
    print("\nLooking for patterns:")
    
    # Look for Grand Total
    grand_total_matches = re.findall(r'Grand\s*Total[:\s]*([0-9,]+\.?\d*)', text, re.IGNORECASE)
    print(f"Grand Total matches: {grand_total_matches}")
    
    # Look for Sub Total
    sub_total_matches = re.findall(r'Sub\s*Total[:\s]*([0-9,]+\.?\d*)', text, re.IGNORECASE)
    print(f"Sub Total matches: {sub_total_matches}")
    
    # Look for item codes in format xxxxx-xxxxx
    item_code_matches = re.findall(r'\b\w{5}-\w{5}\b', text)
    print(f"Item code matches: {item_code_matches}")
    
    # Look for line item headers
    line_item_headers = re.findall(r'Line\s*Item\s*Code|Item\s*Code|Line\s*Item', text, re.IGNORECASE)
    print(f"Line item headers: {line_item_headers}")
    
    # Look for terms and conditions
    terms_matches = re.findall(r'TERMS\s+AND\s+CONDITIONS', text, re.IGNORECASE)
    print(f"Terms and conditions: {terms_matches}")
    
    print("\n" + "="*50)

def main():
    pdf_files = [
        "PO_Reader/App_Data/uploads/7b0eac07-6cbf-4dfd-b812-fabf16e67e7e.pdf",
        "PO_Reader/App_Data/uploads/a522f578-b62a-4caa-be91-f300c4f1e19d.pdf"
    ]
    
    for pdf_file in pdf_files:
        analyze_pdf_content(pdf_file)
        print("\n")

if __name__ == "__main__":
    main()