Imports System.Xml
<ServiceContract()>
Public Interface IKentapAFE

    <OperationContract()>
    Function FindMyCar(emailAddress As String) As XmlElement

    <OperationContract()>
    Function GetSOS(emailAddress As String) As String

    <OperationContract()>
    Function Login(emailAddress As String, password As String) As XmlElement

    <OperationContract()>
    Function SignUp(emailAddress As String, password As String, name As String, sosPhoneNumber As String, adminEmail As String, adminPassword As String) As XmlElement

End Interface