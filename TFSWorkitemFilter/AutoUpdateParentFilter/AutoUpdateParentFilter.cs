using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Configuration;
using System.Net;

namespace TFSWorkitemFilter
{
    public class AutoUpdateParentFilter : ITeamFoundationRequestFilter
    {

        public void BeginRequest(IVssRequestContext requestContext)
        {
            try
            {
                if (!requestContext.ServiceHost.HostType.HasFlag(TeamFoundationHostType.ProjectCollection)) return;

                if (HttpContext.Current.Request.Url.ToString().Contains("/_api/_wit/updateWorkItems"))
                {
                    string content = ReadHttpContextInputStream(HttpContext.Current.Request.InputStream);
                    content = content.Replace("\0", "");
                    Log(content, "BeginRequest");
                    dynamic Workitem = Json.Decode(Json.Decode(content).updatePackage);

                    Log("New Request Started", "BeginRequest");
                    string Url = HttpContext.Current.Request.Url.ToString();
                    Log(Url, "BeginRequest");
                    string projectId = Workitem[0].projectId;
                    string collectionUrl = new Regex(@"(http(s)?://?)\w+(:\d{4})?/tfs/\w+collection").Match(Url).Value;
                    string workItemId = Workitem[0].Id.ToString();
                    Log(workItemId, "BeginRequest");
                    Log($"Collection: {collectionUrl}", "BeginRequest");
                    //string Username = ConfigurationManager.AppSettings["Username"];
                    //string Password = ConfigurationManager.AppSettings["Password"];
                    //Log($"{Username} | {Password}", "BeginRequest");
                    //NetworkCredential credential = new NetworkCredential(Username,Password);
                    TfsTeamProjectCollection tfsTeamProjectCollection = new TfsTeamProjectCollection(new Uri(collectionUrl));
                    //tfsTeamProjectCollection.Authenticate();
                    Guid p = new Guid(projectId);
                    var workItemInstance = tfsTeamProjectCollection.GetService<WorkItemStore>();
                    Project teamProject = workItemInstance.Projects[p];

                    var parentWorkItem = GetParent(workItemId, workItemInstance);
                    var childs = GetChilds(parentWorkItem, workItemInstance);

                    bool parentDone = !childs.Any(x => x.State == "In Progress" || x.State == "To Do");

                    if (parentDone)
                    {
                        parentWorkItem.Open();
                        parentWorkItem.State = "Done";
                        parentWorkItem.Save();
                    }
                    else
                    {
                        parentWorkItem.Open();
                        parentWorkItem.State = "Committed";
                        parentWorkItem.Save();
                    }

                }
            }
            catch (Exception ex)
            {

                Log($"Throwing Exception: {ex.Message}", "Exceptions");
            }
        }

        private void Log(string text, string file)
        {
            using (StreamWriter w = File.AppendText(@"c:\Logs\" + file + ".txt"))
            {
                Log(text, w);
            }
        }
        private static WorkItem GetParent(string Id, WorkItemStore workItemStore)
        {
            WorkItem child = workItemStore.GetWorkItem(int.Parse(Id));
            var parentLink = child.WorkItemLinks.Cast<WorkItemLink>().FirstOrDefault(x => x.LinkTypeEnd.Name == "Parent");
            return parentLink != null ? workItemStore.GetWorkItem(parentLink.TargetId) : null;
        }
        private static List<WorkItem> GetChilds(WorkItem parentWorkItem, WorkItemStore workItemInstance)
        {
            var childLinks = parentWorkItem?.WorkItemLinks.Cast<WorkItemLink>().Where(x => x.LinkTypeEnd.Name == "Child");
            if (childLinks != null)
                return childLinks.Select(childLink => workItemInstance.GetWorkItem(childLink.TargetId)).ToList();
            return new List<WorkItem>();
        }

        public void EndRequest(IVssRequestContext requestContext)
        {
        }

        public void EnterMethod(IVssRequestContext requestContext)
        {

        }

        public void LeaveMethod(IVssRequestContext requestContext)
        {
        }

        public Task PostAuthenticateRequest(IVssRequestContext requestContext)
        {
            return Task.FromResult<int>(0);
        }

        public void PostAuthorizeRequest(IVssRequestContext requestContext)
        {
        }

        public Task PostLogRequestAsync(IVssRequestContext requestContext)
        {
            return Task.FromResult<int>(0);
        }

        public void Log(string logMessage, TextWriter w)
        {
            w.Write("\r\nLog Entry : ");
            w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                DateTime.Now.ToLongDateString());
            w.WriteLine("  :{0}", logMessage);
            w.WriteLine("-------------------------------");
        }
        private string ReadHttpContextInputStream(Stream stream)
        {
            string requestContent = "";
            using (var memoryStream = new MemoryStream())
            {
                byte[] buffer = new byte[1024 * 4];
                int count = 0;
                while ((count = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    memoryStream.Write(buffer, 0, count);
                }
                memoryStream.Seek(0, SeekOrigin.Begin);
                stream.Seek(0, SeekOrigin.Begin);
                requestContent = Encoding.UTF8.GetString(memoryStream.GetBuffer());
            }
            return requestContent;
        }

    }
}
