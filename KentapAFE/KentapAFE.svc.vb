' NOTE: You can use the "Rename" command on the context menu to change the class name "Service1" in code, svc and config file together.
' NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.vb at the Solution Explorer and start debugging.
Imports System.Net
Imports System.Threading
Imports System.Xml
Imports System.Data.SqlClient
Imports System.Text
Imports System.IO
Imports System.Web.Configuration
Imports System.Net.Mail
Imports System.Security.Cryptography

Public Class KentapAFE
    Implements IKentapAFE
    Public Sub New()

    End Sub

    Public Function GetSOS(emailAddress As String) As String Implements IKentapAFE.GetSOS

        Dim connectionstring As String = WebConfigurationManager.ConnectionStrings("DBConnectionString").ToString
        Dim connection As New System.Data.SqlClient.SqlConnection(connectionstring)
        Dim cmd As New SqlCommand
        Dim paramReturn As SqlParameter
        Dim paramEmailAddress As SqlParameter
        Dim paramSOSPhoneNumber As SqlParameter

        Try
            Dim ctx As WebOperationContext = WebOperationContext.Current

            cmd.CommandText = "dbo.GetSOS"
            cmd.CommandType = CommandType.StoredProcedure
            cmd.Connection = connection

            paramReturn = New SqlClient.SqlParameter("@RETURN", 0)
            paramReturn.Direction = ParameterDirection.ReturnValue
            cmd.Parameters.Add(paramReturn)

            paramEmailAddress = New SqlClient.SqlParameter("@EmailAddress", SqlDbType.VarChar, 50)
            paramEmailAddress.Value = emailAddress
            cmd.Parameters.Add(paramEmailAddress)

            paramSOSPhoneNumber = New SqlClient.SqlParameter("@SOSPhoneNumber", SqlDbType.VarChar, 50)
            paramSOSPhoneNumber.Direction = ParameterDirection.Output
            cmd.Parameters.Add(paramSOSPhoneNumber)

            cmd.ExecuteNonQuery()
            Return paramSOSPhoneNumber.Value.ToString

        Catch exp As Exception
            LogError("GetSOS", exp.Message)
            Return ""
        Finally
            connection.Dispose()
            cmd.Dispose()
        End Try

    End Function

    Public Sub LogError(ByVal ErrorSource As String, ByVal ErrorMessage As String)

        Dim connectionstring As String = WebConfigurationManager.ConnectionStrings("DBConnectionString").ToString
        Dim connection As New System.Data.SqlClient.SqlConnection(connectionstring)
        Dim cmd As New SqlCommand
        Dim paramReturn As SqlParameter
        Dim paramErrorSource As SqlParameter
        Dim paramErrorMessage As SqlParameter

        Try
            cmd.CommandText = "dbo.InsertErrorLog"
            cmd.CommandType = CommandType.StoredProcedure
            cmd.Connection = connection

            paramReturn = New SqlClient.SqlParameter("@RETURN", 0)
            paramReturn.Direction = ParameterDirection.ReturnValue
            cmd.Parameters.Add(paramReturn)

            paramErrorSource = New SqlClient.SqlParameter("@ErrorSource", SqlDbType.VarChar, 500)
            paramErrorSource.Value = ErrorSource
            cmd.Parameters.Add(paramErrorSource)

            paramErrorMessage = New SqlClient.SqlParameter("@ErrorMessage", SqlDbType.VarChar, 500)
            paramErrorMessage.Value = ErrorMessage
            cmd.Parameters.Add(paramErrorMessage)

            cmd.ExecuteNonQuery()
            Exit Sub

        Catch exp As Exception
            If Not EventLog.SourceExists(My.Application.Info.ProductName) Then
                EventLog.CreateEventSource(My.Application.Info.ProductName, "Application")
            End If
            EventLog.WriteEntry(My.Application.Info.ProductName, "(" & exp.Message & ") " & ErrorMessage)
        Finally
            connection.Dispose()
            cmd.Dispose()
        End Try
    End Sub

    Public Function FindMyCar(emailAddress As String) As XmlElement Implements IKentapAFE.FindMyCar
        Dim XMLOutDoc As New XmlDocument
        Dim connectionstring As String = WebConfigurationManager.ConnectionStrings("DBConnectionString").ToString
        Dim connection As New System.Data.SqlClient.SqlConnection(connectionstring)
        Dim cmd As New SqlCommand
        Dim paramReturn As SqlParameter
        Dim paramEmailAddress As SqlParameter
        Dim paramXMLData As New SqlParameter

        Try
            Dim ctx As WebOperationContext = WebOperationContext.Current

            cmd.CommandText = "dbo.FindMyCar"
            cmd.CommandType = CommandType.StoredProcedure
            cmd.Connection = connection

            paramReturn = New SqlClient.SqlParameter("@RETURN", 0)
            paramReturn.Direction = ParameterDirection.ReturnValue
            cmd.Parameters.Add(paramReturn)

            paramEmailAddress = New SqlClient.SqlParameter("@EmailAddress", SqlDbType.VarChar, 50)
            paramEmailAddress.Value = emailAddress
            cmd.Parameters.Add(paramEmailAddress)

            paramXMLData.ParameterName = "@XMLData"
            paramXMLData.SqlDbType = SqlDbType.Xml
            paramXMLData.Direction = ParameterDirection.Output

            cmd.ExecuteNonQuery()

            If paramXMLData.Value.ToString = "" Then
                XMLOutDoc.LoadXml("<Error>NODATA</Error>")
                Return XMLOutDoc.DocumentElement
            End If

            XMLOutDoc.LoadXml(paramXMLData.Value)
            Return XMLOutDoc.DocumentElement

        Catch exp As Exception
            LogError("FindMyCar", exp.Message)
            XMLOutDoc.LoadXml("<Error>" + exp.Message + "</Error>")
            Return XMLOutDoc.DocumentElement
        Finally
            connection.Dispose()
            cmd.Dispose()
        End Try

    End Function

    Public Function Login(emailAddress As String, password As String) As XmlElement Implements IKentapAFE.Login
        Dim XMLOutDoc As New XmlDocument
        Try
            Dim ctx As WebOperationContext = WebOperationContext.Current

            If String.IsNullOrEmpty(emailAddress) Then
                XMLOutDoc.LoadXml("<Error>UserName not supplied</Error>")
                Return XMLOutDoc.DocumentElement
            End If

            If String.IsNullOrEmpty(password) Then
                XMLOutDoc.LoadXml("<Error>Password not supplied</Error>")
                Return XMLOutDoc.DocumentElement
            End If

            Dim isValidUser As Boolean = ValidateUser(emailAddress, password)
            If isValidUser Then
                XMLOutDoc.LoadXml("<Success>Login successful</Success>")
            Else
                XMLOutDoc.LoadXml("<Error>Invalid username or password</Error>")
            End If

            Return XMLOutDoc.DocumentElement

        Catch exp As Exception
            LogError("Login", exp.Message)
            XMLOutDoc.LoadXml("<Error>" + exp.Message + "</Error>")
            Return XMLOutDoc.DocumentElement
        Finally

        End Try
    End Function

    Private Function ValidateUser(emailAddress As String, password As String) As Boolean
        Dim connectionString As String = WebConfigurationManager.ConnectionStrings("DBConnectionString").ConnectionString

        Using connection As New SqlConnection(connectionString)
            Dim command As New SqlCommand("SELECT COUNT(1) FROM Users WHERE Username = @Username AND Password = @Password", connection)
            command.Parameters.AddWithValue("@emailAddress", emailAddress)
            command.Parameters.AddWithValue("@Password", password)

            Try
                connection.Open()
                Dim result As Integer = command.ExecuteScalar()
                Return result > 0
            Catch ex As Exception
                Return False
            End Try
        End Using
    End Function






    Public Function SignUp(emailAddress As String, password As String, name As String, sosPhoneNumber As String, adminEmail As String, adminPassword As String) As XmlElement Implements IKentapAFE.SignUp
        Dim XMLOutDoc As New XmlDocument
        Dim connectionstring As String = WebConfigurationManager.ConnectionStrings("DBConnectionString").ToString
        Dim connection As New System.Data.SqlClient.SqlConnection(connectionstring)
        Dim cmd As New SqlCommand
        Dim paramReturn As SqlParameter
        Dim paramEmailAddress As SqlParameter
        Dim paramPassword As SqlParameter
        Dim paramName As SqlParameter
        Dim paramSOSPhoneNumber As SqlParameter
        Dim paramAdminEmailAddress As SqlParameter
        Dim paramAdminPassword As SqlParameter


        Try
            Dim ctx As WebOperationContext = WebOperationContext.Current

            cmd.CommandText = "dbo.SignUp"
            cmd.CommandType = CommandType.StoredProcedure
            cmd.Connection = connection

            paramReturn = New SqlClient.SqlParameter("@RETURN", 0)
            paramReturn.Direction = ParameterDirection.ReturnValue
            cmd.Parameters.Add(paramReturn)

            paramEmailAddress = New SqlClient.SqlParameter("@EmailAddress", SqlDbType.VarChar, 50)
            paramEmailAddress.Value = emailAddress
            cmd.Parameters.Add(paramEmailAddress)

            paramPassword = New SqlClient.SqlParameter("@Password", SqlDbType.VarChar, 50)
            paramPassword.Value = password
            cmd.Parameters.Add(paramPassword)

            paramName = New SqlClient.SqlParameter("@Name", SqlDbType.VarChar, 50)
            paramName.Value = name
            cmd.Parameters.Add(paramName)

            paramSOSPhoneNumber = New SqlClient.SqlParameter("@SOSPhoneNumber", SqlDbType.VarChar, 50)
            paramSOSPhoneNumber.Value = sosPhoneNumber
            cmd.Parameters.Add(paramSOSPhoneNumber)

            paramAdminEmailAddress = New SqlClient.SqlParameter("@AdminEmailAddress", SqlDbType.VarChar, 50)
            paramAdminEmailAddress.Value = adminEmail
            cmd.Parameters.Add(paramAdminEmailAddress)

            paramAdminPassword = New SqlClient.SqlParameter("@AdminPassword", SqlDbType.VarChar, 50)
            paramAdminPassword.Value = adminPassword
            cmd.Parameters.Add(paramAdminPassword)

            cmd.ExecuteNonQuery()

            XMLOutDoc.LoadXml("<Error>SUCCESS</Error>")
            Return XMLOutDoc.DocumentElement

        Catch exp As Exception
            LogError("SignUp", exp.Message)
            XMLOutDoc.LoadXml("<Error>" + exp.Message + "</Error>")
            Return XMLOutDoc.DocumentElement
        Finally
            connection.Dispose()
            cmd.Dispose()
        End Try

        Return XMLOutDoc.DocumentElement
    End Function

End Class
