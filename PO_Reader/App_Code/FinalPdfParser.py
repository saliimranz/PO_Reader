#!/usr/bin/env python3
"""
Final PDF Parser with multi-line table support
This parser handles complex table structures where data spans multiple lines
and uses advanced pattern matching to extract structured data.
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

class FinalPdfParser:
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
        # Split text into lines
        lines = [line.strip() for line in text.split('\n') if line.strip()]
        
        # Extract master data
        master_data = self._extract_master_data(lines)
        
        # Extract details data
        details_data = self._extract_details_data(lines)
        
        return {
            'master': master_data,
            'details': details_data
        }
    
    def _extract_master_data(self, lines: List[str]) -> Dict[str, Any]:
        """
        Extract master data from lines
        """
        result = {}
        
        # Join all lines for pattern matching
        full_text = ' '.join(lines)
        
        # Extract PO Number
        po_number = self._find_po_number(full_text, lines)
        if po_number:
            result['PONumber'] = po_number
        
        # Extract Supplier Information
        supplier_info = self._find_supplier_info(full_text, lines)
        result.update(supplier_info)
        
        # Extract Date
        po_date = self._find_po_date(full_text, lines)
        if po_date:
            result['PODate'] = po_date
        
        # Extract Currency
        currency = self._find_currency(full_text, lines)
        if currency:
            result['Currency'] = currency
        
        # Extract Payment Terms
        payment_terms = self._find_payment_terms(full_text, lines)
        if payment_terms:
            result['PaymentTerms'] = payment_terms
        
        # Extract Shipping Address
        shipping_address = self._find_shipping_address(full_text, lines)
        if shipping_address:
            result['Shipping_Address'] = shipping_address
        
        # Extract IncoTerms
        inco_terms = self._find_inco_terms(full_text, lines)
        if inco_terms:
            result['IncoTerms'] = inco_terms
        
        # Extract PO Description
        po_description = self._find_po_description(full_text, lines)
        if po_description:
            result['PODescription'] = po_description
        
        # Extract Totals
        totals = self._find_totals(full_text, lines)
        result.update(totals)
        
        return result
    
    def _extract_details_data(self, lines: List[str]) -> List[Dict[str, Any]]:
        """
        Extract details data with multi-line table support
        """
        details = []
        
        # Find the start of the table
        table_start = -1
        for i, line in enumerate(lines):
            if any(header in line for header in ['Line', 'Item Code', 'Description', 'Delivery Date', 'UOM', 'Qty', 'Unit Price', 'Amount']):
                table_start = i
                break
        
        if table_start == -1:
            return details
        
        # Find the end of the table
        table_end = -1
        for i in range(table_start + 1, len(lines)):
            line = lines[i]
            if any(footer in line for footer in ['Grand Total', 'TERMS AND CONDITIONS', 'Page', 'Purchase Order Description']):
                table_end = i
                break
        
        if table_end == -1:
            table_end = len(lines)
        
        # Extract table data with multi-line support
        table_lines = lines[table_start:table_end]
        details = self._parse_multi_line_table(table_lines)
        
        return details
    
    def _parse_multi_line_table(self, table_lines: List[str]) -> List[Dict[str, Any]]:
        """
        Parse multi-line table structure
        """
        details = []
        
        # Find all line numbers (like 1.1, 2.1, etc.)
        line_numbers = []
        for i, line in enumerate(table_lines):
            if re.match(r'^\d+\.\d+$', line.strip()):
                line_numbers.append((i, line.strip()))
        
        logger.info(f"Found {len(line_numbers)} line numbers")
        
        # Process each line number
        for i, (line_idx, line_num) in enumerate(line_numbers):
            # Get the next line number index
            next_line_idx = line_numbers[i + 1][0] if i + 1 < len(line_numbers) else len(table_lines)
            
            # Extract data for this line
            detail = self._extract_detail_from_lines(table_lines[line_idx:next_line_idx], line_num)
            if detail and self._is_valid_item_detail(detail):
                details.append(detail)
        
        return details
    
    def _extract_detail_from_lines(self, lines: List[str], line_num: str) -> Optional[Dict[str, Any]]:
        """
        Extract detail from a group of lines
        """
        if not lines:
            return None
        
        # Join all lines for analysis
        full_text = ' '.join(lines)
        
        # Extract item code (usually the first meaningful text after line number)
        item_code = self._extract_item_code(lines)
        
        # Extract description (text that's not numbers or codes)
        description = self._extract_description(lines)
        
        # Extract delivery date
        delivery_date = self._extract_delivery_date(lines)
        
        # Extract quantities and prices
        quantities = self._extract_quantities_and_prices(lines)
        
        # Extract UOM
        uom = self._extract_uom(lines)
        
        # Create detail object
        detail = {
            'LineNumber': self._parse_int(line_num.split('.')[0]),
            'ItemCode': item_code,
            'Description': description,
            'DeliveryDate': delivery_date,
            'UOM': uom,
            'Qty': quantities.get('qty'),
            'UnitPrice': quantities.get('unit_price'),
            'Discount': quantities.get('discount'),
            'NetPrice': quantities.get('net_price'),
            'Amount': quantities.get('amount')
        }
        
        return detail
    
    def _extract_item_code(self, lines: List[str]) -> str:
        """
        Extract item code from lines
        """
        for line in lines:
            # Look for patterns that look like item codes
            if re.match(r'^[A-Z0-9\-]+$', line.strip()):
                return line.strip()
            # Look for patterns with letters and numbers
            if re.match(r'^[A-Z0-9\-]+[A-Z0-9\-]+$', line.strip()):
                return line.strip()
        
        return ''
    
    def _extract_description(self, lines: List[str]) -> str:
        """
        Extract description from lines
        """
        descriptions = []
        
        for line in lines:
            # Skip line numbers, item codes, dates, and numbers
            if (re.match(r'^\d+\.\d+$', line.strip()) or
                re.match(r'^[A-Z0-9\-]+$', line.strip()) or
                re.match(r'^\d{1,2}-[A-Z]{3}-\d{4}$', line.strip()) or
                re.match(r'^\d+\.?\d*$', line.strip()) or
                re.match(r'^[A-Z]{2,3}$', line.strip())):
                continue
            
            # This might be description text
            if len(line.strip()) > 3 and not re.match(r'^[0-9,\.\s]+$', line.strip()):
                descriptions.append(line.strip())
        
        return ' '.join(descriptions).strip()
    
    def _extract_delivery_date(self, lines: List[str]) -> Optional[str]:
        """
        Extract delivery date from lines
        """
        for line in lines:
            # Look for date patterns
            date_match = re.search(r'(\d{1,2}-[A-Z]{3}-\d{4})', line)
            if date_match:
                date_str = date_match.group(1)
                try:
                    date_obj = datetime.strptime(date_str, '%d-%b-%Y')
                    return date_obj.strftime('%Y-%m-%d')
                except ValueError:
                    continue
        
        return None
    
    def _extract_quantities_and_prices(self, lines: List[str]) -> Dict[str, Optional[float]]:
        """
        Extract quantities and prices from lines
        """
        result = {
            'qty': None,
            'unit_price': None,
            'discount': None,
            'net_price': None,
            'amount': None
        }
        
        # Look for numeric patterns
        numbers = []
        for line in lines:
            # Find all numbers in the line
            matches = re.findall(r'(\d+\.?\d*)', line)
            for match in matches:
                try:
                    numbers.append(float(match))
                except ValueError:
                    continue
        
        # Assign numbers based on position and context
        if len(numbers) >= 1:
            result['qty'] = int(numbers[0]) if numbers[0] > 0 else None
        
        if len(numbers) >= 2:
            result['unit_price'] = numbers[1]
        
        if len(numbers) >= 3:
            result['discount'] = numbers[2]
        
        if len(numbers) >= 4:
            result['net_price'] = numbers[3]
        
        if len(numbers) >= 5:
            result['amount'] = numbers[4]
        
        return result
    
    def _extract_uom(self, lines: List[str]) -> str:
        """
        Extract UOM from lines
        """
        for line in lines:
            # Look for common UOM patterns
            if re.match(r'^[A-Z]{2,3}$', line.strip()):
                return line.strip()
        
        return ''
    
    def _find_po_number(self, full_text: str, lines: List[str]) -> Optional[str]:
        """
        Find PO number in the text
        """
        patterns = [
            r'PO\.?\s*Number:\s*([A-Z0-9\-]+)',
            r'Purchase Order:\s*\(([A-Z0-9\-]+)\)',
            r'PO\s*Number:\s*([A-Z0-9\-]+)',
            r'AMAP-PO-(\d+)',
            r'Purchase Order:\s*\(AMAP-PO-(\d+)\)'
        ]
        
        for pattern in patterns:
            match = re.search(pattern, full_text, re.IGNORECASE)
            if match:
                po_num = match.group(1).strip()
                if po_num != '-':
                    return po_num
        
        return None
    
    def _find_supplier_info(self, full_text: str, lines: List[str]) -> Dict[str, str]:
        """
        Find supplier information in the text
        """
        result = {}
        
        # Look for supplier number
        supplier_num_match = re.search(r'Supplier Number:\s*(\d+)', full_text, re.IGNORECASE)
        if supplier_num_match:
            result['SupplierNumber'] = supplier_num_match.group(1).strip()
        
        # Look for supplier name
        supplier_name_patterns = [
            r'Supplier Name:\s*([A-Z\s]+)',
            r'AKAPOLCO INTERNATIONAL LLC',
            r'All Make Auto Parts'
        ]
        
        for pattern in supplier_name_patterns:
            match = re.search(pattern, full_text, re.IGNORECASE)
            if match:
                name = match.group(1).strip() if match.groups() else match.group(0).strip()
                if name and name != 'Supplier Details':
                    result['SupplierName'] = name
                    break
        
        return result
    
    def _find_po_date(self, full_text: str, lines: List[str]) -> Optional[str]:
        """
        Find PO date in the text
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
    
    def _find_currency(self, full_text: str, lines: List[str]) -> Optional[str]:
        """
        Find currency in the text
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
    
    def _find_payment_terms(self, full_text: str, lines: List[str]) -> Optional[str]:
        """
        Find payment terms in the text
        """
        match = re.search(r'Payment Terms:\s*([A-Za-z]+)', full_text, re.IGNORECASE)
        if match:
            return match.group(1).strip()
        
        return None
    
    def _find_shipping_address(self, full_text: str, lines: List[str]) -> Optional[str]:
        """
        Find shipping address in the text
        """
        shipping_match = re.search(r'Shipping Address\s*([^TERMS]+)', full_text, re.IGNORECASE | re.DOTALL)
        if shipping_match:
            address = shipping_match.group(1).strip()
            address = re.sub(r'\s+', ' ', address)
            if address and len(address) > 5:
                return address
        
        return None
    
    def _find_inco_terms(self, full_text: str, lines: List[str]) -> Optional[str]:
        """
        Find IncoTerms in the text
        """
        match = re.search(r'Incoterms:\s*([A-Za-z]+)', full_text, re.IGNORECASE)
        if match:
            return match.group(1).strip()
        
        return None
    
    def _find_po_description(self, full_text: str, lines: List[str]) -> Optional[str]:
        """
        Find PO description in the text
        """
        match = re.search(r'Purchase Order Description:\s*([^TERMS]+)', full_text, re.IGNORECASE | re.DOTALL)
        if match:
            description = match.group(1).strip()
            description = re.sub(r'\s+', ' ', description)
            if description and len(description) > 2:
                return description
        
        return None
    
    def _find_totals(self, full_text: str, lines: List[str]) -> Dict[str, float]:
        """
        Find totals in the text
        """
        result = {}
        
        # Look for subtotal
        subtotal_match = re.search(r'Sub\.?\s*Total\s*Before\s*VAT\s*([\d,]+\.?\d*)', full_text, re.IGNORECASE)
        if subtotal_match:
            result['SubTotal'] = float(subtotal_match.group(1).replace(',', ''))
        
        # Look for VAT
        vat_match = re.search(r'VAT\s*\d+%\s*([\d,]+\.?\d*)', full_text, re.IGNORECASE)
        if vat_match:
            result['VAT'] = float(vat_match.group(1).replace(',', ''))
        
        # Look for grand total
        total_match = re.search(r'Grand\s*Total\s*([\d,]+\.?\d*)', full_text, re.IGNORECASE)
        if total_match:
            result['Total'] = float(total_match.group(1).replace(',', ''))
        
        return result
    
    def _is_valid_item_detail(self, detail: Dict[str, Any]) -> bool:
        """
        Check if a detail object is valid
        """
        # Must have at least item code or description
        if not detail.get('ItemCode') and not detail.get('Description'):
            return False
        
        # Must have some numeric data
        if not any([detail.get('Qty'), detail.get('UnitPrice'), detail.get('Amount')]):
            return False
        
        return True
    
    def _parse_int(self, value: str) -> Optional[int]:
        """
        Parse integer value
        """
        if not value or not value.strip():
            return None
        
        try:
            cleaned = re.sub(r'[^\d\-]', '', value.strip())
            return int(cleaned) if cleaned else None
        except ValueError:
            return None

def main():
    """
    Main function to be called from command line
    """
    if len(sys.argv) != 2:
        print("Usage: python3 FinalPdfParser.py <pdf_path>")
        sys.exit(1)
    
    pdf_path = sys.argv[1]
    
    try:
        parser = FinalPdfParser()
        result = parser.parse_pdf(pdf_path)
        
        # Output result as JSON
        print(json.dumps(result, indent=2, default=str))
        
    except Exception as e:
        logger.error(f"Error: {str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    main()