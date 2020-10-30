using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using TestChunkedUpload.Model;

namespace TestChunkedUpload.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private const string TempUploadFolderName = "uploads";
        private const string UploadFolderName = "uploads";
        private readonly TimeSpan DeleteDelay = new TimeSpan(0, 10, 0);
        private readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".gif", ".png", ".txt" };

        [HttpPost]
        public ActionResult UploadChunk(IFormFile file, [FromForm] int index, [FromForm] int totalCount)
        {

            if (index > totalCount - 1 || index < 0 || totalCount < 1)
                return BadRequest("index out of range");

            if (file == null)
                return BadRequest(nameof(file) + " is null");

            var tempPath = Path.Combine(Path.GetTempPath(), TempUploadFolderName);

            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);
            else
                RemoveTempFilesAfterDelay(tempPath);

            try
            {
                CheckFileExtensionValid(file.FileName);

                var tempFilePath = Path.Combine(tempPath, $"{file.FileName}.{index}.tmp");

                Debug.WriteLine("Temp file: " + tempFilePath);

                // Overwrite existing chunk
                if (System.IO.File.Exists(tempFilePath)) 
                    System.IO.File.Delete(tempFilePath);

                using (var stream = new FileStream(tempFilePath, FileMode.CreateNew, FileAccess.Write))
                {
                    file.CopyTo(stream);
                }

                Debug.WriteLine("Temp file created");
                
                // If we are at the last chunk
                if (index == totalCount - 1)
                {
                    Debug.WriteLine("Merging chunks");

                    // Verify that all chunks were uploaded
                    var chunksPaths = Enumerable.Range(0, totalCount).Select(item => Path.Combine(tempPath, $"{file.FileName}.{item}.tmp")).ToArray();

                    if (chunksPaths.Any(chunk => !System.IO.File.Exists(chunk)))
                        return BadRequest("Cannot merge file with missing chunks");

                    // Merge chunks
                    using var writer = System.IO.File.OpenWrite(Path.Combine(UploadFolderName, file.FileName));
                    foreach (var chunk in chunksPaths)
                    {
                        Debug.WriteLine("Merging chunk: " + chunk);

                        using var reader = System.IO.File.OpenRead(chunk);
                        reader.CopyTo(writer);
                    }

                    foreach(var chunk in chunksPaths) {
                        Debug.WriteLine("Deleting chunk: " + chunk);
                        System.IO.File.Delete(chunk);
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok();
        }

        private void RemoveTempFilesAfterDelay(string path)
        {
            var dir = new DirectoryInfo(path);

            if (!dir.Exists) return;

            foreach (var file in dir.GetFiles("*.tmp").Where(f => f.LastWriteTimeUtc.Add(DeleteDelay) < DateTime.UtcNow))
                file.Delete();
        }

        private void CheckFileExtensionValid(string fileName)
        {
            fileName = fileName.ToLowerInvariant();

            var isValidExtenstion = AllowedExtensions.Any(ext => Path.GetExtension(fileName) == ext);
            if (!isValidExtenstion)
                throw new Exception("Not allowed file extension");
        }
        
    }
}
