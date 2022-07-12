using Microsoft.Extensions.FileProviders;

namespace TASagentTwitchBot.Core.Extensions;

public static class StartupUtilities
{
    public static void UseDocumentsOverrideContent(this WebApplication app)
    {
        string wwwRootPath = BGC.IO.DataManagement.PathForDataDirectory("wwwroot");

        PhysicalFileProvider fileProvider = new PhysicalFileProvider(wwwRootPath);

        app.UseDefaultFiles(new DefaultFilesOptions
        {
            FileProvider = fileProvider,
            RequestPath = ""
        });

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            RequestPath = ""
        });
    }

    public static void UseCoreLibraryContent(
        this WebApplication app,
        string libraryName,
        bool useDefault = true)
    {
        string wwwRootPath;

        if (app.Environment.IsDevelopment())
        {
            //Navigate relative to the current path in Development
            string path = Directory.GetParent(app.Environment.ContentRootPath)!.FullName;
#warning DOTNET CORE 6 FIX
            //Behavior of Directory.GetParent(x) seems to have changed in DotNetCore 6.0.
            //Now Directory.GetParent("/path/to/dir/") returns "/path/to/dir" when it used to return "/path/to"
            path = Directory.GetParent(path)!.FullName;
            wwwRootPath = Path.Combine(
                path,
                "TASagentTwitchBotCore",
                libraryName,
                "wwwroot");
        }
        else
        {
            //Look in published "_content" directory
            wwwRootPath = Path.Combine(app.Environment.WebRootPath, "_content", libraryName);
        }

        PhysicalFileProvider fileProvider = new PhysicalFileProvider(wwwRootPath);

        if (useDefault)
        {
            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = fileProvider,
                RequestPath = ""
            });
        }

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            RequestPath = ""
        });
    }

    public static void UseLibraryContent(
        this WebApplication app,
        string libraryName,
        bool useDefault = true)
    {
        string wwwRootPath;

        if (app.Environment.IsDevelopment())
        {
            //Navigate relative to the current path in Development
            string path = Directory.GetParent(app.Environment.ContentRootPath)!.FullName;
#warning DOTNET CORE 6 FIX
            //Behavior of Directory.GetParent(x) seems to have changed in DotNetCore 6.0.
            //Now Directory.GetParent("/path/to/dir/") returns "/path/to/dir" when it used to return "/path/to"
            path = Directory.GetParent(path)!.FullName;
            wwwRootPath = Path.Combine(
                path,
                libraryName,
                "wwwroot");
        }
        else
        {
            //Look in published "_content" directory
            wwwRootPath = Path.Combine(app.Environment.WebRootPath, "_content", libraryName);
        }

        PhysicalFileProvider fileProvider = new PhysicalFileProvider(wwwRootPath);

        if (useDefault)
        {
            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = fileProvider,
                RequestPath = ""
            });
        }

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            RequestPath = ""
        });
    }
}
