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
                // Базовая директория со статическими файлами
                var staticRoot = "static";

                // Если путь пустой, используем index.html
                if (string.IsNullOrEmpty(path) || path == "/")
                {
                    path = "index.html";
                }

                // Строим полный путь к файлу
                var fullPath = Path.Combine(staticRoot, path);

                // Защита от directory traversal attacks
                fullPath = Path.GetFullPath(fullPath);
                var staticRootFullPath = Path.GetFullPath(staticRoot);

                if (!fullPath.StartsWith(staticRootFullPath))
                {
                    Console.WriteLine($"Access denied: attempted to access {fullPath}");
                    return null;
                }

                // Проверяем существование файла
                if (!File.Exists(fullPath))
                {
                    Console.WriteLine($"File not found: {fullPath}");

                    // Пробуем найти index.html в директории
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

                // Читаем файл
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