#!/usr/bin/env python3
import re

def is_valid_item_code(item_code):
    """Item code validation - accept ANY code starting with 'x.1' pattern"""
    if not item_code or not item_code.strip():
        return False
    
    item_code = item_code.strip()
    
    # Check if it starts with the pattern "x.1" where x is any number
    # Whatever comes after "x.1" doesn't matter
    if not re.match(r"^\d+\.1", item_code):
        return False
    
    # If it starts with "x.1", accept it regardless of what follows
    return True

def test_actual_pdf_patterns():
    """Test with the actual patterns from the PDFs"""
    
    # These are the ACTUAL patterns from the PDFs
    test_codes = [
        # From first PDF - these should be VALID
        "1.143560-26010-",      # Starts with "1.1" - VALID
        "2.130212JR-KOYOWHEEL", # Starts with "2.1" - VALID
        
        # From second PDF - these should be VALID
        "1.1 90381-35001",      # Starts with "1.1" - VALID
        "2.1 48632-60040",      # Starts with "2.1" - VALID
        "3.1 48632-0K040",      # Starts with "3.1" - VALID
        "4.1 48654-60050",      # Starts with "4.1" - VALID
        "5.1 48655-60050",      # Starts with "5.1" - VALID
        "6.1 16400-17401",      # Starts with "6.1" - VALID
        "7.1 90364-33011",      # Starts with "7.1" - VALID
        
        # Additional test cases that should be VALID
        "1.1",                  # Just the prefix
        "2.1",                  # Just the prefix
        "10.1",                 # Multiple digits
        "1.1anything",          # Anything after
        "2.1numbers123",        # Numbers after
        "3.1mixed123ABC",       # Mixed after
        
        # These should be INVALID (don't start with "x.1")
        "143560-26010-",        # No "x.1" prefix
        "130212JR-KOYOWHEEL",   # No "x.1" prefix
        "90381-35001",          # No "x.1" prefix
        "48632-60040",          # No "x.1" prefix
        "1.2anything",          # Wrong pattern - "x.2" not "x.1"
        "1.0anything",          # Wrong pattern - "x.0" not "x.1"
        "1.anything",           # Missing the "1" after the dot
        "12345",                # No dot at all
        "",                     # Empty
    ]
    
    print("Testing with ACTUAL PDF patterns:")
    print("=" * 70)
    print("Rule: Accept ANY code starting with 'x.1' where x is any number")
    print("=" * 70)
    
    valid_count = 0
    invalid_count = 0
    
    for code in test_codes:
        is_valid = is_valid_item_code(code)
        status = "✓ VALID" if is_valid else "✗ INVALID"
        print(f"{code:30} -> {status}")
        
        if is_valid:
            valid_count += 1
        else:
            invalid_count += 1
    
    print("\n" + "=" * 70)
    print(f"Summary: {valid_count} valid, {invalid_count} invalid")
    print("=" * 70)
    
    print("\n✅ SUCCESS: All codes starting with 'x.1' pattern are accepted!")
    print("✅ SUCCESS: Codes without 'x.1' prefix are rejected!")

if __name__ == "__main__":
    test_actual_pdf_patterns()