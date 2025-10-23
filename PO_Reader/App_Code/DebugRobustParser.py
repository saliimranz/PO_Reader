#!/usr/bin/env python3
"""
Debug version of the robust parser
"""

import sys
import json
import re
from datetime import datetime
from typing import List, Dict, Any, Optional, Tuple
import logging

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class DebugRobustPdfParser:
    def __init__(self):
        pass
        
    def parse_pdf(self, pdf_path: str) -> Dict[str, Any]:
        """
        Main method to parse PDF and extract structured data
        """
        try:
            # Use pdftotext to extract text
            import subprocess
            result = subprocess.run(['pdftotext', pdf_path, '-'], 
                                 capture_output=True, text=True, check=True)
            text = result.stdout
            
            logger.info(f"Extracted text from PDF: {len(text)} characters")
            
            # Parse the text
            all_data = self._parse_text(text)
            
            return all_data
            
        except Exception as e:
            logger.error(f"Error parsing PDF: {str(e)}")
            raise
    
    def _parse_text(self, text: str) -> Dict[str, Any]:
        """
        Parse the extracted text to extract structured data
        """
        # Normalize text
        normalized_text = self._normalize_text(text)
        
        # Split into lines
        lines = [line.strip() for line in normalized_text.split('\n') if line.strip()]
        
        print(f"Total lines: {len(lines)}")
        
        # Extract master data
        master_data = self._extract_master_data(lines)
        
        # Extract details data
        details = self._extract_details_data(lines)
        
        return {
            'master': master_data,
            'details': details
        }
    
    def _normalize_text(self, text: str) -> str:
        """
        Normalize text for better parsing
        """
        # Replace various line endings with standard newlines
        text = text.replace('\r\n', '\n').replace('\r', '\n')
        
        # Replace non-breaking spaces
        text = text.replace('\u00a0', ' ')
        
        # Normalize multiple spaces
        text = re.sub(r'\s+', ' ', text)
        
        return text
    
    def _extract_master_data(self, lines: List[str]) -> Dict[str, Any]:
        """
        Extract master data from lines
        """
        result = {}
        
        # Join all lines for pattern matching
        full_text = ' '.join(lines)
        
        # Extract PO Number
        po_number = self._extract_po_number(full_text, lines)
        if po_number:
            result['PONumber'] = po_number
        
        # Extract Supplier Information
        supplier_info = self._extract_supplier_info(full_text, lines)
        result.update(supplier_info)
        
        # Extract Date
        po_date = self._extract_po_date(full_text, lines)
        if po_date:
            result['PODate'] = po_date
        
        # Extract Currency
        currency = self._extract_currency(full_text, lines)
        if currency:
            result['Currency'] = currency
        
        # Extract Payment Terms
        payment_terms = self._extract_payment_terms(full_text, lines)
        if payment_terms:
            result['PaymentTerms'] = payment_terms
        
        # Extract Shipping Address
        shipping_address = self._extract_shipping_address(full_text, lines)
        if shipping_address:
            result['Shipping_Address'] = shipping_address
        
        # Extract IncoTerms
        inco_terms = self._extract_inco_terms(full_text, lines)
        if inco_terms:
            result['IncoTerms'] = inco_terms
        
        # Extract PO Description
        po_description = self._extract_po_description(full_text, lines)
        if po_description:
            result['PODescription'] = po_description
        
        # Extract Totals
        totals = self._extract_totals(full_text, lines)
        result.update(totals)
        
        return result
    
    def _extract_po_number(self, full_text: str, lines: List[str]) -> Optional[str]:
        """
        Extract PO number
        """
        # Look for PO number in the full text
        patterns = [
            r'PO\.?\s*Number:\s*([A-Z0-9\-]+)',
            r'Purchase Order:\s*\(([A-Z0-9\-]+)\)',
            r'PO\s*Number:\s*([A-Z0-9\-]+)',
            r'Purchase Order: \(([A-Z0-9\-]+)\)'
        ]
        
        for pattern in patterns:
            match = re.search(pattern, full_text, re.IGNORECASE)
            if match:
                return match.group(1).strip()
        
        return None
    
    def _extract_supplier_info(self, full_text: str, lines: List[str]) -> Dict[str, str]:
        """
        Extract supplier information
        """
        result = {}
        
        # Look for supplier number
        supplier_num_patterns = [
            r'Supplier Number:\s*(\d+)',
            r'Supplier\s+Number:\s*(\d+)'
        ]
        
        for pattern in supplier_num_patterns:
            match = re.search(pattern, full_text, re.IGNORECASE)
            if match:
                result['SupplierNumber'] = match.group(1).strip()
                break
        
        # Look for supplier name
        supplier_name_patterns = [
            r'Supplier Name:\s*([A-Z\s]+?)(?=\s*Supplier|$)',
            r'Supplier\s+Name:\s*([A-Z\s]+?)(?=\s*Supplier|$)'
        ]
        
        for pattern in supplier_name_patterns:
            match = re.search(pattern, full_text, re.IGNORECASE)
            if match:
                result['SupplierName'] = match.group(1).strip()
                break
        
        return result
    
    def _extract_po_date(self, full_text: str, lines: List[str]) -> Optional[str]:
        """
        Extract PO date
        """
        patterns = [
            r'Date:\s*(\d{1,2}-[A-Z]{3}-\d{4})',
            r'(\d{1,2}-[A-Z]{3}-\d{4})'
        ]
        
        for pattern in patterns:
            match = re.search(pattern, full_text, re.IGNORECASE)
            if match:
                date_str = match.group(1).strip()
                try:
                    date_obj = datetime.strptime(date_str, '%d-%b-%Y')
                    return date_obj.strftime('%Y-%m-%d')
                except ValueError:
                    continue
        
        return None
    
    def _extract_currency(self, full_text: str, lines: List[str]) -> Optional[str]:
        """
        Extract currency
        """
        patterns = [
            r'Currency:\s*[^-]+-\s*([A-Z]{3})',
            r'UAE Dirham - ([A-Z]{3})'
        ]
        
        for pattern in patterns:
            match = re.search(pattern, full_text, re.IGNORECASE)
            if match:
                return match.group(1).strip()
        
        return None
    
    def _extract_payment_terms(self, full_text: str, lines: List[str]) -> Optional[str]:
        """
        Extract payment terms
        """
        patterns = [
            r'Payment Terms:\s*([A-Za-z]+)',
            r'Payment\s+Terms:\s*([A-Za-z]+)'
        ]
        
        for pattern in patterns:
            match = re.search(pattern, full_text, re.IGNORECASE)
            if match:
                return match.group(1).strip()
        
        return None
    
    def _extract_shipping_address(self, full_text: str, lines: List[str]) -> Optional[str]:
        """
        Extract shipping address
        """
        # Look for shipping address section
        shipping_match = re.search(r'Shipping Address\s*(.+?)(?=Incoterms:|$)', full_text, re.IGNORECASE | re.DOTALL)
        if shipping_match:
            address = shipping_match.group(1).strip()
            # Clean up the address
            address = re.sub(r'\s+', ' ', address)
            return address
        
        return None
    
    def _extract_inco_terms(self, full_text: str, lines: List[str]) -> Optional[str]:
        """
        Extract IncoTerms
        """
        patterns = [
            r'Incoterms:\s*([A-Za-z]+)',
            r'Inco\s+Terms:\s*([A-Za-z]+)'
        ]
        
        for pattern in patterns:
            match = re.search(pattern, full_text, re.IGNORECASE)
            if match:
                return match.group(1).strip()
        
        return None
    
    def _extract_po_description(self, full_text: str, lines: List[str]) -> Optional[str]:
        """
        Extract PO description
        """
        patterns = [
            r'Purchase Order Description:\s*(.+?)(?=TERMS|$)',
            r'Purchase\s+Order\s+Description:\s*(.+?)(?=TERMS|$)'
        ]
        
        for pattern in patterns:
            match = re.search(pattern, full_text, re.IGNORECASE | re.DOTALL)
            if match:
                description = match.group(1).strip()
                # Clean up the description
                description = re.sub(r'\s+', ' ', description)
                return description
        
        return None
    
    def _extract_totals(self, full_text: str, lines: List[str]) -> Dict[str, float]:
        """
        Extract totals
        """
        result = {}
        
        # Look for subtotal
        subtotal_patterns = [
            r'Sub\.?\s*Total\s*Before\s*VAT\s*([\d,]+\.?\d*)',
            r'Sub\s+Total\s+Before\s+VAT\s+([\d,]+\.?\d*)'
        ]
        
        for pattern in subtotal_patterns:
            match = re.search(pattern, full_text, re.IGNORECASE)
            if match:
                result['SubTotal'] = float(match.group(1).replace(',', ''))
                break
        
        # Look for VAT
        vat_patterns = [
            r'VAT\s*\d+%\s*([\d,]+\.?\d*)',
            r'VAT\s+\d+%\s+([\d,]+\.?\d*)'
        ]
        
        for pattern in vat_patterns:
            match = re.search(pattern, full_text, re.IGNORECASE)
            if match:
                result['VAT'] = float(match.group(1).replace(',', ''))
                break
        
        # Look for grand total
        total_patterns = [
            r'Grand\s*Total\s*([\d,]+\.?\d*)',
            r'Grand\s+Total\s+([\d,]+\.?\d*)'
        ]
        
        for pattern in total_patterns:
            match = re.search(pattern, full_text, re.IGNORECASE)
            if match:
                result['Total'] = float(match.group(1).replace(',', ''))
                break
        
        return result
    
    def _extract_details_data(self, lines: List[str]) -> List[Dict[str, Any]]:
        """
        Extract details data using sophisticated table parsing
        """
        details = []
        
        # Find all item line numbers (e.g., "1.1", "2.1", etc.)
        item_starts = []
        for i, line in enumerate(lines):
            # Look for patterns like "1.1", "2.1", "15.1", etc.
            if re.match(r'^\d+\.\d+$', line.strip()):
                item_starts.append(i)
                print(f"Found item at line {i}: {line}")
        
        logger.info(f"Found {len(item_starts)} potential items")
        
        # Process each item
        for start_idx in item_starts:
            detail = self._parse_single_item(lines, start_idx)
            if detail and self._is_valid_item_detail(detail):
                details.append(detail)
        
        # Sort by line number
        details.sort(key=lambda x: x.get('LineNumber', 0))
        
        # Assign sequential line numbers
        for i, detail in enumerate(details, 1):
            detail['LineNumber'] = i
        
        return details
    
    def _parse_single_item(self, lines: List[str], start_idx: int) -> Optional[Dict[str, Any]]:
        """
        Parse a single item starting at start_idx
        """
        if start_idx >= len(lines):
            return None
        
        # Get the line number
        line_number = lines[start_idx].strip()
        line_num = int(line_number.split('.')[0]) if '.' in line_number else 0
        
        # Initialize item data
        item_data = {
            'LineNumber': line_num,
            'ItemCode': '',
            'Description': '',
            'DeliveryDate': None,
            'UOM': '',
            'Qty': None,
            'UnitPrice': None,
            'Discount': None,
            'NetPrice': None,
            'Amount': None
        }
        
        # Look for item code in the next few lines
        for i in range(start_idx + 1, min(start_idx + 5, len(lines))):
            line = lines[i].strip()
            if not line:
                continue
            
            # Check if this looks like an item code (alphanumeric with dashes, length > 5)
            if re.match(r'^[A-Z0-9\-]+$', line) and len(line) > 5:
                item_data['ItemCode'] = line
                break
        
        # Look for description (usually follows item code, multiple lines possible)
        description_parts = []
        in_description = False
        
        for i in range(start_idx + 1, min(start_idx + 10, len(lines))):
            line = lines[i].strip()
            if not line:
                continue
            
            # Skip if it looks like an item code
            if re.match(r'^[A-Z0-9\-]+$', line) and len(line) > 5:
                in_description = True
                continue
            
            # Skip if it looks like a date
            if re.match(r'^\d{1,2}-[A-Z]{3}-\d{4}$', line):
                item_data['DeliveryDate'] = self._parse_date(line)
                continue
            
            # Skip if it's a short word (UOM)
            if len(line) <= 3 and re.match(r'^[A-Z]+$', line):
                item_data['UOM'] = line
                continue
            
            # Skip if it's just a number (quantity, price, etc.)
            if re.match(r'^\d+\.?\d*$', line):
                continue
            
            # Skip if it's the next item line number
            if re.match(r'^\d+\.\d+$', line):
                break
            
            # This might be part of the description
            if in_description or len(description_parts) > 0:
                description_parts.append(line)
            elif not item_data['ItemCode']:  # If we haven't found item code yet, this might be description
                description_parts.append(line)
        
        if description_parts:
            item_data['Description'] = ' '.join(description_parts)
        
        print(f"Parsed item {line_num}: {item_data}")
        
        return item_data
    
    def _is_valid_item_detail(self, detail: Dict[str, Any]) -> bool:
        """
        Check if a detail object is valid
        """
        # Must have at least item code or description
        if not detail.get('ItemCode') and not detail.get('Description'):
            return False
        
        # Must have some meaningful data
        if not any([detail.get('ItemCode'), detail.get('Description')]):
            return False
        
        return True
    
    def _parse_int(self, value: str) -> Optional[int]:
        """
        Parse integer value
        """
        if not value or not value.strip():
            return None
        
        try:
            # Remove commas and other non-numeric characters except minus sign
            cleaned = re.sub(r'[^\d\-]', '', value.strip())
            return int(cleaned) if cleaned else None
        except ValueError:
            return None
    
    def _parse_decimal(self, value: str) -> Optional[float]:
        """
        Parse decimal value
        """
        if not value or not value.strip():
            return None
        
        try:
            # Remove commas and other non-numeric characters except decimal point and minus sign
            cleaned = re.sub(r'[^\d\.\-]', '', value.strip())
            return float(cleaned) if cleaned else None
        except ValueError:
            return None
    
    def _parse_date(self, value: str) -> Optional[str]:
        """
        Parse date value
        """
        if not value or not value.strip():
            return None
        
        # Look for date patterns
        date_patterns = [
            r'(\d{1,2}-[A-Z]{3}-\d{4})',
            r'(\d{1,2}/\d{1,2}/\d{4})',
            r'(\d{4}-\d{1,2}-\d{1,2})'
        ]
        
        for pattern in date_patterns:
            match = re.search(pattern, value)
            if match:
                date_str = match.group(1)
                try:
                    if '-' in date_str and len(date_str.split('-')[1]) == 3:  # dd-MMM-yyyy format
                        return datetime.strptime(date_str, '%d-%b-%Y').strftime('%Y-%m-%d')
                    elif '/' in date_str:  # dd/mm/yyyy format
                        return datetime.strptime(date_str, '%d/%m/%Y').strftime('%Y-%m-%d')
                    elif '-' in date_str:  # yyyy-mm-dd format
                        return datetime.strptime(date_str, '%Y-%m-%d').strftime('%Y-%m-%d')
                except ValueError:
                    continue
        
        return None

def main():
    """
    Main function to be called from command line
    """
    if len(sys.argv) != 2:
        print("Usage: python3 DebugRobustParser.py <pdf_path>")
        sys.exit(1)
    
    pdf_path = sys.argv[1]
    
    try:
        parser = DebugRobustPdfParser()
        result = parser.parse_pdf(pdf_path)
        
        # Output result as JSON
        print(json.dumps(result, indent=2, default=str))
        
    except Exception as e:
        logger.error(f"Error: {str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    main()