#!/usr/bin/env python3
"""
Test parser to debug the issue
"""

import sys
import re

def test_patterns():
    test_lines = [
        "1.1",
        "2.1", 
        "15.1",
        "Some other text",
        "43560-26010KOYO",
        "FRT WHEEL BEARING"
    ]
    
    print("Testing patterns:")
    for line in test_lines:
        line = line.strip()
        print(f"Line: '{line}'")
        
        # Test the regex pattern
        if re.match(r'^\d+\.\d+$', line):
            print(f"  -> MATCHES pattern ^\\d+\\.\\d+$")
        else:
            print(f"  -> Does NOT match pattern ^\\d+\\.\\d+$")
        
        print()

if __name__ == "__main__":
    test_patterns()