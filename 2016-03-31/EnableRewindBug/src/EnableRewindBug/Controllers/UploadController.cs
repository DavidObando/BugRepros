using System;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EnableRewindBug.Controllers
{
    [Route("api/upload")]
    public class UploadController : Controller
    {
        public IActionResult Post()
        {
            if (!HasMultipartFormContentType(Request.ContentType))
            {
                return BadRequest("Expecting a multipart content type for the Upload POST command");
            }

            PrintLine("Processing a POST request to /api/upload");

            var form = Request.Form;
            PrintLine($"File count: { form.Files.Count }");
            foreach(var file in form.Files)
            {
                PrintLine($"Read { file.Length } bytes [{ file.FileName }]");
                SaveFile(file);
            }

            PrintLine("Done");

            return Ok();
        }

        private void SaveFile(IFormFile file)
        {
            var filePath = $"C:\\temp\\{ file.FileName }-Server";
            PrintLine($"Writing file to { filePath }");
            using (var fout = new System.IO.FileStream(filePath, System.IO.FileMode.CreateNew))
            {
                var buffer = new byte[4096];
                var read = 0;
                var fin = file.OpenReadStream();
                do
                {
                    read = fin.Read(buffer, 0, buffer.Length);
                    fout.Write(buffer, 0, read);
                } while (read > 0);
            }
            PrintLine($"Done writing { filePath }");
        }

        private static bool HasMultipartFormContentType(string contentType)
        {
            return contentType != null && contentType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase);
        }

        private void PrintLine(string input, params object[] paramStrings)
        {
            Console.Write($"[{ DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) }] ");
            Console.WriteLine(input, paramStrings);
        }
    }
}
