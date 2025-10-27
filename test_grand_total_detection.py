#!/usr/bin/env python3
import re

def test_grand_total_detection():
    """Test the grand total detection patterns"""
    
    # Test lines from the PDFs
    test_lines = [
        "Grand Total 95,392.48",
        "Grand Total: 36,233.80", 
        "Sub Total 50,000.00",
        "Sub Total: 25,000.00",
        "TERMS AND CONDITIONS",
        "Page 1 of 2",
        "Total Amount 100,000.00",
        "Final Total 200,000.00",
        "Line Item Code Description Delivery Date",
        "1.143560-26010- KOYOFRT WHEEL BEARING",
        "2.130212JR-KOYOWHEEL BEARING/30212JR-29-JA",
        "Regular line item content",
        "Another line item",
    ]
    
    # The regex pattern from the VB code
    pattern = r"Grand\s*Total|Sub\s*Total|TERMS\s+AND\s+CONDITIONS|Page\s+\d+\s+of\s+\d+|Total\s+Amount|Final\s+Total"
    
    print("Testing grand total detection:")
    print("=" * 50)
    
    for line in test_lines:
        matches = re.search(pattern, line, re.IGNORECASE)
        is_grand_total = matches is not None
        status = "✓ STOP" if is_grand_total else "✗ CONTINUE"
        print(f"{line:40} -> {status}")
    
    print("\nExpected results:")
    print("- Lines with 'Grand Total', 'Sub Total', 'TERMS AND CONDITIONS', 'Page X of Y' should STOP processing")
    print("- Regular line items should CONTINUE processing")

if __name__ == "__main__":
    test_grand_total_detection()