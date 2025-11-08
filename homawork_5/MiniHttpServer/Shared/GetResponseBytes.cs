using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Shared
{
    internal class GetResponseBytes
    {
        public static byte[]? Invoke(string path)
        {
            try
            {
                var staticRoot = "/static";
             
                if (string.IsNullOrEmpty(path) || path == "/")                
                    path = "/index.html";                

                var fullPath = Path.Combine(staticRoot, path);

                if (!File.Exists(fullPath))
                {
                    Console.WriteLine($"File not found: {fullPath}");

                    if (Directory.Exists(fullPath))
                    {
                        var indexHtmlPath = Path.Combine(fullPath, "index.html");
                        if (File.Exists(indexHtmlPath))
                        {
                            return File.ReadAllBytes(indexHtmlPath);
                        }
                    }

                    return null;
                }

                return File.ReadAllBytes(fullPath);
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("Директория не найдена");
                return null;
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Файл не найден");
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка при чтении файла: {e.Message}");
                return null;
            }
        }
    }
}