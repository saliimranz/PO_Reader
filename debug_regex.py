#!/usr/bin/env python3
import re

def debug_regex():
    """Debug the regex pattern"""
    test_codes = [
        "1.143560-26010-",
        "1.90381-35001", 
        "2.48632-60040",
        "3.48632-0K040",
        "4.48654-60050",
        "5.48655-60050",
        "6.16400-17401",
        "7.90364-33011",
    ]
    
    pattern = r"^\d+\.1"
    
    print("Debugging regex pattern:", pattern)
    print("=" * 50)
    
    for code in test_codes:
        match = re.match(pattern, code)
        print(f"'{code}' -> match: {match is not None}")
        if match:
            print(f"  Matched part: '{match.group()}'")
        else:
            print(f"  No match - let's check what it starts with:")
            print(f"  First 3 chars: '{code[:3]}'")
            print(f"  Starts with digit: {code[0].isdigit() if code else False}")
            print(f"  Second char is dot: {code[1] == '.' if len(code) > 1 else False}")
            print(f"  Third char is 1: {code[2] == '1' if len(code) > 2 else False}")

if __name__ == "__main__":
    debug_regex()