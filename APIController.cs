using HttpMultipartParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Swan;

namespace EmbedStorage
{
    public class APIController : Controller
    {
        public APIController(IHttpContext context) : base(context)
        {

        }

        private string GetMimeByExtention(string ext)
        {
            if (string.IsNullOrEmpty(ext)) return "application/octet-stream";

            if (!MimeTypes.DefaultMimeTypes.ContainsKey(ext))
            {
                return "application/octet-stream";
            }

            return MimeTypes.DefaultMimeTypes[ext];
        }

        private async Task<bool> WrapFileAccess(Func<Task<bool>> access)
        {
            try
            {
                return await access();
            }
            catch (FormatException ex)
            {
                Terminal.Error(ex, ex.Source, ex.Message);
                return await Ok(new { error = -20, status = "file not exist" });
            }
            catch (System.IO.FileNotFoundException ex)
            {
                Terminal.Error(ex, ex.Source, ex.Message);
                Response.StatusCode = 404;
                return await Ok(new { error = -21, status = "file not exist" });
            }
            catch (ArgumentNullException ex)
            {
                Terminal.Error(ex, ex.Source, ex.Message);
                Response.StatusCode = 404;
                return await Ok(new { error = -22, status = "file not exist" });
            }
            catch (System.IO.IOException ex)
            {
                Terminal.Error(ex, ex.Source, ex.Message);
                Response.StatusCode = 503;
                return await Ok(new { error = -23, status = "resource busy" });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return await Error(ex);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/api/file/{fileid}")]
        public async Task<bool> File(string fileid)
        {
            return await WrapFileAccess(() =>
            {
                Guid guid = Guid.Parse(fileid);

                StorageModel storage = Program.FileStorages.FirstOrDefault(s => s.FileId == guid);

                if (storage == null)
                {
                    throw new System.IO.FileNotFoundException();
                }

                var fs = Program.GetWorkFileSystem().OpenFile(storage.Path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);

                Response.ContentType = GetMimeByExtention(storage.Extention);
                return Response.BinaryResponseAsync(fs, false);
            });
        }

        [WebApiHandler(HttpVerbs.Get, "/api/download/{fileid}")]
        public async Task<bool> Download(string fileid)
        {
            return await WrapFileAccess(() =>
            {
                Guid guid = Guid.Parse(fileid);

                StorageModel storage = Program.FileStorages.FirstOrDefault(s => s.FileId == guid);

                if (storage == null)
                {
                    throw new System.IO.FileNotFoundException();
                }

                var fs = Program.GetWorkFileSystem().OpenFile(storage.Path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                Response.AddHeader("Content-Disposition", "attachment;filename=" + System.Net.WebUtility.UrlEncode(storage.FileName));
                Response.AddHeader("Content-Length", storage.FileLength.ToString());
                Response.ContentType = GetMimeByExtention(storage.Extention);
                return Response.BinaryResponseAsync(fs, false);
            });
        }

        [WebApiHandler(HttpVerbs.Post, "/api/upload")]
        public async Task<bool> Upload()
        {
            try
            {
                var parser = new MultipartFormDataParser(Request.InputStream);

                if (parser.Files.Any() == false)
                {
                    return await Ok(new { error = -1, status = "no input file." });
                }

                var file = parser.Files[0];

                DateTime now = DateTime.Now;

                string dir = "/data/" + now.Year + "/" + now.Month + "/" + now.Day;

                if (!Program.GetWorkFileSystem().DirectoryExists(dir))
                {
                    Program.GetWorkFileSystem().CreateDirectory(dir);
                }

                Guid fileid = Guid.NewGuid();

                string ext = System.IO.Path.GetExtension(file.FileName);

                string filepath = dir + "/" + fileid + ext;

                using (var fs = Program.GetWorkFileSystem().OpenFile(filepath,
                    System.IO.FileMode.OpenOrCreate,
                    System.IO.FileAccess.ReadWrite))
                {
                    await file.Data.CopyToAsync(fs);
                }

                var storage = new StorageModel()
                {
                    DateExpires = DateTime.Now.AddYears(20),
                    Extention = ext,
                    FileId = fileid,
                    FileLength = file.Data.Length,
                    FileName = file.FileName,
                    UploadDate = DateTime.Now,
                    Path = filepath
                };

                Program.FileStorages.Add(storage);
                bool rs = Program.Save();

                if (rs)
                {
                    return await Ok(new { error = 0, status = "ok.", fileid = fileid });
                }
                else
                {
                    return await Ok(new { error = -2, status = "write database fail.", fileid = fileid });
                }

            }
            catch (Exception ex)
            {
                Terminal.Error(ex, ex.Source, ex.Message);
                return await Ok(new { error = -3, status = "upload fail.", fileid = "" });
            }
        }
    }
}
