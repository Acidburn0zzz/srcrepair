﻿/*
 * This file is a part of SRC Repair project. For more information
 * visit official site: https://www.easycoding.org/projects/srcrepair
 * 
 * Copyright (c) 2011 - 2018 EasyCoding Team (ECTeam).
 * Copyright (c) 2005 - 2018 EasyCoding Team.
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Windows.Forms; // для работы с формами...
using System.IO; // для работы с файлами...
using System.Reflection; // для работы со сборками...

namespace srcrepair
{
    /// <summary>
    /// Класс, предоставляющий методы для общих целей.
    /// </summary>
    public static class CoreLib
    {
        /// <summary>
        /// Форматирует размер файла для удобства пользователя.
        /// Файлы от 0 до 1 КБ - 1 записываются в байтах, от 1 КБ до
        /// 1 МБ - 1 - в килобайтах, от 1 МБ до 1 ГБ - 1 - в мегабайтах.
        /// </summary>
        /// <param name="InpNumber">Размер файла в байтах</param>
        /// <returns>Форматированная строка</returns>
        public static string SclBytes(long InpNumber)
        {
            // Задаём константы...
            const long B = 1024;
            const long KB = B * B;
            const long MB = B * B * B;
            const long GB = B * B * B * B;
            const string Template = "{0} {1}";

            // Проверяем на размер в байтах...
            if ((InpNumber >= 0) && (InpNumber < B)) { return String.Format(Template, InpNumber, AppStrings.AppSizeBytes); }
            // ...килобайтах...
            else if ((InpNumber >= B) && (InpNumber < KB)) { return String.Format(Template, Math.Round((float)InpNumber / B, 2), AppStrings.AppSizeKilobytes); }
            // ...мегабайтах...
            else if ((InpNumber >= KB) && (InpNumber < MB)) { return String.Format(Template, Math.Round((float)InpNumber / KB, 2), AppStrings.AppSizeMegabytes); }
            // ...гигабайтах.
            else if ((InpNumber >= MB) && (InpNumber < GB)) { return String.Format(Template, Math.Round((float)InpNumber / MB, 2), AppStrings.AppSizeGigabytes); }
            
            // Если размер всё-таки больше, выведем просто строку...
            return InpNumber.ToString();
        }

        /// <summary>
        /// Функция, записывающая в лог-файл строку. Например, сообщение об ошибке.
        /// </summary>
        /// <param name="TextMessage">Сообщение для записи в лог</param>
        public static void WriteStringToLog(string TextMessage)
        {
            if (Properties.Settings.Default.EnableDebugLog) // Пишем в лог если включено...
            {
                try // Начинаем работу...
                {
                    // Сгенерируем путь к файлу с логом...
                    string DebugFileName = Path.Combine(CurrentApp.AppUserPath, Properties.Resources.DebugLogFileName);
                    
                    // Если файл не существует, создадим его и сразу закроем...
                    if (!File.Exists(DebugFileName))
                    {
                        FileManager.CreateFile(DebugFileName);
                    }
                    
                    // Начинаем записывать в лог-файл...
                    using (StreamWriter DFile = new StreamWriter(DebugFileName, true))
                    {
                        // Делаем запись...
                        DFile.WriteLine(String.Format("{0}: {1}", DateTime.Now.ToString(), TextMessage));
                    }
                }
                catch { /* Подавляем исключения... */ }
            }
        }
        
        /// <summary>
        /// Функция, записывающая в лог-файл текст исключения, дату его возникновения
        /// и другую отладочную информацию, а также выводящая дружественное сообщение для
        /// пользователя и подробное для разработчика.
        /// </summary>
        /// <param name="FrindlyMsg">Понятное пользователю сообщение</param>
        /// <param name="WTitle">Текст в заголовке сообщения об ошибке</param>
        /// <param name="DevMsg">Отладочное сообщение</param>
        /// <param name="DevMethod">Метод, вызвавший исключение</param>
        /// <param name="MsgIcon">Тип иконки: предупреждение, ошибка и т.д.</param>
        public static void HandleExceptionEx(string FrindlyMsg, string WTitle, string DevMsg, string DevMethod, MessageBoxIcon MsgIcon)
        {
            // Сгенерируем строку с текстом сообщения...
            string ResultString = String.Format(AppStrings.AppExceptionTemplate, DevMsg, DevMethod);

            // Для режима отладки покажем сообщение, понятное разработчикам, в остальное время - обычное...
            #if DEBUG
            MessageBox.Show(ResultString, WTitle, MessageBoxButtons.OK, MsgIcon);
            #else
            MessageBox.Show(FrindlyMsg, Properties.Resources.AppName, MessageBoxButtons.OK, MsgIcon);
            #endif
            
            // Запишем в файл...
            WriteStringToLog(ResultString);
        }

        /// <summary>
        /// Чистит строку от табуляций и лишних пробелов.
        /// </summary>
        /// <param name="RecvStr">Исходная строка</param>
        /// <param name="CleanQuotes">Задаёт параметры очистки кавычек</param>
        /// <param name="CleanSlashes">Задаёт параметры очистки двойных слэшей</param>
        public static string CleanStrWx(string RecvStr, bool CleanQuotes = false, bool CleanSlashes = false)
        {
            // Почистим от табуляций...
            while (RecvStr.IndexOf("\t") != -1)
            {
                RecvStr = RecvStr.Replace("\t", " ");
            }

            // Заменим все NULL символы на пробелы...
            while (RecvStr.IndexOf("\0") != -1)
            {
                RecvStr = RecvStr.Replace("\0", " ");
            }

            // Удалим все лишние пробелы...
            while (RecvStr.IndexOf("  ") != -1)
            {
                RecvStr = RecvStr.Replace("  ", " ");
            }

            // Удалим кавычки если это разрешено...
            if (CleanQuotes)
            {
                while (RecvStr.IndexOf('"') != -1)
                {
                    RecvStr = RecvStr.Replace(@"""", String.Empty);
                }
            }

            // Удаляем двойные слэши если разрешено...
            if (CleanSlashes)
            {
                while (RecvStr.IndexOf(@"\\") != -1)
                {
                    RecvStr = RecvStr.Replace(@"\\", @"\");
                }
            }

            // Возвращаем результат очистки...
            return RecvStr.Trim();
        }

        /// <summary>
        /// Получает содержимое текстового файла из внутреннего ресурса приложения.
        /// </summary>
        /// <param name="FileName">Внутреннее имя ресурсного файла</param>
        /// <returns>Содержимое текстового файла</returns>
        public static string GetTemplateFromResource(string FileName)
        {
            string Result = String.Empty;
            using (StreamReader Reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(FileName)))
            {
                Result = Reader.ReadToEnd();
            }
            return Result;
        }
    }
}
