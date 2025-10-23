#!/usr/bin/env python3
"""
Improved PDF Parser using PDFPig with Visual Positioning
This parser combines the reliability of PDFPig text extraction with
visual positioning information to better understand the document structure.
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

class ImprovedPdfParser:
    def __init__(self):
        self.tesseract_config = '--oem 3 --psm 6'
        
    def parse_pdf(self, pdf_path: str) -> Dict[str, Any]:
        """
        Main method to parse PDF and extract structured data
        """
        try:
            # Use PDFPig for reliable text extraction with positioning
            from UglyToad.PdfPig import PdfDocument
            from UglyToad.PdfPig.Content import Page
            
            all_data = {
                'master': {},
                'details': []
            }
            
            with PdfDocument.open(pdf_path) as document:
                pages = list(document.get_pages())
                logger.info(f"Processing {len(pages)} pages")
                
                for page_num, page in enumerate(pages):
                    logger.info(f"Processing page {page_num + 1}")
                    page_data = self._process_page_with_positioning(page, page_num)
                    
                    if page_num == 0:  # First page contains master data
                        all_data['master'].update(page_data.get('master', {}))
                    
                    # All pages may contain detail data
                    if 'details' in page_data:
                        all_data['details'].extend(page_data['details'])
            
            # Clean and validate data
            all_data = self._clean_and_validate_data(all_data)
            
            return all_data
            
        except Exception as e:
            logger.error(f"Error parsing PDF: {str(e)}")
            raise
    
    def _process_page_with_positioning(self, page, page_num: int) -> Dict[str, Any]:
        """
        Process a single page using PDFPig with positioning information
        """
        # Get words with positioning
        words = list(page.get_words())
        
        # Get text content
        text = page.text
        
        # Extract header information (only from first page)
        master_data = {}
        if page_num == 0:
            master_data = self._extract_header_with_positioning(words, text)
        
        # Extract item details from table
        details = self._extract_item_details_with_positioning(words, text, page_num)
        
        return {
            'master': master_data,
            'details': details
        }
    
    def _extract_header_with_positioning(self, words: List, text: str) -> Dict[str, Any]:
        """
        Extract header information using word positioning
        """
        result = {}
        
        # Group words by Y position to find lines
        lines = self._group_words_by_y_position(words)
        
        # Extract PO Number
        po_number = self._find_po_number(lines)
        if po_number:
            result['PONumber'] = po_number
        
        # Extract Supplier Information
        supplier_info = self._find_supplier_info(lines)
        result.update(supplier_info)
        
        # Extract Date
        po_date = self._find_po_date(lines)
        if po_date:
            result['PODate'] = po_date
        
        # Extract Currency
        currency = self._find_currency(lines)
        if currency:
            result['Currency'] = currency
        
        # Extract Payment Terms
        payment_terms = self._find_payment_terms(lines)
        if payment_terms:
            result['PaymentTerms'] = payment_terms
        
        # Extract Shipping Address
        shipping_address = self._find_shipping_address(lines)
        if shipping_address:
            result['Shipping_Address'] = shipping_address
        
        # Extract IncoTerms
        inco_terms = self._find_inco_terms(lines)
        if inco_terms:
            result['IncoTerms'] = inco_terms
        
        # Extract PO Description
        po_description = self._find_po_description(lines)
        if po_description:
            result['PODescription'] = po_description
        
        # Extract Totals
        totals = self._find_totals(lines)
        result.update(totals)
        
        return result
    
    def _extract_item_details_with_positioning(self, words: List, text: str, page_num: int) -> List[Dict[str, Any]]:
        """
        Extract item details using word positioning
        """
        details = []
        
        # Group words by Y position to find lines
        lines = self._group_words_by_y_position(words)
        
        # Find the table data region
        table_lines = self._find_table_data_lines(lines)
        
        # Parse each table line
        for line in table_lines:
            detail = self._parse_table_line(line)
            if detail and self._is_valid_item_detail(detail):
                details.append(detail)
        
        return details
    
    def _group_words_by_y_position(self, words: List) -> List[List]:
        """
        Group words by their Y position to form lines
        """
        if not words:
            return []
        
        # Sort words by Y position (top to bottom)
        sorted_words = sorted(words, key=lambda w: -w.bounding_box.top)
        
        lines = []
        current_line = []
        current_y = None
        tolerance = 5.0  # Y position tolerance for grouping words into lines
        
        for word in sorted_words:
            word_y = word.bounding_box.top
            
            if current_y is None or abs(word_y - current_y) <= tolerance:
                current_line.append(word)
                current_y = word_y
            else:
                if current_line:
                    # Sort current line by X position (left to right)
                    current_line.sort(key=lambda w: w.bounding_box.left)
                    lines.append(current_line)
                current_line = [word]
                current_y = word_y
        
        # Add the last line
        if current_line:
            current_line.sort(key=lambda w: w.bounding_box.left)
            lines.append(current_line)
        
        return lines
    
    def _find_po_number(self, lines: List[List]) -> Optional[str]:
        """
        Find PO number in the lines
        """
        for line in lines:
            line_text = ' '.join([word.text for word in line])
            
            # Look for PO number patterns
            patterns = [
                r'PO\.?\s*Number:\s*([A-Z0-9\-]+)',
                r'Purchase Order:\s*\(([A-Z0-9\-]+)\)',
                r'PO\s*Number:\s*([A-Z0-9\-]+)'
            ]
            
            for pattern in patterns:
                match = re.search(pattern, line_text, re.IGNORECASE)
                if match:
                    return match.group(1).strip()
        
        return None
    
    def _find_supplier_info(self, lines: List[List]) -> Dict[str, str]:
        """
        Find supplier information in the lines
        """
        result = {}
        
        for line in lines:
            line_text = ' '.join([word.text for word in line])
            
            # Look for supplier number
            if 'Supplier Number:' in line_text:
                match = re.search(r'Supplier Number:\s*(\d+)', line_text, re.IGNORECASE)
                if match:
                    result['SupplierNumber'] = match.group(1).strip()
            
            # Look for supplier name
            if 'Supplier Name:' in line_text:
                match = re.search(r'Supplier Name:\s*([A-Z\s]+)', line_text, re.IGNORECASE)
                if match:
                    result['SupplierName'] = match.group(1).strip()
        
        return result
    
    def _find_po_date(self, lines: List[List]) -> Optional[str]:
        """
        Find PO date in the lines
        """
        for line in lines:
            line_text = ' '.join([word.text for word in line])
            
            # Look for date patterns
            patterns = [
                r'Date:\s*(\d{1,2}-[A-Z]{3}-\d{4})',
                r'(\d{1,2}-[A-Z]{3}-\d{4})'
            ]
            
            for pattern in patterns:
                match = re.search(pattern, line_text, re.IGNORECASE)
                if match:
                    date_str = match.group(1).strip()
                    try:
                        # Convert to standard format
                        date_obj = datetime.strptime(date_str, '%d-%b-%Y')
                        return date_obj.strftime('%Y-%m-%d')
                    except ValueError:
                        continue
        
        return None
    
    def _find_currency(self, lines: List[List]) -> Optional[str]:
        """
        Find currency in the lines
        """
        for line in lines:
            line_text = ' '.join([word.text for word in line])
            
            # Look for currency patterns
            patterns = [
                r'Currency:\s*[^-]+-\s*([A-Z]{3})',
                r'UAE Dirham - ([A-Z]{3})'
            ]
            
            for pattern in patterns:
                match = re.search(pattern, line_text, re.IGNORECASE)
                if match:
                    return match.group(1).strip()
        
        return None
    
    def _find_payment_terms(self, lines: List[List]) -> Optional[str]:
        """
        Find payment terms in the lines
        """
        for line in lines:
            line_text = ' '.join([word.text for word in line])
            
            if 'Payment Terms:' in line_text:
                match = re.search(r'Payment Terms:\s*([A-Za-z]+)', line_text, re.IGNORECASE)
                if match:
                    return match.group(1).strip()
        
        return None
    
    def _find_shipping_address(self, lines: List[List]) -> Optional[str]:
        """
        Find shipping address in the lines
        """
        for line in lines:
            line_text = ' '.join([word.text for word in line])
            
            if 'Shipping Address' in line_text:
                # Look for address in the same line or next lines
                address_parts = []
                for word in line:
                    if word.text.lower() not in ['shipping', 'address', ':', 'shipping address']:
                        address_parts.append(word.text)
                
                if address_parts:
                    return ' '.join(address_parts).strip()
        
        return None
    
    def _find_inco_terms(self, lines: List[List]) -> Optional[str]:
        """
        Find IncoTerms in the lines
        """
        for line in lines:
            line_text = ' '.join([word.text for word in line])
            
            if 'Incoterms:' in line_text:
                match = re.search(r'Incoterms:\s*([A-Za-z]+)', line_text, re.IGNORECASE)
                if match:
                    return match.group(1).strip()
        
        return None
    
    def _find_po_description(self, lines: List[List]) -> Optional[str]:
        """
        Find PO description in the lines
        """
        for line in lines:
            line_text = ' '.join([word.text for word in line])
            
            if 'Purchase Order Description:' in line_text:
                # Extract description after the colon
                match = re.search(r'Purchase Order Description:\s*(.+)', line_text, re.IGNORECASE)
                if match:
                    return match.group(1).strip()
        
        return None
    
    def _find_totals(self, lines: List[List]) -> Dict[str, float]:
        """
        Find totals in the lines
        """
        result = {}
        
        for line in lines:
            line_text = ' '.join([word.text for word in line])
            
            # Look for subtotal
            if 'Sub. Total Before VAT' in line_text or 'Sub Total Before VAT' in line_text:
                match = re.search(r'Sub\.?\s*Total\s*Before\s*VAT\s*([\d,]+\.?\d*)', line_text, re.IGNORECASE)
                if match:
                    result['SubTotal'] = float(match.group(1).replace(',', ''))
            
            # Look for VAT
            if 'VAT' in line_text and '%' in line_text:
                match = re.search(r'VAT\s*\d+%\s*([\d,]+\.?\d*)', line_text, re.IGNORECASE)
                if match:
                    result['VAT'] = float(match.group(1).replace(',', ''))
            
            # Look for grand total
            if 'Grand Total' in line_text:
                match = re.search(r'Grand\s*Total\s*([\d,]+\.?\d*)', line_text, re.IGNORECASE)
                if match:
                    result['Total'] = float(match.group(1).replace(',', ''))
        
        return result
    
    def _find_table_data_lines(self, lines: List[List]) -> List[List]:
        """
        Find lines that contain table data
        """
        table_lines = []
        in_table = False
        
        for line in lines:
            line_text = ' '.join([word.text for word in line])
            
            # Check if this line starts the table
            if any(header in line_text for header in ['Line', 'Item Code', 'Description', 'Delivery Date', 'UOM', 'Qty', 'Unit Price', 'Amount']):
                in_table = True
                continue
            
            # Check if this line ends the table
            if any(footer in line_text for footer in ['Grand Total', 'TERMS AND CONDITIONS', 'Page', 'Purchase Order Description']):
                in_table = False
                continue
            
            # If we're in the table and this line looks like data
            if in_table and self._looks_like_table_data(line_text):
                table_lines.append(line)
        
        return table_lines
    
    def _looks_like_table_data(self, line_text: str) -> bool:
        """
        Check if a line looks like table data
        """
        # Skip empty lines
        if not line_text.strip():
            return False
        
        # Skip lines that are clearly headers or footers
        if any(header in line_text for header in ['Line', 'Item Code', 'Description', 'Delivery Date', 'UOM', 'Qty', 'Unit Price', 'Amount']):
            return False
        
        if any(footer in line_text for footer in ['Grand Total', 'TERMS AND CONDITIONS', 'Page', 'Purchase Order Description']):
            return False
        
        # Look for patterns that suggest this is data
        # Should have numbers (quantities, prices, amounts)
        has_numbers = bool(re.search(r'\d+', line_text))
        
        # Should not be just text
        has_meaningful_content = len(line_text.strip()) > 3
        
        return has_numbers and has_meaningful_content
    
    def _parse_table_line(self, line: List) -> Optional[Dict[str, Any]]:
        """
        Parse a table line into a detail object
        """
        if not line:
            return None
        
        # Get the text of the line
        line_text = ' '.join([word.text for word in line])
        
        # Split by multiple spaces to get columns
        columns = re.split(r'\s{2,}', line_text)
        
        # Clean up columns
        columns = [col.strip() for col in columns if col.strip()]
        
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
    
    def _clean_and_validate_data(self, data: Dict[str, Any]) -> Dict[str, Any]:
        """
        Clean and validate extracted data
        """
        # Clean master data
        if 'master' in data:
            master = data['master']
            # Ensure required fields have default values
            master.setdefault('PONumber', '')
            master.setdefault('SupplierName', '')
            master.setdefault('SupplierNumber', '')
            master.setdefault('Currency', '')
            master.setdefault('PODescription', '')
        
        # Clean details data
        if 'details' in data:
            details = data['details']
            # Remove invalid details
            details = [d for d in details if d and self._is_valid_item_detail(d)]
            
            # Assign line numbers
            for i, detail in enumerate(details, 1):
                detail['LineNumber'] = i
            
            data['details'] = details
        
        return data

def main():
    """
    Main function to be called from command line
    """
    if len(sys.argv) != 2:
        print("Usage: python3 ImprovedPdfParser.py <pdf_path>")
        sys.exit(1)
    
    pdf_path = sys.argv[1]
    
    try:
        parser = ImprovedPdfParser()
        result = parser.parse_pdf(pdf_path)
        
        # Output result as JSON
        print(json.dumps(result, indent=2, default=str))
        
    except Exception as e:
        logger.error(f"Error: {str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    main()