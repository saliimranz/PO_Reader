#!/usr/bin/env python3
"""
Debug PDF Parser to understand the table structure
"""

import sys
import re
from typing import List

def debug_pdf(pdf_path: str):
    import subprocess
    result = subprocess.run(['pdftotext', pdf_path, '-'], 
                         capture_output=True, text=True, check=True)
    text = result.stdout
    
    lines = [line.strip() for line in text.split('\n') if line.strip()]
    
    print("=== DEBUGGING PDF STRUCTURE ===")
    print(f"Total lines: {len(lines)}")
    
    # Find table-related lines
    table_keywords = ['Line', 'Item Code', 'Description', 'Delivery Date', 'UOM', 'Qty', 'Unit Price', 'Amount']
    
    print("\n=== TABLE HEADERS FOUND ===")
    for i, line in enumerate(lines):
        if any(keyword in line for keyword in table_keywords):
            print(f"Line {i}: {line}")
    
    print("\n=== LOOKING FOR ITEM PATTERNS ===")
    for i, line in enumerate(lines):
        # Look for line number patterns
        if re.match(r'^\d+\.\d+$', line):
            print(f"Line {i}: {line} (Potential item start)")
            # Show next few lines
            for j in range(i+1, min(i+5, len(lines))):
                print(f"  Line {j}: {lines[j]}")
            print()
    
    print("\n=== NUMERIC DATA LINES ===")
    for i, line in enumerate(lines):
        if re.match(r'^\d+\.\d+$', line) and '.' in line:
            print(f"Line {i}: {line} (Decimal number)")
    
    print("\n=== ALPHANUMERIC CODES ===")
    for i, line in enumerate(lines):
        if re.match(r'^[A-Z0-9\-]+$', line) and len(line) > 5:
            print(f"Line {i}: {line} (Potential item code)")

if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python3 DebugPdfParser.py <pdf_path>")
        sys.exit(1)
    
    debug_pdf(sys.argv[1])