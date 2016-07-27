using System.Web.Http;
using Owin;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin;

namespace CryptBitWeb
{
    public static class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public static void ConfigureApp(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            System.Net.ServicePointManager.DefaultConnectionLimit = 256;
            
           
            PhysicalFileSystem physicalFileSystem = new PhysicalFileSystem(@".\wwwroot");
            FileServerOptions fileOptions = new FileServerOptions();

            fileOptions.EnableDefaultFiles = true;
            fileOptions.RequestPath = PathString.Empty;
            fileOptions.FileSystem = physicalFileSystem;
            fileOptions.DefaultFilesOptions.DefaultFileNames = new[] { "index.html" };
            fileOptions.StaticFileOptions.FileSystem = fileOptions.FileSystem = physicalFileSystem;
            fileOptions.StaticFileOptions.ServeUnknownFileTypes = true;

            config.MapHttpAttributeRoutes();

            appBuilder.UseWebApi(config);
            appBuilder.UseFileServer(fileOptions);

            appBuilder.UseWebApi(config);
        }
    }
}
