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


        Dim tempPath = Server.MapPath("~/App_Data/uploads/")
        If Not Directory.Exists(tempPath) Then Directory.CreateDirectory(tempPath)
        Dim savePath = Path.Combine(tempPath, Guid.NewGuid().ToString() & ".pdf")
        fuPdf.SaveAs(savePath)


        Try
            Dim parser As New PdfPoParser()
            Dim parsed = parser.Parse(savePath)
            If parsed.Details IsNot Nothing Then
                parsed.Details.Sort(Function(a, b) a.LineNumber.CompareTo(b.LineNumber))
            End If
            Me.Parsed = parsed


            ' Bind preview
            dvMaster.DataSource = New ParsedMaster() {parsed.Master}
            dvMaster.DataBind()


            gvDetails.DataSource = parsed.Details
            gvDetails.DataBind()


            ' keep a JSON snapshot if you want client-side use
            hfParsedJson.Value = JsonConvert.SerializeObject(parsed)


            btnSave.Enabled = True
            ShowInfo("Preview generated. Please verify then click Save to DB.")
        Catch ex As Exception
            ShowError("Failed to parse PDF: " & ex.Message)
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
            Dim repo As New PoRepository(System.Configuration.ConfigurationManager.ConnectionStrings("DefaultConnection").ConnectionString)
            Dim masterId = repo.InsertMaster(Parsed.Master)
            repo.InsertDetails(masterId, Parsed.Details)


            btnSave.Enabled = False
            ShowInfo("✅ Saved successfully.")
        Catch ex As Exception
            ShowError("DB save failed: " & ex.Message)
        End Try
    End Sub

    Protected Sub btnReset_Click(sender As Object, e As EventArgs)
        Session.Remove("ParsedPo")
        dvMaster.DataSource = Nothing : dvMaster.DataBind()
        gvDetails.DataSource = Nothing : gvDetails.DataBind()
        btnSave.Enabled = False
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
        End If
    End Sub
End Class