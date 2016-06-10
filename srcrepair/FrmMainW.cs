﻿/*
 * Основной модуль программы SRC Repair.
 * 
 * Copyright 2011 - 2016 EasyCoding Team (ECTeam).
 * Copyright 2005 - 2016 EasyCoding Team.
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
using System.Drawing; // аналогично Forms...
using System.Linq;
using System.Text;
using System.Windows.Forms; // для работы с формами...
using System.IO; // для работы с файлами...
using System.Diagnostics; // для управления процессами...
using Microsoft.Win32; // для работы с реестром...
using System.Resources; // для управления ресурсами...
using System.Net; // для скачивания файлов...
using System.Xml; // для разбора (парсинга) XML...
using System.Text.RegularExpressions; // для работы с регулярными выражениями...

namespace srcrepair
{
    public partial class frmMainW : Form
    {
        public frmMainW()
        {
            // Инициализация...
            InitializeComponent();
            
            // Импортируем настройки из предыдущей версии...
            if (Properties.Settings.Default.CallUpgrade)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.CallUpgrade = false;
            }
        }

        #region Internal Variables

        private string CFGFileName;
        private CurrentApp App;
        private List<SourceGame> SourceGames;
        private SourceGame SelGame;

        #endregion

        #region Internal Functions

        /// <summary>
        /// Сохраняет содержимое таблицы в файл конфигурации, указанный в
        /// параметре. Используется в Save и SaveAs Редактора конфигов.
        /// </summary>
        /// <param name="Path">Полный путь к файлу конфига</param>
        private void WriteTableToFileNow(string Path)
        {
            // Начинаем сохранять содержимое редактора в файл...
            using (StreamWriter CFile = new StreamWriter(Path))
            {
                CFile.WriteLine(); // вставляем пустую строку
                for (int i = 0; i < CE_Editor.Rows.Count; i++) // запускаем цикл
                {
                    CFile.Write(CE_Editor.Rows[i].Cells[0].Value); // вставляем содержимое первого столбца (название переменной)
                    CFile.Write(" "); // вставляем пробел
                    CFile.WriteLine(CE_Editor.Rows[i].Cells[1].Value); // вставляем содержимое второго столбца (значение переменной)
                }
            }
        }

        /// <summary>
        /// Используется для записи значений в таблицу Редактора конфигов.
        /// Используется делегатом. Прямой вызов не допускается.
        /// </summary>
        /// <param name="Cv">Название переменной</param>
        /// <param name="Cn">Значение переменной</param>
        private void AddRowToTable(string Cv, string Cn)
        {
            CE_Editor.Rows.Add(Cv, Cn);
        }

        /// <summary>
        /// Возвращает описание переданной в качестве параметра переменной, получая
        /// эту информацию из ресурса CVList с учётом локализации.
        /// </summary>
        /// <param name="CVar">Название переменной</param>
        /// <returns>Описание переменной с учётом локализации</returns>
        private string GetCVDescription(string CVar)
        {
            ResourceManager DM = new ResourceManager("srcrepair.CVList", typeof(frmMainW).Assembly);
            return DM.GetString(CVar);
        }

        /// <summary>
        /// Определяет установленные игры и заполняет комбо-бокс выбора
        /// доступных управляемых игр.
        /// </summary>
        /// <param name="SteamPath">Путь к клиенту Steam</param>
        /// <param name="SteamAppsDir">Имя каталога SteamApps</param>
        private void DetectInstalledGames(string SteamPath, string SteamAppsDir)
        {
            // Очистим список игр...
            AppSelector.Items.Clear();

            // При использовании нового метода поиска установленных игр, считаем их из конфига Steam...
            List<String> GameDirs = CoreLib.FormatInstallDirs(App.FullSteamPath);
            
            // Формируем список для поддерживаемых игр...
            List<String> AvailableGames = new List<String>();

            try
            {
                // Создаём поток с XML-файлом...
                using (FileStream XMLFS = new FileStream(Path.Combine(App.FullAppPath, Properties.Settings.Default.GameListFile), FileMode.Open, FileAccess.Read))
                {
                    // Создаём объект документа XML...
                    XmlDocument XMLD = new XmlDocument();
                    
                    // Загружаем поток в объект XML документа...
                    XMLD.Load(XMLFS);
                    
                    // Обходим полученный список в цикле...
                    XmlNodeList XMLNode = XMLD.GetElementsByTagName("Game");
                    for (int i = 0; i < XMLNode.Count; i++)
                    {
                        AvailableGames.Add(XMLD.GetElementsByTagName("DirName")[i].InnerText);
                        try
                        {
                            SourceGame SG = new SourceGame(XMLNode[i].Attributes["Name"].Value, XMLD.GetElementsByTagName("DirName")[i].InnerText, XMLD.GetElementsByTagName("SmallName")[i].InnerText, XMLD.GetElementsByTagName("Executable")[i].InnerText, XMLD.GetElementsByTagName("SID")[i].InnerText, XMLD.GetElementsByTagName("VFDir")[i].InnerText, XMLD.GetElementsByTagName("HasVF")[i].InnerText == "1", XMLD.GetElementsByTagName("UserDir")[i].InnerText == "1", XMLD.GetElementsByTagName("HUDsAvail")[i].InnerText == "1", App.FullAppPath, App.AppUserDir, App.FullSteamPath, GameDirs);
                            if (SG.IsInstalled)
                            {
                                SourceGames.Add(SG);
                                AppSelector.Items.Add(XMLNode[i].Attributes["Name"].Value);
                            }
                        }
                        catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); }
                    }
                }
            }
            catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); }
        }

        /// <summary>
        /// Записывает настройки GCF-игры в реестр Windows.
        /// </summary>
        /// <param name="SAppName">Краткое имя игры</param>
        private void WriteGCFGameSettings(string SAppName)
        {
            // Создаём новый объект без получения данных из реестра...
            GCFVideo Video = new GCFVideo(SelGame.SmallAppName, false);

            // Записываем пользовательские настройки...
            Video.SetScreenWidth((int)GT_ResHor.Value);
            Video.SetScreenHeight((int)GT_ResVert.Value);
            Video.SetDisplayMode(GT_ScreenType.SelectedIndex);
            Video.SetModelQuality(GT_ModelQuality.SelectedIndex);
            Video.SetTextureQuality(GT_TextureQuality.SelectedIndex);
            Video.SetShaderQuality(GT_ShaderQuality.SelectedIndex);
            Video.SetReflectionsQuality(GT_WaterQuality.SelectedIndex);
            Video.SetShadowQuality(GT_ShadowQuality.SelectedIndex);
            Video.SetColorCorrection(GT_ColorCorrectionT.SelectedIndex);
            Video.SetAntiAliasing(GT_AntiAliasing.SelectedIndex);
            Video.SetFilteringMode(GT_Filtering.SelectedIndex);
            Video.SetVSync(GT_VSync.SelectedIndex);
            Video.SetMotionBlur(GT_MotionBlur.SelectedIndex);
            Video.SetDirectXMode(GT_DxMode.SelectedIndex);
            Video.SetHDRType(GT_HDR.SelectedIndex);

            // Записываем настройки в реестр...
            Video.WriteSettings();
        }

        /// <summary>
        /// Сохраняет настройки NCF игры в файл.
        /// </summary>
        /// <param name="VFileName">Имя файла опций</param>
        private void WriteNCFGameSettings(string VFileName)
        {
            // Создаём новый объект без получения данных из файла...
            NCFVideo Video = new NCFVideo(VFileName, false);

            // Записываем пользовательские настройки...
            Video.SetScreenWidth((int)GT_NCF_HorRes.Value);
            Video.SetScreenHeight((int)GT_NCF_VertRes.Value);
            Video.SetScreenRatio(GT_NCF_Ratio.SelectedIndex);
            Video.SetScreenGamma(GT_NCF_Brightness.Text);
            Video.SetShadowQuality(GT_NCF_Shadows.SelectedIndex);
            Video.SetMotionBlur(GT_NCF_MBlur.SelectedIndex);
            Video.SetScreenMode(GT_NCF_DispMode.SelectedIndex);
            Video.SetAntiAliasing(GT_NCF_AntiAlias.SelectedIndex);
            Video.SetFilteringMode(GT_NCF_Filtering.SelectedIndex);
            Video.SetVSync(GT_NCF_VSync.SelectedIndex);
            Video.SetRenderingMode(GT_NCF_Multicore.SelectedIndex);
            Video.SetShaderEffects(GT_NCF_ShaderE.SelectedIndex);
            Video.SetEffects(GT_NCF_EffectD.SelectedIndex);
            Video.SetMemoryPool(GT_NCF_MemPool.SelectedIndex);
            Video.SetModelQuality(GT_NCF_Quality.SelectedIndex);

            // Записываем настройки в файл...
            Video.WriteSettings();
        }

        /// <summary>
        /// Получает настройки GCF-игры из реестра и заполняет полученными
        /// данными страницу графического твикера.
        /// </summary>
        /// <param name="SAppName">Краткое имя игры</param>
        private void ReadGCFGameSettings(string SAppName)
        {
            try
            {
                // Получаем графические настройки...
                GCFVideo Video = new GCFVideo(SAppName, true);

                // Заполняем общие настройки...
                GT_ResHor.Value = Video.GetScreenWidth();
                GT_ResVert.Value = Video.GetScreenHeight();

                // Заполняем остальные настройки...
                GT_ScreenType.SelectedIndex = Video.GetDisplayMode();
                GT_ModelQuality.SelectedIndex = Video.GetModelQuality();
                GT_TextureQuality.SelectedIndex = Video.GetTextureQuality();
                GT_ShaderQuality.SelectedIndex = Video.GetShaderQuality();
                GT_WaterQuality.SelectedIndex = Video.GetReflectionsQuality();
                GT_ShadowQuality.SelectedIndex = Video.GetShadowQuality();
                GT_ColorCorrectionT.SelectedIndex = Video.GetColorCorrection();
                GT_AntiAliasing.SelectedIndex = Video.GetAntiAliasing();
                GT_Filtering.SelectedIndex = Video.GetFilteringMode();
                GT_VSync.SelectedIndex = Video.GetVSync();
                GT_MotionBlur.SelectedIndex = Video.GetMotionBlur();
                GT_DxMode.SelectedIndex = Video.GetDirectXMode();
                GT_HDR.SelectedIndex = Video.GetHDRType();
            }
            catch (Exception Ex)
            {
                // Выводим сообщение об ошибке...
                CoreLib.HandleExceptionEx(AppStrings.GT_RegOpenErr, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Получает настройки NCF-игры из файла и заполняет ими таблицу
        /// графического твикера программы.
        /// </summary>
        /// <param name="VFileName">Путь к файлу с настройками</param>
        private void ReadNCFGameSettings(string VFileName)
        {
            try
            {
                // Получаем графические настройки...
                NCFVideo Video = new NCFVideo(VFileName, true);

                // Заполняем общие настройки...
                GT_NCF_HorRes.Value = Video.GetScreenWidth();
                GT_NCF_VertRes.Value = Video.GetScreenHeight();

                // Заполняем остальные настройки...
                GT_NCF_Ratio.SelectedIndex = Video.GetScreenRatio();
                GT_NCF_Brightness.Text = Video.GetScreenGamma();
                GT_NCF_Shadows.SelectedIndex = Video.GetShadowQuality();
                GT_NCF_MBlur.SelectedIndex = Video.GetMotionBlur();
                GT_NCF_DispMode.SelectedIndex = Video.GetScreenMode();
                GT_NCF_AntiAlias.SelectedIndex = Video.GetAntiAliasing();
                GT_NCF_Filtering.SelectedIndex = Video.GetFilteringMode();
                GT_NCF_VSync.SelectedIndex = Video.GetVSync();
                GT_NCF_Multicore.SelectedIndex = Video.GetRenderingMode();
                GT_NCF_ShaderE.SelectedIndex = Video.GetShaderEffects();
                GT_NCF_EffectD.SelectedIndex = Video.GetEffects();
                GT_NCF_MemPool.SelectedIndex = Video.GetMemoryPool();
                GT_NCF_Quality.SelectedIndex = Video.GetModelQuality();
            }
            catch (Exception Ex)
            {
                // Выводим сообщение об ошибке...
                CoreLib.HandleExceptionEx(AppStrings.GT_NCFLoadFailure, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Проверяет наличие обновлений для программы. Используется в модуле автообновления.
        /// </summary>
        /// <param name="CurrentVersion">Текущая версия</param>
        /// <param name="ChURI">URL проверки обновлений</param>
        /// <param name="UserAgent">Заголовок HTTP UserAgent</param>
        /// <returns>Возвращает true при обнаружении обновлений</returns>
        private bool AutoUpdateCheck(string CurrentVersion, string ChURI, string UserAgent)
        {
            string NewVersion, DnlStr;
            using (WebClient Downloader = new WebClient())
            {
                Downloader.Headers.Add("User-Agent", UserAgent);
                DnlStr = Downloader.DownloadString(ChURI);
            }
            NewVersion = DnlStr.Substring(0, DnlStr.IndexOf("!"));
            return CoreLib.CompareVersions(CurrentVersion, NewVersion);
        }

        /// <summary>
        /// Открывает конфиг, имя которого передано в качестве параметра
        /// и заполняет им Редактор конфигов с одноимённой страницы.
        /// </summary>
        /// <param name="ConfFileName">Полный путь к файлу конфига</param>
        private void ReadConfigFromFile(string ConfFileName)
        {
            // Описываем буферные переменные...
            string ImpStr, CVarName, CVarContent;

            // Проверяем, существует ли файл...
            if (File.Exists(ConfFileName))
            {
                // Получаем имя открытого в Редакторе файла без пути...
                CFGFileName = ConfFileName;
                
                // Проверяем, не открыл ли пользователь файл config.cfg и, если да, то сообщаем об этом...
                if (Path.GetFileName(CFGFileName) == "config.cfg") { MessageBox.Show(AppStrings.CE_RestConfigOpenWarn, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning); }

                // Очищаем область редактирования...
                CE_Editor.Rows.Clear();
                
                // Загружаем содержимое конфига из файла...
                try
                {
                    // Открываем поток с нужным нам файлом...
                    using (StreamReader ConfigFile = new StreamReader(ConfFileName, Encoding.Default))
                    {
                        // Читаем файл в потоковом режиме от начала и до конца...
                        while (ConfigFile.Peek() >= 0)
                        {
                            // Почистим строку от лишних пробелов и табуляций...
                            ImpStr = CoreLib.CleanStrWx(ConfigFile.ReadLine());

                            // Проверяем, не пустая ли строка...
                            if (!(String.IsNullOrEmpty(ImpStr)))
                            {
                                // Проверяем, не комментарий ли...
                                if (ImpStr[0] != '/')
                                {
                                    // Строка почищена, продолжаем...
                                    if (ImpStr.IndexOf(" ") != -1)
                                    {
                                        // Выделяем переменную...
                                        CVarName = ImpStr.Substring(0, ImpStr.IndexOf(" "));
                                        ImpStr = ImpStr.Remove(0, ImpStr.IndexOf(" ") + 1);

                                        // Выделяем значение...
                                        CVarContent = ImpStr.IndexOf("//") >= 1 ? ImpStr.Substring(0, ImpStr.IndexOf("//") - 1) : ImpStr;

                                        // Вставляем в таблицу...
                                        CE_Editor.Rows.Add(CVarName, CVarContent);
                                    }
                                    else
                                    {
                                        CE_Editor.Rows.Add(ImpStr, String.Empty);
                                    }
                                }
                            }
                        }
                    }

                    // Изменяем содержимое строки статуса...
                    UpdateStatusBar(MainTabControl.SelectedIndex);
                }
                catch (Exception Ex)
                {
                    // Произошло исключение...
                    CoreLib.HandleExceptionEx(AppStrings.CE_ExceptionDetected, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show(AppStrings.CE_OpenFailed, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Генерирует удобочитаемое название для файла резервной копии.
        /// </summary>
        /// <param name="FileName">Указатель на файл резервной копии</param>
        /// <returns>Возвращает пару "тип архива" и "удобочитаемое название"</returns>
        private Tuple<string, string> GenUserFriendlyBackupDesc(FileInfo FileName)
        {
            string BufName = Path.GetFileNameWithoutExtension(FileName.Name);
            string Buf = String.Empty;

            switch (FileName.Extension)
            {
                case ".reg":
                    Buf = AppStrings.BU_BType_Reg;
                    if (BufName.IndexOf("Game_Options", StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        BufName = String.Format(Properties.Resources.BU_TablePrefix, AppStrings.BU_BName_GRGame, FileName.CreationTime);
                    }
                    else if (BufName.IndexOf("Source_Options", StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        BufName = String.Format(Properties.Resources.BU_TablePrefix, AppStrings.BU_BName_SRCAll, FileName.CreationTime);
                    }
                    else if (BufName.IndexOf("Steam_BackUp", StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        BufName = String.Format(Properties.Resources.BU_TablePrefix, AppStrings.BU_BName_SteamAll, FileName.CreationTime);
                    }
                    else if (BufName.IndexOf("Game_AutoBackUp", StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        BufName = String.Format(Properties.Resources.BU_TablePrefix, AppStrings.BU_BName_GameAuto, FileName.CreationTime);
                    }
                    break;
                case ".bud":
                    Buf = AppStrings.BU_BType_Cont;
                    if (BufName.IndexOf(Properties.Resources.BU_PrefixDef, StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        BufName = String.Format(Properties.Resources.BU_TablePrefix, AppStrings.BU_BName_Bud, FileName.CreationTime);
                    }
                    else if (BufName.IndexOf(Properties.Resources.BU_PrefixCfg, StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        Buf = AppStrings.BU_BType_Cfg;
                        BufName = String.Format(Properties.Resources.BU_TablePrefix, AppStrings.BU_BName_Config, FileName.CreationTime);
                    }
                    else if (BufName.IndexOf(Properties.Resources.BU_PrefixVChat, StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        Buf = AppStrings.BU_BType_DB;
                        BufName = String.Format(Properties.Resources.BU_TablePrefix, AppStrings.BU_BName_VChat, FileName.CreationTime);
                    }
                    else if (BufName.IndexOf(Properties.Resources.BU_PrefixVideo, StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        Buf = AppStrings.BU_BType_Video;
                        BufName = String.Format(Properties.Resources.BU_TablePrefix, AppStrings.BU_BName_GRGame, FileName.CreationTime);
                    }
                    else if (BufName.IndexOf(Properties.Resources.BU_PrefixVidAuto, StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        Buf = AppStrings.BU_BType_Video;
                        BufName = String.Format(Properties.Resources.BU_TablePrefix, AppStrings.BU_BName_GameAuto, FileName.CreationTime);
                    }
                    break;
                default:
                    Buf = AppStrings.BU_BType_Unkn;
                    break;
            }

            return Tuple.Create(Buf, BufName);
        }

        /// <summary>
        /// Считывает файлы резервных копий из указанного каталога и помещает в таблицу.
        /// </summary>
        /// <param name="BUpDir">Путь к каталогу с резервными копиями</param>
        private void ReadBackUpList2Table(string BUpDir)
        {
            // Очистим таблицу...
            Invoke((MethodInvoker)delegate() { BU_LVTable.Items.Clear(); });
            
            // Открываем каталог...
            DirectoryInfo DInfo = new DirectoryInfo(BUpDir);
            
            // Считываем список файлов по заданной маске...
            FileInfo[] DirList = DInfo.GetFiles("*.*");
            
            // Начинаем обход массива...
            foreach (FileInfo DItem in DirList)
            {
                // Обрабатываем найденное...
                var Rs = GenUserFriendlyBackupDesc(DItem);

                // Добавляем в таблицу...
                ListViewItem LvItem = new ListViewItem(Rs.Item2);
                if (Properties.Settings.Default.HighlightOldBackUps) { if (DateTime.UtcNow - DItem.CreationTimeUtc > TimeSpan.FromDays(30)) { LvItem.BackColor = Color.LightYellow; } }
                LvItem.SubItems.Add(Rs.Item1);
                LvItem.SubItems.Add(CoreLib.SclBytes(DItem.Length));
                LvItem.SubItems.Add(DItem.CreationTime.ToString());
                LvItem.SubItems.Add(DItem.Name);
                Invoke((MethodInvoker)delegate() { BU_LVTable.Items.Add(LvItem); });
            }
        }

        /// <summary>
        /// Обнуляет контролы на странице графических настроек. Функция-заглушка.
        /// </summary>
        private void NullGraphOptions()
        {
            GT_NCF_HorRes.Value = 640;
            GT_NCF_VertRes.Value = 480;
            GT_NCF_Brightness.SelectedIndex = -1;
            GT_NCF_Shadows.SelectedIndex = -1;
            GT_NCF_MBlur.SelectedIndex = -1;
            GT_NCF_Ratio.SelectedIndex = -1;
            GT_NCF_DispMode.SelectedIndex = -1;
            GT_NCF_AntiAlias.SelectedIndex = -1;
            GT_NCF_Filtering.SelectedIndex = -1;
            GT_NCF_VSync.SelectedIndex = -1;
            GT_NCF_Multicore.SelectedIndex = -1;
            GT_NCF_ShaderE.SelectedIndex = -1;
            GT_NCF_EffectD.SelectedIndex = -1;
            GT_NCF_MemPool.SelectedIndex = -1;
            GT_NCF_Quality.SelectedIndex = -1;
        }

        /// <summary>
        /// Переключает состояние некоторых элементов управления на форме.
        /// </summary>
        private void HandleControlsOnSelGame()
        {
            // Включаем основные элементы управления (контролы)...
            MainTabControl.Enabled = true;
            
            // Очистим список FPS-конфигов и HUD-ов...
            FP_ConfigSel.Items.Clear();
            HD_HSel.Items.Clear();

            // Отключим кнопку редактирования FPS-конфигов...
            FP_OpenNotepad.Enabled = false;

            // Отключим кнопку установки FPS-конфигов...
            FP_Install.Enabled = false;

            // Отключим контролы в менеджере HUD...
            HD_Install.Enabled = false;
            HD_Homepage.Enabled = false;
            HD_Uninstall.Enabled = false;
            HD_OpenDir.Enabled = false;
            HD_Warning.Visible = false;
            HD_GB_Pbx.Image = null;

            // Включаем заблокированные ранее контролы...
            MNUInstaller.Enabled = true;
        }

        /// <summary>
        /// Переключает вид страницы графического твикера с GCF на NCF приложение
        /// и наоборот.
        /// </summary>
        /// <param name="GCFGame">Тип управляемого приложения</param>
        private void SetGTOptsType(bool GCFGame)
        {
            GT_GCF_Group.Visible = GCFGame;
            GT_NCF_Group.Visible = !GCFGame;
        }

        /// <summary>
        /// Изменяет вид значка и текст безопасной очистки в соответствии с её статусом
        /// в строке состояния программы.
        /// </summary>
        private void CheckSafeClnStatus()
        {
            if (Properties.Settings.Default.SafeCleanup)
            {
                SB_App.Text = AppStrings.AppSafeClnStTextOn;
                SB_App.Image = Properties.Resources.green_circle;
            }
            else
            {
                SB_App.Text = AppStrings.AppSafeClnStTextOff;
                SB_App.Image = Properties.Resources.red_circle;
            }
        }

        /// <summary>
        /// Требует указать путь к Steam вручную при невозможности автоопределения.
        /// </summary>
        private string GetPathByMEnter()
        {
            string Result = null;
            FldrBrwse.Description = AppStrings.SteamPathEnterText; // Указываем текст в диалоге поиска каталога...
            if (FldrBrwse.ShowDialog() == DialogResult.OK) // Отображаем стандартный диалог поиска каталога...
            {
                if (!(File.Exists(Path.Combine(FldrBrwse.SelectedPath, Properties.Resources.SteamExecBin))))
                {
                    throw new FileNotFoundException("Invalid Steam directory entered by user", Path.Combine(FldrBrwse.SelectedPath, Properties.Resources.SteamExecBin));
                }
                else
                {
                    Result = FldrBrwse.SelectedPath;
                }
            }
            else
            {
                throw new OperationCanceledException("User closed opendir window");
            }
            return Result;
        }

        /// <summary>
        /// Проверяет значение OldPath на наличие верного пути к клиенту Steam.
        /// </summary>
        /// <param name="OldPath">Проверяемый путь</param>
        private string CheckLastSteamPath(string OldPath)
        {
            return (!(String.IsNullOrWhiteSpace(OldPath)) && File.Exists(Path.Combine(OldPath, Properties.Resources.SteamExecBin))) ? OldPath : GetPathByMEnter();
        }

        /// <summary>
        /// Получает путь и обрабатывает возможные исключения.
        /// </summary>
        private void ValidateAndHandle()
        {
            try
            {
                App.FullSteamPath = CheckLastSteamPath(Properties.Settings.Default.LastSteamPath);
            }
            catch (FileNotFoundException Ex)
            {
                CoreLib.HandleExceptionEx(AppStrings.SteamPathEnterErr, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Error);
                Environment.Exit(7);
            }
            catch (OperationCanceledException Ex)
            {
                CoreLib.HandleExceptionEx(AppStrings.SteamPathCancel, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Error);
                Environment.Exit(19);
            }
            catch (Exception Ex)
            {
                CoreLib.HandleExceptionEx(AppStrings.AppGenericError, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Error);
                Environment.Exit(24);
            }
        }

        /// <summary>
        /// Устанавливает статус элементам управления, зависящим от платформы или спец. прав.
        /// </summary>
        /// <param name="State">Устанавливаемый статус</param>
        private void ChangePrvControlState(bool State)
        {
            MNUWinMnuDisabler.Enabled = State;
        }

        /// <summary>
        /// Выполняет определение и вывод названия файловой системы на диске установки клиента Steam.
        /// </summary>
        /// <param name="SteamPath">Каталог установки Steam</param>
        private void DetectFS(string SteamPath)
        {
            try
            {
                PS_OSDrive.Text = String.Format(PS_OSDrive.Text, CoreLib.DetectDriveFileSystem(Path.GetPathRoot(SteamPath)));
            }
            catch (Exception Ex)
            {
                PS_OSDrive.Text = String.Format(PS_OSDrive.Text, "Unknown");
                CoreLib.WriteStringToLog(Ex.Message);
            }
        }

        /// <summary>
        /// Проверяет количество найденных установленных игр и выполняет нужные действия.
        /// </summary>
        /// <param name="LoginCount">Количество найденных игр</param>
        private void CheckGames(int GamesCount)
        {
            switch (GamesCount)
            {
                case 0:
                    {
                        // Запишем в лог...
                        CoreLib.WriteStringToLog(String.Format(AppStrings.AppNoGamesDLog, App.FullSteamPath));
                        // Нет, не нашлись, выведем сообщение...
                        MessageBox.Show(AppStrings.AppNoGamesDetected, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        // Завершим работу приложения...
                        Environment.Exit(11);
                    }
                    break;
                case 1:
                    {
                        // При наличии единственной игры в списке, выберем её автоматически...
                        AppSelector.SelectedIndex = 0;
                        UpdateStatusBar(MainTabControl.SelectedIndex);
                    }
                    break;
                default:
                    {
                        // Выберем последнюю использованную игру...
                        int Ai = AppSelector.Items.IndexOf(Properties.Settings.Default.LastGameName);
                        AppSelector.SelectedIndex = Ai != -1 ? Ai : 0;
                    }
                    break;
            }
        }

        /// <summary>
        /// Запускает проверку на наличие запрещённых символов в пути установки клиента Steam.
        /// </summary>
        /// <param name="SteamPath">Каталог установки Steam</param>
        private void CheckSymbolsSteam(string SteamPath)
        {
            if (!(CoreLib.CheckNonASCII(SteamPath)))
            {
                PS_PathSteam.ForeColor = Color.Red;
                PS_PathSteam.Image = Properties.Resources.upd_err;
                CoreLib.WriteStringToLog(String.Format(AppStrings.AppRestrSymbLog, SteamPath));
            }
        }

        /// <summary>
        /// Запускает проверку на наличие запрещённых символов в пути установки игры.
        /// </summary>
        /// <param name="GamePath">Каталог установки игры</param>
        private void CheckSymbolsGame(string GamePath)
        {
            if (!(CoreLib.CheckNonASCII(GamePath)))
            {
                PS_PathGame.ForeColor = Color.Red;
                PS_PathGame.Image = Properties.Resources.upd_err;
                CoreLib.WriteStringToLog(String.Format(AppStrings.AppRestrSymbLog, GamePath));
            }
            else
            {
                PS_PathGame.ForeColor = Color.Green;
                PS_PathGame.Image = Properties.Resources.upd_nx;
            }
        }

        /// <summary>
        /// Генерирует массив, содержащий пути к FPS-конфигам.
        /// </summary>
        /// <param name="GamePath">Каталог управляемого приложения</param>
        /// <param name="UserDir">Указывает использует ли управляемое приложение пользовательский каталог</param>
        /// <returns>Возвращает массив с сгенерированными путями до FPS-конфигов</returns>
        private List<String> ListFPSConfigs(string GamePath, bool UserDir)
        {
            List<String> Result = new List<String>();
            Result.Add(Path.Combine(GamePath, "cfg", "autoexec.cfg"));
            if (UserDir) { Result.Add(Path.Combine(GamePath, "custom", "autoexec.cfg")); }
            return Result;
        }

        /// <summary>
        /// Ищет файлы по указанным маскам в указанных каталогах
        /// </summary>
        /// <param name="CleanDirs">Каталоги для выполнения очистки с маской имени</param>
        /// <param name="IsRecursive">Включает / отключает рекурсивный поиск</param>
        /// <returns>Возвращает массив с именами файлов и полными путями</returns>
        private List<String> ExpandFileList(List<String> CleanDirs, bool IsRecursive)
        {
            List<String> Result = new List<String>();
            foreach (string DirMs in CleanDirs)
            {
                string CleanDir = Path.GetDirectoryName(DirMs);
                string CleanMask = Path.GetFileName(DirMs);
                if (Directory.Exists(CleanDir))
                {
                    try
                    {
                        DirectoryInfo DInfo = new DirectoryInfo(CleanDir);
                        FileInfo[] DirList = DInfo.GetFiles(CleanMask);
                        foreach (FileInfo DItem in DirList) { Result.Add(DItem.FullName); }

                        if (IsRecursive)
                        {
                            try
                            {
                                List<String> SubDirs = new List<string>();
                                foreach (DirectoryInfo Dir in DInfo.GetDirectories()) { SubDirs.Add(Path.Combine(Dir.FullName, CleanMask)); }
                                if (SubDirs.Count > 0) { Result.AddRange(ExpandFileList(SubDirs, true)); }
                            }
                            catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); }
                        }
                    }
                    catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); }
                }
            }
            return Result;
        }

        /// <summary>
        /// Управляет выводом значка активного FPS-конфига и кнопки их удаления.
        /// </summary>
        /// <param name="GameDir">Полный путь к каталогу игры</param>
        /// <param name="UserDir">Флаг использования кастомного каталога</param>
        private void HandleConfigs(string GameDir, bool UserDir)
        {
            SelGame.FPSConfigs = ExpandFileList(ListFPSConfigs(GameDir, UserDir), true);
            GT_Warning.Visible = SelGame.FPSConfigs.Count > 0;
            FP_Uninstall.Enabled = SelGame.FPSConfigs.Count > 0;
        }

        /// <summary>
        /// Управляет видимостью специальных кнопок модуля Менеджер HUD.
        /// </summary>
        /// <param name="State">Статус выбранного HUD</param>
        private void SetHUDButtons(bool State)
        {
            HD_Install.Text = State ? AppStrings.HD_BtnUpdateText : AppStrings.HD_BtnInstallText;
            HD_Uninstall.Enabled = State;
            HD_OpenDir.Enabled = State;
        }

        /// <summary>
        /// Проверяет актуальность базы HUD.
        /// </summary>
        /// <param name="LastHUDUpdate">Дата последней проверки обновлений базы HUD</param>
        /// <returns>Булево актуальности базы HUD</returns>
        private bool CheckHUDDatabase(DateTime LastHUDUpdate)
        {
            return (DateTime.Now - LastHUDUpdate).Days >= 7;
        }

        /// <summary>
        /// Обновляет содержимое строки состояния в зависимости от контекста.
        /// </summary>
        /// <param name="Index">ID текущей вкладки</param>
        private void UpdateStatusBar(int Index)
        {
            switch (Index)
            {
                case 1: // Открыта страница "Редактор конфигов"...
                    {
                        MNUShowEdHint.Enabled = true;
                        SB_Status.ForeColor = Color.Black;
                        SB_Status.Text = String.Format(AppStrings.StatusOpenedFile, String.IsNullOrEmpty(CFGFileName) ? AppStrings.UnnamedFileName : Path.GetFileName(CFGFileName));
                    }
                    break;
                case 4:
                    {
                        bool HUDDbStatus = CheckHUDDatabase(Properties.Settings.Default.LastHUDTime);
                        MNUShowEdHint.Enabled = false;
                        SB_Status.ForeColor = HUDDbStatus ? Color.Red : Color.Black;
                        SB_Status.Text = String.Format(AppStrings.HD_DynBarText, HUDDbStatus ? AppStrings.HD_StatusOutdated : AppStrings.HD_StatusUpdated, Properties.Settings.Default.LastHUDTime);
                    }
                    break;
                default: // Открыта другая страница...
                    {
                        MNUShowEdHint.Enabled = false;
                        SB_Status.ForeColor = Color.Black;
                        SB_Status.Text = AppStrings.StatusNormal;
                    }
                    break;
            }
        }

        /// <summary>
        /// Проверяет верность заполнения графических настроек
        /// </summary>
        /// <param name="GameType">Тип игры: GCF или NCF</param>
        private bool ValidateGameSettings(bool GameType)
        {
            return !GameType ? ((GT_ScreenType.SelectedIndex != -1) && (GT_ModelQuality.SelectedIndex != -1)
                && (GT_TextureQuality.SelectedIndex != -1) && (GT_ShaderQuality.SelectedIndex != -1)
                && (GT_WaterQuality.SelectedIndex != -1) && (GT_ShadowQuality.SelectedIndex != -1)
                && (GT_ColorCorrectionT.SelectedIndex != -1) && (GT_AntiAliasing.SelectedIndex != -1)
                && (GT_Filtering.SelectedIndex != -1) && (GT_VSync.SelectedIndex != -1)
                && (GT_MotionBlur.SelectedIndex != -1) && (GT_DxMode.SelectedIndex != -1)
                && (GT_HDR.SelectedIndex != -1)) : ((GT_NCF_Quality.SelectedIndex != -1)
                && (GT_NCF_MemPool.SelectedIndex != -1) && (GT_NCF_EffectD.SelectedIndex != -1)
                && (GT_NCF_ShaderE.SelectedIndex != -1) && (GT_NCF_Multicore.SelectedIndex != -1)
                && (GT_NCF_VSync.SelectedIndex != -1) && (GT_NCF_Filtering.SelectedIndex != -1)
                && (GT_NCF_AntiAlias.SelectedIndex != -1) && (GT_NCF_DispMode.SelectedIndex != -1)
                && (GT_NCF_Ratio.SelectedIndex != -1) && (GT_NCF_Brightness.SelectedIndex != -1)
                && (GT_NCF_Shadows.SelectedIndex != -1) && (GT_NCF_MBlur.SelectedIndex != -1));
        }

        /// <summary>
        /// Закрывает все открытые в Редакторе конфигов файлы и очищает таблицу...
        /// </summary>
        private void CloseEditorConfigs()
        {
            CFGFileName = String.Empty;
            CE_Editor.Rows.Clear();
        }

        /// <summary>
        /// Получает список резеервных копий и заносит их в таблицу...
        /// </summary>
        private void UpdateBackUpList(string BackUpDir)
        {
            try
            {
                // Считываем и выводим в таблицу файлы резервных копий...
                ReadBackUpList2Table(BackUpDir);
            }
            catch (Exception Ex)
            {
                // Произошло исключение. Запишем в журнал...
                CoreLib.WriteStringToLog(Ex.Message);

                // Создадим каталог для хранения резервных копий если его ещё нет...
                if (!Directory.Exists(BackUpDir)) { Directory.CreateDirectory(BackUpDir); }
            }
        }

        /// <summary>
        /// Ищет установленные игры и выполняет ряд необходимых проверок.
        /// </summary>
        /// <param name="SteamDir">Путь к каталогу установки Steam</param>
        /// <param name="SteamApps">Название каталога SteamApps в зависимости от платформы</param>
        /// <param name="ErrMsg">Текст сообщения об ошибке</param>
        private void FindGames(string SteamDir, string SteamApps, string ErrMsg)
        {
            // Начинаем определять установленные игры...
            try { DetectInstalledGames(SteamDir, SteamApps); } catch (Exception Ex) { CoreLib.HandleExceptionEx(ErrMsg, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Error); Environment.Exit(16); }

            // Проверим нашлись ли игры...
            CheckGames(AppSelector.Items.Count);
        }

        /// <summary>
        /// Переключает вид страницы модуля Менеджер HUD.
        /// </summary>
        /// <param name="Mode">Булево режима</param>
        private void HandleHUDMode(bool Mode)
        {
            HUD_Panel.Visible = Mode;
            HUD_NotAvailable.Visible = !Mode;
        }

        /// <summary>
        /// Генерирует ссылку онлайновой справочной системы на основе информации
        /// о текущей вкладке.
        /// </summary>
        /// <param name="TabIndex">Индекс текущей вкладки</param>
        /// <returns>Возвращает URL, пригодный для загрузки в веб-браузере</returns>
        private string GetHelpWebPage(int TabIndex)
        {
            // Создаём буферную переменную...
            string Result = "";

            // Генерируем ID для справочной системы сайта...
            switch (TabIndex)
            {
                case 0: /* графический твикер. */
                    Result = "gtweaker";
                    break;
                case 1: /* Редактор конфигов. */
                    Result = "cfgeditor";
                    break;
                case 2: /* Устранение проблем и очистка. */
                    Result = "cleanup";
                    break;
                case 3: /* FPS-конфиги. */
                    Result = "fpscfgs";
                    break;
                case 4: /* Менеджер HUD. */
                    Result = "hudman";
                    break;
                case 5: /* Резервные копии. */
                    Result = "backups";
                    break;
            }

            // Возвращаем финальный URL...
            return String.Format(Properties.Resources.AppURLHelpSystem, Result);
        }

        #endregion

        #region Internal Workers

        private void BW_UpChk_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Вычисляем разницу между текущей датой и датой последнего обновления...
                TimeSpan TS = DateTime.Now - Properties.Settings.Default.LastUpdateTime;
                if (TS.Days >= 7) // Проверяем не прошла ли неделя с момента последней прверки...
                {
                    // Требуется проверка обновлений...
                    if (AutoUpdateCheck(App.AppVersionInfo, Properties.Resources.UpdateChURI, App.UserAgent))
                    {
                        // Доступны обновления...
                        MessageBox.Show(String.Format(AppStrings.AppUpdateAvailable, Properties.Resources.AppName), Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        // Установим время последней проверки обновлений...
                        Properties.Settings.Default.LastUpdateTime = DateTime.Now;
                    }
                }
            }
            catch (Exception Ex)
            {
                CoreLib.WriteStringToLog(Ex.Message);
            }
        }

        private void BW_FPRecv_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Получаем список установленных конфигов из БД...
                SelGame.CFGMan = new ConfigManager(Path.Combine(App.FullAppPath, Properties.Settings.Default.CfgDbFile), AppStrings.AppLangPrefix);

                // Выведем установленные в форму...
                foreach (string Str in SelGame.CFGMan.GetAllCfg())
                {
                    Invoke((MethodInvoker)delegate () { FP_ConfigSel.Items.Add(Str); });
                }
            }
            catch (Exception Ex)
            {
                // FPS-конфигов не найдено. Запишем в лог...
                CoreLib.WriteStringToLog(Ex.Message);
                
                // Выводим текст об этом...
                FP_Description.Text = AppStrings.FP_NoCfgGame;
                FP_Description.ForeColor = Color.Red;
                
                // ...и блокируем контролы, отвечающие за установку...
                FP_Install.Enabled = false;
                FP_ConfigSel.Enabled = false;
                FP_OpenNotepad.Enabled = false;
            }
        }

        private void BW_FPRecv_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Проверяем, нашлись ли конфиги...
            if (FP_ConfigSel.Items.Count >= 1)
            {
                FP_Description.Text = AppStrings.FP_SelectFromList;
                FP_Description.ForeColor = Color.Black;
                FP_ConfigSel.Enabled = true;
            }
        }

        private void BW_BkUpRecv_DoWork(object sender, DoWorkEventArgs e)
        {
            // Получаем список резеверных копий...
            UpdateBackUpList(SelGame.FullBackUpDirPath);
        }

        private void BW_HUDList_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Получаем список доступных HUD...
                SelGame.HUDMan = new HUDManager(Path.Combine(App.FullAppPath, Properties.Settings.Default.HUDDbFile), SelGame.AppHUDDir);

                // Вносим HUD текущей игры в форму...
                Invoke((MethodInvoker)delegate () { HD_HSel.Items.AddRange(SelGame.HUDMan.GetHUDNames(SelGame.SmallAppName).ToArray<object>()); });
            }
            catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); }
        }

        private void BW_HUDScreen_DoWork(object sender, DoWorkEventArgs e)
        {
            // Сгенерируем путь к файлу со скриншотом...
            string ScreenFile = Path.Combine(SelGame.AppHUDDir, Path.GetFileName(SelGame.HUDMan.SelectedHUD.Preview));

            try
            {
                // Загрузим файл если не существует...
                if (!File.Exists(ScreenFile))
                {
                    using (WebClient Downloader = new WebClient())
                    {
                        Downloader.Headers.Add("User-Agent", App.UserAgent);
                        Downloader.DownloadFile(SelGame.HUDMan.SelectedHUD.Preview, ScreenFile);
                    }
                }

                // Установим...
                Invoke((MethodInvoker)delegate() { HD_GB_Pbx.Image = Image.FromFile(ScreenFile); });
            }
            catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); if (File.Exists(ScreenFile)) { File.Delete(ScreenFile); } }
        }

        private void BW_HudInstall_DoWork(object sender, DoWorkEventArgs e)
        {
            // Сохраняем предыдующий текст кнопки...
            string CaptText = HD_Install.Text;
            string InstallTmp = Path.Combine(SelGame.CustomInstallDir, "hudtemp");

            try
            {
                // Изменяем текст на "Идёт установка" и отключаем её...
                Invoke((MethodInvoker)delegate() { HD_Install.Text = AppStrings.HD_InstallBtnProgress; HD_Install.Enabled = false; });

                // Устанавливаем и очищаем временный каталог...
                try { Directory.Move(Path.Combine(InstallTmp, SelGame.HUDMan.SelectedHUD.FormatIntDir(SelGame.HUDMan.SelectedHUD.ArchiveDir)), Path.Combine(SelGame.CustomInstallDir, SelGame.HUDMan.SelectedHUD.InstallDir)); }
                finally { if (Directory.Exists(InstallTmp)) { Directory.Delete(InstallTmp, true); } }

                // Сохраняем или удаляем загруженный архив в зависимости от настроек приложения...
                if (!Properties.Settings.Default.HUDSaveArchives) { if (File.Exists(SelGame.HUDMan.SelectedHUD.LocalFile)) { File.Delete(SelGame.HUDMan.SelectedHUD.LocalFile); } }
            }
            finally
            {
                // Возвращаем сохранённый...
                Invoke((MethodInvoker)delegate() { HD_Install.Text = CaptText; HD_Install.Enabled = true; });
            }
        }

        private void BW_HudInstall_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Выводим сообщение...
            if (e.Error == null) { MessageBox.Show(AppStrings.HD_InstallSuccessfull, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information); } else { CoreLib.HandleExceptionEx(AppStrings.HD_InstallError, Properties.Resources.AppName, e.Error.Message, e.Error.Source, MessageBoxIcon.Error); }

            // Включаем кнопку удаления если HUD установлен...
            SetHUDButtons(HUDManager.CheckInstalledHUD(SelGame.CustomInstallDir, SelGame.HUDMan.SelectedHUD.InstallDir));
        }

        #endregion

        private void frmMainW_Load(object sender, EventArgs e)
        {
            // Событие инициализации формы...
            App = new CurrentApp();
            SourceGames = new List<SourceGame>();

            // Узнаем путь к установленному клиенту Steam...
            try { App.FullSteamPath = CoreLib.GetSteamPath(); } catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); ValidateAndHandle(); }

            // Начинаем платформо-зависимые процедуры...
            ChangePrvControlState(CoreLib.IsCurrentUserAdmin());

            // Сохраним последний путь к Steam в файл конфигурации...
            Properties.Settings.Default.LastSteamPath = App.FullSteamPath;

            // Вставляем информацию о версии в заголовок формы...
            Text = String.Format(Text, Properties.Resources.AppName, Properties.Resources.PlatformFriendlyName, App.AppVersionInfo);

            // Укажем статус Безопасной очистки...
            CheckSafeClnStatus();

            // Укажем путь к Steam на странице "Устранение проблем"...
            PS_StPath.Text = String.Format(PS_StPath.Text, App.FullSteamPath);
            
            // Проверим на наличие запрещённых символов в пути к установленному клиенту Steam...
            CheckSymbolsSteam(App.FullSteamPath);

            // Распознаем файловую систему на диске со Steam...
            DetectFS(App.FullSteamPath);

            // Запустим поиск установленных игр и проверим нашлось ли что-то...
            FindGames(App.FullSteamPath, Properties.Resources.SteamAppsFolderName, AppStrings.AppXMLParseError);

            try
            {
                // Проверим наличие обновлений программы (если разрешено в настройках)...
                if (Properties.Settings.Default.EnableAutoUpdate)
                {
                    if (!BW_UpChk.IsBusy) { BW_UpChk.RunWorkerAsync(); }
                }
            }
            catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); }
        }

        private void PS_CleanBlobs_CheckedChanged(object sender, EventArgs e)
        {
            // Управляем доступностью кнопки запуска очистки...
            PS_ExecuteNow.Enabled = PS_CleanBlobs.Checked || PS_CleanRegistry.Checked;
        }

        private void PS_CleanRegistry_CheckedChanged(object sender, EventArgs e)
        {
            // Включаем список с доступными языками клиента Steam...
            PS_SteamLang.Enabled = PS_CleanRegistry.Checked;

            // Выбираем язык по умолчанию согласно языку приложения...
            PS_SteamLang.SelectedIndex = Convert.ToInt32(AppStrings.AppDefaultSteamLangID);

            // Управляем доступностью кнопки запуска очистки...
            PS_ExecuteNow.Enabled = PS_CleanRegistry.Checked || PS_CleanBlobs.Checked;
        }

        private void PS_ExecuteNow_Click(object sender, EventArgs e)
        {
            // Начинаем очистку...

            // Запрашиваем подтверждение у пользователя...
            if (MessageBox.Show(AppStrings.PS_ExecuteMSG, Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                // Подтверждение получено...
                if ((PS_CleanBlobs.Checked) || (PS_CleanRegistry.Checked))
                {
                    // Найдём и завершим работу клиента Steam...
                    if (CoreLib.ProcessTerminate("Steam") != 0)
                    {
                        MessageBox.Show(AppStrings.PS_ProcessDetected, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    // Проверяем нужно ли чистить блобы...
                    if (PS_CleanBlobs.Checked)
                    {
                        try
                        {
                            // Чистим блобы...
                            CoreLib.CleanBlobsNow(App.FullSteamPath);
                        }
                        catch (Exception Ex)
                        {
                            CoreLib.HandleExceptionEx(AppStrings.PS_CleanException, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
                        }
                    }

                    // Проверяем нужно ли чистить реестр...
                    if (PS_CleanRegistry.Checked)
                    {
                        try
                        {
                            // Проверяем выбрал ли пользователь язык из выпадающего списка...
                            if (PS_SteamLang.SelectedIndex != -1)
                            {
                                // Всё в порядке, чистим реестр...
                                CoreLib.CleanRegistryNow(PS_SteamLang.SelectedIndex);
                            }
                            else
                            {
                                // Пользователь не выбрал язык, поэтому будем использовать английский...
                                MessageBox.Show(AppStrings.PS_NoLangSelected, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                CoreLib.CleanRegistryNow(0);
                            }
                        }
                        catch (Exception Ex)
                        {
                            CoreLib.HandleExceptionEx(AppStrings.PS_CleanException, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
                        }
                    }

                    // Выполнение закончено, выведем сообщение о завершении...
                    MessageBox.Show(AppStrings.PS_SeqCompleted, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Запустим Steam...
                    if (File.Exists(Path.Combine(App.FullSteamPath, Properties.Resources.SteamExecBin))) { Process.Start(Path.Combine(App.FullSteamPath, Properties.Resources.SteamExecBin)); }
                }
            }
        }

        private void frmMainW_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                // Запрашиваем подтверждение у пользователя на закрытие формы...
                e.Cancel = ((Properties.Settings.Default.ConfirmExit && !(MessageBox.Show(String.Format(AppStrings.FrmCloseQuery, Properties.Resources.AppName), Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)) || (BW_BkUpRecv.IsBusy || BW_FPRecv.IsBusy || BW_HudInstall.IsBusy || BW_HUDList.IsBusy || BW_HUDScreen.IsBusy || BW_UpChk.IsBusy));
            }
        }

        private void AppSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                // Получаем нужные значения...
                SelGame = SourceGames.Find(Item => String.Equals(Item.FullAppName, AppSelector.Text, StringComparison.CurrentCultureIgnoreCase));

                // Переключаем состояние некоторых контролов...
                HandleControlsOnSelGame();
                
                // Проверим наличие запрещённых символов в пути...
                CheckSymbolsGame(SelGame.FullGamePath);

                // Считаем настройки графики...
                if (SelGame.IsUsingVideoFile) { SelGame.UpdateVideoFilesList(SelGame.GameInternalID, App.FullSteamPath, SelGame.GamePath, SelGame.ConfDir); string VideoFile = SelGame.GetActualVideoFile(); if (File.Exists(VideoFile)) { ReadNCFGameSettings(VideoFile); } else { CoreLib.WriteStringToLog(String.Format(AppStrings.AppVideoDbNotFound, SelGame.FullAppName, VideoFile)); NullGraphOptions(); } } else { ReadGCFGameSettings(SelGame.SmallAppName); }

                // Переключаем графический твикер в режим GCF/NCF...
                SetGTOptsType(!SelGame.IsUsingVideoFile);

                // Проверим, установлен ли FPS-конфиг...
                HandleConfigs(SelGame.FullGamePath, SelGame.IsUsingUserDir);

                // Закроем открытые конфиги в редакторе...
                if (!(String.IsNullOrEmpty(CFGFileName))) { CloseEditorConfigs(); }

                // Считаем имеющиеся FPS-конфиги...
                if (!BW_FPRecv.IsBusy) { BW_FPRecv.RunWorkerAsync(); }

                // Обновляем статус...
                UpdateStatusBar(MainTabControl.SelectedIndex);

                // Сохраним ID последней выбранной игры...
                Properties.Settings.Default.LastGameName = AppSelector.Text;

                // Переключаем вид страницы менеджера HUD...
                HandleHUDMode(SelGame.IsHUDsAvailable);

                // Считаем список доступных HUD для данной игры...
                if (SelGame.IsHUDsAvailable) { if (!BW_HUDList.IsBusy) { BW_HUDList.RunWorkerAsync(); } }
                
                // Считаем список бэкапов...
                if (!BW_BkUpRecv.IsBusy) { BW_BkUpRecv.RunWorkerAsync(); }

                // Создадим каталоги...
                if (!Directory.Exists(SelGame.AppHUDDir)) { Directory.CreateDirectory(SelGame.AppHUDDir); }
            }
            catch (Exception Ex)
            {
                CoreLib.HandleExceptionEx(AppStrings.AppFailedToGetData, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
            }
        }

        private void AppRefresh_Click(object sender, EventArgs e)
        {
            // Попробуем обновить список игр...
            FindGames(App.FullSteamPath, Properties.Resources.SteamAppsFolderName, AppStrings.AppXMLParseError);
        }

        private void GT_Maximum_Graphics_Click(object sender, EventArgs e)
        {
            // Нажатие этой кнопки устанавливает графические настройки на рекомендуемый максимум...
            // Зададим вопрос, а нужно ли это юзеру?
            if (MessageBox.Show(AppStrings.GT_MaxPerfMsg, Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (!SelGame.IsUsingVideoFile)
                {
                    // Пользователь согласился, продолжаем...
                    GT_ScreenType.SelectedIndex = 0; // полноэкранный режим
                    GT_ModelQuality.SelectedIndex = 2; // высокая детализация моделей
                    GT_TextureQuality.SelectedIndex = 2; // высокая детализация текстур
                    GT_ShaderQuality.SelectedIndex = 1; // высокое качество шейдерных эффектов
                    GT_WaterQuality.SelectedIndex = 1; // отражать мир в воде
                    GT_ShadowQuality.SelectedIndex = 1; // высокое качество теней
                    GT_ColorCorrectionT.SelectedIndex = 1; // корренкция цвета включена
                    GT_AntiAliasing.SelectedIndex = 5; // сглаживание MSAA 8x
                    GT_Filtering.SelectedIndex = 5; // анизотропная фильтрация 16x
                    GT_VSync.SelectedIndex = 0; // вертикальная синхронизация выключена
                    GT_MotionBlur.SelectedIndex = 0; // размытие движения выключено
                    GT_DxMode.SelectedIndex = 3; // режим DirecX 9.0c
                    GT_HDR.SelectedIndex = 2; // HDR полные
                }
                else
                {
                    GT_NCF_DispMode.SelectedIndex = 0;
                    GT_NCF_Ratio.SelectedIndex = 1;
                    GT_NCF_Brightness.Text = "22";
                    GT_NCF_AntiAlias.SelectedIndex = 5;
                    GT_NCF_Filtering.SelectedIndex = 5;
                    GT_NCF_Shadows.SelectedIndex = 3;
                    GT_NCF_MBlur.SelectedIndex = 1;
                    GT_NCF_VSync.SelectedIndex = 0;
                    GT_NCF_Multicore.SelectedIndex = 1;
                    GT_NCF_ShaderE.SelectedIndex = 3;
                    GT_NCF_EffectD.SelectedIndex = 2;
                    GT_NCF_MemPool.SelectedIndex = 2;
                    GT_NCF_Quality.SelectedIndex = 2;
                }
                MessageBox.Show(AppStrings.GT_PerfSet, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void GT_Maximum_Performance_Click(object sender, EventArgs e)
        {
            // Нажатие этой кнопки устанавливает графические настройки на рекомендуемый минимум...
            // Спросим пользователя.
            if (MessageBox.Show(AppStrings.GT_MinPerfMsg, Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (!SelGame.IsUsingVideoFile)
                {
                    // Пользователь согласился, продолжаем...
                    GT_ScreenType.SelectedIndex = 0; // полноэкранный режим
                    GT_ModelQuality.SelectedIndex = 0; // низкая детализация моделей
                    GT_TextureQuality.SelectedIndex = 0; // низкая детализация текстур
                    GT_ShaderQuality.SelectedIndex = 0; // низкое качество шейдерных эффектов
                    GT_WaterQuality.SelectedIndex = 0; // простые отражения в воде
                    GT_ShadowQuality.SelectedIndex = 0; // низкое качество теней
                    GT_ColorCorrectionT.SelectedIndex = 0; // корренкция цвета выключена
                    GT_AntiAliasing.SelectedIndex = 0; // сглаживание выключено
                    GT_Filtering.SelectedIndex = 1; // трилинейная фильтрация текстур
                    GT_VSync.SelectedIndex = 0; // вертикальная синхронизация выключена
                    GT_MotionBlur.SelectedIndex = 0; // размытие движения выключено
                    GT_DxMode.SelectedIndex = MessageBox.Show(AppStrings.GT_DxLevelMsg, Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes ? 0 : 3; // Спросим у пользователя о режиме DirectX...
                    GT_HDR.SelectedIndex = 0; // эффекты HDR выключены
                }
                else
                {
                    GT_NCF_DispMode.SelectedIndex = 0;
                    GT_NCF_Ratio.SelectedIndex = 1;
                    GT_NCF_Brightness.Text = "22";
                    GT_NCF_AntiAlias.SelectedIndex = 0;
                    GT_NCF_Filtering.SelectedIndex = 1;
                    GT_NCF_Shadows.SelectedIndex = 0;
                    GT_NCF_MBlur.SelectedIndex = 0;
                    GT_NCF_VSync.SelectedIndex = 0;
                    GT_NCF_Multicore.SelectedIndex = 1;
                    GT_NCF_ShaderE.SelectedIndex = 0;
                    GT_NCF_EffectD.SelectedIndex = 0;
                    GT_NCF_MemPool.SelectedIndex = 0;
                    GT_NCF_Quality.SelectedIndex = 0;
                }
                MessageBox.Show(AppStrings.GT_PerfSet, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void GT_SaveApply_Click(object sender, EventArgs e)
        {
            // Сохраняем изменения в графических настройках...
            if (ValidateGameSettings(SelGame.IsUsingVideoFile))
            {
                // Запрашиваем подтверждение у пользователя...
                if (MessageBox.Show(AppStrings.GT_SaveMsg, Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    // Определим тип игры...
                    if (!SelGame.IsUsingVideoFile)
                    {
                        // Это GCF-приложение, будем писать настройки в реестр...
                        if (Properties.Settings.Default.SafeCleanup)
                        {
                            // Создаём резервную копию...
                            try { CoreLib.CreateRegBackUpNow(Path.Combine("HKEY_CURRENT_USER", "Software", "Valve", "Source", SelGame.SmallAppName, "Settings"), "Game_AutoBackUp", SelGame.FullBackUpDirPath); } catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); }
                        }

                        try
                        {
                            // Проверим существование ключа реестра и в случае необходимости создадим...
                            if (!(CoreLib.CheckIfHKCUSKeyExists(Path.Combine("Software", "Valve", "Source", SelGame.SmallAppName, "Settings")))) { Registry.CurrentUser.CreateSubKey(Path.Combine("Software", "Valve", "Source", SelGame.SmallAppName, "Settings")); }
                            
                            // Записываем выбранные настройки в реестр...
                            WriteGCFGameSettings(SelGame.SmallAppName);
                            
                            // Выводим подтверждающее сообщение...
                            MessageBox.Show(AppStrings.GT_SaveSuccess, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception Ex) { CoreLib.HandleExceptionEx(AppStrings.GT_SaveFailure, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning); }
                    }
                    else
                    {
                        // Создадим бэкап файла с графическими настройками...
                        if (Properties.Settings.Default.SafeCleanup)
                        {
                            CoreLib.CreateConfigBackUp(SelGame.VideoCfgFiles, SelGame.FullBackUpDirPath, Properties.Resources.BU_PrefixVidAuto);
                        }

                        try
                        {
                            // Записываем...
                            WriteNCFGameSettings(SelGame.GetActualVideoFile());
                            
                            // Выводим сообщение...
                            MessageBox.Show(AppStrings.GT_SaveSuccess, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception Ex)
                        {
                            // Произошла ошибка...
                            CoreLib.HandleExceptionEx(AppStrings.GT_NCFFailure, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show(AppStrings.GT_NCFNReady, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void FP_ConfigSel_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                // Загружаем данные выбранного конфига...
                try { SelGame.CFGMan.Select(FP_ConfigSel.Text); } catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); }

                // Выводим описание...
                FP_Description.Text = SelGame.CFGMan.FPSConfig.Description;

                // Включаем кнопку открытия конфига в Блокноте...
                FP_OpenNotepad.Enabled = true;

                // Включаем кнопку установки конфига...
                FP_Install.Enabled = true;
            }
            catch (Exception Ex)
            {
                // Не получилось загрузить описание выбранного конфига. Выведем стандартное сообщение...
                CoreLib.WriteStringToLog(Ex.Message);
                FP_Description.Text = AppStrings.FP_NoDescr;
            }
        }

        private void FP_Install_Click(object sender, EventArgs e)
        {
            // Начинаем устанавливать FPS-конфиг в управляемое приложение...
            if (FP_ConfigSel.SelectedIndex != -1)
            {
                if (MessageBox.Show(AppStrings.FP_InstallQuestion, Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    // Проверим, не нужно ли создавать резервную копию...
                    if (Properties.Settings.Default.SafeCleanup)
                    {
                        // Проверяем есть ли установленные конфиги...
                        if (SelGame.FPSConfigs.Count > 0)
                        {
                            // Создаём резервную копию...
                            CoreLib.CompressFiles(SelGame.FPSConfigs, CoreLib.GenerateBackUpFileName(SelGame.FullBackUpDirPath, Properties.Resources.BU_PrefixCfg));
                        }
                    }

                    try
                    {
                        // Устанавливаем...
                        CoreLib.InstallConfigNow(SelGame.CFGMan.FPSConfig.FileName, App.FullAppPath, SelGame.FullGamePath, SelGame.IsUsingUserDir);
                        
                        // Выводим сообщение об успешной установке...
                        MessageBox.Show(AppStrings.FP_InstallSuccessful, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        // Перечитаем конфиги...
                        HandleConfigs(SelGame.FullGamePath, SelGame.IsUsingUserDir);
                    }
                    catch (Exception Ex)
                    {
                        // Установка не удалась. Выводим сообщение об этом...
                        CoreLib.HandleExceptionEx(AppStrings.FP_InstallFailed, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
                    }
                }
            }
            else
            {
                // Пользователь не выбрал конфиг. Сообщим об этом...
                MessageBox.Show(AppStrings.FP_NothingSelected, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void FP_Uninstall_Click(object sender, EventArgs e)
        {
            try
            {
                // Проверим есть ли кандидаты на удаление...
                if (SelGame.FPSConfigs.Count > 0)
                {
                    // Удаляем конфиги...
                    CoreLib.OpenCleanupWindow(SelGame.FPSConfigs, ((Button)sender).Text.ToLower(), AppStrings.FP_RemoveSuccessful, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile, false, false, false, Properties.Settings.Default.SafeCleanup);

                    // Перечитаем список конфигов...
                    HandleConfigs(SelGame.FullGamePath, SelGame.IsUsingUserDir);
                }
                else
                {
                    MessageBox.Show(AppStrings.FP_RemoveNotExists, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception Ex)
            {
                CoreLib.HandleExceptionEx(AppStrings.FP_RemoveFailed, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Error);
            }
        }

        private void GT_Warning_Click(object sender, EventArgs e)
        {
            // Выдадим сообщение о наличии FPS-конфига...
            MessageBox.Show(AppStrings.GT_FPSCfgDetected, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void CE_New_Click(object sender, EventArgs e)
        {
            // Закрываем все открытые конфиги в Редакторе конфигов и создаём новый пустой файл...
            CloseEditorConfigs();

            // Обновляем содержимое строки статуса...
            UpdateStatusBar(MainTabControl.SelectedIndex);
        }

        private void CE_Open_Click(object sender, EventArgs e)
        {
            // Прочитаем конфиг и заполним его содержимым нашу таблицу редактора...
            
            // Указываем стартовый каталог в диалоге открытия файла на каталог с конфигами игры...
            CE_OpenCfgDialog.InitialDirectory = SelGame.FullCfgPath;

            // Считывает файл конфига и помещает записи в таблицу
            if (CE_OpenCfgDialog.ShowDialog() == DialogResult.OK) // Отображаем стандартный диалог открытия файла...
            {
                // Считываем...
                ReadConfigFromFile(CE_OpenCfgDialog.FileName);
            }
        }

        private void CE_Save_Click(object sender, EventArgs e)
        {
            // Указываем путь по умолчанию к конфигам управляемого приложения...
            CE_SaveCfgDialog.InitialDirectory = SelGame.FullCfgPath;

            // Проверяем, открыт ли какой-либо файл...
            if (!(String.IsNullOrEmpty(CFGFileName)))
            {
                // Будем бэкапить все файлы, сохраняемые в Редакторе...
                if (Properties.Settings.Default.SafeCleanup)
                {
                    // Создаём резервную копию...
                    if (File.Exists(CFGFileName)) { CoreLib.CreateConfigBackUp(CoreLib.SingleToArray(CFGFileName), SelGame.FullBackUpDirPath, Properties.Resources.BU_PrefixCfg); }
                }

                // Начинаем сохранение в тот же файл...
                try { WriteTableToFileNow(CFGFileName); } catch (Exception Ex) { CoreLib.HandleExceptionEx(AppStrings.CE_CfgSVVEx, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning); }
            }
            else
            {
                // Зададим стандартное имя (см. issue 21)...
                CE_SaveCfgDialog.FileName = File.Exists(Path.Combine(SelGame.FullCfgPath, "autoexec.cfg")) ? AppStrings.UnnamedFileName : "autoexec.cfg";

                // Файл не был открыт. Отображаем стандартный диалог сохранения файла...
                if (CE_SaveCfgDialog.ShowDialog() == DialogResult.OK)
                {
                    WriteTableToFileNow(CE_SaveCfgDialog.FileName);
                    CFGFileName = CE_SaveCfgDialog.FileName;
                    UpdateStatusBar(MainTabControl.SelectedIndex);
                }
            }
        }

        private void CE_SaveAs_Click(object sender, EventArgs e)
        {
            // Сохраняем файл с другим, выбранным пользователем, именем...
            CE_SaveCfgDialog.InitialDirectory = SelGame.FullCfgPath;

            // Отображаем стандартный диалог сохранения файла...
            if (CE_SaveCfgDialog.ShowDialog() == DialogResult.OK)
            {
                WriteTableToFileNow(CE_SaveCfgDialog.FileName);
            }
        }

        private void PS_RemCustMaps_Click(object sender, EventArgs e)
        {
            // Удаляем кастомные (нестандартные) карты...
            List<String> CleanDirs = new List<string>();
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "custom", "*.bsp"));
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "download", "*.bsp"));
            CleanDirs.Add(Path.Combine(SelGame.AppWorkshopDir, "*.bsp"));
            if (Properties.Settings.Default.AllowUnSafeCleanup) { CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "maps", "*.bsp")); }
            CoreLib.OpenCleanupWindow(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void PS_RemDnlCache_Click(object sender, EventArgs e)
        {
            // Удаляем кэш загрузок...
            List<String> CleanDirs = new List<string>();
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "download", "*.*"));
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "downloads", "*.*"));
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "streams", "*.*"));
            CoreLib.OpenCleanupWindow(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void PS_RemSoundCache_Click(object sender, EventArgs e)
        {
            // Удаляем звуковой кэш...
            List<String> CleanDirs = new List<string>();
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "maps", "graphs", "*.*"));
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "maps", "soundcache", "*.*"));
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "download", "sound", "*.*"));
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "*.cache"));
            CoreLib.OpenCleanupWindow(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void PS_RemScreenShots_Click(object sender, EventArgs e)
        {
            // Удаляем все скриншоты...
            List<String> CleanDirs = new List<string>();
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "screenshots", "*.*"));
            CoreLib.OpenCleanupWindow(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile, false, false, false);
        }

        private void PS_RemDemos_Click(object sender, EventArgs e)
        {
            // Удаляем все записанные демки...
            List<String> CleanDirs = new List<string>();
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "demos", "*.*"));
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "*.dem"));
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "*.mp4"));
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "*.tga"));
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "*.wav"));
            CoreLib.OpenCleanupWindow(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile, false, false, false, false);
        }

        private void PS_RemGameOpts_Click(object sender, EventArgs e)
        {
            // Создаём список файлов для удаления...
            List<String> CleanDirs = new List<string>();

            // Запрашиваем у пользователя подтверждение удаления...
            if (MessageBox.Show(String.Format(AppStrings.AppQuestionTemplate, ((Button)sender).Text), Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                try
                {
                    // Удаляем графические настройки...
                    if (!SelGame.IsUsingVideoFile)
                    {
                        // Создаём резервную копию куста реестра...
                        if (Properties.Settings.Default.SafeCleanup) { try { CoreLib.CreateRegBackUpNow(Path.Combine("HKEY_CURRENT_USER", "Software", "Valve", "Source", SelGame.SmallAppName, "Settings"), "Game_AutoBackUp", SelGame.FullBackUpDirPath); } catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); } }

                        // Удаляем ключ HKEY_CURRENT_USER\Software\Valve\Source\tf\Settings из реестра...
                        Registry.CurrentUser.DeleteSubKeyTree(Path.Combine("Software", "Valve", "Source", SelGame.SmallAppName, "Settings"), false);
                    }
                    else
                    {
                        // Создадим бэкап файла с графическими настройками...
                        if (Properties.Settings.Default.SafeCleanup) { CoreLib.CreateConfigBackUp(SelGame.VideoCfgFiles, SelGame.FullBackUpDirPath, Properties.Resources.BU_PrefixVidAuto); }

                        // Помечаем его на удаление...
                        CleanDirs.AddRange(SelGame.VideoCfgFiles);
                    }

                    // Создаём резервную копию...
                    if (Properties.Settings.Default.SafeCleanup) { CoreLib.CreateConfigBackUp(SelGame.CloudConfigs, SelGame.FullBackUpDirPath, Properties.Resources.BU_PrefixCfg); }

                    // Помечаем конфиги игры на удаление...
                    CleanDirs.Add(Path.Combine(SelGame.FullCfgPath, "config.cfg"));
                    CleanDirs.AddRange(SelGame.CloudConfigs);

                    // Удаляем всю очередь...
                    CoreLib.RemoveFileDirectoryEx(CleanDirs);

                    // Выводим сообщение...
                    MessageBox.Show(AppStrings.PS_CleanupSuccess, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception Ex)
                {
                    CoreLib.HandleExceptionEx(AppStrings.PS_CleanupErr, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
                }
            }
        }

        private void PS_RemOldBin_Click(object sender, EventArgs e)
        {
            // Удаляем старые бинарники...
            List<String> CleanDirs = new List<string>();
            CleanDirs.Add(Path.Combine(SelGame.GamePath, Path.GetDirectoryName(SelGame.SmallAppName), "bin", "*.*"));
            if (Properties.Settings.Default.AllowUnSafeCleanup) { CleanDirs.Add(Path.Combine(SelGame.GamePath, "platform", "*.*")); }
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "bin", "*.*"));
            CleanDirs.Add(Path.Combine(SelGame.GamePath, "*.exe"));
            CoreLib.OpenCleanupWindow(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CacheChkReq, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void PS_CheckCache_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(String.Format(AppStrings.AppQuestionTemplate, ((Button)sender).Text), Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                try { Process.Start(String.Format("steam://validate/{0}", SelGame.GameInternalID)); } catch (Exception Ex) { CoreLib.HandleExceptionEx(AppStrings.AppStartSteamFailed, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning); }
            }
        }

        private void MNUReportBuilder_Click(object sender, EventArgs e)
        {
            if ((AppSelector.Items.Count > 0) && (AppSelector.SelectedIndex != -1))
            {
                // Запускаем форму создания отчёта для Техподдержки...
                using (frmRepBuilder RBF = new frmRepBuilder(App.AppUserDir, App.FullSteamPath, SelGame.FullCfgPath))
                {
                    RBF.ShowDialog();
                }
            }
            else
            {
                MessageBox.Show(AppStrings.AppNoGamesSelected, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void MNUInstaller_Click(object sender, EventArgs e)
        {
            // Запускаем форму установщика спреев, демок и конфигов...
            using (frmInstaller InstF = new frmInstaller(SelGame.FullGamePath, SelGame.IsUsingUserDir, SelGame.CustomInstallDir))
            {
                InstF.ShowDialog();
            }
        }

        private void MNUExit_Click(object sender, EventArgs e)
        {
            // Завершаем работу программы...
            Environment.Exit(0);
        }

        private void MNUAbout_Click(object sender, EventArgs e)
        {
            // Отобразим форму "О программе"...
            using (frmAbout AboutFrm = new frmAbout())
            {
                AboutFrm.ShowDialog();
            }
        }

        private void MNUReportBug_Click(object sender, EventArgs e)
        {
            // Перейдём в баг-трекер...
            CoreLib.OpenWebPage(Properties.Resources.AppBtURL);
        }

        private void BUT_Refresh_Click(object sender, EventArgs e)
        {
            // Обновим список резервных копий...
            UpdateBackUpList(SelGame.FullBackUpDirPath);
        }

        private void BUT_RestoreB_Click(object sender, EventArgs e)
        {
            // Восстановим выделенный бэкап...
            if (BU_LVTable.Items.Count > 0)
            {
                if (BU_LVTable.SelectedItems.Count > 0)
                {
                    // Получаем имя файла...
                    string FName = BU_LVTable.SelectedItems[0].SubItems[4].Text;

                    // Запрашиваем подтверждение...
                    if (MessageBox.Show(String.Format(AppStrings.BU_QMsg, Path.GetFileNameWithoutExtension(FName), BU_LVTable.SelectedItems[0].SubItems[3].Text), Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                    {
                        // Проверяем что восстанавливать: конфиг или реестр...
                        switch (Path.GetExtension(FName))
                        {
                            case ".reg":
                                // Восстанавливаем файл реестра...
                                try
                                {
                                    // Восстанавливаем...
                                    Process.Start("regedit.exe", String.Format("/s \"{0}\"", Path.Combine(SelGame.FullBackUpDirPath, FName)));
                                    
                                    // Показываем сообщение об успешном восстановлении...
                                    MessageBox.Show(AppStrings.BU_RestSuccessful, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                catch (Exception Ex)
                                {
                                    // Произошло исключение...
                                    CoreLib.HandleExceptionEx(AppStrings.BU_RestFailed, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
                                }
                                break;
                            case ".bud":
                                // Распаковываем архив с выводом прогресса...
                                CoreLib.ExtractFiles(Path.Combine(SelGame.FullBackUpDirPath, FName), Path.GetPathRoot(App.FullSteamPath));
                                
                                // Обновляем список FPS-конфигов...
                                HandleConfigs(SelGame.FullGamePath, SelGame.IsUsingUserDir);
                                
                                // Выводим сообщение об успехе...
                                MessageBox.Show(AppStrings.BU_RestSuccessful, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                                break;
                            default:
                                // Выводим сообщение о неизвестном формате резервной копии...
                                MessageBox.Show(AppStrings.BU_UnknownType, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                break;
                        }
                    }
                }
                else
                {
                    MessageBox.Show(AppStrings.BU_NoSelected, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show(AppStrings.BU_NoFiles, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BUT_DelB_Click(object sender, EventArgs e)
        {
            if (BU_LVTable.Items.Count > 0)
            {
                if (BU_LVTable.SelectedItems.Count > 0)
                {
                    // Удалим выбранный бэкап...
                    string FName = BU_LVTable.SelectedItems[0].SubItems[4].Text;
                    
                    // Запросим подтверждение...
                    if (MessageBox.Show(AppStrings.BU_DelMsg, Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                    {
                        try
                        {
                            // Удаляем файл...
                            File.Delete(Path.Combine(SelGame.FullBackUpDirPath, FName));
                            
                            // Удаляем строку...
                            BU_LVTable.Items.Remove(BU_LVTable.SelectedItems[0]);
                            
                            // Показываем сообщение об успешном удалении...
                            MessageBox.Show(AppStrings.BU_DelSuccessful, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception Ex)
                        {
                            // Произошло исключение при попытке удаления файла резервной копии...
                            CoreLib.HandleExceptionEx(AppStrings.BU_DelFailed, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
                        }
                    }
                }
                else
                {
                    MessageBox.Show(AppStrings.BU_NoSelected, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show(AppStrings.BU_NoFiles, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BUT_CrBkupReg_ButtonClick(object sender, EventArgs e)
        {
            // Отображаем выпадающее меню...
            BUT_CrBkupReg.ShowDropDown();
        }

        private void BUT_L_GameSettings_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(AppStrings.BU_RegCreate, Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // Создадим резервную копию графических настроек игры...
                try
                {
                    if (!SelGame.IsUsingVideoFile)
                    {
                        // Создаём конфиг ветки реестра...
                        CoreLib.CreateRegBackUpNow(Path.Combine("HKEY_CURRENT_USER", "Software", "Valve", "Source", SelGame.SmallAppName, "Settings"), "Game_Options", SelGame.FullBackUpDirPath);
                    }
                    else
                    {
                        // Проверяем существование файла с графическими настройками игры...
                        CoreLib.CreateConfigBackUp(SelGame.VideoCfgFiles, SelGame.FullBackUpDirPath, Properties.Resources.BU_PrefixVideo);
                    }
                    
                    // Выводим сообщение об успехе...
                    MessageBox.Show(AppStrings.BU_RegDone, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Обновляем список резервных копий...
                    UpdateBackUpList(SelGame.FullBackUpDirPath);
                }
                catch (Exception Ex)
                {
                    // Выводим сообщение об ошибке и пишем в журнал отладки...
                    CoreLib.HandleExceptionEx(AppStrings.BU_RegErr, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
                }
            }
        }

        private void BUT_L_AllSteam_Click(object sender, EventArgs e)
        {
            // Создадим резервную копию всех настроек Steam...
            if (MessageBox.Show(AppStrings.BU_RegCreate, Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    // Создаём...
                    CoreLib.CreateRegBackUpNow(Path.Combine("HKEY_CURRENT_USER", "Software", "Valve"), "Steam_BackUp", SelGame.FullBackUpDirPath);
                    
                    // Выводим сообщение...
                    MessageBox.Show(AppStrings.BU_RegDone, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Обновим список бэкапов...
                    UpdateBackUpList(SelGame.FullBackUpDirPath);
                }
                catch (Exception Ex)
                {
                    // Произошло исключение, уведомим пользователя...
                    CoreLib.HandleExceptionEx(AppStrings.BU_RegErr, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
                }
            }
        }

        private void BUT_L_AllSRC_Click(object sender, EventArgs e)
        {
            // Созданим резервную копию графических настроек всех Source-игр...
            if (MessageBox.Show(AppStrings.BU_RegCreate, Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    CoreLib.CreateRegBackUpNow(Path.Combine("HKEY_CURRENT_USER", "Software", "Valve", "Source"), "Source_Options", SelGame.FullBackUpDirPath);
                    MessageBox.Show(AppStrings.BU_RegDone, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    UpdateBackUpList(SelGame.FullBackUpDirPath);
                }
                catch (Exception Ex)
                {
                    CoreLib.HandleExceptionEx(AppStrings.BU_RegErr, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
                }
            }
        }

        private void MainTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Пользователь переключил вкладку. Обновляем содержимое строки статуса...
            UpdateStatusBar(((TabControl)sender).SelectedIndex);
        }

        private void CE_ShowHint_Click(object sender, EventArgs e)
        {
            try
            {
                string Buf = CE_Editor.Rows[CE_Editor.CurrentRow.Index].Cells[0].Value.ToString();
                if (!(String.IsNullOrEmpty(Buf))) { Buf = GetCVDescription(Buf); if (!(String.IsNullOrEmpty(Buf))) { MessageBox.Show(Buf, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information); } else { MessageBox.Show(AppStrings.CE_ClNoDescr, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning); } } else { MessageBox.Show(AppStrings.CE_ClSelErr, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            }
            catch
            {
                MessageBox.Show(AppStrings.CE_ClSelErr, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void MNUHelp_Click(object sender, EventArgs e)
        {
            // Отобразим справочную систему в зависимости от контекста...
            CoreLib.OpenWebPage(GetHelpWebPage(MainTabControl.SelectedIndex));
        }

        private void MNUOpinion_Click(object sender, EventArgs e)
        {
            CoreLib.OpenWebPage(Properties.Resources.AppURLReply);
        }

        private void MNUSteamGroup_Click(object sender, EventArgs e)
        {
            try { Process.Start(Properties.Resources.AppURLSteamGrID); } catch { CoreLib.OpenWebPage(Properties.Resources.AppURLSteamGroup); }
        }

        private void MNULnkEasyCoding_Click(object sender, EventArgs e)
        {
            CoreLib.OpenWebPage(Properties.Resources.AppURLOffSite);
        }

        private void MNULnkTFRU_Click(object sender, EventArgs e)
        {
            CoreLib.OpenWebPage(Properties.Resources.AppURLSpnTFSU);
        }

        private void MNUHEd_Click(object sender, EventArgs e)
        {
            // Отобразим форму редактора файла hosts...
            using (frmHEd HEdFrm = new frmHEd())
            {
                HEdFrm.ShowDialog();
            }
        }

        private void CE_RmRow_Click(object sender, EventArgs e)
        {
            try
            {
                if (CE_Editor.Rows.Count > 0)
                {
                    CE_Editor.Rows.Remove(CE_Editor.CurrentRow);
                }
            }
            catch (Exception Ex)
            {
                CoreLib.WriteStringToLog(Ex.Message);
            }
        }

        private void CE_Copy_Click(object sender, EventArgs e)
        {
            if (CE_Editor.Rows[CE_Editor.CurrentRow.Index].Cells[CE_Editor.CurrentCell.ColumnIndex].Value != null)
            {
                Clipboard.SetText(CE_Editor.Rows[CE_Editor.CurrentRow.Index].Cells[CE_Editor.CurrentCell.ColumnIndex].Value.ToString());
            }
        }

        private void CE_Cut_Click(object sender, EventArgs e)
        {
            if (CE_Editor.Rows[CE_Editor.CurrentRow.Index].Cells[CE_Editor.CurrentCell.ColumnIndex].Value != null)
            {
                // Копируем в буфер...
                Clipboard.SetText(CE_Editor.Rows[CE_Editor.CurrentRow.Index].Cells[CE_Editor.CurrentCell.ColumnIndex].Value.ToString());
                
                // Удаляем из ячейки...
                CE_Editor.Rows[CE_Editor.CurrentRow.Index].Cells[CE_Editor.CurrentCell.ColumnIndex].Value = null;
            }
        }

        private void CE_Paste_Click(object sender, EventArgs e)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    CE_Editor.Rows[CE_Editor.CurrentRow.Index].Cells[CE_Editor.CurrentCell.ColumnIndex].Value = Clipboard.GetText();
                }
            }
            catch (Exception Ex)
            {
                CoreLib.WriteStringToLog(Ex.Message);
            }
        }

        private void FP_OpenNotepad_Click(object sender, EventArgs e)
        {
            // Сгенерируем путь к файлу...
            string ConfigFile = Path.Combine(App.FullAppPath, "cfgs", FP_ConfigSel.Text);
            
            // Проверим зажал ли пользователь Shift перед тем, как кликнуть по кнопке...
            if (Control.ModifierKeys == Keys.Shift)
            {
                // Загрузим выбранный конфиг в Редактор конфигов...
                ReadConfigFromFile(ConfigFile);

                // Переключимся на него...
                MainTabControl.SelectedIndex = 1;
            }
            else
            {
                // Загрузим файл в Блокноте...
                CoreLib.OpenTextEditor(ConfigFile);
            }
        }

        private void MNUUpdateCheck_Click(object sender, EventArgs e)
        {
            // Откроем форму модуля проверки обновлений...
            using (frmUpdate UpdFrm = new frmUpdate(App.UserAgent, App.FullAppPath, App.AppVersionInfo, App.AppUserDir))
            {
                UpdFrm.ShowDialog();
            }
            
            // Перечитаем базу игр...
            FindGames(App.FullSteamPath, Properties.Resources.SteamAppsFolderName, AppStrings.AppXMLParseError);
        }

        private void BUT_OpenNpad_Click(object sender, EventArgs e)
        {
            // Откроем выбранный бэкап в Блокноте Windows...
            if (BU_LVTable.Items.Count > 0)
            {
                if (BU_LVTable.SelectedItems.Count > 0)
                {
                    if (Regex.IsMatch(Path.GetExtension(BU_LVTable.SelectedItems[0].SubItems[4].Text), @"\.(txt|cfg|[0-9]|reg)")) { CoreLib.OpenTextEditor(Path.Combine(SelGame.FullBackUpDirPath, BU_LVTable.SelectedItems[0].SubItems[4].Text)); } else { MessageBox.Show(AppStrings.BU_BinaryFile, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                }
                else
                {
                    MessageBox.Show(AppStrings.BU_NoSelected, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show(AppStrings.BU_NoFiles, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void MNUAppOptions_Click(object sender, EventArgs e)
        {
            // Показываем форму настроек...
            using (frmOptions OptsFrm = new frmOptions())
            {
                OptsFrm.ShowDialog();
            }
        }

        private void BU_LVTable_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            // Запрещаем изменение размеров столбцов таблицы...
            e.NewWidth = BU_LVTable.Columns[e.ColumnIndex].Width;
            e.Cancel = true;
        }

        private void BUT_ExploreBUp_Click(object sender, EventArgs e)
        {
            if (BU_LVTable.Items.Count > 0)
            {
                if (BU_LVTable.SelectedItems.Count > 0)
                {
                    // Откроем выбранный бэкап в Проводнике Windows...
                    CoreLib.OpenExplorer(Path.Combine(SelGame.FullBackUpDirPath, BU_LVTable.SelectedItems[0].SubItems[4].Text));
                }
                else
                {
                    MessageBox.Show(AppStrings.BU_NoSelected, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show(AppStrings.BU_NoFiles, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void frmMainW_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Форма была закрыта, сохраняем настройки приложения...
            Properties.Settings.Default.Save();
        }

        private void MNUWinMnuDisabler_Click(object sender, EventArgs e)
        {
            // Показываем модуля отключения клавиш...
            using (frmKBHelper KBHlp = new frmKBHelper())
            {
                KBHlp.ShowDialog();
            }
        }

        private void CE_OpenInNotepad_Click(object sender, EventArgs e)
        {
            if (!(String.IsNullOrEmpty(CFGFileName)))
            {
                CoreLib.OpenTextEditor(CFGFileName);
            }
            else
            {
                MessageBox.Show(AppStrings.CE_NoFileOpened, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void PS_PathDetector_Click(object sender, EventArgs e)
        {
            if (((Label)sender).ForeColor == Color.Red) { MessageBox.Show(AppStrings.SteamNonASCIIDetected, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning); } else { MessageBox.Show(AppStrings.SteamNonASCIINotDetected, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }

        private void PS_PathGame_Click(object sender, EventArgs e)
        {
            if (((Label)sender).ForeColor == Color.Red) { MessageBox.Show(AppStrings.GameNonASCIIDetected, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning); } else { MessageBox.Show(AppStrings.GameNonASCIINotDetected, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }

        private void PS_RemReplays_Click(object sender, EventArgs e)
        {
            // Удаляем все реплеи...
            List<String> CleanDirs = new List<string>();
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "replay", "*.*"));
            CoreLib.OpenCleanupWindow(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void PS_RemTextures_Click(object sender, EventArgs e)
        {
            // Удаляем все кастомные текстуры...
            List<String> CleanDirs = new List<string>();
            
            // Чистим базы игр со старой системой. Удалить после полного перехода на новую...
            if (Properties.Settings.Default.AllowUnSafeCleanup)
            {
                CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "materials", "*.*"));
                CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "models", "*.*"));
            }
            
            // Чистим загруженные с серверов модели и текстуры...
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "download", "*.vt*"));
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "download", "*.vmt"));
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "download", "*.mdl"));
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "download", "*.phy"));
            
            // Чистим установленные пользователем модели и текстуры...
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "custom", "*.vt*"));
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "custom", "*.vmt"));
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "custom", "*.mdl"));
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "custom", "*.phy"));
            CoreLib.OpenCleanupWindow(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void PS_RemSecndCache_Click(object sender, EventArgs e)
        {
            // Удаляем содержимое вторичного кэша загрузок...
            List<String> CleanDirs = new List<string>();
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "cache", "*.*")); // Кэш...
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "custom", "user_custom", "*.*")); // Кэш спреев игр с н.с.к...
            CleanDirs.Add(Path.Combine(SelGame.GamePath, "config", "html", "*.*")); // Кэш MOTD...
            CoreLib.OpenCleanupWindow(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void SB_App_DoubleClick(object sender, EventArgs e)
        {
            // Переключим статус безопасной очистки...
            Properties.Settings.Default.SafeCleanup = !Properties.Settings.Default.SafeCleanup;
            
            // Сообщим пользователю если он отключил безопасную очистку...
            if (!Properties.Settings.Default.SafeCleanup)
            {
                MessageBox.Show(AppStrings.AppSafeClnDisabled, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            
            // Обновим статусную строку...
            CheckSafeClnStatus();
        }

        private void CE_OpenCVList_Click(object sender, EventArgs e)
        {
            CoreLib.OpenWebPage(AppStrings.AppCVListURL);
        }

        private void CE_ManualBackUpCfg_Click(object sender, EventArgs e)
        {
            if (!(String.IsNullOrEmpty(CFGFileName)))
            {
                if (File.Exists(CFGFileName))
                {
                    CoreLib.CreateConfigBackUp(CoreLib.SingleToArray(CFGFileName), SelGame.FullBackUpDirPath, Properties.Resources.BU_PrefixCfg);
                    MessageBox.Show(String.Format(AppStrings.CE_BackUpCreated, Path.GetFileName(CFGFileName)), Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show(AppStrings.CE_NoFileOpened, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void PS_RemSounds_Click(object sender, EventArgs e)
        {
            // Удаляем кастомные звуки...
            List<String> CleanDirs = new List<string>();
            if (Properties.Settings.Default.AllowUnSafeCleanup) { CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "sound", "*.*")); }
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "download", "*.mp3"));
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "download", "*.wav"));
            CoreLib.OpenCleanupWindow(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void PS_RemCustDir_Click(object sender, EventArgs e)
        {
            // Удаляем пользовательного каталога...
            List<String> CleanDirs = new List<string>();
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "custom", "*.*"));
            CleanDirs.Add(Path.Combine(SelGame.AppWorkshopDir, "*.*"));
            CoreLib.OpenCleanupWindow(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void PS_DeepCleanup_Click(object sender, EventArgs e)
        {
            // Проведём глубокую очистку...
            List<String> CleanDirs = new List<string>();

            // Удалим старые бинарники и лаунчеры...
            CleanDirs.Add(Path.Combine(SelGame.GamePath, Path.GetDirectoryName(SelGame.SmallAppName), "bin", "*.*"));
            if (Properties.Settings.Default.AllowUnSafeCleanup) { CleanDirs.Add(Path.Combine(SelGame.GamePath, "platform", "*.*")); }
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "bin", "*.*"));
            CleanDirs.Add(Path.Combine(SelGame.GamePath, "*.exe"));
            
            // Удалим кэш загрузок...
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "download", "*.*"));
            
            // Удалим кастомные файлы...
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "custom", "*.*"));
            CleanDirs.Add(Path.Combine(SelGame.AppWorkshopDir, "*.*"));
            
            // Удалим другие кэши...
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "cache", "*.*"));
            
            // Удалим пользовательские конфиги...
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "cfg", "*.cfg"));
            CleanDirs.AddRange(SelGame.CloudConfigs);

            // Удаляем графические настройки NCF-игры...
            if (SelGame.IsUsingVideoFile) { CleanDirs.AddRange(SelGame.VideoCfgFiles); }

            // Запускаем процесс очистки...
            CoreLib.OpenCleanupWindow(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CacheChkReq, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void PS_RemConfigs_Click(object sender, EventArgs e)
        {
            // Удаляем пользовательного каталога...
            List<String> CleanDirs = new List<string>();
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "cfg", "*.*"));
            CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "custom", "*.cfg"));
            CleanDirs.AddRange(SelGame.CloudConfigs);
            CoreLib.OpenCleanupWindow(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void GT_ResAvailable_Btn_Click(object sender, EventArgs e)
        {
            // Получим список доступных разрешений...
            List<String> Resolutions = CoreLib.GetDesktopResolutions();

            // Очистим список...
            GT_ResAvailable.Items.Clear();

            // Пройдём массив в цикле...
            foreach (string CRes in Resolutions)
            {
                GT_ResAvailable.Items.Add(CRes);
            }

            // Если нашли, включим контрол выбора...
            GT_ResAvailable.Enabled = GT_ResAvailable.Items.Count > 0;
        }

        private void GT_ResAvailable_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (((ComboBox)sender).Items.Count > 0)
                {
                    string[] CR = ((ComboBox)sender).Text.Substring(0, ((ComboBox)sender).Text.IndexOf('@')).Split('x');
                    if (CR.Length >= 2) { if (!SelGame.IsUsingVideoFile) { GT_ResHor.Value = Convert.ToInt32(CR[0]); GT_ResVert.Value = Convert.ToInt32(CR[1]); } else { GT_NCF_HorRes.Value = Convert.ToInt32(CR[0]); GT_NCF_VertRes.Value = Convert.ToInt32(CR[1]); } }
                }
            }
            catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); }
        }

        private void HD_HSel_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Получим информацию о выбранном HUD...
            try { SelGame.HUDMan.Select(HD_HSel.Text); } catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); }
                
            // Проверяем результат...
            bool Success = !String.IsNullOrEmpty(SelGame.HUDMan.SelectedHUD.Name);

            // Переключаем статус элементов управления...
            HD_GB_Pbx.Image = Properties.Resources.LoadingFile;
            HD_Install.Enabled = Success;
            HD_Homepage.Enabled = Success;
            HD_Warning.Visible = Success && !SelGame.HUDMan.SelectedHUD.IsUpdated;

            // Выводим информацию о последнем обновлении HUD...
            HD_LastUpdate.Visible = Success;
            if (Success) { HD_LastUpdate.Text = String.Format(AppStrings.HD_LastUpdateInfo, CoreLib.Unix2DateTime(SelGame.HUDMan.SelectedHUD.LastUpdate).ToLocalTime()); }

            // Проверяем установлен ли выбранный HUD...
            SetHUDButtons(HUDManager.CheckInstalledHUD(SelGame.CustomInstallDir, SelGame.HUDMan.SelectedHUD.InstallDir));

            // Загрузим скриншот выбранного HUD...
            if (Success && !BW_HUDScreen.IsBusy) { BW_HUDScreen.RunWorkerAsync(); }
        }

        private void HD_Install_Click(object sender, EventArgs e)
        {
            if (!CheckHUDDatabase(Properties.Settings.Default.LastHUDTime))
            {
                // Проверим поддерживает ли выбранный HUD последнюю версию игры...
                if (SelGame.HUDMan.SelectedHUD.IsUpdated)
                {
                    // Спросим пользователя о необходимости установки/обновления HUD...
                    if (MessageBox.Show(String.Format("{0}?", ((Button)sender).Text), Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                    {
                        // Проверим установлен ли выбранный HUD...
                        if (HUDManager.CheckInstalledHUD(SelGame.CustomInstallDir, SelGame.HUDMan.SelectedHUD.InstallDir))
                        {
                            // Удаляем уже установленные файлы HUD...
                            CoreLib.RemoveFileDirectoryEx(CoreLib.SingleToArray(Path.Combine(SelGame.CustomInstallDir, SelGame.HUDMan.SelectedHUD.InstallDir)));
                        }

                        // Начинаем загрузку если файл не существует...
                        if (!File.Exists(SelGame.HUDMan.SelectedHUD.LocalFile)) { CoreLib.DownloadFileEx(Properties.Settings.Default.HUDUseUpstream ? SelGame.HUDMan.SelectedHUD.UpURI : SelGame.HUDMan.SelectedHUD.URI, SelGame.HUDMan.SelectedHUD.LocalFile); }

                        // Распаковываем загруженный архив с файлами HUD...
                        CoreLib.ExtractFiles(SelGame.HUDMan.SelectedHUD.LocalFile, Path.Combine(SelGame.CustomInstallDir, "hudtemp"));

                        // Запускаем установку пакета в отдельном потоке...
                        if (!BW_HudInstall.IsBusy) { BW_HudInstall.RunWorkerAsync(); }
                    }
                }
                else
                {
                    // Выбран устаревший HUD. Выведем сообщение об этом...
                    MessageBox.Show(AppStrings.HD_Outdated, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                // База HUD устарела. Требуется обновление. Выведем сообщение...
                MessageBox.Show(AppStrings.HD_DbOutdated, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void HD_Uninstall_Click(object sender, EventArgs e)
        {
            // Спросим пользователя о необходимости удаления HUD...
            if (MessageBox.Show(String.Format("{0}?", ((Button)sender).Text), Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                // Сгенерируем полный путь к установленному HUD...
                string HUDPath = Path.Combine(SelGame.CustomInstallDir, SelGame.HUDMan.SelectedHUD.InstallDir);

                // Воспользуемся модулем быстрой очистки для удаления выбранного HUD...
                CoreLib.RemoveFileDirectoryEx(CoreLib.SingleToArray(HUDPath));

                // Проверяем установлен ли выбранный HUD...
                bool IsInstalled = HUDManager.CheckInstalledHUD(SelGame.CustomInstallDir, SelGame.HUDMan.SelectedHUD.InstallDir);

                // При успешном удалении HUD выводим сообщение и сносим и его каталог...
                if (!IsInstalled) { MessageBox.Show(AppStrings.PS_CleanupSuccess, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information); if (Directory.Exists(HUDPath)) { Directory.Delete(HUDPath); } }

                // Включаем / отключаем кнопки...
                SetHUDButtons(IsInstalled);
            }
        }

        private void HD_Homepage_Click(object sender, EventArgs e)
        {
            // Откроем домашнюю страницу выбранного HUD...
            if (!String.IsNullOrEmpty(SelGame.HUDMan.SelectedHUD.Site)) { CoreLib.OpenWebPage(SelGame.HUDMan.SelectedHUD.Site); }
        }

        private void MNUExtClnAppCache_Click(object sender, EventArgs e)
        {
            // Очистим загруженные приложением файлы...
            List<String> CleanDirs = new List<string>();
            CleanDirs.Add(Path.Combine(App.AppUserDir, Properties.Settings.Default.HUDLocalDir, "*.*"));
            CoreLib.OpenCleanupWindow(CleanDirs, ((ToolStripMenuItem)sender).Text.ToLower().Replace("&", String.Empty), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void MNUExtClnTmpDir_Click(object sender, EventArgs e)
        {
            // Очистим каталоги с временными файлами системы...
            List<String> CleanDirs = new List<string>();
            CleanDirs.Add(Path.Combine(Path.GetTempPath(), "*.*"));
            CoreLib.OpenCleanupWindow(CleanDirs, ((ToolStripMenuItem)sender).Text.ToLower().Replace("&", String.Empty), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void MNUShowLog_Click(object sender, EventArgs e)
        {
            // Выведем на экран содержимое отладочного журнала...
            if (Properties.Settings.Default.EnableDebugLog)
            {
                string DFile = Path.Combine(CoreLib.GetApplicationPath(), Properties.Settings.Default.DebugLogFileName);
                if (File.Exists(DFile)) { using (frmLogView Lv = new frmLogView(DFile)) { Lv.ShowDialog(); } } else { MessageBox.Show(AppStrings.AppNoDebugFile, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            }
            else
            {
                MessageBox.Show(AppStrings.AppDebugDisabled, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void HD_Warning_Click(object sender, EventArgs e)
        {
            // Выведем предупреждающие сообщения...
            if (!SelGame.HUDMan.SelectedHUD.IsUpdated) { MessageBox.Show(AppStrings.HD_NotTested, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void HD_OpenDir_Click(object sender, EventArgs e)
        {
            // Покажем файлы установленного HUD в Проводнике...
            CoreLib.OpenExplorer(Path.Combine(SelGame.CustomInstallDir, SelGame.HUDMan.SelectedHUD.InstallDir));
        }

        private void MNUExtClnSteam_Click(object sender, EventArgs e)
        {
            // Запустим модуль очистки кэшей Steam...
            using (FrmStmClean StmCln = new FrmStmClean(App.FullSteamPath, SelGame.FullBackUpDirPath))
            {
                StmCln.ShowDialog();
            }
        }

        private void MNUMuteMan_Click(object sender, EventArgs e)
        {
            // Запустим менеджер управления отключёнными игроками...
            using (FrmMute FMm = new FrmMute(SelGame.BanlistFileName, SelGame.FullBackUpDirPath))
            {
                FMm.ShowDialog();
            }
        }
    }
}
