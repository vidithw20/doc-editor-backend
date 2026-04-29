using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Pdf;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace EJ2APIServices.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RichTextEditorController : ControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public RichTextEditorController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }
        
        public class ExportParam
        {
            public string html { get; set; }
        }

        [HttpPost("ExportToDocs")]
        public IActionResult ExportToDocs([FromBody] ExportParam args)
        {
            if (string.IsNullOrWhiteSpace(args?.html))
                return BadRequest("HTML content is required.");

            using WordDocument document = new WordDocument();

            document.EnsureMinimal();

            document.HTMLImportSettings.ImageNodeVisited += OpenImage;

            bool isValidHtml = document.LastSection.Body.IsValidXHTML(
                args.html,
                XHTMLValidationType.None
            );

            if (isValidHtml)
            {
                document.Sections[0]
                    .Body
                    .Paragraphs[0]
                    .AppendHTML(args.html);
            }
            else
            {
                document.LastParagraph.AppendHTML(args.html);
            }

            document.HTMLImportSettings.ImageNodeVisited -= OpenImage;

            MemoryStream stream = new MemoryStream();

            document.Save(stream, FormatType.Docx);

            stream.Position = 0;

            return File(
                stream,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "Document.docx"
            );
        }

        [HttpPost("ExportToPdf")]
        public IActionResult ExportToPdf([FromBody] ExportParam args)
        {
            if (string.IsNullOrWhiteSpace(args?.html))
                return BadRequest("HTML content is required.");

            using WordDocument wordDocument = new WordDocument();

            wordDocument.EnsureMinimal();

            wordDocument.HTMLImportSettings.ImageNodeVisited += OpenImage;

            wordDocument.LastParagraph.AppendHTML(args.html);

            wordDocument.HTMLImportSettings.ImageNodeVisited -= OpenImage;

            using DocIORenderer renderer = new DocIORenderer();

            PdfDocument pdfDocument = renderer.ConvertToPDF(wordDocument);

            MemoryStream stream = new MemoryStream();

            pdfDocument.Save(stream);

            stream.Position = 0;

            return File(
                stream,
                "application/pdf",
                "Document.pdf"
            );
        }

        [HttpPost("ImportFromDocs")]
        public IActionResult ImportFromDocs(IList<IFormFile> UploadFiles)
        {
            if (UploadFiles == null || UploadFiles.Count == 0)
                return BadRequest("No files uploaded.");

            string htmlString = string.Empty;

            foreach (var file in UploadFiles)
            {
                string fileName = ContentDispositionHeaderValue
                    .Parse(file.ContentDisposition)
                    .FileName
                    .ToString()
                    .Trim('"');

                string savePath = Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    fileName
                );

                using FileStream fs = System.IO.File.Create(savePath);
                file.CopyTo(fs);
            }

            return Ok(htmlString);
        }

        private static void OpenImage(
            object sender,
            ImageNodeVisitedEventArgs args
        )
        {
            if (!string.IsNullOrWhiteSpace(args.Uri) &&
                args.Uri.StartsWith("http"))
            {
                using WebClient client = new WebClient();

                byte[] imageBytes = client.DownloadData(args.Uri);

                args.ImageStream = new MemoryStream(imageBytes);
            }
        }

        private string ExtractBodyContent(string html)
        {
            if (html.Contains("<body"))
            {
                int start = html.IndexOf("<body");

                start = html.IndexOf(">", start) + 1;

                int end = html.IndexOf("</body>");

                if (end > start)
                    return html.Substring(start, end - start);
            }

            return html;
        }

        private string SanitizeHtml(string html)
        {
            return Regex.Replace(html, @"[^\x20-\x7E]", " ");
        }
    }
}