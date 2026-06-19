using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Syncfusion.EJ2.PdfViewer;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

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
                        return NotFound(jsonData["document"] + " not found");
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

        [HttpPost("Merge")]
        [RequestSizeLimit(200_000_000)]
        public async Task<IActionResult> Merge([FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count < 2)
            {
                return BadRequest("Please upload at least two PDF files.");
            }

            using PdfDocument mergedDocument = new PdfDocument();

            foreach (IFormFile file in files)
            {
                if (file.Length == 0)
                {
                    continue;
                }

                if (!IsPdfFile(file))
                {
                    return BadRequest($"Invalid file type: {file.FileName}");
                }

                await using Stream uploadedStream = file.OpenReadStream();
                using MemoryStream inputStream = new MemoryStream();

                await uploadedStream.CopyToAsync(inputStream);
                inputStream.Position = 0;

                using PdfLoadedDocument loadedDocument = new PdfLoadedDocument(inputStream);

                if (loadedDocument.Pages.Count > 0)
                {
                    mergedDocument.ImportPageRange(
                        loadedDocument,
                        0,
                        loadedDocument.Pages.Count - 1
                    );
                }
            }

            if (mergedDocument.Pages.Count == 0)
            {
                return BadRequest("No valid PDF pages were found.");
            }

            using MemoryStream outputStream = new MemoryStream();
            mergedDocument.Save(outputStream);

            return File(
                outputStream.ToArray(),
                "application/pdf",
                "merged.pdf"
            );
        }

        [HttpPost("SaveEdited")]
        [RequestSizeLimit(200_000_000)]
        public async Task<IActionResult> SaveEdited(
            [FromForm] IFormFile file,
            [FromForm] string fileName
        )
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No PDF file was uploaded.");
            }

            if (!IsPdfFile(file))
            {
                return BadRequest("Only PDF files are allowed.");
            }

            string safeFileName = Path.GetFileName(
                string.IsNullOrWhiteSpace(fileName)
                    ? file.FileName
                    : fileName
            );

            if (string.IsNullOrWhiteSpace(safeFileName))
            {
                safeFileName = "edited.pdf";
            }

            if (!safeFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                safeFileName += ".pdf";
            }

            string dataFolder = Path.Combine(_hostingEnvironment.ContentRootPath, "Data");
            Directory.CreateDirectory(dataFolder);

            string outputPath = Path.Combine(dataFolder, safeFileName);

            await using FileStream outputStream = new FileStream(
                outputPath,
                FileMode.Create,
                FileAccess.Write
            );

            await file.CopyToAsync(outputStream);

            return Ok(new
            {
                message = "PDF saved successfully.",
                fileName = safeFileName
            });
        }

        private static bool IsPdfFile(IFormFile file)
        {
            if (file == null)
            {
                return false;
            }

            bool hasPdfExtension = file.FileName.EndsWith(
                ".pdf",
                StringComparison.OrdinalIgnoreCase
            );

            bool hasPdfContentType = string.Equals(
                file.ContentType,
                "application/pdf",
                StringComparison.OrdinalIgnoreCase
            );

            return hasPdfExtension || hasPdfContentType;
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
            if (string.IsNullOrWhiteSpace(document))
            {
                return string.Empty;
            }

            string safeFileName = Path.GetFileName(document);

            string dataFolder = Path.Combine(_hostingEnvironment.ContentRootPath, "Data");
            string documentPath = Path.Combine(dataFolder, safeFileName);

            if (System.IO.File.Exists(documentPath))
            {
                return documentPath;
            }

            return string.Empty;
        }
    }
}