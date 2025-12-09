using MiniHttpServer.Core.abstracts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MiniHttpServer.Shared
{
    /// <summary>
    /// Реализация рендерера HTML шаблонов с поддержкой интерполяции, условий и циклов
    /// </summary>
    public class HtmlTemplateRenderer : IHtmlTemplateRenderer
    {
        public string RenderFromString(string htmlTemplate, object dataModel)
        {
            if (htmlTemplate == null || dataModel == null)
                return htmlTemplate;

            var result = htmlTemplate;

            // Замена if else и циклов Foreach 
            string previous = result;
            result = ProcessIfElseConditions(result, dataModel);
            result = ProcessForeachLoops(result, dataModel);

            // Цикл будет продолжаться до тех пор пока происходят изменения
            while (result != previous)
            {
                previous = result;
                result = ProcessForeachLoops(previous, dataModel);
                result = ProcessIfElseConditions(result, dataModel);
            }            

            // Замена Интерполяций
            result = ReplaceInterpolation(result, dataModel);

            return result;
        }

        public string RenderFromFile(string filePath, object dataModel)
        {
            try
            {
                // чтение файла и перевод для строчного представления
                var htmlTemplate = File.ReadAllText(filePath);

                return RenderFromString(htmlTemplate, dataModel);
            }
            catch (FileLoadException e) 
            { 
                Console.WriteLine("Ошибка при загрузке файла");
                return "";
            }
            catch (Exception e) 
            { 
                Console.WriteLine(e.Message);
                return "";
            }
        }

        public string RenderToFile(string inputFilePath, string outputFilePath, object dataModel)
        {
            try
            {
                var result = RenderFromFile(inputFilePath, dataModel);

                // Запись html на новый файл
                File.WriteAllText(outputFilePath, result);
                return result;
            }
            catch (FileLoadException e)
            {
                Console.WriteLine("Ошибка при загрузке файла");
                return "";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return "";
            }
        }

        /// <summary>
        /// Заменяет интерполяции ${property} в шаблоне на значения из модели
        /// </summary>
        private string ReplaceInterpolation(string htmlTemplate, object dataModel)
        {
            // StringBuilder для сборки имени свойства
            var wordAssemble = new StringBuilder();
            // Флаг нахождения внутри интерполяции ${...}
            bool asseblerFlag = false;
            // Начальная позиция интерполяции
            int startIndex = 0;

            for (int i = 0;  i < htmlTemplate.Length; i++)
            {
                // Проверка конца интерполяции }
                if (htmlTemplate[i] == '}')
                {
                    // Замена интерполяции на значение
                    string replacement = GetPropertyValue(wordAssemble.ToString(), dataModel).ToString();
                    htmlTemplate = ReplaceWord(htmlTemplate, startIndex, i, replacement);

                    asseblerFlag = false;
                    wordAssemble.Clear();
                    i = startIndex + replacement.Length - 1;
                }

                // Если внутри интерполяции, добавляем символ к имени свойства  
                if (asseblerFlag)
                    wordAssemble.Append(htmlTemplate[i]);

                // Проверка начала интерполяции ${
                if (i < htmlTemplate.Length - 1 && htmlTemplate[i] == '$' && htmlTemplate[i + 1] == '{')
                {
                    asseblerFlag = true;
                    startIndex = i;
                    i++;
                    continue;
                }             
            }

            return htmlTemplate;
        }

        /// <summary>
        /// Обрабатывает циклы foreach в шаблоне
        /// </summary>
        private string ProcessForeachLoops(string template, object dataModel)
        {
            // Регулярное выражение для поиска циклов foreach
            template = Regex.Replace(template,
                @"\$foreach\(([^)]+)\)\s*(.*?)\s*\$endfor",
                match =>
                {
                    string loopExpression = match.Groups[1].Value.Trim();
                    string loopContent = match.Groups[2].Value;

                    return ProcessForeachLoop(loopExpression, loopContent, dataModel);
                },
                RegexOptions.Singleline);

            return template;
        }

        /// <summary>
        /// Обрабатывает отдельный цикл foreach
        /// </summary>
        private string ProcessForeachLoop(string loopExpression, string loopContent, object dataModel)
        {
            // Разбиение выражения на части: item, collection
            var parts = loopExpression.Split(new[] { " in " }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                return loopContent;

            string itemName = parts[0].Trim();
            string collectionPath = parts[1].Trim();

            var collection = GetPropertyValue(collectionPath, dataModel) as IEnumerable;

            if (collection == null)
                return "";

            var result = new StringBuilder();

            // Заполнение содержимым коллекции
            foreach (var item in collection)
            {
                // Создаем объект с динамическим свойством, имя которого берется из itemName
                var iterationData = new Dictionary<string, object>
                {
                    [itemName] = item
                };

                string itemResult = ReplaceInterpolation(loopContent, iterationData);
                result.Append(itemResult + "\n");
            }

            return result.ToString();
        }

        /// <summary>
        /// Обрабатывает условные конструкции if/else в шаблоне
        /// </summary>
        private string ProcessIfElseConditions(string template, object dataModel)
        {
            // Обработка конструкций if-else-endif
            template = Regex.Replace(template,
                @"\$if\(([^)]+)\)\s*(.*?)\s*\$else\s*(.*?)\s*\$endif",
                match =>
                {
                    string condition = match.Groups[1].Value.Trim();
                    string ifContent = match.Groups[2].Value;
                    string elseContent = match.Groups[3].Value;

                    bool conditionResult = EvaluateCondition(dataModel, condition);
                    return conditionResult ? ifContent : elseContent;
                },
                RegexOptions.Singleline);

            // Обработка конструкций if-endif без else
            template = Regex.Replace(template,
                @"\$if\(([^)]+)\)\s*(.*?)\s*\$endif",
                match =>
                {
                    string condition = match.Groups[1].Value.Trim();
                    string ifContent = match.Groups[2].Value;

                    bool conditionResult = EvaluateCondition(dataModel, condition);
                    return conditionResult ? ifContent : "";
                },
                RegexOptions.Singleline);

            return template;
        }

        /// <summary>
        /// Вычисляет значение условия для условных конструкций
        /// </summary>
        private bool EvaluateCondition(object dataModel, string condition)
        {
            // Получение значения условия из модели
            var value = GetPropertyValue(condition, dataModel);

            // Если значение bool, возвращаем его, иначе проверяем на null
            return value is bool ? (bool)value : value != null;
        }

        /// <summary>
        /// Получает значение свойства из модели данных по пути (property.subproperty)
        /// </summary>
        private object GetPropertyValue(string propertyPath, object dataModel)
        {
            // Если dataModel - это словарь, то возвращаем его значение для цикла Foreach
            if (dataModel is IDictionary<string, object> dict)
            {
                if (dict.Values.Count == 1)
                {
                    dataModel = dict.FirstOrDefault().Value;
                    var temp = propertyPath.Split('.').Skip(1);
                    propertyPath = string.Join("", temp);
                }                                                       
                else if (dict.TryGetValue(propertyPath, out var value))
                    return value;                
            }

            try
            {
                var properties = propertyPath.ToString().Split('.');
                object current = dataModel;
                foreach (var property in properties)
                {
                    if (current == null) return null;

                    var propInfo = current.GetType()
                                          .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                          .FirstOrDefault(p => string.Equals(p.Name, property, StringComparison.OrdinalIgnoreCase));

                    if (propInfo == null) return null;

                    current = propInfo.GetValue(current);
                }

                return current;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message, dataModel);
                return null;
            }                       
        }

        /// <summary>
        /// Заменяет подстроку в исходной строке
        /// </summary>

        private string ReplaceWord(string original, int startIndex, int endIndex, string replacement)
        {
            string before = original.Substring(0, startIndex);
            string after = original.Substring(endIndex + 1);

            return before + replacement + after;
        }
    }
}
