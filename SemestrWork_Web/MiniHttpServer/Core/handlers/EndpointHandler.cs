using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MiniHttpServer.Sharer.Core.abstracts;
using MiniHttpServer.Sharer.Core.Attributes;

namespace MiniHttpServer.Core
{
    internal class EndpointHandler : Handler
    {
        public override async Task HandleRequest(HttpListenerContext context)
        {
            if (context?.Request == null || context.Response == null)
            {
                Console.WriteLine("❌ Context, Request or Response is null");
                return;
            }

            var request = context.Request;
            var response = context.Response;

            string path = request.Url?.AbsolutePath ?? "/";
            bool isStaticFile = path.Contains('.') && !path.EndsWith("/");
            if (isStaticFile)
            {
                if (Successor != null)
                {
                    await Successor.HandleRequest(context);
                }
                return;
            }

            var pathSegments = request.Url?.AbsolutePath.Split('/')
                                    .Where(s => !string.IsNullOrEmpty(s))
                                    .ToArray() ?? Array.Empty<string>();

            if (pathSegments.Length == 0)
            {                
                var assembler = Assembly.GetExecutingAssembly();
                var endpointTypes = assembler.GetTypes()
                    .FirstOrDefault(t => t.GetCustomAttribute<EndPointAttribute>() != null);

                if (endpointTypes != null)
                {
                    var methodEndpoint = FindMethod(endpointTypes, "GET", methodName: null);
                    if (methodEndpoint != null)
                    {
                        try
                        {
                            var endpointInstance = Activator.CreateInstance(endpointTypes);
                            var result = methodEndpoint.Invoke(endpointInstance, null);
                            await HandleMethodResult(result, response);
                            return;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка вызова Index(): {ex.Message}");
                            response.StatusCode = 500;
                            await WriteResponse(response, "Ошибка сервера");
                            return;
                        }
                    }
                }

                
                if (Successor != null)
                    await Successor.HandleRequest(context);
                return;
            }

            var endpointName = pathSegments[0];
            var methodName = pathSegments.Length > 1 ? pathSegments[1] : null;

            var assembly = Assembly.GetExecutingAssembly();
            var endpointType = assembly.GetTypes()
                .Where(x => x.GetCustomAttribute<EndPointAttribute>() != null)
                .FirstOrDefault(end => IsCheckedNameEndpoint(end.Name, endpointName));

            if (endpointType == null)
            {
                if (Successor != null)
                    await Successor.HandleRequest(context);
                return;
            }

            var method = FindMethod(endpointType, request.HttpMethod, methodName);
            if (method == null)
            {
                response.StatusCode = 404;
                await WriteResponse(response, "Method not found");
                return;
            }

            try
            {
                var endpointInstance = Activator.CreateInstance(endpointType);
                
                var parameters = method.GetParameters();

                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(HttpListenerContext))
                {                    
                    method.Invoke(endpointInstance, new object[] { context });
                }
                else
                {
                    object? result = null;
                    if (request.HttpMethod == "POST")
                    {
                        result = await HandlePostRequest(method, request, endpointInstance, context);
                    }
                    else
                    {
                        result = HandleGetRequest(method, request, endpointInstance);
                    }

                    if (result != null)
                    {
                        await HandleMethodResult(result, response);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error invoking method: {ex.Message}");
                response.StatusCode = 500;
                await WriteResponse(response, $"Error: {ex.Message}");
            }
        }

        private MethodInfo? FindMethod(Type endpointType, string httpMethod, string? methodName)
        {
            Console.WriteLine($"Поиск метода в {endpointType.Name}:");
            Console.WriteLine($"   - methodName: '{methodName}'");
            Console.WriteLine($"   - httpMethod: '{httpMethod}'");

            var methods = endpointType.GetMethods()
                .Where(m => m.GetCustomAttributes(true)
                    .Any(attr => attr.GetType().Name.Equals($"Http{httpMethod}", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var method in methods)
            {
                var attrs = method.GetCustomAttributes(true);
                foreach (var attr in attrs)
                {
                    if (attr is HttpGet getAttr)
                    {
                        Console.WriteLine($"   - {method.Name} [HttpGet(\"{getAttr.Route}\")]");
                    }
                    else
                    {
                        Console.WriteLine($"   - {method.Name} [{attr.GetType().Name}]");
                    }
                }
            }

            if (methodName != null)
            {
                Console.WriteLine($"Поиск метода по имени: '{methodName}'");

                var method = methods.FirstOrDefault(m =>
                    m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase) ||
                    HasRouteAttribute(m, methodName));

                if (method != null)
                {
                    Console.WriteLine($"Найден метод: {method.Name}");
                }
                else
                {
                    Console.WriteLine($"Метод не найден: '{methodName}'");
                }

                return method;
            }

            var defaultMethod = methods.FirstOrDefault(m => !HasSpecificRoute(m));
            Console.WriteLine(defaultMethod != null
                ? $"Найден метод по умолчанию: {defaultMethod.Name}"
                : "Метод по умолчанию не найден");

            return defaultMethod;
        }

        private bool HasRouteAttribute(MethodInfo method, string route)
        {
            var attributes = method.GetCustomAttributes(true);
            foreach (var attr in attributes)
            {
                if (attr is HttpGet getAttr && getAttr.Route == route)
                    return true;
                if (attr is HttpPost postAttr && postAttr.Route == route)
                    return true;
            }
            return false;
        }

        private bool HasSpecificRoute(MethodInfo method)
        {
            var attributes = method.GetCustomAttributes(true);
            foreach (var attr in attributes)
            {
                if (attr is HttpGet getAttr && !string.IsNullOrEmpty(getAttr.Route))
                    return true;
                if (attr is HttpPost postAttr && !string.IsNullOrEmpty(postAttr.Route))
                    return true;
            }
            return false;
        }

        private async Task<object?> HandlePostRequest(MethodInfo method, HttpListenerRequest request, object endpointInstance, HttpListenerContext context)
        {
            var parameters = method.GetParameters();
            
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(HttpListenerContext))
            {
                return method.Invoke(endpointInstance, new object[] { context });
            }

            if (parameters.Length == 0)
                return method.Invoke(endpointInstance, null);

            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var body = await reader.ReadToEndAsync();

            if (!string.IsNullOrEmpty(body) && body.Trim().StartsWith("{"))
            {
                try
                {
                    var jsonDocument = JsonDocument.Parse(body);
                    var paramValues = new object[parameters.Length];

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];

                        if (jsonDocument.RootElement.TryGetProperty(param.Name!, out var property))
                        {
                            paramValues[i] = JsonSerializer.Deserialize(property.GetRawText(), param.ParameterType)
                                ?? GetDefaultValue(param.ParameterType);
                            Console.WriteLine($"Parameter {param.Name} found: {property}");
                        }
                        else
                        {
                            paramValues[i] = GetDefaultValue(param.ParameterType);
                            Console.WriteLine($"Parameter {param.Name} not found in JSON");
                        }
                    }

                    return method.Invoke(endpointInstance, paramValues);
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"JSON parse error: {jsonEx.Message}");
                }
            }

            return method.Invoke(endpointInstance, null);
        }

        private object? HandleGetRequest(MethodInfo method, HttpListenerRequest request, object endpointInstance)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0)
                return method.Invoke(endpointInstance, null);

            var queryParams = System.Web.HttpUtility.ParseQueryString(request.Url?.Query ?? "");
            var paramValues = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var value = queryParams[param.Name];
                var targetType = param.ParameterType;

                if (value == null)
                {
                    paramValues[i] = GetDefaultValue(targetType);
                    continue;
                }
                
                Type underlyingType = Nullable.GetUnderlyingType(targetType);
                if (underlyingType != null)
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        paramValues[i] = null;
                    }
                    else
                    {
                        try
                        {
                            paramValues[i] = Convert.ChangeType(value, underlyingType);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Не удалось преобразовать параметр '{param.Name}' = '{value}' к типу {underlyingType}: {ex.Message}");
                            paramValues[i] = null; 
                        }
                    }
                }
                else
                {                    
                    try
                    {
                        paramValues[i] = Convert.ChangeType(value, targetType);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠Ошибка преобразования параметра '{param.Name}' = '{value}' к типу {targetType}: {ex.Message}");
                        paramValues[i] = GetDefaultValue(targetType);
                    }
                }
            }

            return method.Invoke(endpointInstance, paramValues);
        }

        private async Task HandleMethodResult(object? result, HttpListenerResponse response)
        {
            if (result == null)
            {
                response.StatusCode = 204;
                response.Close(); 
                return;
            }

            try
            {
                if (result is string stringResult)
                {
                    response.StatusCode = 200;
                    response.ContentType = "text/html; charset=utf-8";
                    var buffer = Encoding.UTF8.GetBytes(stringResult);
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
                else
                {
                    response.StatusCode = 200;
                    response.ContentType = "application/json; charset=utf-8";

                    string json;
                    try
                    {
                        json = JsonSerializer.Serialize(result, new JsonSerializerOptions
                        {
                            WriteIndented = false,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        });
                    }
                    catch (Exception serializeEx)
                    {
                        Console.WriteLine($"Ошибка сериализации: {serializeEx.Message}");
                        json = $"{{\"error\": \"Ошибка при сериализации данных: {serializeEx.Message}\"}}";
                        response.StatusCode = 500;
                    }

                    var buffer = Encoding.UTF8.GetBytes(json);
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке ответа: {ex.Message}");
                response.StatusCode = 500;
                var errorBuffer = Encoding.UTF8.GetBytes($"{{\"error\": \"Неизвестная ошибка сервера: {ex.Message}\"}}");
                await response.OutputStream.WriteAsync(errorBuffer, 0, errorBuffer.Length);
            }
            finally
            {
                response.Close();
            }
        }

        private async Task WriteResponse(HttpListenerResponse response, string message)
        {
            response.ContentType = "text/plain; charset=utf-8";
            var buffer = Encoding.UTF8.GetBytes(message);
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }

        private object? GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private bool IsCheckedNameEndpoint(string endpointName, string className) =>
            endpointName.Equals(className, StringComparison.OrdinalIgnoreCase) ||
            endpointName.Equals($"{className}Endpoint", StringComparison.OrdinalIgnoreCase);
    }
}