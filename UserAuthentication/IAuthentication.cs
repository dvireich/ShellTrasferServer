using DBManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace UserAuthentication
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IAuthentication
    {
        [OperationContract]
        string Authenticate(string userName, string password, out string error);

        [OperationContract]
        bool SignIn(string userName, string password, out string error);

        [OperationContract]
        bool ChangeUserPassword(string userName, string oldPassword, string newPassword, out string error);

        [OperationContract]
        bool SetSecurityQuestionAndAnswer(string userName, string password, string question, string answer, out string error);

        [OperationContract]
        string GetSecurityQuestion(string userName, out string error);

        [OperationContract]
        string RestorePasswordFromUserNameAndSecurityQuestion(string userName, string answer, out string error);
    }


}
