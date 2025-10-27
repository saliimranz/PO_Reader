#!/usr/bin/env python3
import re

def is_valid_item_code(item_code):
    """Item code validation - only accept codes starting with 'x.1' pattern"""
    if not item_code or not item_code.strip():
        return False
    
    item_code = item_code.strip()
    
    # Check if it starts with the pattern "x.1" where x is a number
    # This is the base requirement - if it doesn't start with this pattern, reject it
    if not re.match(r"^\d+\.1", item_code):
        return False
    
    # If it starts with "x.1", accept it (like it was before)
    # The item code can have any format after the "x.1" prefix
    return True

def test_final_validation():
    """Test with actual patterns from both PDFs"""
    
    # These are the actual patterns from the PDFs
    test_codes = [
        # From first PDF - these should be VALID (they start with "x.1")
        "1.143560-26010-",      # Starts with "1.1" - VALID
        "2.130212JR-KOYOWHEEL", # Starts with "2.1" - VALID
        
        # From second PDF - these should be INVALID (they don't start with "x.1")
        "90381-35001",          # No "x.1" prefix - INVALID
        "48632-60040",          # No "x.1" prefix - INVALID
        "48632-0K040",          # No "x.1" prefix - INVALID
        "48654-60050",          # No "x.1" prefix - INVALID
        "48655-60050",          # No "x.1" prefix - INVALID
        "16400-17401",          # No "x.1" prefix - INVALID
        "90364-33011",          # No "x.1" prefix - INVALID
        
        # Additional test cases
        "10.12345-67890",       # Starts with "10.1" - VALID
        "1.1ABC-DEF",           # Starts with "1.1" - VALID
        "1.1",                  # Just "1.1" - VALID
        "1.1anything",          # Starts with "1.1" - VALID
        
        # These should be INVALID
        "143560-26010-",        # No "x.1" prefix
        "130212JR-KOYOWHEEL",   # No "x.1" prefix  
        "35070-NSKCL",          # No "x.1" prefix
        "1.2anything",          # Wrong pattern - should be "x.1" not "x.2"
        "1.0anything",          # Wrong pattern - should be "x.1" not "x.0"
        "1.anything",           # Missing the "1" after the dot
        "12345",                # No dot at all
        "",                     # Empty
    ]
    
    print("Final item code validation test:")
    print("=" * 60)
    print("Rule: ONLY accept codes starting with pattern 'x.1' where x is a number")
    print("=" * 60)
    
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
    
    print("\n" + "=" * 60)
    print(f"Summary: {valid_count} valid, {invalid_count} invalid")
    print("=" * 60)
    
    print("\nIMPORTANT NOTE:")
    print("This validation will REJECT all item codes from the second PDF")
    print("because they don't start with 'x.1' pattern.")
    print("Only codes from the first PDF that start with 'x.1' will be accepted.")

if __name__ == "__main__":
    test_final_validation()