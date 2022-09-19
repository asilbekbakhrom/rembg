using System.IO.Compression;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using RemoveBg.Models;

namespace RemoveBg.Controllers;

public class ServiceController : Controller
{
    private readonly ILogger<ServiceController> _logger;
    private readonly HttpClient _client;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private static List<Result>? results;

    public ServiceController(ILogger<ServiceController> logger, IWebHostEnvironment webHostEnvironment)
    {
        _logger = logger;
        _client = new HttpClient();
        _webHostEnvironment = webHostEnvironment;
    }

    public IActionResult RemoveBack() => View();

    [HttpPost]
    public async Task<IActionResult> RemoveBack(Photo photo)
    {
        var photos = new List<Result>();
        var folder = Environment.CurrentDirectory + "/images";
        Directory.CreateDirectory(folder);
        foreach(var i in photo.Images)
        {
            MultipartFormDataContent form = new MultipartFormDataContent();
            var stream = i.OpenReadStream();
            var content = new StreamContent(stream);
            content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "file",
                FileName = i.FileName
            };
            form.Add(content);
            var response = await _client.PostAsync("http://localhost:5000/", form);
            var image = Convert.ToBase64String(await response.Content.ReadAsByteArrayAsync());
            photos.Add(new Result(){ Photo = "data:image/png;base64," + image });
            FileInfo fi = new FileInfo(i.FileName);
            var newFileName = Guid.NewGuid() + ".png";
            var path = folder + "/" + newFileName;
            using (var stream2 = new FileStream(path, FileMode.Create))
            {
                await response.Content.CopyToAsync(stream2);
            }
        }

        ZipFile.CreateFromDirectory(folder, Path.Combine(Environment.CurrentDirectory, $"images.zip"));
        Directory.Delete(folder, true);
        
        return File(System.IO.File.ReadAllBytes(Path.Combine(Environment.CurrentDirectory, $"images.zip")), "application/zip");
        return View("ResultPhoto", photos);
    }
}
