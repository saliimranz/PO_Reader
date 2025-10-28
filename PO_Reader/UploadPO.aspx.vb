Imports System
Imports System.IO
Imports Newtonsoft.Json
Imports UglyToad.PdfPig

Public Class UploadPO
    Inherits System.Web.UI.Page

    Private Property Parsed As ParsedPo
        Get
            Return TryCast(Session("ParsedPo"), ParsedPo)
        End Get
        Set(value As ParsedPo)
            Session("ParsedPo") = value
        End Set
    End Property

    Protected Sub Page_Load(sender As Object, e As EventArgs)
        If Not IsPostBack Then
            LoadExistingPOs()
        End If
    End Sub

    Private Sub LoadExistingPOs()
        Try
            Dim repo As New PoRepository(System.Configuration.ConfigurationManager.ConnectionStrings("DBCS").ConnectionString)
            Dim poList = repo.GetAllPOs()

            ddlExistingPOs.Items.Clear()
            ddlExistingPOs.Items.Add(New ListItem("-- Select a Purchase Order --", "0"))

            For Each po In poList
                ' Only show PO Number if it's not empty, otherwise show a default
                Dim displayText As String = If(String.IsNullOrWhiteSpace(po.PONumber),
                    $"PO-{po.POMasterID}",
                    po.PONumber)
                ' Use PO Number as the value, fallback to PO Master ID if PO Number is empty
                Dim valueText As String = If(String.IsNullOrWhiteSpace(po.PONumber),
                    po.POMasterID.ToString(),
                    po.PONumber)
                ddlExistingPOs.Items.Add(New ListItem(displayText, valueText))
            Next

            ' Show success message if POs were loaded
            If poList.Count > 0 Then
                ShowInfo($"Loaded {poList.Count} existing Purchase Orders.")
            Else
                ShowInfo("No existing Purchase Orders found in database.")
            End If
        Catch ex As Exception
            ShowError("Failed to load existing POs: " & ex.Message)
        End Try
    End Sub

    Protected Sub btnUpload_Click(sender As Object, e As EventArgs)
        lblInfo.Text = "" : lblInfo.Style("display") = "none"
        lblError.Text = "" : lblError.Style("display") = "none"

        If Not fuPdf.HasFile Then
            ShowError("Please select a PDF file.")
            Return
        End If
        If Not fuPdf.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) Then
            ShowError("Only PDF files are allowed.")
            Return
        End If

        Try
            Dim parser As New PdfPoParser()
            ' Parse directly from the uploaded file stream (no disk write)
            Dim parsed = parser.Parse(fuPdf.FileContent)
            If parsed.Details IsNot Nothing Then
                parsed.Details.Sort(Function(a, b) a.LineNumber.CompareTo(b.LineNumber))
            End If
            Me.Parsed = parsed

            ' Bind preview
            dvMaster.DataSource = New ParsedMaster() {parsed.Master}
            dvMaster.DataBind()

            ' Configure GridView for better pagination
            gvDetails.PageSize = 50
            gvDetails.DataSource = parsed.Details
            gvDetails.DataBind()

            ' Update statistics
            UpdateStatistics(parsed.Details)

            ' keep a JSON snapshot if you want client-side use
            hfParsedJson.Value = JsonConvert.SerializeObject(parsed)

            btnSave.Enabled = True
            btnUpdatePO.Enabled = False
            hfCurrentPOMasterID.Value = "0"
            ddlExistingPOs.SelectedIndex = 0
            ShowInfo("✅ Preview generated successfully. Please verify the data and click 'Save to Database' when ready.")
        Catch ex As Exception
            ShowError("❌ Failed to parse PDF: " & ex.Message)
        End Try
    End Sub

    Protected Sub btnSave_Click(sender As Object, e As EventArgs)
        lblInfo.Text = "" : lblInfo.Style("display") = "none"
        lblError.Text = "" : lblError.Style("display") = "none"


        If Parsed Is Nothing Then
            ShowError("Nothing to save. Upload a PDF first.")
            Return
        End If


        Try
            ' Read any edits from the master DetailsView inputs before saving
            Dim m = Parsed.Master
            If m Is Nothing Then m = New ParsedMaster()

            Dim txtPONumber = TryCast(dvMaster.FindControl("txtPONumber"), TextBox)
            Dim txtSupplierName = TryCast(dvMaster.FindControl("txtSupplierName"), TextBox)
            Dim txtSupplierNumber = TryCast(dvMaster.FindControl("txtSupplierNumber"), TextBox)
            Dim txtPODate = TryCast(dvMaster.FindControl("txtPODate"), TextBox)
            Dim txtCurrency = TryCast(dvMaster.FindControl("txtCurrency"), TextBox)
            Dim txtSubTotal = TryCast(dvMaster.FindControl("txtSubTotal"), TextBox)
            Dim txtVAT = TryCast(dvMaster.FindControl("txtVAT"), TextBox)
            Dim txtTotal = TryCast(dvMaster.FindControl("txtTotal"), TextBox)
            Dim txtPaymentTerms = TryCast(dvMaster.FindControl("txtPaymentTerms"), TextBox)
            Dim txtShipping_Address = TryCast(dvMaster.FindControl("txtShipping_Address"), TextBox)
            Dim txtIncoTerms = TryCast(dvMaster.FindControl("txtIncoTerms"), TextBox)
            Dim txtPODescription = TryCast(dvMaster.FindControl("txtPODescription"), TextBox)

            If txtPONumber IsNot Nothing Then m.PONumber = txtPONumber.Text
            If txtSupplierName IsNot Nothing Then m.SupplierName = txtSupplierName.Text
            If txtSupplierNumber IsNot Nothing Then m.SupplierNumber = txtSupplierNumber.Text
            If txtPODate IsNot Nothing Then
                Dim dt As DateTime
                If DateTime.TryParse(txtPODate.Text, dt) Then m.PODate = dt
            End If
            If txtCurrency IsNot Nothing Then m.Currency = txtCurrency.Text
            If txtSubTotal IsNot Nothing Then
                Dim d As Decimal
                If Decimal.TryParse(txtSubTotal.Text.Replace(",", ""), Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, d) Then m.SubTotal = d
            End If
            If txtVAT IsNot Nothing Then
                Dim d As Decimal
                If Decimal.TryParse(txtVAT.Text.Replace(",", ""), Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, d) Then m.VAT = d
            End If
            If txtTotal IsNot Nothing Then
                Dim d As Decimal
                If Decimal.TryParse(txtTotal.Text.Replace(",", ""), Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, d) Then m.Total = d
            End If
            If txtPaymentTerms IsNot Nothing Then m.PaymentTerms = txtPaymentTerms.Text
            If txtShipping_Address IsNot Nothing Then m.Shipping_Address = txtShipping_Address.Text
            If txtIncoTerms IsNot Nothing Then m.IncoTerms = txtIncoTerms.Text
            If txtPODescription IsNot Nothing Then m.PODescription = txtPODescription.Text

            Parsed.Master = m

            Dim repo As New PoRepository(System.Configuration.ConfigurationManager.ConnectionStrings("DBCS").ConnectionString)
            Dim masterId = repo.InsertMaster(Parsed.Master)
            repo.InsertDetails(masterId, Parsed.Details)


            btnSave.Enabled = False
            btnUpdatePO.Enabled = False
            hfCurrentPOMasterID.Value = "0"
            ddlExistingPOs.SelectedIndex = 0
            LoadExistingPOs() ' Refresh the dropdown
            ShowInfo("✅ Saved successfully.")
        Catch ex As Exception
            ShowError("DB save failed: " & ex.Message)
        End Try
    End Sub

    Protected Sub btnFetchPO_Click(sender As Object, e As EventArgs)
        lblInfo.Text = "" : lblInfo.Style("display") = "none"
        lblError.Text = "" : lblError.Style("display") = "none"

        If ddlExistingPOs.SelectedValue = "0" Then
            ShowError("Please select a Purchase Order to fetch.")
            Return
        End If

        Try
            Dim selectedValue As String = ddlExistingPOs.SelectedValue
            Dim repo As New PoRepository(System.Configuration.ConfigurationManager.ConnectionStrings("DBCS").ConnectionString)
            Dim fetchedPO As ParsedPo = Nothing
            Dim poMasterID As Integer = 0

            ' Check if the selected value is a PO Number (contains letters) or PO Master ID (numeric)
            If selectedValue.All(Function(c) Char.IsDigit(c)) Then
                ' It's a PO Master ID (numeric)
                poMasterID = Integer.Parse(selectedValue)
                fetchedPO = repo.GetPOByID(poMasterID)
            Else
                ' It's a PO Number (contains letters)
                fetchedPO = repo.GetPOByPONumber(selectedValue)
                ' Get the PO Master ID for the update functionality
                If fetchedPO IsNot Nothing AndAlso fetchedPO.Master IsNot Nothing Then
                    ' Query to get POMasterID for this PO Number
                    Using con As New SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings("DBCS").ConnectionString)
                        con.Open()
                        Using cmd As New SqlCommand("SELECT POMasterID FROM dbo.IBL_PO_Master WHERE PONumber = @PONumber", con)
                            cmd.Parameters.AddWithValue("@PONumber", selectedValue)
                            Using reader = cmd.ExecuteReader()
                                If reader.Read() AndAlso Not reader.IsDBNull("POMasterID") Then
                                    poMasterID = reader.GetInt32("POMasterID")
                                End If
                            End Using
                        End Using
                    End Using
                End If
            End If

            If fetchedPO IsNot Nothing Then
                Me.Parsed = fetchedPO
                hfCurrentPOMasterID.Value = poMasterID.ToString()

                ' Bind preview
                dvMaster.DataSource = New ParsedMaster() {fetchedPO.Master}
                dvMaster.DataBind()

                ' Configure GridView for better pagination
                gvDetails.PageSize = 50
                gvDetails.DataSource = fetchedPO.Details
                gvDetails.DataBind()

                ' Update statistics
                UpdateStatistics(fetchedPO.Details)

                ' Update UI state
                btnSave.Enabled = False
                btnUpdatePO.Enabled = True
                fuPdf.Enabled = False
                btnUpload.Enabled = False

                ShowInfo("✅ PO fetched successfully. You can now edit the details and click 'Update Changes' to save.")
            Else
                ShowError("❌ Failed to fetch PO details.")
            End If
        Catch ex As Exception
            ShowError("❌ Failed to fetch PO: " & ex.Message)
        End Try
    End Sub

    Protected Sub btnUpdatePO_Click(sender As Object, e As EventArgs)
        lblInfo.Text = "" : lblInfo.Style("display") = "none"
        lblError.Text = "" : lblError.Style("display") = "none"

        If Parsed Is Nothing OrElse hfCurrentPOMasterID.Value = "0" Then
            ShowError("Nothing to update. Please fetch a PO first.")
            Return
        End If

        Try
            Dim poMasterID As Integer = Integer.Parse(hfCurrentPOMasterID.Value)
            Dim m = Parsed.Master
            If m Is Nothing Then m = New ParsedMaster()

            ' Read any edits from the master DetailsView inputs before saving
            Dim txtPONumber = TryCast(dvMaster.FindControl("txtPONumber"), TextBox)
            Dim txtSupplierName = TryCast(dvMaster.FindControl("txtSupplierName"), TextBox)
            Dim txtSupplierNumber = TryCast(dvMaster.FindControl("txtSupplierNumber"), TextBox)
            Dim txtPODate = TryCast(dvMaster.FindControl("txtPODate"), TextBox)
            Dim txtCurrency = TryCast(dvMaster.FindControl("txtCurrency"), TextBox)
            Dim txtSubTotal = TryCast(dvMaster.FindControl("txtSubTotal"), TextBox)
            Dim txtVAT = TryCast(dvMaster.FindControl("txtVAT"), TextBox)
            Dim txtTotal = TryCast(dvMaster.FindControl("txtTotal"), TextBox)
            Dim txtPaymentTerms = TryCast(dvMaster.FindControl("txtPaymentTerms"), TextBox)
            Dim txtShipping_Address = TryCast(dvMaster.FindControl("txtShipping_Address"), TextBox)
            Dim txtIncoTerms = TryCast(dvMaster.FindControl("txtIncoTerms"), TextBox)
            Dim txtPODescription = TryCast(dvMaster.FindControl("txtPODescription"), TextBox)

            If txtPONumber IsNot Nothing Then m.PONumber = txtPONumber.Text
            If txtSupplierName IsNot Nothing Then m.SupplierName = txtSupplierName.Text
            If txtSupplierNumber IsNot Nothing Then m.SupplierNumber = txtSupplierNumber.Text
            If txtPODate IsNot Nothing Then
                Dim dt As DateTime
                If DateTime.TryParse(txtPODate.Text, dt) Then m.PODate = dt
            End If
            If txtCurrency IsNot Nothing Then m.Currency = txtCurrency.Text
            If txtSubTotal IsNot Nothing Then
                Dim d As Decimal
                If Decimal.TryParse(txtSubTotal.Text.Replace(",", ""), Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, d) Then m.SubTotal = d
            End If
            If txtVAT IsNot Nothing Then
                Dim d As Decimal
                If Decimal.TryParse(txtVAT.Text.Replace(",", ""), Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, d) Then m.VAT = d
            End If
            If txtTotal IsNot Nothing Then
                Dim d As Decimal
                If Decimal.TryParse(txtTotal.Text.Replace(",", ""), Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, d) Then m.Total = d
            End If
            If txtPaymentTerms IsNot Nothing Then m.PaymentTerms = txtPaymentTerms.Text
            If txtShipping_Address IsNot Nothing Then m.Shipping_Address = txtShipping_Address.Text
            If txtIncoTerms IsNot Nothing Then m.IncoTerms = txtIncoTerms.Text
            If txtPODescription IsNot Nothing Then m.PODescription = txtPODescription.Text

            Parsed.Master = m

            Dim repo As New PoRepository(System.Configuration.ConfigurationManager.ConnectionStrings("DBCS").ConnectionString)
            repo.UpdateMaster(poMasterID, Parsed.Master)
            repo.UpdateDetails(poMasterID, Parsed.Details)

            btnUpdatePO.Enabled = False
            LoadExistingPOs() ' Refresh the dropdown
            ShowInfo("✅ PO updated successfully.")
        Catch ex As Exception
            ShowError("❌ Update failed: " & ex.Message)
        End Try
    End Sub

    Protected Sub btnReset_Click(sender As Object, e As EventArgs)
        Session.Remove("ParsedPo")
        dvMaster.DataSource = Nothing : dvMaster.DataBind()
        gvDetails.DataSource = Nothing : gvDetails.DataBind()
        btnSave.Enabled = False
        btnUpdatePO.Enabled = False
        hfCurrentPOMasterID.Value = "0"
        fuPdf.Enabled = True
        btnUpload.Enabled = True
        ddlExistingPOs.SelectedIndex = 0
        lblInfo.Text = "" : lblInfo.Style("display") = "none"
        lblError.Text = "" : lblError.Style("display") = "none"
    End Sub


    Private Sub ShowInfo(msg As String)
        lblInfo.Text = msg
        lblInfo.Style("display") = "block"
    End Sub


    Private Sub ShowError(msg As String)
        lblError.Text = msg
        lblError.Style("display") = "block"
    End Sub

    Protected Sub gvDetails_PageIndexChanging(sender As Object, e As GridViewPageEventArgs)
        gvDetails.PageIndex = e.NewPageIndex
        If Parsed IsNot Nothing Then
            gvDetails.DataSource = Parsed.Details
            gvDetails.DataBind()
            UpdateStatistics(Parsed.Details)
        End If
    End Sub

    Private Sub UpdateStatistics(details As List(Of ParsedDetail))
        If details IsNot Nothing AndAlso details.Count > 0 Then
            Dim totalItemsCount = details.Count
            Dim totalPages = Math.Ceiling(totalItemsCount / gvDetails.PageSize)
            Dim currentPageNum = gvDetails.PageIndex + 1
            Dim totalAmountValue = details.Sum(Function(d) If(d.Amount, 0))

            ' Update stats bar
            statsBar.Style("display") = "block"
            totalItems.InnerText = $"Total Items: {totalItemsCount:N0}"
            currentPage.InnerText = $"Page {currentPageNum} of {totalPages}"
            totalAmount.InnerText = $"Total Amount: {totalAmountValue:C2}"

            ' Show pagination if more than one page
            If totalPages > 1 Then
                paginationContainer.Style("display") = "block"
            Else
                paginationContainer.Style("display") = "none"
            End If
        Else
            statsBar.Style("display") = "none"
            paginationContainer.Style("display") = "none"
        End If
    End Sub
End Class