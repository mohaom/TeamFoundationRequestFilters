using Microsoft.TeamFoundation.Framework.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Script.Serialization;

namespace TFSWorkitemFilter
{
    public class TFSWorkitemFilter : ITeamFoundationRequestFilter
    {

        public void BeginRequest(IVssRequestContext requestContext)
        {
            if (!requestContext.ServiceHost.HostType.HasFlag(TeamFoundationHostType.ProjectCollection)) return;
            if (HttpContext.Current.Request.Url.ToString().Contains("/_api/_wit/updateWorkItems"))
            {
                string content = ReadHttpContextInputStream(HttpContext.Current.Request.InputStream);
                content = content.Replace("\0", "");
                dynamic Workitem = Json.Decode(Json.Decode(content).updatePackage);
                if (Workitem[0].fields["10015"] > 2)
                {
                    throw new ValidationRequestFilterException("Priorities grater than 2 are not allowed.");
                }

        }
    }

    public void EndRequest(IVssRequestContext requestContext)
    {
    }

    public void EnterMethod(IVssRequestContext requestContext)
    {
        using (StreamWriter w = File.AppendText(@"c:\Logs\EnterRequestLog.txt"))
        {
            Log($"Enter Request - {requestContext.RawUrl()}", w);
        }
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
