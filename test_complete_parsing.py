#!/usr/bin/env python3
import re

def is_valid_item_code(item_code):
    """Item code validation logic"""
    if not item_code or not item_code.strip():
        return False
    
    item_code = item_code.strip()
    
    patterns = [
        r"^\w{5}-\w{4,}$",  # 5 chars, dash, 4+ chars
        r"^\w{4,}-\w{4,}$"  # 4+ chars, dash, 4+ chars
    ]
    
    for pattern in patterns:
        if re.match(pattern, item_code):
            return True
    
    return False

def is_grand_total_line(line):
    """Check if line indicates end of line items"""
    pattern = r"Grand\s*Total|Sub\s*Total|TERMS\s+AND\s+CONDITIONS|Page\s+\d+\s+of\s+\d+|Total\s+Amount|Final\s+Total"
    return re.search(pattern, line, re.IGNORECASE) is not None

def is_header_line(line):
    """Check if line is a header row"""
    pattern = r"\b(Line|Code|Description|Delivery|UOM|Qty|Unit|Discount|Net|Amount)\b"
    return re.search(pattern, line, re.IGNORECASE) is not None

def simulate_parsing():
    """Simulate the parsing process with sample data"""
    
    # Sample lines that might appear in a PDF (based on the actual PDF content)
    sample_lines = [
        "Line Item Code Description Delivery Date Deliver to UOM Qty. Unit Price Discount Net Price Amount",
        "1.143560-26010- KOYOFRT WHEEL BEARING 4 BOLT-29-JAN-2026 All Makes Auto Parts General Trading FZE EA 100 89.5000 0 89.5000 8,950.00",
        "2.130212JR-KOYOWHEEL BEARING/30212JR-29-JA All Makes Auto Parts General Trading FZE EA 50 120.0000 0 120.0000 6,000.00",
        "3.35070-NSKCL WHEEL BEARING All Makes Auto Parts General Trading FZE EA 25 150.0000 0 150.0000 3,750.00",
        "4.90381-35001 BUSH SOLID All Makes Auto Parts General Trading FZE EA 35 10.8800 0 10.8800 380.80",
        "5.48632-60040 BUSH UPR ARM All Makes Auto Parts General Trading FZE EA 20 45.9500 0 45.9500 919.00",
        "6.48632-0K040 SUSPENSION BUSH All Makes Auto Parts General Trading FZE EA 15 25.0000 0 25.0000 375.00",
        "Grand Total 95,392.48",
        "TERMS AND CONDITIONS",
        "Page 1 of 2",
        "Some random text after grand total",
        "More invalid content",
    ]
    
    print("Simulating PDF parsing process:")
    print("=" * 60)
    
    found_grand_total = False
    valid_line_items = []
    rejected_items = []
    
    for i, line in enumerate(sample_lines):
        print(f"\nProcessing line {i+1}: {line[:50]}...")
        
        # Check if we've reached the end of line items
        if is_grand_total_line(line):
            print("  -> Found grand total/end marker - STOPPING processing")
            found_grand_total = True
            continue
        
        # If we've found grand total, stop processing
        if found_grand_total:
            print("  -> Already found grand total - SKIPPING")
            continue
        
        # Skip header rows
        if is_header_line(line):
            print("  -> Header row - SKIPPING")
            continue
        
        # Extract item code (simplified - in real code this would use the Slice function)
        # For this test, let's extract the first part before the first space
        parts = line.split()
        if len(parts) > 0:
            potential_item_code = parts[0]
            
            # Remove leading numbers and dots (like "1.", "2.", etc.)
            item_code = re.sub(r'^\d+\.', '', potential_item_code)
            
            print(f"  -> Extracted item code: '{item_code}'")
            
            # Validate item code
            if is_valid_item_code(item_code):
                print(f"  -> Item code is VALID - ACCEPTING")
                valid_line_items.append({
                    'line_number': len(valid_line_items) + 1,
                    'item_code': item_code,
                    'line': line
                })
            else:
                print(f"  -> Item code is INVALID - REJECTING")
                rejected_items.append({
                    'item_code': item_code,
                    'line': line,
                    'reason': 'Invalid item code format'
                })
        else:
            print("  -> No parts found - SKIPPING")
    
    print("\n" + "=" * 60)
    print("PARSING RESULTS:")
    print("=" * 60)
    
    print(f"\nValid line items ({len(valid_line_items)}):")
    for item in valid_line_items:
        print(f"  Line {item['line_number']}: {item['item_code']} - {item['line'][:50]}...")
    
    print(f"\nRejected items ({len(rejected_items)}):")
    for item in rejected_items:
        print(f"  {item['item_code']} - {item['reason']} - {item['line'][:50]}...")
    
    print(f"\nSummary:")
    print(f"  - Total lines processed: {len(sample_lines)}")
    print(f"  - Valid line items: {len(valid_line_items)}")
    print(f"  - Rejected items: {len(rejected_items)}")
    print(f"  - Grand total found: {found_grand_total}")

if __name__ == "__main__":
    simulate_parsing()