﻿/*
 * Модуль просмотра журналов SRC Repair.
 * 
 * Copyright 2011 - 2017 EasyCoding Team (ECTeam).
 * Copyright 2005 - 2017 EasyCoding Team.
 * 
 * Лицензия: GPL v3 (см. файл GPL.txt).
 * Лицензия контента: Creative Commons 3.0 BY.
 * 
 * Запрещается использовать этот файл при использовании любой
 * лицензии, отличной от GNU GPL версии 3 и с ней совместимой.
 * 
 * Официальный блог EasyCoding Team: https://www.easycoding.org/
 * Официальная страница проекта: https://www.easycoding.org/projects/srcrepair
 * 
 * Более подробная инфорация о программе в readme.txt,
 * о лицензии - в GPL.txt.
*/
using System;
using System.Windows.Forms;
using System.IO;

namespace srcrepair
{
    /// <summary>
    /// Класс формы модуля просмотра журналов.
    /// </summary>
    public partial class FrmLogView : Form
    {
        /// <summary>
        /// Хранит путь к файлу журнала.
        /// </summary>
        private string LogFileName { get; set; }

        /// <summary>
        /// Конструктор класса формы модуля просмотра журналов.
        /// </summary>
        /// <param name="LogFile">Путь к файлу журнала</param>
        public FrmLogView(string LogFile)
        {
            InitializeComponent();
            LogFileName = LogFile;
        }

        /// <summary>
        /// Непосредственно загружает содержимое текстового файла в TextBox на форме.
        /// </summary>
        /// <param name="FileName">Путь к текстовому файлу</param>
        private void LoadTextFile(string FileName)
        {
            LV_LogArea.Clear();
            LV_LogArea.AppendText(File.ReadAllText(FileName));
        }

        /// <summary>
        /// Загружает содержимое журнала в TextBox на форме.
        /// </summary>
        /// <param name="FileName">Путь к файлу журнала</param>
        private void LoadLog(string FileName)
        {
            try { LoadTextFile(FileName); } catch (Exception Ex) { CoreLib.HandleExceptionEx(AppStrings.LV_LoadFailed, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning); }
        }

        /// <summary>
        /// Метод, срабатывающий при возникновении события "загрузка формы".
        /// </summary>
        private void FrmLogView_Load(object sender, EventArgs e)
        {
            // Считаем содержимое выбранного файла...
            LoadLog(LogFileName);
        }

        /// <summary>
        /// Метод, срабатывающий при нажатии на кнопку "Перечитать файл".
        /// </summary>
        private void LV_MenuFileReload_Click(object sender, EventArgs e)
        {
            // Перечитаем содержимое журнала...
            LoadLog(LogFileName);
        }

        /// <summary>
        /// Метод, срабатывающий при нажатии на кнопку выхода.
        /// </summary>
        private void LV_MenuFileExit_Click(object sender, EventArgs e)
        {
            // Закроем модуль...
            Close();
        }

        /// <summary>
        /// Метод, срабатывающий при нажатии на кнопку "О модуле".
        /// </summary>
        private void LV_MenuHelpAbout_Click(object sender, EventArgs e)
        {
            // Выводим сообщение с краткой информацией о плагине...
            FormManager.FormShowAboutApp();
        }

        /// <summary>
        /// Метод, срабатывающий при нажатии на кнопку "Очистить журнал".
        /// </summary>
        private void LV_MunuFileClearLog_Click(object sender, EventArgs e)
        {
            // Очистим форму...
            LV_LogArea.Clear();

            // Очистим файл журнала...
            try { if (File.Exists(LogFileName)) { File.Delete(LogFileName); FileManager.CreateFile(LogFileName); } } catch (Exception Ex) { CoreLib.HandleExceptionEx(AppStrings.LV_ClearEx, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning); }
        }
    }
}
