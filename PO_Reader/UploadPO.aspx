<%@ Page Language="vb" AutoEventWireup="true" CodeBehind="UploadPO.aspx.vb" Inherits="PO_Reader.UploadPO" MasterPageFile="~/Site.Master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        .po-container {
            background: #f8f9fa;
            min-height: 100vh;
            padding: 0;
            margin: 0;
        }
        
        .po-header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 2rem 0;
            margin-bottom: 2rem;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        }
        
        .header-content {
            display: flex;
            justify-content: space-between;
            align-items: center;
            max-width: 1400px;
            margin: 0 auto;
            padding: 0 1rem;
        }
        
        .header-text {
            flex: 1;
            text-align: center;
        }
        
        .po-header h1 {
            font-size: 2.5rem;
            font-weight: 300;
            margin: 0;
        }
        
        .po-header .subtitle {
            font-size: 1.1rem;
            opacity: 0.9;
            margin-top: 0.5rem;
        }
        
        .header-actions {
            display: flex;
            align-items: center;
            gap: 1rem;
        }
        
        .btn-refresh {
            background: rgba(255, 255, 255, 0.2);
            border: 2px solid rgba(255, 255, 255, 0.3);
            color: white;
            width: 50px;
            height: 50px;
            border-radius: 50%;
            font-size: 1.5rem;
            cursor: pointer;
            transition: all 0.3s ease;
            display: flex;
            align-items: center;
            justify-content: center;
            backdrop-filter: blur(10px);
        }
        
        .btn-refresh:hover {
            background: rgba(255, 255, 255, 0.3);
            border-color: rgba(255, 255, 255, 0.5);
            transform: rotate(180deg) scale(1.1);
            box-shadow: 0 4px 15px rgba(0,0,0,0.2);
        }
        
        .btn-refresh:active {
            transform: rotate(180deg) scale(0.95);
        }
        
        .main-content {
            max-width: 1400px;
            margin: 0 auto;
            padding: 0 1rem;
        }
        
        .upload-section {
            background: white;
            border-radius: 12px;
            padding: 2rem;
            margin-bottom: 2rem;
            box-shadow: 0 2px 10px rgba(0,0,0,0.08);
            border: 1px solid #e9ecef;
        }
        
        .upload-controls {
            display: flex;
            gap: 1rem;
            align-items: center;
            flex-wrap: wrap;
            margin-bottom: 1rem;
        }
        
        .file-upload-wrapper {
            position: relative;
            flex: 1;
            min-width: 300px;
        }
        
        .file-upload {
            width: 100%;
            padding: 0.75rem 1rem;
            border: 2px dashed #dee2e6;
            border-radius: 8px;
            background: #f8f9fa;
            font-size: 1rem;
            transition: all 0.3s ease;
        }
        
        .file-upload:hover {
            border-color: #667eea;
            background: #f0f4ff;
        }
        
        .btn {
            padding: 0.75rem 1.5rem;
            border: none;
            border-radius: 8px;
            font-size: 1rem;
            font-weight: 500;
            cursor: pointer;
            transition: all 0.3s ease;
            display: inline-flex;
            align-items: center;
            gap: 0.5rem;
        }
        
        .btn-primary {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
        }
        
        .btn-primary:hover:not(:disabled) {
            transform: translateY(-2px);
            box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
        }
        
        .btn-success {
            background: linear-gradient(135deg, #28a745 0%, #20c997 100%);
            color: white;
        }
        
        .btn-success:hover:not(:disabled) {
            transform: translateY(-2px);
            box-shadow: 0 4px 12px rgba(40, 167, 69, 0.4);
        }
        
        .btn-warning {
            background: linear-gradient(135deg, #ffc107 0%, #fd7e14 100%);
            color: white;
        }
        
        .btn-warning:hover:not(:disabled) {
            transform: translateY(-2px);
            box-shadow: 0 4px 12px rgba(255, 193, 7, 0.4);
        }
        
        .btn:disabled {
            opacity: 0.6;
            cursor: not-allowed;
            transform: none !important;
        }
        
        .workflow-note {
            background: #e3f2fd;
            border-left: 4px solid #2196f3;
            padding: 1rem;
            border-radius: 0 8px 8px 0;
            margin-top: 1rem;
            font-size: 0.95rem;
            color: #1565c0;
        }
        
        .data-section {
            background: white;
            border-radius: 12px;
            padding: 2rem;
            margin-bottom: 2rem;
            box-shadow: 0 2px 10px rgba(0,0,0,0.08);
            border: 1px solid #e9ecef;
        }
        
        .section-title {
            font-size: 1.5rem;
            font-weight: 600;
            color: #2c3e50;
            margin-bottom: 1.5rem;
            padding-bottom: 0.5rem;
            border-bottom: 2px solid #e9ecef;
            display: flex;
            align-items: center;
            gap: 0.75rem;
        }
        
        .section-title::before {
            content: "";
            font-size: 1.2rem;
        }
        
        .details-view {
            border: none;
            width: 100%;
        }
        
        .details-view tr {
            border-bottom: 1px solid #e9ecef;
        }
        
        .details-view tr:nth-child(even) {
            background: #f8f9fa;
        }
        
        .details-view th {
            background: #f8f9fa;
            color: #495057;
            font-weight: 600;
            padding: 1rem;
            text-align: left;
            width: 200px;
            border: none;
            font-size: 0.9rem;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }
        
        .details-view td {
            padding: 1rem;
            border: none;
        }
        
        .details-view input[type="text"], 
        .details-view textarea {
            width: 100%;
            padding: 0.75rem;
            border: 1px solid #ced4da;
            border-radius: 6px;
            font-size: 1rem;
            transition: border-color 0.3s ease;
        }
        
        .details-view input[type="text"]:focus, 
        .details-view textarea:focus {
            outline: none;
            border-color: #667eea;
            box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
        }
        
        .line-items-container {
            background: white;
            border-radius: 12px;
            padding: 2rem;
            margin-bottom: 2rem;
            box-shadow: 0 2px 10px rgba(0,0,0,0.08);
            border: 1px solid #e9ecef;
        }
        
        .line-items-title {
            font-size: 1.5rem;
            font-weight: 600;
            color: #2c3e50;
            margin-bottom: 1.5rem;
            padding-bottom: 0.5rem;
            border-bottom: 2px solid #e9ecef;
            display: flex;
            align-items: center;
            gap: 0.75rem;
        }
        
        .line-items-title::before {
            content: "";
            font-size: 1.2rem;
        }
        
        .table-container {
            overflow-x: auto;
            border-radius: 8px;
            border: 1px solid #e9ecef;
            max-height: 600px;
            overflow-y: auto;
        }
        
        .line-items-table {
            width: 100%;
            border-collapse: collapse;
            margin: 0;
            font-size: 0.9rem;
        }
        
        .line-items-table th {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 1rem 0.75rem;
            text-align: left;
            font-weight: 600;
            position: sticky;
            top: 0;
            z-index: 10;
            font-size: 0.85rem;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }
        
        .line-items-table td {
            padding: 0.75rem;
            border-bottom: 1px solid #e9ecef;
            vertical-align: top;
        }
        
        .line-items-table tr:nth-child(even) {
            background: #f8f9fa;
        }
        
        .line-items-table tr:hover {
            background: #e3f2fd;
        }
        
        .pagination-container {
            position: fixed;
            bottom: 20px;
            left: 50%;
            transform: translateX(-50%);
            background: white;
            padding: 1rem 2rem;
            border-radius: 50px;
            box-shadow: 0 4px 20px rgba(0,0,0,0.15);
            border: 1px solid #e9ecef;
            z-index: 1000;
            display: none;
        }
        
        .pagination {
            display: flex;
            gap: 0.5rem;
            align-items: center;
            margin: 0;
        }
        
        .pagination a, .pagination span {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            width: 40px;
            height: 40px;
            border-radius: 50%;
            text-decoration: none;
            font-weight: 500;
            transition: all 0.3s ease;
            border: 1px solid #e9ecef;
        }
        
        .pagination a {
            color: #667eea;
            background: white;
        }
        
        .pagination a:hover {
            background: #667eea;
            color: white;
            transform: translateY(-2px);
        }
        
        .pagination .current {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            border-color: #667eea;
        }
        
        .alert {
            padding: 1rem 1.5rem;
            border-radius: 8px;
            margin-top: 1rem;
            font-weight: 500;
            display: none;
        }
        
        .alert-success {
            background: #d4edda;
            color: #155724;
            border: 1px solid #c3e6cb;
        }
        
        .alert-error {
            background: #f8d7da;
            color: #721c24;
            border: 1px solid #f5c6cb;
        }
        
        .stats-bar {
            background: #f8f9fa;
            padding: 1rem;
            border-radius: 8px;
            margin-bottom: 1rem;
            display: flex;
            justify-content: space-between;
            align-items: center;
            font-size: 0.9rem;
            color: #6c757d;
        }
        
        .section-header {
            margin-bottom: 1.5rem;
            text-align: center;
        }
        
        .section-header h3 {
            color: #2c3e50;
            font-size: 1.3rem;
            font-weight: 600;
            margin: 0 0 0.5rem 0;
        }
        
        .section-header p {
            color: #6c757d;
            font-size: 0.95rem;
            margin: 0;
        }
        
        .section-divider {
            text-align: center;
            margin: 2rem 0;
            position: relative;
        }
        
        .section-divider::before {
            content: '';
            position: absolute;
            top: 50%;
            left: 0;
            right: 0;
            height: 1px;
            background: #dee2e6;
        }
        
        .section-divider span {
            background: white;
            padding: 0 1rem;
            color: #6c757d;
            font-weight: 500;
            font-size: 0.9rem;
        }
        
        .hidden-section {
            display: none !important;
        }
        
        .disabled-section {
            opacity: 0.6;
            pointer-events: none;
        }
        
        .fetch-controls {
            display: flex;
            gap: 1rem;
            align-items: center;
            flex-wrap: wrap;
            margin-bottom: 1rem;
        }
        
        .dropdown-wrapper {
            flex: 1;
            min-width: 300px;
        }
        
        .po-dropdown {
            width: 100%;
            padding: 0.75rem 1rem;
            border: 2px solid #dee2e6;
            border-radius: 8px;
            background: white;
            font-size: 1rem;
            transition: all 0.3s ease;
            appearance: none;
            background-image: url("data:image/svg+xml;charset=UTF-8,%3csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3e%3cpolyline points='6,9 12,15 18,9'%3e%3c/polyline%3e%3c/svg%3e");
            background-repeat: no-repeat;
            background-position: right 0.75rem center;
            background-size: 1rem;
            padding-right: 2.5rem;
        }
        
        .po-dropdown:hover {
            border-color: #667eea;
        }
        
        .po-dropdown:focus {
            outline: none;
            border-color: #667eea;
            box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
        }
        
        @media (max-width: 768px) {
            .upload-controls, .fetch-controls {
                flex-direction: column;
                align-items: stretch;
            }
            
            .file-upload-wrapper, .dropdown-wrapper {
                min-width: auto;
            }
            
            .main-content {
                padding: 0 0.5rem;
            }
            
            .header-content {
                flex-direction: column;
                gap: 1rem;
                text-align: center;
            }
            
            .header-actions {
                order: -1;
                justify-content: center;
            }
            
            .po-header h1 {
                font-size: 2rem;
            }
            
            .btn-refresh {
                width: 45px;
                height: 45px;
                font-size: 1.3rem;
            }
            
            .pagination-container {
                left: 10px;
                right: 10px;
                transform: none;
                border-radius: 8px;
            }
        }
    </style>

    <div class="po-container">
        <div class="po-header">
            <div class="header-content">
                <div class="header-text">
                    <h1>Purchase Order Management System</h1>
                    <div class="subtitle">Upload, Preview & Process Purchase Orders</div>
                </div>
                <div class="header-actions">
                    <asp:Button ID="btnRefresh" runat="server" CssClass="btn-refresh" OnClick="btnRefresh_Click" 
                        ToolTip="Refresh/Reset All" Text="🔄" />
                </div>
            </div>
        </div>
        
        <div class="main-content">
            <div class="upload-section">
                <!-- New PO Upload Section -->
                <asp:Panel ID="pnlUploadSection" runat="server">
                    <div class="section-header">
                        <h3>Upload New Purchase Order</h3>
                        <p>Upload a PDF file to parse and create a new purchase order</p>
                    </div>
                    <div class="upload-controls">
                        <div class="file-upload-wrapper">
                            <asp:FileUpload ID="fuPdf" runat="server" CssClass="file-upload" accept="application/pdf" />
                        </div>
                        <asp:Button ID="btnUpload" runat="server" CssClass="btn btn-primary" Text="Upload & Preview" OnClick="btnUpload_Click" />
                        <asp:Button ID="btnSave" runat="server" CssClass="btn btn-success" Text="Save to Database" OnClick="btnSave_Click" Enabled="false" />
                    </div>
                </asp:Panel>
                
                <!-- Divider -->
                <asp:Panel ID="pnlDivider" runat="server">
                    <div class="section-divider">
                        <span>OR</span>
                    </div>
                </asp:Panel>
                
                <!-- Fetch Existing PO Section -->
                <asp:Panel ID="pnlFetchSection" runat="server">
                    <div class="section-header">
                        <h3>Edit Existing Purchase Order</h3>
                        <p>Select an existing PO from the database to view and edit its details</p>
                    </div>
                    <div class="fetch-controls">
                        <div class="dropdown-wrapper">
                            <asp:DropDownList ID="ddlExistingPOs" runat="server" CssClass="po-dropdown">
                                <asp:ListItem Text="-- Select a Purchase Order --" Value="0" />
                            </asp:DropDownList>
                        </div>
                        <asp:Button ID="btnFetchPO" runat="server" CssClass="btn btn-primary" Text="Fetch & Edit" OnClick="btnFetchPO_Click" />
                        <asp:Button ID="btnUpdatePO" runat="server" CssClass="btn btn-success" Text="Update Changes" OnClick="btnUpdatePO_Click" Enabled="false" />
                    </div>
                </asp:Panel>
                
                <div class="workflow-note">
                    <strong>Workflow:</strong> Either upload a new PDF to create a PO, or select an existing PO to edit its details.
                </div>
                <asp:Label ID="lblInfo" runat="server" CssClass="alert alert-success"></asp:Label>
                <asp:Label ID="lblError" runat="server" CssClass="alert alert-error"></asp:Label>
            </div>


            <div class="data-section">
                <div class="section-title">Purchase Order Master Information</div>
                <asp:DetailsView ID="dvMaster" runat="server" AutoGenerateRows="false" CssClass="details-view" DefaultMode="Edit">
                    <Fields>
                        <asp:TemplateField HeaderText="PO Number">
                            <EditItemTemplate>
                                <asp:TextBox ID="txtPONumber" runat="server" Text='<%# Bind("PONumber") %>' placeholder="Enter PO Number" />
                            </EditItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Supplier Name">
                            <EditItemTemplate>
                                <asp:TextBox ID="txtSupplierName" runat="server" Text='<%# Bind("SupplierName") %>' placeholder="Enter Supplier Name" />
                            </EditItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Supplier Number">
                            <EditItemTemplate>
                                <asp:TextBox ID="txtSupplierNumber" runat="server" Text='<%# Bind("SupplierNumber") %>' placeholder="Enter Supplier Number" />
                            </EditItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="PO Date">
                            <EditItemTemplate>
                                <asp:TextBox ID="txtPODate" runat="server" Text='<%# Bind("PODate", "{0:yyyy-MM-dd}") %>' placeholder="YYYY-MM-DD" />
                            </EditItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Currency">
                            <EditItemTemplate>
                                <asp:TextBox ID="txtCurrency" runat="server" Text='<%# Bind("Currency") %>' placeholder="e.g., USD, EUR" />
                            </EditItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Sub Total">
                            <EditItemTemplate>
                                <asp:TextBox ID="txtSubTotal" runat="server" Text='<%# Bind("SubTotal", "{0:N2}") %>' placeholder="0.00" />
                            </EditItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="VAT Amount">
                            <EditItemTemplate>
                                <asp:TextBox ID="txtVAT" runat="server" Text='<%# Bind("VAT", "{0:N2}") %>' placeholder="0.00" />
                            </EditItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Total Amount">
                            <EditItemTemplate>
                                <asp:TextBox ID="txtTotal" runat="server" Text='<%# Bind("Total", "{0:N2}") %>' placeholder="0.00" />
                            </EditItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Payment Terms">
                            <EditItemTemplate>
                                <asp:TextBox ID="txtPaymentTerms" runat="server" Text='<%# Bind("PaymentTerms") %>' placeholder="e.g., Net 30 days" />
                            </EditItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Shipping Address">
                            <EditItemTemplate>
                                <asp:TextBox ID="txtShipping_Address" runat="server" Text='<%# Bind("Shipping_Address") %>' placeholder="Enter shipping address" />
                            </EditItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Incoterms">
                            <EditItemTemplate>
                                <asp:TextBox ID="txtIncoTerms" runat="server" Text='<%# Bind("IncoTerms") %>' placeholder="e.g., FOB, CIF" />
                            </EditItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="PO Description">
                            <EditItemTemplate>
                                <asp:TextBox ID="txtPODescription" runat="server" TextMode="MultiLine" Rows="3" Text='<%# Bind("PODescription") %>' placeholder="Enter PO description or notes" />
                            </EditItemTemplate>
                        </asp:TemplateField>
                    </Fields>
                </asp:DetailsView>
            </div>

            <div class="line-items-container">
                <div class="line-items-title">Line Items Details</div>
                <div class="stats-bar" id="statsBar" runat="server" style="display: none;">
                    <span id="totalItems" runat="server">Total Items: 0</span>
                    <span id="currentPage" runat="server">Page 1 of 1</span>
                    <span id="totalAmount" runat="server">Total Amount: $0.00</span>
                </div>
                <div class="table-container">
                    <asp:GridView ID="gvDetails" runat="server" AutoGenerateColumns="false" AllowPaging="true" PageSize="50" OnPageIndexChanging="gvDetails_PageIndexChanging" CssClass="line-items-table">
                        <Columns>
                            <asp:BoundField DataField="LineNumber" HeaderText="Item #" ItemStyle-Width="80px" />
                            <asp:BoundField DataField="ItemCode" HeaderText="Item Code" ItemStyle-Width="120px" />
                            <asp:BoundField DataField="Description" HeaderText="Description" ItemStyle-Width="200px" />
                            <asp:BoundField DataField="DeliveryDate" HeaderText="Delivery Date" DataFormatString="{0:yyyy-MM-dd}" ItemStyle-Width="120px" />
                            <asp:BoundField DataField="UOM" HeaderText="Unit" ItemStyle-Width="80px" />
                            <asp:BoundField DataField="Qty" HeaderText="Quantity" DataFormatString="{0:N0}" ItemStyle-Width="100px" ItemStyle-HorizontalAlign="Right" />
                            <asp:BoundField DataField="UnitPrice" HeaderText="Unit Price" DataFormatString="{0:N2}" ItemStyle-Width="120px" ItemStyle-HorizontalAlign="Right" />
                            <asp:BoundField DataField="NetPrice" HeaderText="Net Price" DataFormatString="{0:N2}" ItemStyle-Width="120px" ItemStyle-HorizontalAlign="Right" />
                            <asp:BoundField DataField="Amount" HeaderText="Amount" DataFormatString="{0:N2}" ItemStyle-Width="120px" ItemStyle-HorizontalAlign="Right" />
                        </Columns>
                        <PagerStyle CssClass="pagination" />
                    </asp:GridView>
                </div>
            </div>

            <!-- Floating Pagination -->
            <div class="pagination-container" id="paginationContainer" runat="server">
                <div class="pagination" id="paginationControls" runat="server">
                    <!-- Pagination will be generated by JavaScript -->
                </div>
            </div>

            <asp:HiddenField ID="hfParsedJson" runat="server" />
            <asp:HiddenField ID="hfCurrentPOMasterID" runat="server" Value="0" />
        </div>
    </div>

    <script>
        // Enhanced pagination and UI interactions
        function initializeUI() {
            togglePagination();
            enhanceTableInteractions();
            addSmoothScrolling();
        }

        function togglePagination() {
            var paginationContainer = document.getElementById('<%= paginationContainer.ClientID %>');
            var gridView = document.getElementById('<%= gvDetails.ClientID %>');
            
            if (gridView && gridView.rows.length > 1) {
                paginationContainer.style.display = 'block';
                generatePagination();
            } else {
                paginationContainer.style.display = 'none';
            }
        }

        function generatePagination() {
            var paginationControls = document.getElementById('<%= paginationControls.ClientID %>');
            var gridView = document.getElementById('<%= gvDetails.ClientID %>');
            
            if (!gridView || !paginationControls) return;
            
            // Get pagination info from the GridView's pager
            var pagerRow = gridView.querySelector('tr[class*="pager"]');
            if (!pagerRow) return;
            
            var currentPage = 1;
            var totalPages = 1;
            
            // Try to extract page info from the pager
            var pageLinks = pagerRow.querySelectorAll('a, span');
            if (pageLinks.length > 0) {
                // This is a simplified approach - in a real implementation,
                // you'd parse the actual page numbers from the GridView pager
                totalPages = Math.max(1, Math.ceil((gridView.rows.length - 1) / 50)); // Assuming 50 items per page
            }
            
            // Clear existing pagination
            paginationControls.innerHTML = '';
            
            // Add previous button
            if (currentPage > 1) {
                var prevLink = document.createElement('a');
                prevLink.href = '#';
                prevLink.innerHTML = '‹';
                prevLink.title = 'Previous Page';
                prevLink.onclick = function(e) { e.preventDefault(); goToPage(currentPage - 1); };
                paginationControls.appendChild(prevLink);
            }
            
            // Add page numbers (show max 7 pages)
            var startPage = Math.max(1, currentPage - 3);
            var endPage = Math.min(totalPages, startPage + 6);
            
            for (var i = startPage; i <= endPage; i++) {
                var pageLink = document.createElement('a');
                if (i === currentPage) {
                    pageLink.className = 'current';
                } else {
                    pageLink.href = '#';
                    pageLink.onclick = function(e) { e.preventDefault(); goToPage(parseInt(this.innerHTML)); };
                }
                pageLink.innerHTML = i;
                pageLink.title = 'Page ' + i;
                paginationControls.appendChild(pageLink);
            }
            
            // Add next button
            if (currentPage < totalPages) {
                var nextLink = document.createElement('a');
                nextLink.href = '#';
                nextLink.innerHTML = '›';
                nextLink.title = 'Next Page';
                nextLink.onclick = function(e) { e.preventDefault(); goToPage(currentPage + 1); };
                paginationControls.appendChild(nextLink);
            }
        }

        function goToPage(pageNumber) {
            // Scroll to top smoothly
            window.scrollTo({
                top: 0,
                behavior: 'smooth'
            });
            
            // In a real implementation, this would trigger a postback
            // For now, we'll just show a message
            console.log('Navigating to page ' + pageNumber);
        }

        function enhanceTableInteractions() {
            var table = document.querySelector('.line-items-table');
            if (!table) return;
            
            // Add hover effects and better visual feedback
            var rows = table.querySelectorAll('tbody tr');
            rows.forEach(function(row, index) {
                row.addEventListener('mouseenter', function() {
                    this.style.transform = 'translateX(2px)';
                    this.style.boxShadow = '0 2px 8px rgba(0,0,0,0.1)';
                });
                
                row.addEventListener('mouseleave', function() {
                    this.style.transform = 'translateX(0)';
                    this.style.boxShadow = 'none';
                });
            });
        }

        function addSmoothScrolling() {
            // Add smooth scrolling to all internal links
            var links = document.querySelectorAll('a[href^="#"]');
            links.forEach(function(link) {
                link.addEventListener('click', function(e) {
                    e.preventDefault();
                    var target = document.querySelector(this.getAttribute('href'));
                    if (target) {
                        target.scrollIntoView({
                            behavior: 'smooth',
                            block: 'start'
                        });
                    }
                });
            });
        }

        // Initialize when DOM is ready
        document.addEventListener('DOMContentLoaded', initializeUI);
        
        // Re-initialize after postbacks
        if (typeof Sys !== 'undefined' && Sys.WebForms) {
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(initializeUI);
        }
    </script>
</asp:Content>
