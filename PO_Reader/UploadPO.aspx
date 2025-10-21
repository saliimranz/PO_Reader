<%@ Page Language="vb" AutoEventWireup="true" CodeBehind="UploadPO.aspx.vb" Inherits="PO_Reader.UploadPO" %>
<!DOCTYPE html>
<html>
<head runat="server">
    <title>PO PDF Uploader</title>
    <meta charset="utf-8" />
    <style>
        body{font-family:Segoe UI,Arial,sans-serif;background:#f5f5f5;margin:0}
        .wrap{max-width:1100px;margin:40px auto;background:#fff;padding:24px 28px;border-radius:12px;box-shadow:0 6px 18px rgba(0,0,0,.08)}
        h1{margin:0 0 18px;color:#4B0082}
        .card{border:1px solid #eee;border-radius:10px;padding:16px;margin-top:16px}
        .row{display:flex;gap:12px;align-items:center;flex-wrap:wrap}
        .btn{background:#4B0082;color:#fff;border:none;border-radius:8px;padding:9px 16px;cursor:pointer}
        .btn:disabled{opacity:.5;cursor:not-allowed}
        .green{background:#0b7b32}
        .warn{background:#d97706}
        .note{color:#666;font-size:.9rem}
        table{border-collapse:collapse;width:100%}
        th,td{border:1px solid #e5e7eb;padding:8px;text-align:left}
        th{background:#4B0082;color:#fff}
        .grid-wrap{overflow:auto;max-height:480px}
        .success{display:none;margin-top:12px;padding:10px 12px;border-radius:8px;background:#d1fae5;color:#065f46}
        .error{display:none;margin-top:12px;padding:10px 12px;border-radius:8px;background:#fee2e2;color:#7f1d1d}
    </style>
</head>
<body>
    <!-- IMPORTANT: server form wrapper + enctype for file uploads -->
    <form id="form1" runat="server" enctype="multipart/form-data">
        <div class="wrap">
            <h1>Purchase Order – Upload & Preview</h1>

            <div class="card">
                <div class="row">
                    <asp:FileUpload ID="fuPdf" runat="server" accept="application/pdf" />
                    <asp:Button ID="btnUpload" runat="server" CssClass="btn" Text="Upload & Preview" OnClick="btnUpload_Click" />
                    <asp:Button ID="btnSave" runat="server" CssClass="btn green" Text="Save to DB" OnClick="btnSave_Click" Enabled="false" />
                    <asp:Button ID="btnReset" runat="server" CssClass="btn warn" Text="Reset" OnClick="btnReset_Click" />
                </div>
                <div class="note">Flow: Upload PDF → Preview parsed data → Click "Save to DB" if everything looks correct.</div>
                <asp:Label ID="lblInfo" runat="server" CssClass="success"></asp:Label>
                <asp:Label ID="lblError" runat="server" CssClass="error"></asp:Label>
            </div>

            <div class="card">
                <h3>Master (IBL_PO_Master)</h3>
                <asp:DetailsView ID="dvMaster" runat="server" AutoGenerateRows="false" GridLines="Both" BorderWidth="1">
                    <Fields>
                        <asp:BoundField DataField="PONumber" HeaderText="PONumber" />
                        <asp:BoundField DataField="SupplierName" HeaderText="SupplierName" />
                        <asp:BoundField DataField="SupplierNumber" HeaderText="SupplierNumber" />
                        <asp:BoundField DataField="PODate" HeaderText="PODate" DataFormatString="{0:yyyy-MM-dd}" />
                        <asp:BoundField DataField="Currency" HeaderText="Currency" />
                        <asp:BoundField DataField="SubTotal" HeaderText="SubTotal" DataFormatString="{0:N2}" />
                        <asp:BoundField DataField="VAT" HeaderText="VAT" DataFormatString="{0:N2}" />
                        <asp:BoundField DataField="Total" HeaderText="Total" DataFormatString="{0:N2}" />
                        <asp:BoundField DataField="PaymentTerms" HeaderText="PaymentTerms" />
                        <asp:BoundField DataField="Shipping_Address" HeaderText="Shipping_Address" />
                        <asp:BoundField DataField="IncoTerms" HeaderText="IncoTerms" />
                        <asp:BoundField DataField="PODescription" HeaderText="PODescription" />
                    </Fields>
                </asp:DetailsView>
            </div>

            <div class="card">
                <h3>Details (IBL_PO_Detail)</h3>
                <div class="grid-wrap">
                    <asp:GridView ID="gvDetails" runat="server" AutoGenerateColumns="false" AllowPaging="true" PageSize="20">
                        <Columns>
                            <asp:BoundField DataField="ItemCode" HeaderText="ItemCode" />
                            <asp:BoundField DataField="Description" HeaderText="Description" />
                            <asp:BoundField DataField="DeliveryDate" HeaderText="DeliveryDate" DataFormatString="{0:yyyy-MM-dd}" />
                            <asp:BoundField DataField="UOM" HeaderText="UOM" />
                            <asp:BoundField DataField="Qty" HeaderText="Qty" />
                            <asp:BoundField DataField="UnitPrice" HeaderText="UnitPrice" DataFormatString="{0:N2}" />
                            <asp:BoundField DataField="NetPrice" HeaderText="NetPrice" DataFormatString="{0:N2}" />
                            <asp:BoundField DataField="Amount" HeaderText="Amount" DataFormatString="{0:N2}" />
                        </Columns>
                    </asp:GridView>
                </div>
            </div>

            <asp:HiddenField ID="hfParsedJson" runat="server" />
        </div>
    </form>
</body>
</html>
