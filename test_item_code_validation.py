#!/usr/bin/env python3
import re

def is_valid_item_code(item_code):
    """Test the item code validation logic"""
    if not item_code or not item_code.strip():
        return False
    
    item_code = item_code.strip()
    
    # Check if it matches common item code patterns:
    # 1. xxxxx-xxxxx (5 characters, dash, 5 characters) - e.g., 90381-35001
    # 2. xxxxx-xxxxx (5 characters, dash, 4+ characters) - e.g., 35070-NSKCL
    # 3. Other alphanumeric patterns with dashes
    patterns = [
        r"^\w{5}-\w{4,}$",  # 5 chars, dash, 4+ chars
        r"^\w{4,}-\w{4,}$"  # 4+ chars, dash, 4+ chars
    ]
    
    for pattern in patterns:
        if re.match(pattern, item_code):
            return True
    
    return False

def test_item_codes():
    """Test various item codes from the PDFs"""
    test_codes = [
        # From first PDF
        "35070-NSKCL",
        "1.143560-26010-",
        "2.130212JR-KOYOWHEEL",
        
        # From second PDF
        "90381-35001",
        "48632-60040", 
        "48632-0K040",
        "48654-60050",
        "48655-60050",
        "16400-17401",
        "90364-33011",
        
        # Invalid codes
        "12345",  # No dash
        "123-45",  # Too short
        "12345-",  # Missing second part
        "-12345",  # Missing first part
        "12345-123456789",  # Too long second part
        "",  # Empty
        "   ",  # Whitespace only
    ]
    
    print("Testing item code validation:")
    print("=" * 50)
    
    for code in test_codes:
        is_valid = is_valid_item_code(code)
        status = "✓ VALID" if is_valid else "✗ INVALID"
        print(f"{code:20} -> {status}")
    
    print("\nExpected results:")
    print("- 35070-NSKCL should be VALID (5 chars, dash, 5+ chars)")
    print("- 90381-35001 should be VALID (5 chars, dash, 5 chars)")
    print("- 1.143560-26010- should be INVALID (starts with number and dot)")
    print("- 2.130212JR-KOYOWHEEL should be INVALID (starts with number and dot)")

if __name__ == "__main__":
    test_item_codes()