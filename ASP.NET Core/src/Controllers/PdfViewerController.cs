using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Syncfusion.EJ2.PdfViewer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EJ2APIServices_NET8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PdfViewerController : ControllerBase
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IMemoryCache _cache;

        public PdfViewerController(IWebHostEnvironment hostingEnvironment, IMemoryCache cache)
        {
            _hostingEnvironment = hostingEnvironment;
            _cache = cache;
        }

        [HttpPost("Load")]
        public IActionResult Load([FromBody] JsonElement jsonObject)
        {
            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            MemoryStream stream = new MemoryStream();

            Dictionary<string, string> jsonData = ToStringDictionary(jsonObject);

            if (jsonData.ContainsKey("document"))
            {
                bool isFileName = jsonData.TryGetValue("isFileName", out var isFileNameValue)
                    && bool.TryParse(isFileNameValue, out var parsedIsFileName)
                    && parsedIsFileName;

                if (isFileName)
                {
                    string documentPath = GetDocumentPath(jsonData["document"]);
                    if (!string.IsNullOrEmpty(documentPath))
                    {
                        byte[] bytes = System.IO.File.ReadAllBytes(documentPath);
                        stream = new MemoryStream(bytes);
                    }
                    else
                    {
                        return Content(jsonData["document"] + " not found");
                    }
                }
                else
                {
                    byte[] bytes = Convert.FromBase64String(jsonData["document"]);
                    stream = new MemoryStream(bytes);
                }
            }

            object result = pdfviewer.Load(stream, jsonData);
            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        [HttpPost("RenderPdfPages")]
        public IActionResult RenderPdfPages([FromBody] JsonElement jsonObject)
        {
            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            Dictionary<string, string> jsonData = ToStringDictionary(jsonObject);
            object result = pdfviewer.GetPage(jsonData);
            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        [HttpPost("RenderPdfTexts")]
        public IActionResult RenderPdfTexts([FromBody] JsonElement jsonObject)
        {
            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            Dictionary<string, string> jsonData = ToStringDictionary(jsonObject);
            object result = pdfviewer.GetDocumentText(jsonData);
            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        [HttpPost("RenderThumbnailImages")]
        public IActionResult RenderThumbnailImages([FromBody] JsonElement jsonObject)
        {
            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            Dictionary<string, string> jsonData = ToStringDictionary(jsonObject);
            object result = pdfviewer.GetThumbnailImages(jsonData);
            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        [HttpPost("RenderAnnotationComments")]
        public IActionResult RenderAnnotationComments([FromBody] JsonElement jsonObject)
        {
            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            Dictionary<string, string> jsonData = ToStringDictionary(jsonObject);
            object result = pdfviewer.GetAnnotationComments(jsonData);
            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        [HttpPost("Bookmarks")]
        public IActionResult Bookmarks([FromBody] JsonElement jsonObject)
        {
            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            Dictionary<string, string> jsonData = ToStringDictionary(jsonObject);
            object result = pdfviewer.GetBookmarks(jsonData);
            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        [HttpPost("Download")]
        public IActionResult Download([FromBody] JsonElement jsonObject)
        {
            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            Dictionary<string, string> jsonData = ToStringDictionary(jsonObject);
            string result = pdfviewer.GetDocumentAsBase64(jsonData);
            return Content(result);
        }

        [HttpPost("PrintImages")]
        public IActionResult PrintImages([FromBody] JsonElement jsonObject)
        {
            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            Dictionary<string, string> jsonData = ToStringDictionary(jsonObject);
            object result = pdfviewer.GetPrintImage(jsonData);
            return Content(JsonConvert.SerializeObject(result), "application/json");
        }

        [HttpPost("Unload")]
        public IActionResult Unload([FromBody] JsonElement jsonObject)
        {
            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            Dictionary<string, string> jsonData = ToStringDictionary(jsonObject);
            pdfviewer.ClearCache(jsonData);
            return Content("Cache cleared");
        }

        private static Dictionary<string, string> ToStringDictionary(JsonElement jsonObject)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (JsonProperty prop in jsonObject.EnumerateObject())
            {
                result[prop.Name] = JsonElementToString(prop.Value);
            }

            return result;
        }

        private static string JsonElementToString(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? string.Empty,
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => value.ToString(),
                JsonValueKind.Null => string.Empty,
                _ => value.GetRawText()
            };
        }

        private string GetDocumentPath(string document)
        {
            string documentPath = string.Empty;

            if (!System.IO.File.Exists(document))
            {
                var path = _hostingEnvironment.ContentRootPath;
                if (System.IO.File.Exists(Path.Combine(path, "Data", document)))
                {
                    documentPath = Path.Combine(path, "Data", document);
                }
            }
            else
            {
                documentPath = document;
            }

            return documentPath;
        }
    }
}