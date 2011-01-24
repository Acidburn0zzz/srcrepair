﻿/*
 * Модуль "Создание отчёта для Техподдержки" программы SRC Repair.
 * 
 * Copyright 2011 EasyCoding Team (ECTeam).
 * Copyright 2005 - 2011 EasyCoding Team.
 * 
 * Лицензия: GPL v3 (см. файл GPL.txt).
 * Лицензия контента: Creative Commons 3.0 BY.
 * 
 * Запрещается использовать этот файл при использовании любой
 * лицензии, отличной от GNU GPL версии 3 и с ней совместимой.
 * 
 * Официальный блог EasyCoding Team: http://www.easycoding.org/
 * Официальная страница проекта: http://www.easycoding.org/projects/srcrepair
 * 
 * Более подробная инфорация о программе в readme.txt,
 * о лицензии - в GPL.txt.
*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace srcrepair
{
    public partial class frmRepBuilder : Form
    {
        public frmRepBuilder()
        {
            InitializeComponent();
        }

        private const string PluginName = "Report Builder";

        private void frmRepBuilder_Load(object sender, EventArgs e)
        {
            // Событие создания формы...
            if ((File.Exists(GV.FullAppPath + "7z.exe")) && (File.Exists(GV.FullAppPath + "7z.dll")))
            {
                Compress.Enabled = true;
            }
            else
            {
                Compress.Enabled = false;
            }
        }

        private void GenerateNow_Click(object sender, EventArgs e)
        {
            if (GenerateNow.Text != frmMainW.RM.GetString("RPB_CloseCpt"))
            {
                MessageBox.Show(frmMainW.RM.GetString("RPB_GenWarn"), PluginName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (MessageBox.Show(frmMainW.RM.GetString("RPB_GenQst"), PluginName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    // Отключим кнопку...
                    GenerateNow.Enabled = false;
                    GenerateNow.Text = frmMainW.RM.GetString("RPB_CptWrk");
                    this.ControlBox = false;
                    // Сгенерируем путь для каталога с рапортами...
                    string RepDir = GV.FullAppPath + @"\Reports\";
                    // Проверим чтобы каталог для рапортов существовал...
                    if (!Directory.Exists(RepDir))
                    {
                        // Не существует, поэтому создадим...
                        Directory.CreateDirectory(RepDir);
                    }
                    // Начинаем создавать отчёт...
                    string FilePath = "msinfo32.exe"; // Указываем имя exe-файла для запуска
                    string FileName = "Report_" + CoreLib.WriteDateToString(DateTime.Now, true);
                    string RepName = FileName + ".txt";
                    string Params = "/report " + '"' + RepDir + RepName + '"'; // Генерируем параметы для exe-файла...
                    try
                    {
                        // Запускаем последовательность...
                        CoreLib.StartProcessAndWait(FilePath, Params);
                        if (Compress.Checked)
                        {
                            CoreLib.StartProcessAndWait(GV.FullAppPath + "7z.exe", "a " + @"Reports\" + FileName + ".7z " + @"Reports\" + RepName);
                            File.Delete(RepDir + RepName); // удаляем несжатый отчёт
                            MessageBox.Show(String.Format(frmMainW.RM.GetString("RPB_ComprGen"), FileName), PluginName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show(String.Format(frmMainW.RM.GetString("RPB_Generated"), RepName), PluginName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        
                        // Открываем каталог с отчётами в Windows Explorer...
                        Process.Start(RepDir);
                    }
                    catch
                    {
                        // Произошло исключение...
                        MessageBox.Show(frmMainW.RM.GetString("RPB_GenException"), PluginName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    // Снова активируем кнопку...
                    GenerateNow.Text = frmMainW.RM.GetString("RPB_CloseCpt");
                    GenerateNow.Enabled = true;
                    this.ControlBox = true;
                }
            }
            else
            {
                Close();
            }
        }
    }
}
