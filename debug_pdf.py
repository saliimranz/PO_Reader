#!/usr/bin/env python3
import sys
import fitz  # PyMuPDF
import re

def analyze_pdf(pdf_path):
    print("=== PDF Debug Analysis ===")
    print()
    
    doc = fitz.open(pdf_path)
    print(f"Total pages: {len(doc)}")
    print()
    
    for page_num in range(len(doc)):
        page = doc[page_num]
        print(f"=== PAGE {page_num + 1} ===")
        print("Raw text:")
        print(page.get_text())
        print()
        
        # Get text blocks with positions
        blocks = page.get_text("dict")
        print("Text blocks with positions:")
        for block in blocks["blocks"]:
            if "lines" in block:
                for line in block["lines"]:
                    for span in line["spans"]:
                        text = span["text"].strip()
                        if text:
                            bbox = span["bbox"]
                            print(f"'{text}' at ({bbox[0]:.1f}, {bbox[1]:.1f}, {bbox[2]:.1f}, {bbox[3]:.1f})")
        print()
        print("=" * 50)
        print()
    
    doc.close()

if __name__ == "__main__":
    pdf_path = "/workspace/PO_Reader/App_Data/uploads/59971afc-f79c-4af5-8ddd-6f489a597597.pdf"
    analyze_pdf(pdf_path)