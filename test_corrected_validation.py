#!/usr/bin/env python3
import re

def is_valid_line_number(line_number):
    """Check if it's a valid line number pattern (x.1)"""
    if not line_number or not line_number.strip():
        return False
    
    line_number = line_number.strip()
    # Check if it starts with the pattern "x.1" where x is any number
    return re.match(r"^\d+\.1$", line_number) is not None

def is_valid_item_code(item_code):
    """Check if it's a valid item code pattern (xxxxx-xxxxx)"""
    if not item_code or not item_code.strip():
        return False
    
    item_code = item_code.strip()
    # Check if it matches the xxxxx-xxxxx pattern
    return re.match(r"^\w{5}-\w{4,}$", item_code) is not None

def parse_line_item(line):
    """Parse a line to extract line number and item code"""
    parts = line.split()
    if len(parts) < 2:
        return None, None, "Not enough parts"
    
    line_number = parts[0]
    item_code = parts[1]
    
    return line_number, item_code, None

def test_corrected_validation():
    """Test the corrected validation logic"""
    
    # Test cases based on actual PDF content
    test_lines = [
        # Valid line items (should be ACCEPTED)
        "1.1 90381-35001 BUSHSOLID- 30-NOV-2025JF01 -Jebel",
        "2.1 48632-60040 BUSH UPR ARM- 30-NOV-2025JF01 -Jebel", 
        "3.1 48632-0K040 SUSPENSION BUSH- 30-NOV-2025JF01 -Jebel",
        "4.1 48654-60050 BUSH, LWR ARM- 30-NOV-2025JF01 -Jebel",
        "5.1 48655-60050 BUSH, LWR ARM, NO.2- 30-NOV-2025JF01 -Jebel",
        "6.1 31210-36330COVER ASSY,",
        "7.1 48190-0K010CAM ASSY, CAMBER",
        "8.1 16400-17401 Radiator Assy- 30-NOV-2025JF01 -Jebel",
        "9.1 90364-33011 BEARING, NEEDLE- 30-NOV-2025JF01 -Jebel",
        
        # Invalid line items (should be REJECTED)
        "1.2 12345-67890 INVALID LINE NUMBER",  # Wrong line number pattern
        "1.0 12345-67890 INVALID LINE NUMBER",  # Wrong line number pattern
        "1.1 12345 INVALID ITEM CODE",          # Wrong item code pattern
        "1.1 12345-67 INVALID ITEM CODE",       # Wrong item code pattern
        "1.1 12345-67890-EXTRA INVALID",        # Wrong item code pattern
        
        # Header and other content (should be SKIPPED)
        "Line Item Code Description Delivery Date Deliver to UOM Qty. Unit Price Discount Net Price Amount",
        "Grand Total 95,392.48",
        "TERMS AND CONDITIONS",
    ]
    
    print("Testing CORRECTED validation logic:")
    print("=" * 70)
    print("Rule: Line must start with 'x.1' AND have valid item code 'xxxxx-xxxxx'")
    print("=" * 70)
    
    valid_items = []
    rejected_items = []
    skipped_items = []
    
    for i, line in enumerate(test_lines):
        print(f"\nProcessing line {i+1}: {line[:50]}...")
        
        # Skip header rows
        if "Line Item Code" in line or "Grand Total" in line or "TERMS" in line:
            print("  -> Header/Total row - SKIPPING")
            skipped_items.append(line)
            continue
        
        # Parse the line
        line_number, item_code, error = parse_line_item(line)
        
        if error:
            print(f"  -> Parse error: {error} - REJECTING")
            rejected_items.append({"line": line, "reason": error})
            continue
        
        print(f"  -> Line number: '{line_number}', Item code: '{item_code}'")
        
        # Validate line number
        if not is_valid_line_number(line_number):
            print(f"  -> Invalid line number pattern - REJECTING")
            rejected_items.append({"line": line, "reason": "Invalid line number pattern"})
            continue
        
        # Validate item code
        if not is_valid_item_code(item_code):
            print(f"  -> Invalid item code pattern - REJECTING")
            rejected_items.append({"line": line, "reason": "Invalid item code pattern"})
            continue
        
        # Both validations passed
        print(f"  -> Both validations passed - ACCEPTING")
        valid_items.append({
            "line_number": line_number,
            "item_code": item_code,
            "line": line
        })
    
    print("\n" + "=" * 70)
    print("RESULTS:")
    print("=" * 70)
    
    print(f"\n✅ VALID ITEMS ({len(valid_items)}):")
    for item in valid_items:
        print(f"  {item['line_number']:6} -> {item['item_code']:15} - {item['line'][:40]}...")
    
    print(f"\n❌ REJECTED ITEMS ({len(rejected_items)}):")
    for item in rejected_items:
        print(f"  {item['reason']:30} - {item['line'][:40]}...")
    
    print(f"\n🚫 SKIPPED ITEMS ({len(skipped_items)}):")
    for item in skipped_items:
        print(f"  {item[:50]}...")
    
    print(f"\n📊 SUMMARY:")
    print(f"  - Total lines processed: {len(test_lines)}")
    print(f"  - Valid line items: {len(valid_items)}")
    print(f"  - Rejected items: {len(rejected_items)}")
    print(f"  - Skipped items: {len(skipped_items)}")
    
    print(f"\n🎯 VALIDATION LOGIC:")
    print(f"  1. Line must start with 'x.1' pattern (where x is any number)")
    print(f"  2. Second part must be valid item code 'xxxxx-xxxxx' pattern")
    print(f"  3. Both conditions must be met for acceptance")

if __name__ == "__main__":
    test_corrected_validation()