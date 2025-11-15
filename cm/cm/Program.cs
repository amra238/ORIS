using System.Net;
using System.Text;

HttpListener server = new HttpListener();
server.Prefixes.Add("http://127.0.0.1:8888/connection/");

server.Start(); // начинаем прослушивать входящие подключения

// получаем контекст
var context = await server.GetContextAsync();
var path = context.Request.Url.AbsolutePath;


var response = context.Response;
// отправляемый в ответ код htmlвозвращает
string responseText =
    @"<!DOCTYPE html>
    <html>
        <head>
            <meta charset='utf8'>
            <title>METANIT.COM</title>
        </head>
        <body>
            <h2>Hello METANIT.COM</h2>
        </body>
    </html>";
byte[] buffer = Encoding.UTF8.GetBytes(responseText);
// получаем поток ответа и пишем в него ответ
response.ContentLength64 = buffer.Length;
using Stream output = response.OutputStream;
// отправляем данные
await output.WriteAsync(buffer);
await output.FlushAsync();

Console.WriteLine("Запрос обработан");

server.Stop();