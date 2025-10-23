#!/usr/bin/env python3
"""
Advanced PDF Parser using OCR and Visual Positioning
This parser uses computer vision techniques to extract data from PDFs
by understanding the visual layout rather than relying on text extraction.
"""

import sys
import json
import cv2
import numpy as np
import pandas as pd
from pdf2image import convert_from_path
import pytesseract
from PIL import Image
import re
from datetime import datetime
from typing import List, Dict, Any, Optional, Tuple
import logging

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class AdvancedPdfParser:
    def __init__(self):
        self.tesseract_config = '--oem 3 --psm 6'
        
    def parse_pdf(self, pdf_path: str) -> Dict[str, Any]:
        """
        Main method to parse PDF and extract structured data
        """
        try:
            # Convert PDF to images
            images = convert_from_path(pdf_path, dpi=300)
            logger.info(f"Converted PDF to {len(images)} images")
            
            # Process each page
            all_data = {
                'master': {},
                'details': []
            }
            
            for page_num, image in enumerate(images):
                logger.info(f"Processing page {page_num + 1}")
                page_data = self._process_page(image, page_num)
                
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
    
    def _process_page(self, image: Image.Image, page_num: int) -> Dict[str, Any]:
        """
        Process a single page of the PDF
        """
        # Convert PIL image to OpenCV format
        cv_image = cv2.cvtColor(np.array(image), cv2.COLOR_RGB2BGR)
        
        # Detect table structure
        table_data = self._detect_table_structure(cv_image)
        
        # Extract header information (only from first page)
        master_data = {}
        if page_num == 0:
            master_data = self._extract_header_data(cv_image)
        
        # Extract item details from table
        details = self._extract_item_details(cv_image, table_data)
        
        return {
            'master': master_data,
            'details': details
        }
    
    def _detect_table_structure(self, image: np.ndarray) -> Dict[str, Any]:
        """
        Detect table structure using computer vision
        """
        # Convert to grayscale
        gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
        
        # Apply threshold to get binary image
        _, binary = cv2.threshold(gray, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)
        
        # Detect horizontal lines
        horizontal_kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (40, 1))
        horizontal_lines = cv2.morphologyEx(binary, cv2.MORPH_OPEN, horizontal_kernel)
        
        # Detect vertical lines
        vertical_kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (1, 40))
        vertical_lines = cv2.morphologyEx(binary, cv2.MORPH_OPEN, vertical_kernel)
        
        # Combine lines
        table_mask = cv2.addWeighted(horizontal_lines, 0.5, vertical_lines, 0.5, 0.0)
        
        # Find contours
        contours, _ = cv2.findContours(table_mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        
        # Filter contours to find table cells
        table_cells = []
        for contour in contours:
            area = cv2.contourArea(contour)
            if area > 1000:  # Filter small contours
                x, y, w, h = cv2.boundingRect(contour)
                table_cells.append({
                    'x': x, 'y': y, 'width': w, 'height': h,
                    'area': area
                })
        
        # Sort cells by position
        table_cells.sort(key=lambda cell: (cell['y'], cell['x']))
        
        return {
            'cells': table_cells,
            'table_mask': table_mask
        }
    
    def _extract_header_data(self, image: np.ndarray) -> Dict[str, Any]:
        """
        Extract header information using OCR with position awareness
        """
        # Get image dimensions
        height, width = image.shape[:2]
        
        # Define regions of interest for different header fields
        regions = {
            'po_number': (0, 0, width//2, height//4),
            'supplier_info': (0, height//4, width//2, height//2),
            'dates': (width//2, 0, width, height//4),
            'currency': (width//2, height//4, width, height//2),
            'totals': (0, height//2, width, height)
        }
        
        header_data = {}
        
        # Extract text from each region
        for field, (x1, y1, x2, y2) in regions.items():
            roi = image[y1:y2, x1:x2]
            text = self._extract_text_from_region(roi)
            
            if field == 'po_number':
                header_data.update(self._parse_po_number(text))
            elif field == 'supplier_info':
                header_data.update(self._parse_supplier_info(text))
            elif field == 'dates':
                header_data.update(self._parse_dates(text))
            elif field == 'currency':
                header_data.update(self._parse_currency(text))
            elif field == 'totals':
                header_data.update(self._parse_totals(text))
        
        return header_data
    
    def _extract_item_details(self, image: np.ndarray, table_data: Dict[str, Any]) -> List[Dict[str, Any]]:
        """
        Extract item details from table structure
        """
        details = []
        
        # Find the main data table region
        table_region = self._find_main_table_region(image)
        if table_region is None:
            return details
        
        # Extract text from table region
        table_text = self._extract_text_from_region(table_region)
        
        # Parse table rows
        rows = self._parse_table_rows(table_text, table_region)
        
        # Convert rows to detail objects
        for row in rows:
            if self._is_valid_item_row(row):
                detail = self._create_detail_from_row(row)
                if detail:
                    details.append(detail)
        
        return details
    
    def _find_main_table_region(self, image: np.ndarray) -> Optional[np.ndarray]:
        """
        Find the main data table region in the image
        """
        # Look for patterns that indicate the start of the item table
        gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
        
        # Use template matching to find table headers
        headers = ['Line', 'Item Code', 'Description', 'Delivery Date', 'UOM', 'Qty', 'Unit Price', 'Amount']
        
        for header in headers:
            # This is a simplified approach - in practice, you'd use more sophisticated methods
            # like template matching or OCR to find the exact table start
            pass
        
        # For now, return a region that likely contains the table
        height, width = image.shape[:2]
        return image[height//3:height*3//4, 0:width]
    
    def _extract_text_from_region(self, region: np.ndarray) -> str:
        """
        Extract text from a specific region using OCR
        """
        try:
            # Preprocess the region for better OCR
            processed = self._preprocess_for_ocr(region)
            
            # Extract text using Tesseract
            text = pytesseract.image_to_string(processed, config=self.tesseract_config)
            
            return text.strip()
        except Exception as e:
            logger.warning(f"OCR failed for region: {str(e)}")
            return ""
    
    def _preprocess_for_ocr(self, image: np.ndarray) -> np.ndarray:
        """
        Preprocess image for better OCR results
        """
        # Convert to grayscale
        if len(image.shape) == 3:
            gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
        else:
            gray = image
        
        # Apply denoising
        denoised = cv2.fastNlMeansDenoising(gray)
        
        # Apply threshold
        _, binary = cv2.threshold(denoised, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)
        
        # Morphological operations to clean up
        kernel = np.ones((1, 1), np.uint8)
        cleaned = cv2.morphologyEx(binary, cv2.MORPH_CLOSE, kernel)
        
        return cleaned
    
    def _parse_po_number(self, text: str) -> Dict[str, Any]:
        """
        Parse PO number from text
        """
        result = {}
        
        # Look for PO number patterns
        po_patterns = [
            r'PO\.?\s*Number:\s*([A-Z0-9\-]+)',
            r'Purchase Order:\s*([A-Z0-9\-]+)',
            r'PO\s*Number:\s*([A-Z0-9\-]+)'
        ]
        
        for pattern in po_patterns:
            match = re.search(pattern, text, re.IGNORECASE)
            if match:
                result['PONumber'] = match.group(1).strip()
                break
        
        return result
    
    def _parse_supplier_info(self, text: str) -> Dict[str, Any]:
        """
        Parse supplier information from text
        """
        result = {}
        
        # Parse supplier number
        supplier_num_match = re.search(r'Supplier Number:\s*(\d+)', text, re.IGNORECASE)
        if supplier_num_match:
            result['SupplierNumber'] = supplier_num_match.group(1).strip()
        
        # Parse supplier name
        supplier_name_match = re.search(r'Supplier Name:\s*([A-Z\s]+)', text, re.IGNORECASE)
        if supplier_name_match:
            result['SupplierName'] = supplier_name_match.group(1).strip()
        
        return result
    
    def _parse_dates(self, text: str) -> Dict[str, Any]:
        """
        Parse dates from text
        """
        result = {}
        
        # Parse PO date
        date_patterns = [
            r'Date:\s*(\d{1,2}-[A-Z]{3}-\d{4})',
            r'(\d{1,2}-[A-Z]{3}-\d{4})'
        ]
        
        for pattern in date_patterns:
            match = re.search(pattern, text, re.IGNORECASE)
            if match:
                date_str = match.group(1).strip()
                try:
                    result['PODate'] = datetime.strptime(date_str, '%d-%b-%Y').strftime('%Y-%m-%d')
                    break
                except ValueError:
                    continue
        
        return result
    
    def _parse_currency(self, text: str) -> Dict[str, Any]:
        """
        Parse currency information from text
        """
        result = {}
        
        # Look for currency patterns
        currency_match = re.search(r'Currency:\s*[^-]+-\s*([A-Z]{3})', text, re.IGNORECASE)
        if currency_match:
            result['Currency'] = currency_match.group(1).strip()
        
        return result
    
    def _parse_totals(self, text: str) -> Dict[str, Any]:
        """
        Parse totals from text
        """
        result = {}
        
        # Parse subtotal
        subtotal_match = re.search(r'Sub\.?\s*Total\s*Before\s*VAT\s*([\d,]+\.?\d*)', text, re.IGNORECASE)
        if subtotal_match:
            result['SubTotal'] = float(subtotal_match.group(1).replace(',', ''))
        
        # Parse VAT
        vat_match = re.search(r'VAT\s*\d+%\s*([\d,]+\.?\d*)', text, re.IGNORECASE)
        if vat_match:
            result['VAT'] = float(vat_match.group(1).replace(',', ''))
        
        # Parse grand total
        total_match = re.search(r'Grand\s*Total\s*([\d,]+\.?\d*)', text, re.IGNORECASE)
        if total_match:
            result['Total'] = float(total_match.group(1).replace(',', ''))
        
        return result
    
    def _parse_table_rows(self, text: str, region: np.ndarray) -> List[Dict[str, str]]:
        """
        Parse table rows from text
        """
        rows = []
        lines = text.split('\n')
        
        for line in lines:
            line = line.strip()
            if not line:
                continue
            
            # Split line into columns (this is a simplified approach)
            # In practice, you'd use more sophisticated column detection
            columns = self._split_into_columns(line)
            
            if len(columns) >= 6:  # Minimum expected columns
                rows.append({
                    'line': columns[0] if len(columns) > 0 else '',
                    'item_code': columns[1] if len(columns) > 1 else '',
                    'description': columns[2] if len(columns) > 2 else '',
                    'delivery_date': columns[3] if len(columns) > 3 else '',
                    'uom': columns[4] if len(columns) > 4 else '',
                    'qty': columns[5] if len(columns) > 5 else '',
                    'unit_price': columns[6] if len(columns) > 6 else '',
                    'discount': columns[7] if len(columns) > 7 else '',
                    'net_price': columns[8] if len(columns) > 8 else '',
                    'amount': columns[9] if len(columns) > 9 else ''
                })
        
        return rows
    
    def _split_into_columns(self, line: str) -> List[str]:
        """
        Split a line into columns based on spacing and patterns
        """
        # This is a simplified approach - in practice, you'd use more sophisticated methods
        # like analyzing character positions or using machine learning
        
        # Split by multiple spaces
        columns = re.split(r'\s{2,}', line)
        
        # Clean up columns
        columns = [col.strip() for col in columns if col.strip()]
        
        return columns
    
    def _is_valid_item_row(self, row: Dict[str, str]) -> bool:
        """
        Check if a row represents a valid item
        """
        # Check if it has essential fields
        if not row.get('item_code') and not row.get('description'):
            return False
        
        # Skip header rows
        if any(header in row.get('line', '').lower() for header in ['line', 'item', 'description']):
            return False
        
        # Skip empty rows
        if not any(row.values()):
            return False
        
        return True
    
    def _create_detail_from_row(self, row: Dict[str, str]) -> Optional[Dict[str, Any]]:
        """
        Create a detail object from a parsed row
        """
        try:
            detail = {
                'LineNumber': self._parse_int(row.get('line', '')),
                'ItemCode': row.get('item_code', '').strip(),
                'Description': row.get('description', '').strip(),
                'DeliveryDate': self._parse_date(row.get('delivery_date', '')),
                'UOM': row.get('uom', '').strip(),
                'Qty': self._parse_int(row.get('qty', '')),
                'UnitPrice': self._parse_decimal(row.get('unit_price', '')),
                'NetPrice': self._parse_decimal(row.get('net_price', '')),
                'Amount': self._parse_decimal(row.get('amount', ''))
            }
            
            return detail
            
        except Exception as e:
            logger.warning(f"Failed to create detail from row: {str(e)}")
            return None
    
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
            details = [d for d in details if d and self._is_valid_detail(d)]
            
            # Assign line numbers
            for i, detail in enumerate(details, 1):
                detail['LineNumber'] = i
            
            data['details'] = details
        
        return data
    
    def _is_valid_detail(self, detail: Dict[str, Any]) -> bool:
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

def main():
    """
    Main function to be called from command line
    """
    if len(sys.argv) != 2:
        print("Usage: python3 AdvancedPdfParser.py <pdf_path>")
        sys.exit(1)
    
    pdf_path = sys.argv[1]
    
    try:
        parser = AdvancedPdfParser()
        result = parser.parse_pdf(pdf_path)
        
        # Output result as JSON
        print(json.dumps(result, indent=2, default=str))
        
    except Exception as e:
        logger.error(f"Error: {str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    main()