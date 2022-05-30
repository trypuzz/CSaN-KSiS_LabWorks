using System.Text.Json;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FileStorage
{
    class FileStorage
    {
        static void Main()
        {
            var server = new Http_server();
            Task.Run(() => server.Start());
            Console.ReadLine();
        }
    }
    public class Http_server
    {

        public void Start()
        {
            var httpListenerr = new HttpListener();
            try
            {
                httpListenerr.Prefixes.Add("http://localhost:8000/");
                httpListenerr.Start();
                Console.WriteLine("Ready to Listen");

                while (true)
                {
                    HttpListenerContext context = httpListenerr.GetContext();
                    var request = context.Request.HttpMethod;
                    Console.WriteLine($"\n{request}");
                    HttpListenerResponse response = context.Response;
                    try
                    {
                        switch (request)
                        {
                            case "GET":
                                {
                                    GET_Function(context.Request, response);
                                    break;
                                }
                            case "PUT":
                                {
                                    PUT_Function(context.Request, response);
                                    break;
                                }
                            case "HEAD":
                                {
                                    HEAD_Function(context.Request, response);
                                    break;
                                }
                            case "DELETE":
                                {
                                    DELETE_Function(context.Request, response);
                                    break;
                                }
                            default:
                                {
                                    Console.WriteLine("Unexpected command");
                                    break;
                                }
                        }
                    }
                    catch
                    {
                        response.StatusCode = 404;
                        Console.WriteLine($"Error: {response.StatusCode} Not Found");
                    }
                }
            }
            finally
            {
                httpListenerr.Stop();
            }
        }


        public void GET_Function(HttpListenerRequest request, HttpListenerResponse response)
        {
            Stream output = response.OutputStream;
            var writer = new StreamWriter(output);
            string fullPath = Directory.GetCurrentDirectory() + request.RawUrl;

            try
            {
                String getPath = request.Url.LocalPath;
                var localPath = getPath.Substring(1);
                FileInfo fileToDownload = new FileInfo(localPath);
                if (fileToDownload.Exists) //rawurl
                {
                    response.ContentType = "application/force-download";
                    response.Headers.Add("Content-Transfer-Encoding", "binary");
                    response.Headers.Add("Content-Disposition", $"attachment; filename={fileToDownload.Name}");
                    using (var outputt = response.OutputStream)
                    {
                        response.ContentLength64 = fileToDownload.Length;
                        var buffer = File.ReadAllBytes(localPath);
                        outputt.Write(buffer, 0, buffer.Length);
                    }

                    response.StatusCode = 200;
                    Console.WriteLine($"Success: {response.StatusCode} OK");
                    return; //copyto
                }
                

                if (File.Exists(fullPath))
                {
                    try
                    {
                        using (var file = File.Open(fullPath, FileMode.Open))
                        {
                            file.CopyTo(output);
                            file.Close();
                        }

                        response.StatusCode = 200;
                        Console.WriteLine($"Success: {response.StatusCode} OK");
                    }
                    catch
                    {
                        response.StatusCode = 500;
                        Console.WriteLine($"Error: {response.StatusCode} Internal Server Error");
                    }

                }

                if (!File.Exists(fullPath))
                {
                    try
                    {
                        var result = new List<object>();
                        foreach (var entry in Directory.GetDirectories(fullPath).Concat(Directory.GetFiles(fullPath)))
                        {
                            result.Add(new
                            {
                                Name = entry.Substring(Directory.GetCurrentDirectory().Length),
                                CreationTime = Directory.GetCreationTime(entry).GetDateTimeFormats('R')[0],
                            });
                        }

                        writer.Write(JsonSerializer.Serialize(result));
                        writer.Flush();

                        response.StatusCode = 200;
                        Console.WriteLine($"Success: {response.StatusCode} OK");
                    }
                    catch
                    {
                        response.StatusCode = 404;
                        Console.WriteLine($"Error: {response.StatusCode} Not Found");
                    }
                }


            }
            catch
            {
                response.StatusCode = 404;
                Console.WriteLine($"Error: {response.StatusCode} Not Found");
            }
            finally
            {
                response.OutputStream.Close();
                writer.Dispose();
            }
        }

        public void PUT_Function(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                String getPath = request.Url.LocalPath;
                var localPath = getPath.Substring(1);//.Replace("/_/", String.Empty);
                var index = localPath.LastIndexOf("/", StringComparison.Ordinal);
                var dirpath = localPath.Substring(0, index);

                if (!Directory.Exists(dirpath))
                {
                    Directory.CreateDirectory(dirpath);
                }
                using (var input = request.InputStream)
                {
                    FileStream fileStream = File.Create(localPath);
                    input.CopyTo(fileStream);
                    fileStream.Close();
                }

                response.StatusCode = 200;
                Console.WriteLine($"Success: {response.StatusCode} OK");

            }
            finally
            {
                response.OutputStream.Close();
            }


        }

        public void HEAD_Function(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                string fullPath = Directory.GetCurrentDirectory() + request.RawUrl;
                if (Directory.Exists(fullPath))
                {
                    var info = new DirectoryInfo(fullPath);
                    response.Headers.Add("Date", info.CreationTime.ToString());
                    response.Headers.Add("Name", info.Name.ToString());
                    response.Headers.Add("Directory", info.Root.ToString());
                    response.Headers.Add("LastWriteTime", info.LastWriteTime.ToString());

                    response.StatusCode = 200;
                    Console.WriteLine($"Success: {response.StatusCode} OK");
                }
                else if (File.Exists(fullPath))
                {
                    FileInfo info = new FileInfo(fullPath);
                    response.Headers.Add("Date", info.CreationTime.ToString());
                    response.Headers.Add("Name", info.Name.ToString());
                    response.Headers.Add("LastWriteTime", info.LastWriteTime.ToString());
                    response.Headers.Add("Size", info.Length.ToString());

                    response.StatusCode = 200;
                    Console.WriteLine($"Success: {response.StatusCode} OK");
                }
                else
                {
                    response.StatusCode = 404;
                    Console.WriteLine($"Error: {response.StatusCode} Not Found");
                }

            }
            finally
            {
                response.OutputStream.Close();
            }
        }

        public void DELETE_Function(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                string name = Directory.GetCurrentDirectory() + "/";
                string fullPath = Directory.GetCurrentDirectory() + request.RawUrl;
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);

                    response.StatusCode = 200;
                    Console.WriteLine($"Success: {response.StatusCode} OK");
                }
                else if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);

                    response.StatusCode = 200;
                    Console.WriteLine($"Success: {response.StatusCode} OK");
                }
                else
                {
                    response.StatusCode = 404;
                    Console.WriteLine($"Error: {response.StatusCode} Not Found");
                }
            }
            finally
            {
                response.OutputStream.Close();
            }
        }
    }
}
