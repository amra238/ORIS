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
    internal class HtmlTemplateRenderer : IHtmlTemplateRenderer
    {
        public string RenderFromString(string htmlTemplate, object dataModel)
        {
            if (htmlTemplate == null || dataModel == null)
                return htmlTemplate;

            var result = htmlTemplate;

            result = ReplaceInterpolation(result, dataModel);

            string previous = result;
            result = ProcessIfElseConditions(result, dataModel);
            result = ProcessForeachLoops(result, dataModel);

            while (result != previous)
            {
                previous = result;
                result = ProcessForeachLoops(previous, dataModel);
                result = ProcessIfElseConditions(result, dataModel);                
            }            

            return result;
        }

        public string RenderFromFile(string filePath, object dataModel)
        {
            try
            {
                var htmlTemplate = File.ReadAllText(filePath);

                var result = htmlTemplate;
                result = ReplaceInterpolation(result, dataModel);

                string previous = result;
                result = ProcessIfElseConditions(result, dataModel);
                result = ProcessForeachLoops(result, dataModel);

                while (result != previous)
                {
                    previous = result;
                    result = ProcessForeachLoops(previous, dataModel);
                    result = ProcessIfElseConditions(result, dataModel);
                }

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

        public string RenderToFile(string inputFilePath, string outputFilePath, object dataModel)
        {
            try
            {
                var htmlTemplate = File.ReadAllText(outputFilePath);

                var result = htmlTemplate;
                result = ReplaceInterpolation(result, dataModel);

                string previous = result;
                result = ProcessIfElseConditions(result, dataModel);
                result = ProcessForeachLoops(result, dataModel);

                while (result != previous)
                {
                    previous = result;
                    result = ProcessForeachLoops(previous, dataModel);
                    result = ProcessIfElseConditions(result, dataModel);
                }

                File.WriteAllText(result, inputFilePath);
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

        private string ReplaceInterpolation(string htmlTemplate, object dataModel)
        {
            var wordAssemble = new StringBuilder();
            bool asseblerFlag = false;
            int startIndex = 0;

            for (int i = 0;  i < htmlTemplate.Length; i++)
            {
                if (asseblerFlag)
                    wordAssemble.Append(htmlTemplate[i]);               

                if (i < htmlTemplate.Length - 1 && htmlTemplate[i] == '$' && htmlTemplate[i + 1] == '{')
                {
                    asseblerFlag = true;
                    startIndex = i;
                    i++;
                    continue;
                }

                if (htmlTemplate[i] == '}')
                {                    
                    string replacement = GetPropertyValue(wordAssemble.ToString(), dataModel).ToString();
                    htmlTemplate = ReplaceWord(htmlTemplate, startIndex, i, replacement);

                    asseblerFlag = false;
                    wordAssemble.Clear();
                    i = startIndex + replacement.Length - 1;
                }
            }

            return htmlTemplate;
        }

        private string ProcessForeachLoops(string template, object dataModel)
        {
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

        private string ProcessForeachLoop(string loopExpression, string loopContent, object dataModel)
        {
            var parts = loopExpression.Split("in");

            if (parts.Length != 2)
                return loopContent;

            string itemName = parts[0].Trim();
            string collectionPath = parts[1].Trim();
            
            var collection = GetPropertyValue(collectionPath, dataModel) as IEnumerable;

            if (collection == null)
                return "";

            var result = new StringBuilder();
            foreach (var item in collection)
            {
                string itemResult = item.ToString();
                result.Append(itemResult + "\n");
            }

            return result.ToString();
        }

        private string ProcessIfElseConditions(string template, object dataModel)
        {
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

        private bool EvaluateCondition(object dataModel, string condition)
        {
            var value = GetPropertyValue(condition, dataModel);

            return value is bool ? (bool)value : value != null;
        }

        private object GetPropertyValue(string propertyPath, object dataModel)
        {
            var properties = propertyPath.ToString().Split('.');
            object current = dataModel;
            foreach (var property in properties)
            {
                if (current == null) return null;

                PropertyInfo propInfo = current.GetType().GetProperty(property, BindingFlags.Public
                    | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (propInfo == null) return null;

                current = propInfo.GetValue(current);
            }

            return current;
        }

        private string ReplaceWord(string original, int startIndex, int endIndex, string replacement)
        {
            string before = original.Substring(0, startIndex);
            string after = original.Substring(endIndex + 1);

            return before + replacement + after;
        }
    }
}
