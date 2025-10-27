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

def test_correct_patterns():
    """Test with the correct patterns - all should be VALID"""
    
    # These are the patterns you mentioned - all should be VALID
    test_codes = [
        # Pattern: x.1 (where x is any number)
        "1.1",                    # Just the prefix
        "2.1",                    # Just the prefix  
        "3.1",                    # Just the prefix
        "4.1",                    # Just the prefix
        "5.1",                    # Just the prefix
        "10.1",                   # Multiple digits
        "123.1",                  # Multiple digits
        
        # Pattern: x.1 + anything after
        "1.1anything",            # Letters after
        "2.1numbers123",          # Numbers after
        "3.1mixed123ABC",         # Mixed after
        "4.1special-chars!@#",    # Special chars after
        "5.1verylongstringwithlotsofcharacters",  # Long string after
        
        # Pattern: x.1 + item codes (like from PDFs)
        "1.1143560-26010-",       # From first PDF
        "2.1130212JR-KOYOWHEEL",  # From first PDF
        "1.190381-35001",         # From second PDF (with prefix)
        "2.148632-60040",         # From second PDF (with prefix)
        "3.148632-0K040",         # From second PDF (with prefix)
        "4.148654-60050",         # From second PDF (with prefix)
        "5.148655-60050",         # From second PDF (with prefix)
        "6.116400-17401",         # From second PDF (with prefix)
        "7.190364-33011",         # From second PDF (with prefix)
        
        # These should be INVALID (don't start with "x.1")
        "143560-26010-",          # No "x.1" prefix
        "130212JR-KOYOWHEEL",     # No "x.1" prefix
        "90381-35001",            # No "x.1" prefix
        "48632-60040",            # No "x.1" prefix
        "1.2anything",            # Wrong pattern - "x.2" not "x.1"
        "1.0anything",            # Wrong pattern - "x.0" not "x.1"
        "1.anything",             # Missing the "1" after the dot
        "12345",                  # No dot at all
        "123-45",                 # No dot at all
        "",                       # Empty
        "   ",                    # Whitespace only
    ]
    
    print("Testing item code validation with correct patterns:")
    print("=" * 70)
    print("Rule: Accept ANY code starting with 'x.1' where x is any number")
    print("Whatever comes after 'x.1' doesn't matter")
    print("=" * 70)
    
    valid_count = 0
    invalid_count = 0
    
    for code in test_codes:
        is_valid = is_valid_item_code(code)
        status = "✓ VALID" if is_valid else "✗ INVALID"
        print(f"{code:40} -> {status}")
        
        if is_valid:
            valid_count += 1
        else:
            invalid_count += 1
    
    print("\n" + "=" * 70)
    print(f"Summary: {valid_count} valid, {invalid_count} invalid")
    print("=" * 70)
    
    print("\nKey points:")
    print("- All codes starting with '1.1', '2.1', '3.1', etc. should be VALID")
    print("- Whatever comes after 'x.1' doesn't matter")
    print("- Only codes NOT starting with 'x.1' should be INVALID")

if __name__ == "__main__":
    test_correct_patterns()