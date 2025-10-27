#!/usr/bin/env python3
import re

def is_valid_item_code(item_code):
    """Updated item code validation logic"""
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

def test_updated_item_codes():
    """Test the updated item code validation with various patterns"""
    test_codes = [
        # Valid codes (should be ACCEPTED)
        "1.143560-26010-",      # From first PDF - starts with "1.1"
        "2.130212JR-KOYOWHEEL", # From first PDF - starts with "2.1" 
        "3.35070-NSKCL",        # From first PDF - starts with "3.1"
        "1.90381-35001",        # From second PDF - starts with "1.1"
        "2.48632-60040",        # From second PDF - starts with "2.1"
        "3.48632-0K040",        # From second PDF - starts with "3.1"
        "4.48654-60050",        # From second PDF - starts with "4.1"
        "5.48655-60050",        # From second PDF - starts with "5.1"
        "6.16400-17401",        # From second PDF - starts with "6.1"
        "7.90364-33011",        # From second PDF - starts with "7.1"
        "10.12345-67890",       # Multiple digits before dot
        "1.1ABC-DEF",           # Letters after the prefix
        "1.1",                  # Just the prefix
        "1.1anything",          # Anything after the prefix
        
        # Invalid codes (should be REJECTED)
        "143560-26010-",        # No "x.1" prefix
        "130212JR-KOYOWHEEL",   # No "x.1" prefix
        "35070-NSKCL",          # No "x.1" prefix
        "90381-35001",          # No "x.1" prefix
        "1.143560-26010",       # Starts with "1.1" but missing dash at end
        "2.130212JR",           # Starts with "2.1" but no dash
        "1.2anything",          # Wrong pattern - should be "x.1" not "x.2"
        "1.0anything",          # Wrong pattern - should be "x.1" not "x.0"
        "1.anything",           # Missing the "1" after the dot
        "12345",                # No dot at all
        "123-45",               # No dot at all
        "12345-",               # No dot at all
        "-12345",               # No dot at all
        "",                     # Empty
        "   ",                  # Whitespace only
        "abc.1anything",        # Letters before the dot
        ".1anything",           # No number before the dot
    ]
    
    print("Testing updated item code validation:")
    print("=" * 60)
    print("Rule: Item code must start with pattern 'x.1' where x is a number")
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
    
    print("\nExpected behavior:")
    print("- Codes starting with 'x.1' (like '1.143560-26010-') should be VALID")
    print("- Codes not starting with 'x.1' (like '143560-26010-') should be INVALID")
    print("- The format after 'x.1' doesn't matter - it can be anything")

if __name__ == "__main__":
    test_updated_item_codes()