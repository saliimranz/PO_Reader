#!/usr/bin/env python3
import re
import fitz  # PyMuPDF
from datetime import datetime

def parse_dd_mmm_yyyy(s):
    """Parse date in dd-MMM-yyyy format"""
    if not s:
        return None
    try:
        return datetime.strptime(s, "%d-%b-%Y")
    except:
        return None

def parse_decimal(s):
    """Parse decimal number"""
    if not s:
        return None
    try:
        return float(s.replace(",", ""))
    except:
        return None

def parse_int(s):
    """Parse integer"""
    if not s:
        return None
    try:
        return int(s.replace(",", ""))
    except:
        return None

def rx_val(text, pattern, flags=re.IGNORECASE):
    """Extract value using regex"""
    match = re.search(pattern, text, flags)
    if match:
        return match.group("v").strip()
    return None

def money_after_label_line(text, pattern):
    """Extract money value after label"""
    match = re.search(pattern, text, re.IGNORECASE)
    if match:
        return parse_decimal(match.group("n"))
    return None

def parse_header(text):
    """Parse header information"""
    header = {}
    
    # PO number
    header['PONumber'] = rx_val(text, r"Purchase\s+Order:\s*\((?P<v>[A-Z0-9\-]+)\)")
    
    # Date
    s_date = rx_val(text, r"Date:\s*(?P<v>\d{1,2}\-[A-Z]{3}\-\d{4})")
    if s_date:
        header['PODate'] = parse_dd_mmm_yyyy(s_date)
    else:
        header['PODate'] = None
    
    # Supplier Number
    header['SupplierNumber'] = rx_val(text, r"Supplier\s+Number:\s*(?P<v>[A-Za-z0-9\-]+)")
    
    # Supplier Name
    header['SupplierName'] = rx_val(text, r"Supplier\s+Name:\s*(?P<v>[^\r\n]+)")
    
    # Currency
    header['Currency'] = rx_val(text, r"Currency:\s*[A-Za-z ]+\-\s*(?P<v>[A-Z]{3})")
    
    # Payment Terms
    header['PaymentTerms'] = rx_val(text, r"Payment\s*Terms:\s*(?P<v>[^\r\n]+)")
    
    # Incoterms
    header['IncoTerms'] = rx_val(text, r"Incoterms?:\s*(?P<v>[^\r\n]+)")
    
    # Shipping Address
    header['Shipping_Address'] = rx_val(text, r"Shipping\s+Address:\s*(?P<v>[^\r\n]+)")
    
    # Totals
    header['SubTotal'] = money_after_label_line(text, r"Sub\.?\s*Total\s*Before\s*VAT\s+(?P<n>\-?\d{1,3}(?:,\d{3})*(?:\.\d{2})?)")
    header['VAT'] = money_after_label_line(text, r"VAT\s*\d+%\s+(?P<n>\-?\d{1,3}(?:,\d{3})*(?:\.\d{2})?)")
    header['Total'] = money_after_label_line(text, r"Grand\s*Total\s+(?P<n>\-?\d{1,3}(?:,\d{3})*(?:\.\d{2})?)")
    
    return header

def slice_row(words, cuts, idx):
    """Slice words based on column cuts"""
    if idx >= len(cuts) - 1:
        return ""
    
    x_start = cuts[idx]
    x_end = cuts[idx + 1] if idx + 1 < len(cuts) else float('inf')
    
    result_words = []
    for word in words:
        if x_start <= word['x'] < x_end:
            result_words.append(word['text'])
    
    return " ".join(result_words)

def first_date_like(s):
    """Find first date-like string"""
    match = re.search(r"\d{1,2}\-[A-Z]{3}\-\d{4}", s)
    if match:
        return match.group()
    return None

def parse_items_on_page(page_text, words_data):
    """Parse items from a page"""
    # Group words by Y position (rows)
    rows = []
    current_row = []
    current_y = None
    tolerance = 3.5
    
    for word in words_data:
        if current_y is None or abs(word['y'] - current_y) <= tolerance:
            current_row.append(word)
            current_y = word['y']
        else:
            if current_row:
                rows.append(current_row)
            current_row = [word]
            current_y = word['y']
    
    if current_row:
        rows.append(current_row)
    
    # Sort words in each row by X position
    for row in rows:
        row.sort(key=lambda w: w['x'])
    
    # Column cuts based on analysis
    cuts = [0, 35, 100, 200, 250, 300, 325, 360, 415, 450, 515, 570]
    
    items = []
    
    for row in rows:
        line = " ".join([w['text'] for w in row])
        
        # Skip header rows
        if re.search(r"\b(Line|Item\s+Code|Description|Delivery\s+Date|Deliver\s+to|UOM|Qty|Unit\s+Price|Discount|Net\s+Price|Amount)\b", line, re.IGNORECASE):
            continue
        
        # Skip footer and page info
        if re.search(r"Grand\s*Total|TERMS\s+AND\s+CONDITIONS|Page\s+\d+\s+of\s+\d+", line, re.IGNORECASE):
            continue
        
        # Skip lines that are just "Trading FZE" or similar
        if re.search(r"^\s*Trading\s+FZE\s*$", line, re.IGNORECASE):
            continue
        
        # Extract item data
        item = {}
        item['ItemCode'] = slice_row(row, cuts, 1)
        item['Description'] = slice_row(row, cuts, 2)
        
        delivery_date_str = first_date_like(slice_row(row, cuts, 3))
        item['DeliveryDate'] = parse_dd_mmm_yyyy(delivery_date_str) if delivery_date_str else None
        
        item['UOM'] = slice_row(row, cuts, 5)
        item['Qty'] = parse_int(slice_row(row, cuts, 6))
        item['UnitPrice'] = parse_decimal(slice_row(row, cuts, 7))
        item['NetPrice'] = parse_decimal(slice_row(row, cuts, 9))
        item['Amount'] = parse_decimal(slice_row(row, cuts, 10))
        
        # Cleanup logic
        row_text = " ".join([w['text'] for w in row]).upper()
        if "SUPPLIER DETAILS" in row_text or row_text.startswith("LINE ITEM CODE"):
            continue
        
        # Ignore non-item blocks
        if re.search(r"^(SUPPLIER|DETAILS|TERMS|PAYMENT|INVOICE|DELIVERY|ADDRESS|ALL\s+MAKES|AUTO\s+PARTS|GENERAL|TRADING\s+FZE)\b", item['Description'], re.IGNORECASE):
            continue
        
        # Skip rows that don't have meaningful data
        if not item['ItemCode'] and not item['Description'] and not item['Qty'] and not item['Amount']:
            continue
        
        if item['ItemCode'] or (item['Amount'] and item['Amount'] > 0):
            items.append(item)
    
    return items

def main():
    pdf_path = "/workspace/PO_Reader/App_Data/uploads/59971afc-f79c-4af5-8ddd-6f489a597597.pdf"
    
    print("=== PDF PARSING TEST ===")
    print()
    
    doc = fitz.open(pdf_path)
    
    # Get full text
    full_text = ""
    all_words = []
    
    for page_num in range(len(doc)):
        page = doc[page_num]
        full_text += page.get_text() + "\n\n"
        
        # Get words with positions
        blocks = page.get_text("dict")
        for block in blocks["blocks"]:
            if "lines" in block:
                for line in block["lines"]:
                    for span in line["spans"]:
                        text = span["text"].strip()
                        if text:
                            bbox = span["bbox"]
                            all_words.append({
                                'text': text,
                                'x': bbox[0],
                                'y': bbox[1],
                                'width': bbox[2] - bbox[0],
                                'height': bbox[3] - bbox[1]
                            })
    
    doc.close()
    
    # Parse header
    print("=== HEADER PARSING ===")
    header = parse_header(full_text)
    for key, value in header.items():
        print(f"{key}: {value}")
    print()
    
    # Parse items
    print("=== ITEM PARSING ===")
    items = parse_items_on_page(full_text, all_words)
    print(f"Total items found: {len(items)}")
    print()
    
    for i, item in enumerate(items[:5]):  # Show first 5 items
        print(f"Item {i + 1}:")
        for key, value in item.items():
            print(f"  {key}: {value}")
        print()

if __name__ == "__main__":
    main()