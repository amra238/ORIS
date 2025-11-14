using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Core.abstracts
{
    // Интерфейс для рендеринга HTML шаблонов с использованием модели данных
    internal interface IHtmlTemplateRenderer
    {
        /// <summary>
        /// Рендерит HTML из строкового шаблона
        /// </summary>
        /// <param name="htmlTemplate">Строка с HTML шаблоном</param>
        /// <param name="dataModel">Модель данных для подстановки</param>
        /// <returns>Отрендеренный HTML</returns>
        string RenderFromString(string htmlTemplate, object dataModel);

        /// <summary>
        /// Рендерит HTML из файлового шаблона
        /// </summary>
        /// <param name="filePath">Путь к файлу шаблона</param>
        /// <param name="dataModel">Модель данных для подстановки</param>
        /// <returns>Отрендеренный HTML</returns>
        string RenderFromFile(string filePath, object dataModel);

        /// <summary>
        /// Рендерит шаблон и сохраняет результат в файл
        /// </summary>
        /// <param name="inputFilePath">Путь к входному файлу шаблона</param>
        /// <param name="outputFilePath">Путь для сохранения результата</param>
        /// <param name="dataModel">Модель данных для подстановки</param>
        /// <returns>Отрендеренный HTML</returns>
        string RenderToFile(string inputFilePath, string outputFilePath, object dataModel);
    }
}
