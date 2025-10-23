#!/usr/bin/env python3
"""
Enhanced PDF Parser with improved table parsing
This parser uses advanced text analysis to better understand the document structure
and extract data from complex tables.
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

class EnhancedPdfParser:
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
        
        # Extract PO Number - look for the actual PO number in the text
        po_number = self._find_po_number_enhanced(full_text, lines)
        if po_number:
            result['PONumber'] = po_number
        
        # Extract Supplier Information
        supplier_info = self._find_supplier_info_enhanced(full_text, lines)
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
        shipping_address = self._find_shipping_address_enhanced(full_text, lines)
        if shipping_address:
            result['Shipping_Address'] = shipping_address
        
        # Extract IncoTerms
        inco_terms = self._find_inco_terms(full_text, lines)
        if inco_terms:
            result['IncoTerms'] = inco_terms
        
        # Extract PO Description
        po_description = self._find_po_description_enhanced(full_text, lines)
        if po_description:
            result['PODescription'] = po_description
        
        # Extract Totals
        totals = self._find_totals_enhanced(full_text, lines)
        result.update(totals)
        
        return result
    
    def _extract_details_data(self, lines: List[str]) -> List[Dict[str, Any]]:
        """
        Extract details data from lines with enhanced table parsing
        """
        details = []
        
        # Find the start and end of the table
        table_start = -1
        table_end = -1
        
        for i, line in enumerate(lines):
            # Look for table header
            if any(header in line for header in ['Line', 'Item Code', 'Description', 'Delivery Date', 'UOM', 'Qty', 'Unit Price', 'Amount']):
                table_start = i
                break
        
        if table_start == -1:
            return details
        
        # Find the end of the table
        for i in range(table_start + 1, len(lines)):
            line = lines[i]
            # Look for table end markers
            if any(footer in line for footer in ['Grand Total', 'TERMS AND CONDITIONS', 'Page', 'Purchase Order Description']):
                table_end = i
                break
        
        if table_end == -1:
            table_end = len(lines)
        
        # Extract table data with enhanced parsing
        table_lines = lines[table_start:table_end]
        details = self._parse_table_enhanced(table_lines)
        
        return details
    
    def _find_po_number_enhanced(self, full_text: str, lines: List[str]) -> Optional[str]:
        """
        Find PO number with enhanced logic
        """
        # Look for PO number patterns
        patterns = [
            r'PO\.?\s*Number:\s*([A-Z0-9\-]+)',
            r'Purchase Order:\s*\(([A-Z0-9\-]+)\)',
            r'PO\s*Number:\s*([A-Z0-9\-]+)',
            r'AMAP-PO-(\d+)',  # Specific pattern for this PDF
            r'Purchase Order:\s*\(AMAP-PO-(\d+)\)'
        ]
        
        for pattern in patterns:
            match = re.search(pattern, full_text, re.IGNORECASE)
            if match:
                po_num = match.group(1).strip()
                if po_num != '-':  # Skip placeholder values
                    return po_num
        
        return None
    
    def _find_supplier_info_enhanced(self, full_text: str, lines: List[str]) -> Dict[str, str]:
        """
        Find supplier information with enhanced logic
        """
        result = {}
        
        # Look for supplier number
        supplier_num_match = re.search(r'Supplier Number:\s*(\d+)', full_text, re.IGNORECASE)
        if supplier_num_match:
            result['SupplierNumber'] = supplier_num_match.group(1).strip()
        
        # Look for supplier name - find the actual company name
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
        # Look for date patterns
        patterns = [
            r'Date:\s*(\d{1,2}-[A-Z]{3}-\d{4})',
            r'(\d{1,2}-[A-Z]{3}-\d{4})'
        ]
        
        for pattern in patterns:
            match = re.search(pattern, full_text, re.IGNORECASE)
            if match:
                date_str = match.group(1).strip()
                try:
                    # Convert to standard format
                    date_obj = datetime.strptime(date_str, '%d-%b-%Y')
                    return date_obj.strftime('%Y-%m-%d')
                except ValueError:
                    continue
        
        return None
    
    def _find_currency(self, full_text: str, lines: List[str]) -> Optional[str]:
        """
        Find currency in the text
        """
        # Look for currency patterns
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
    
    def _find_shipping_address_enhanced(self, full_text: str, lines: List[str]) -> Optional[str]:
        """
        Find shipping address with enhanced logic
        """
        # Look for shipping address section
        shipping_match = re.search(r'Shipping Address\s*([^TERMS]+)', full_text, re.IGNORECASE | re.DOTALL)
        if shipping_match:
            address = shipping_match.group(1).strip()
            # Clean up the address
            address = re.sub(r'\s+', ' ', address)
            if address and len(address) > 5:  # Ensure it's a meaningful address
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
    
    def _find_po_description_enhanced(self, full_text: str, lines: List[str]) -> Optional[str]:
        """
        Find PO description with enhanced logic
        """
        match = re.search(r'Purchase Order Description:\s*([^TERMS]+)', full_text, re.IGNORECASE | re.DOTALL)
        if match:
            description = match.group(1).strip()
            # Clean up the description
            description = re.sub(r'\s+', ' ', description)
            if description and len(description) > 2:  # Ensure it's a meaningful description
                return description
        
        return None
    
    def _find_totals_enhanced(self, full_text: str, lines: List[str]) -> Dict[str, float]:
        """
        Find totals with enhanced logic
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
    
    def _parse_table_enhanced(self, table_lines: List[str]) -> List[Dict[str, Any]]:
        """
        Parse table with enhanced logic
        """
        details = []
        
        # Find the header row
        header_row = -1
        for i, line in enumerate(table_lines):
            if any(header in line for header in ['Line', 'Item Code', 'Description', 'Delivery Date', 'UOM', 'Qty', 'Unit Price', 'Amount']):
                header_row = i
                break
        
        if header_row == -1:
            return details
        
        # Process data rows
        for i in range(header_row + 1, len(table_lines)):
            line = table_lines[i]
            
            # Skip empty lines
            if not line.strip():
                continue
            
            # Skip footer lines
            if any(footer in line for footer in ['Grand Total', 'TERMS AND CONDITIONS', 'Page', 'Purchase Order Description']):
                break
            
            # Parse the line
            detail = self._parse_table_line_enhanced(line)
            if detail and self._is_valid_item_detail(detail):
                details.append(detail)
        
        return details
    
    def _parse_table_line_enhanced(self, line: str) -> Optional[Dict[str, Any]]:
        """
        Parse a table line with enhanced logic
        """
        if not line or not line.strip():
            return None
        
        # Skip header lines
        if any(header in line for header in ['Line', 'Item Code', 'Description', 'Delivery Date', 'UOM', 'Qty', 'Unit Price', 'Amount']):
            return None
        
        # Skip footer lines
        if any(footer in line for footer in ['Grand Total', 'TERMS AND CONDITIONS', 'Page', 'Purchase Order Description']):
            return None
        
        # Enhanced column splitting
        columns = self._split_table_line_enhanced(line)
        
        if len(columns) < 6:  # Need at least 6 columns for valid data
            return None
        
        try:
            detail = {
                'LineNumber': self._parse_int(columns[0]) if len(columns) > 0 else None,
                'ItemCode': columns[1] if len(columns) > 1 else '',
                'Description': columns[2] if len(columns) > 2 else '',
                'DeliveryDate': self._parse_date(columns[3]) if len(columns) > 3 else None,
                'UOM': columns[4] if len(columns) > 4 else '',
                'Qty': self._parse_int(columns[5]) if len(columns) > 5 else None,
                'UnitPrice': self._parse_decimal(columns[6]) if len(columns) > 6 else None,
                'Discount': self._parse_decimal(columns[7]) if len(columns) > 7 else None,
                'NetPrice': self._parse_decimal(columns[8]) if len(columns) > 8 else None,
                'Amount': self._parse_decimal(columns[9]) if len(columns) > 9 else None
            }
            
            return detail
            
        except Exception as e:
            logger.warning(f"Failed to parse table line: {str(e)}")
            return None
    
    def _split_table_line_enhanced(self, line: str) -> List[str]:
        """
        Split table line with enhanced logic
        """
        # First, try to split by multiple spaces
        columns = re.split(r'\s{2,}', line)
        
        # If we don't have enough columns, try a different approach
        if len(columns) < 6:
            # Look for patterns that suggest column boundaries
            # This is a simplified approach - in practice, you'd use more sophisticated methods
            
            # Try splitting by single spaces and then group related items
            words = line.split()
            columns = []
            current_column = []
            
            for word in words:
                # If the word looks like a number (quantity, price, amount), it might be a new column
                if re.match(r'^\d+\.?\d*$', word) or re.match(r'^\d+,\d+\.?\d*$', word):
                    if current_column:
                        columns.append(' '.join(current_column))
                        current_column = []
                    columns.append(word)
                else:
                    current_column.append(word)
            
            if current_column:
                columns.append(' '.join(current_column))
        
        # Clean up columns
        columns = [col.strip() for col in columns if col.strip()]
        
        return columns
    
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
        print("Usage: python3 EnhancedPdfParser.py <pdf_path>")
        sys.exit(1)
    
    pdf_path = sys.argv[1]
    
    try:
        parser = EnhancedPdfParser()
        result = parser.parse_pdf(pdf_path)
        
        # Output result as JSON
        print(json.dumps(result, indent=2, default=str))
        
    except Exception as e:
        logger.error(f"Error: {str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    main()