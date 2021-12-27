using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SortPDFOnGDrive;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;
using File = Google.Apis.Drive.v3.Data.File;


// If modifying these scopes, delete your previously saved credentials
// at ~/.credentials/drive-dotnet-quickstart.json



var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<Program>();

var configuration = builder.Build();
var services = new ServiceCollection()
    .Configure<UserSecrets>(configuration.GetSection(nameof(UserSecrets)))
    .AddOptions()
    .BuildServiceProvider();

var myConf = services.GetService<IOptions<UserSecrets>>();
var secrets = myConf.Value;
List<PdfFileInfo> results = new();
if (string.IsNullOrEmpty(secrets.LocalOverload))
{
    var files = GetFilesFromGoogleDrive(myConf.Value.Credentials);
}
else
{
    results = GetInfoFromLocal(secrets.LocalOverload);

}

var table = new Table();
table.SetHeaders("Fichier","Auteur","Createur","Producteur");
foreach (var info in results)
{
    table.AddRow(Path.GetFileNameWithoutExtension(info.File), info.Author, info.Creator,info.Producer);

}
Console.WriteLine(table.ToString());

List<PdfFileInfo> GetInfoFromLocal(string localOverload)
{
    List<PdfFileInfo> results = new List<PdfFileInfo>();
    foreach (var f in Directory.GetFiles(localOverload, "*.pdf"))
    {
        Stream file2 = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.Read);
        var info = ExtractPDFInfoAndText(file2, f);
        results.Add(info);
    }
    return results;
}

IList<File>? GetFilesFromGoogleDrive(string credentialFiles)
{
    string applicationName = "SortPDFOnGDrive";
    string[] scopes = { DriveService.Scope.DriveReadonly };
    var credential = GoogleCredential.FromFile(credentialFiles).CreateScoped(scopes);

    var service = new DriveService(new BaseClientService.Initializer()
    {
        HttpClientInitializer = credential,
        ApplicationName = applicationName,
    });

    // Define parameters of request.
    FilesResource.ListRequest listRequest = service.Files.List();
    listRequest.PageSize = 10;
    listRequest.Fields = "nextPageToken, files(id, name)";

    // List files.
    IList<Google.Apis.Drive.v3.Data.File> list = listRequest.Execute()
        .Files;
    Console.WriteLine("Files:");
    if (list != null && list.Count > 0)
    {
        foreach (var file in list)
        {
            Console.WriteLine("{0} ({1})", file.Name, file.Id);
        }
    }
    else
    {
        Console.WriteLine("No files found.");
    }

    Console.Read();
    return list;
}

PdfFileInfo ExtractPDFInfoAndText(Stream stream, string file)
{
    PdfLoadedDocument loadedDocument = new PdfLoadedDocument(stream);
    string path = Path.GetFullPath(file);
    var di = loadedDocument.DocumentInformation;
    var pdfFileInfo = new PdfFileInfo(file);
    pdfFileInfo.Author = di.Author;
    pdfFileInfo.Producer = di.Producer;
    pdfFileInfo.Creator = di.Creator;
    string text =string.Empty, textLayout=string.Empty;
    for (int i = 0; i < loadedDocument.PageCount; i++)
    {
        PdfPageBase p = loadedDocument.Pages[i];
        text += p.ExtractText(false) + Environment.NewLine;
        textLayout += p.ExtractText((true)) + Environment.NewLine;
    }

    System.IO.File.WriteAllText(path+ Path.GetFileNameWithoutExtension(file) + ".txt",text);
    System.IO.File.WriteAllText(path + Path.GetFileNameWithoutExtension(file) + "-layout.txt", textLayout);
    loadedDocument.Close(true);
    return pdfFileInfo;
}