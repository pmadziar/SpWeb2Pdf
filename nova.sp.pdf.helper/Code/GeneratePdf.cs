using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.SharePoint;
using System.Runtime.InteropServices;
using System.Security;
using System.Web.WebSockets;

namespace nova.sp.pdf.helper.Code
{
    public class GeneratePdf: IHttpHandler
    {
        private Dictionary<string, string> ress = new Dictionary<string, string>()
        {
            {"Config", "pdf-helper-config.json"},
            {"Script", "rasterize.js"}
        };
        private const string ResourceDir = "PhantomFiles";

        private string msg = string.Empty;

        private Dictionary<string, string> saveResourcesToDisk(string tempDirPath)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            Assembly asm = this.GetType().Assembly;
            string nameSpaceName = asm.GetName().Name;
            var xxx = asm.GetManifestResourceNames();

            foreach (var key in ress.Keys )
            {
                string resName = $"{nameSpaceName}.{ResourceDir}.{ress[key]}";
                string resDestPath = Path.Combine(tempDirPath, ress[key]);
                var resStream = asm.GetManifestResourceStream(resName);
                var resLen = (int)resStream.Length;
                byte[] contentBytes = new byte[resLen];
                resStream.Read(contentBytes, 0, resLen);
                File.WriteAllBytes(resDestPath, contentBytes);
                ret.Add(key, resDestPath);
            }
            return ret;
        }

        public static SecureString ToSecureString(string Source)
        {
            if (string.IsNullOrWhiteSpace(Source))
                return null;
            else
            {
                SecureString Result = new SecureString();
                foreach (char c in Source.ToCharArray())
                    Result.AppendChar(c);
                return Result;
            }
        }

        private bool runEex(string exePath, string arguments)
        {
            bool ret = true;
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    ProcessStartInfo info = new ProcessStartInfo(exePath)
                    {
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    };
                    //info.UserName = @"MADZIAR\spssetup";
                    //info.Password = ToSecureString("1q2w3e!Q@W#E");
                    using (Process process = Process.Start(info))
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                    }
                });
            }
            catch (Exception ex)
            {
                msg += $"<br />Exception: {ex.Message}";
                msg += $"<br /><pre>{ex.Source}</pre>";
                msg += $"<br /><pre>{ex.StackTrace}</pre>";
                ret = false;
            }
            return ret;
        }

        public void ProcessRequest(HttpContext context)
        {
            SPContext ctx = SPContext.GetContext(context);
            string apPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
            bool succeed = true;
            string tempFolder = string.Empty;
            string pdfPath = string.Empty;
            try
            {
                var path = Path.Combine(apPath, @"bin\phantomjs.exe");
                var bytes = File.ReadAllBytes(path);

                var aspTemp = HttpRuntime.CodegenDir;

                tempFolder = Path.Combine(aspTemp, Guid.NewGuid().ToString("D").ToLower());
                if (!Directory.Exists(tempFolder))
                {
                    Directory.CreateDirectory(tempFolder);
                }

                Dictionary<string, string> files = this.saveResourcesToDisk(tempFolder);

                pdfPath = Path.Combine(tempFolder, "auto-generated.pdf");

                string exeArgs = $"\"{files["Script"]}\" http://govconnect/Pages/Health-Matters.aspx \"{pdfPath}\" --config=\"{files["Config"]}\"";
                succeed = this.runEex(path, exeArgs);
            }
            catch (Exception ex)
            {
                msg += $"<br />Exception: {ex.Message}";
                msg += $"<br /><pre>{ex.Source}</pre>";
                msg += $"<br /><pre>{ex.StackTrace}</pre>";
                succeed = false;
            }

            if (succeed && !File.Exists(pdfPath))
            {
                msg += $"<br />Pdf file \"{pdfPath}\" does not exist.";
                succeed = false;
            }

            if (succeed)
            {
                var pdfBytes = File.ReadAllBytes(pdfPath);
                /*
                 * Content-Type: application/pdf
Content-Length: xxxxx
Content-Disposition: attachment;filename='downloaded.pdf'
*/
                context.Response.ContentType = "application/pdf";
                context.Response.Headers.Add("Content-Disposition", "attachment;filename='auto-generated.pdf'");
                context.Response.Headers.Add("Content-Length", pdfBytes.Length.ToString());
                context.Response.WriteFile(pdfPath);
            }
            else
            {
                context.Response.ContentType = "text/html";
                context.Response.Output.WriteLine(@"<html>
    <head><title>Its alive</title></head>
    <body>
        <h1>Error generatin PDF file</h1>
        <div>{0}</div>
    </body>
</html>", msg);
            }
            context.Response.Flush();
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }
        }

        public bool IsReusable => false;
    }
}







