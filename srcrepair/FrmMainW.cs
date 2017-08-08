﻿/*
 * Основной модуль программы SRC Repair.
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing; // аналогично Forms...
using System.Linq;
using System.Text;
using System.Windows.Forms; // для работы с формами...
using System.IO; // для работы с файлами...
using System.Diagnostics; // для управления процессами...
using System.Net; // для скачивания файлов...
using System.Xml; // для разбора (парсинга) XML...
using System.Text.RegularExpressions; // для работы с регулярными выражениями...

namespace srcrepair
{
    public partial class FrmMainW : Form
    {
        public FrmMainW()
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
                for (int i = 0; i < CE_Editor.Rows.Count; i++) // запускаем цикл
                {
                    CFile.WriteLine("{0} {1}", CE_Editor.Rows[i].Cells[0].Value, CE_Editor.Rows[i].Cells[1].Value);
                }
            }
        }

        /// <summary>
        /// Определяет установленные игры и заполняет комбо-бокс выбора
        /// доступных управляемых игр.
        /// </summary>
        private void DetectInstalledGames()
        {
            // Очистим список игр...
            AppSelector.Items.Clear();
            SourceGames.Clear();

            // При использовании нового метода поиска установленных игр, считаем их из конфига Steam...
            List<String> GameDirs = SteamManager.FormatInstallDirs(App.FullSteamPath, App.Platform.SteamAppsFolderName);

            try
            {
                // Создаём поток с XML-файлом...
                using (FileStream XMLFS = new FileStream(Path.Combine(App.FullAppPath, Properties.Resources.GameListFile), FileMode.Open, FileAccess.Read))
                {
                    // Создаём объект документа XML...
                    XmlDocument XMLD = new XmlDocument();
                    
                    // Загружаем поток в объект XML документа...
                    XMLD.Load(XMLFS);
                    
                    // Обходим полученный список в цикле...
                    XmlNodeList XMLNode = XMLD.GetElementsByTagName("Game");
                    for (int i = 0; i < XMLNode.Count; i++)
                    {
                        try
                        {
                            if (XMLD.GetElementsByTagName("Enabled")[i].InnerText == "1")
                            {
                                SourceGame SG = new SourceGame(XMLNode[i].Attributes["Name"].Value, XMLD.GetElementsByTagName("DirName")[i].InnerText, XMLD.GetElementsByTagName("SmallName")[i].InnerText, XMLD.GetElementsByTagName("Executable")[i].InnerText, XMLD.GetElementsByTagName("SID")[i].InnerText, XMLD.GetElementsByTagName("SVer")[i].InnerText, XMLD.GetElementsByTagName("VFDir")[i].InnerText, App.Platform.OS == CurrentPlatform.OSType.Windows ? XMLD.GetElementsByTagName("HasVF")[i].InnerText == "1" : true, XMLD.GetElementsByTagName("UserDir")[i].InnerText == "1", XMLD.GetElementsByTagName("HUDsAvail")[i].InnerText == "1", App.FullAppPath, App.AppUserDir, App.FullSteamPath, App.Platform.SteamAppsFolderName, GameDirs);
                                if (SG.IsInstalled)
                                {
                                    SourceGames.Add(SG);
                                    AppSelector.Items.Add(XMLNode[i].Attributes["Name"].Value);
                                }
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
        private void WriteType1VideoSettings()
        {
            // Создаём новый объект без получения данных из реестра...
            Type1Video Video = new Type1Video(SelGame.SmallAppName, false)
            {
                // Записываем пользовательские настройки...
                ScreenWidth = (int)GT_ResHor.Value,
                ScreenHeight = (int)GT_ResVert.Value,
                DisplayMode = GT_ScreenType.SelectedIndex,
                ModelQuality = GT_ModelQuality.SelectedIndex,
                TextureQuality = GT_TextureQuality.SelectedIndex,
                ShaderQuality = GT_ShaderQuality.SelectedIndex,
                ReflectionsQuality = GT_WaterQuality.SelectedIndex,
                ShadowQuality = GT_ShadowQuality.SelectedIndex,
                ColorCorrection = GT_ColorCorrectionT.SelectedIndex,
                AntiAliasing = GT_AntiAliasing.SelectedIndex,
                FilteringMode = GT_Filtering.SelectedIndex,
                VSync = GT_VSync.SelectedIndex,
                MotionBlur = GT_MotionBlur.SelectedIndex,
                DirectXMode = GT_DxMode.SelectedIndex,
                HDRType = GT_HDR.SelectedIndex
            };

            // Записываем настройки в реестр...
            Video.WriteSettings();
        }

        /// <summary>
        /// Сохраняет настройки NCF игры в файл.
        /// </summary>
        private void WriteType2VideoSettings()
        {
            // Создаём новый объект без получения данных из файла...
            Type2Video Video = new Type2Video(SelGame.GetActualVideoFile(), SelGame.SourceType, false)
            {
                // Записываем пользовательские настройки...
                ScreenWidth = (int)GT_NCF_HorRes.Value,
                ScreenHeight = (int)GT_NCF_VertRes.Value,
                ScreenRatio = GT_NCF_Ratio.SelectedIndex,
                ScreenGamma = GT_NCF_Brightness.Text,
                ShadowQuality = GT_NCF_Shadows.SelectedIndex,
                MotionBlur = GT_NCF_MBlur.SelectedIndex,
                ScreenMode = GT_NCF_DispMode.SelectedIndex,
                AntiAliasing = GT_NCF_AntiAlias.SelectedIndex,
                FilteringMode = GT_NCF_Filtering.SelectedIndex,
                VSync = GT_NCF_VSync.SelectedIndex,
                RenderingMode = GT_NCF_Multicore.SelectedIndex,
                ShaderEffects = GT_NCF_ShaderE.SelectedIndex,
                Effects = GT_NCF_EffectD.SelectedIndex,
                MemoryPool = GT_NCF_MemPool.SelectedIndex,
                ModelQuality = GT_NCF_Quality.SelectedIndex
            };

            // Записываем настройки в файл...
            Video.WriteSettings();
        }

        /// <summary>
        /// Получает настройки первого типа игры из реестра и заполняет
        /// полученными данными страницу графического твикера.
        /// </summary>
        private void ReadType1VideoSettings()
        {
            try
            {
                // Получаем графические настройки...
                Type1Video Video = new Type1Video(SelGame.SmallAppName, true);

                // Заполняем общие настройки...
                GT_ResHor.Value = Video.ScreenWidth;
                GT_ResVert.Value = Video.ScreenHeight;

                // Заполняем остальные настройки...
                GT_ScreenType.SelectedIndex = Video.DisplayMode;
                GT_ModelQuality.SelectedIndex = Video.ModelQuality;
                GT_TextureQuality.SelectedIndex = Video.TextureQuality;
                GT_ShaderQuality.SelectedIndex = Video.ShaderQuality;
                GT_WaterQuality.SelectedIndex = Video.ReflectionsQuality;
                GT_ShadowQuality.SelectedIndex = Video.ShadowQuality;
                GT_ColorCorrectionT.SelectedIndex = Video.ColorCorrection;
                GT_AntiAliasing.SelectedIndex = Video.AntiAliasing;
                GT_Filtering.SelectedIndex = Video.FilteringMode;
                GT_VSync.SelectedIndex = Video.VSync;
                GT_MotionBlur.SelectedIndex = Video.MotionBlur;
                GT_DxMode.SelectedIndex = Video.DirectXMode;
                GT_HDR.SelectedIndex = Video.HDRType;
            }
            catch (Exception Ex)
            {
                // Выводим сообщение об ошибке...
                CoreLib.HandleExceptionEx(AppStrings.GT_RegOpenErr, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Получает настройки второго типа игры из файла и заполняет ими
        /// таблицу графического твикера программы.
        /// </summary>
        private void ReadType2VideoSettings()
        {
            try
            {
                // Получаем актуальный файл с настройками видео...
                string VFileName = SelGame.GetActualVideoFile();

                // Загружаем содержимое если он существует...
                if (File.Exists(VFileName))
                {
                    // Получаем графические настройки...
                    Type2Video Video = new Type2Video(VFileName, SelGame.SourceType, true);

                    // Заполняем общие настройки...
                    GT_NCF_HorRes.Value = Video.ScreenWidth;
                    GT_NCF_VertRes.Value = Video.ScreenHeight;

                    // Заполняем остальные настройки...
                    GT_NCF_Ratio.SelectedIndex = Video.ScreenRatio;
                    GT_NCF_Brightness.Text = Video.ScreenGamma;
                    GT_NCF_Shadows.SelectedIndex = Video.ShadowQuality;
                    GT_NCF_MBlur.SelectedIndex = Video.MotionBlur;
                    GT_NCF_DispMode.SelectedIndex = Video.ScreenMode;
                    GT_NCF_AntiAlias.SelectedIndex = Video.AntiAliasing;
                    GT_NCF_Filtering.SelectedIndex = Video.FilteringMode;
                    GT_NCF_VSync.SelectedIndex = Video.VSync;
                    GT_NCF_Multicore.SelectedIndex = Video.RenderingMode;
                    GT_NCF_ShaderE.SelectedIndex = Video.ShaderEffects;
                    GT_NCF_EffectD.SelectedIndex = Video.Effects;
                    GT_NCF_MemPool.SelectedIndex = Video.MemoryPool;
                    GT_NCF_Quality.SelectedIndex = Video.ModelQuality;
                }
                else
                {
                    // Записываем в журнал сообщение об ошибке...
                    CoreLib.WriteStringToLog(String.Format(AppStrings.AppVideoDbNotFound, SelGame.FullAppName, VFileName));
                }
            }
            catch (Exception Ex)
            {
                // Выводим сообщение об ошибке...
                CoreLib.HandleExceptionEx(AppStrings.GT_NCFLoadFailure, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Проверяет наличие обновлений для программы. Используется главной формой.
        /// </summary>
        /// <returns>Возвращает true при обнаружении обновлений</returns>
        private bool AutoUpdateCheck()
        {
            UpdateManager UpMan = new UpdateManager(App.FullAppPath, App.UserAgent);
            return UpMan.CheckAppUpdate();
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
                    UpdateStatusBar();
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
        private void ReadBackUpList2Table()
        {
            // Очистим таблицу...
            Invoke((MethodInvoker)delegate() { BU_LVTable.Items.Clear(); });
            
            // Открываем каталог...
            DirectoryInfo DInfo = new DirectoryInfo(SelGame.FullBackUpDirPath);
            
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
        /// Обнуляет контролы на странице графических настроек для Type 1 игры.
        /// </summary>
        private void NullType1Settings()
        {
            GT_ResHor.Value = 640;
            GT_ResVert.Value = 640;
            GT_ScreenType.SelectedIndex = -1;
            GT_ModelQuality.SelectedIndex = -1;
            GT_TextureQuality.SelectedIndex = -1;
            GT_ShaderQuality.SelectedIndex = -1;
            GT_WaterQuality.SelectedIndex = -1;
            GT_ShadowQuality.SelectedIndex = -1;
            GT_ColorCorrectionT.SelectedIndex = -1;
            GT_AntiAliasing.SelectedIndex = -1;
            GT_Filtering.SelectedIndex = -1;
            GT_VSync.SelectedIndex = -1;
            GT_MotionBlur.SelectedIndex = -1;
            GT_DxMode.SelectedIndex = -1;
            GT_HDR.SelectedIndex = -1;
        }

        /// <summary>
        /// Обнуляет контролы на странице графических настроек для Type 2 игры.
        /// </summary>
        private void NullType2Settings()
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
        /// Обнуляет контролы на странице графических настроек. Функция-заглушка.
        /// </summary>
        private void NullGraphSettings()
        {
            switch (SelGame.SourceType)
            {
                case "1":
                    if (App.Platform.OS == CurrentPlatform.OSType.Windows) { NullType1Settings(); } else { NullType2Settings(); }
                    break;
                case "2":
                    NullType2Settings();
                    break;
            }
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

            // Отключим контролы модуля управления FPS-конфигами...
            FP_Install.Enabled = false;
            FP_Comp.Visible = false;

            // Отключим контролы в менеджере HUD...
            HD_Install.Enabled = false;
            HD_Homepage.Enabled = false;
            HD_Uninstall.Enabled = false;
            HD_OpenDir.Enabled = false;
            HD_Warning.Visible = false;
            HD_GB_Pbx.Image = null;
            HD_LastUpdate.Visible = false;

            // Включаем заблокированные ранее контролы...
            MNUInstaller.Enabled = true;
        }

        /// <summary>
        /// Загружает настройки видео для выбранной игры.
        /// </summary>
        private void LoadGraphicSettings()
        {
            // Обнуляем графические настройки...
            NullGraphSettings();

            // Загружаем настройки графики согласно указанного движка...
            switch (SelGame.SourceType)
            {
                case "1": /* Source 1, Type 1 (ex. GCF). */
                    if (App.Platform.OS == CurrentPlatform.OSType.Windows) { ReadType1VideoSettings(); } else { ReadType2VideoSettings(); }
                    break;
                case "2": /* Source 1, Type 2 (ex. NCF). */
                    ReadType2VideoSettings();
                    break;
                default:
                    throw new NotSupportedException();
            }

            // Переключаем графический твикер в режим GCF/NCF...
            SelectGraphicWidget((App.Platform.OS != CurrentPlatform.OSType.Windows) && (SelGame.SourceType == "1") ? "2": SelGame.SourceType);
        }

        /// <summary>
        /// Выполняет особые действия и начинает процесс сохранения настроек видео
        /// для Type 1 игры.
        /// </summary>
        private void PrepareWriteType1VideoSettings()
        {
            // Генерируем путь к ветке реестра с настройками...
            string GameRegKey = Type1Video.GetGameRegKey(SelGame.SmallAppName);

            // Создаём резервную копию если включена опция безопасной очистки...
            if (Properties.Settings.Default.SafeCleanup)
            {
                try { Type1Video.BackUpVideoSettings(GameRegKey, "Game_AutoBackUp", SelGame.FullBackUpDirPath); } catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); }
            }

            // Запускаем процесс...
            try
            {
                // Проверяем существование ключа реестра и если его нет, создаём...
                if (!(Type1Video.CheckRegKeyExists(GameRegKey))) { Type1Video.CreateRegKey(GameRegKey); }

                // Записываем настройки в реестр...
                WriteType1VideoSettings();

                // Выводим сообщение об успехе...
                MessageBox.Show(AppStrings.GT_SaveSuccess, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception Ex)
            {
                // Записываем в журнал и выводим сообщение об ошибке...
                CoreLib.HandleExceptionEx(AppStrings.GT_SaveFailure, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Выполняет особые действия и начинает процесс сохранения настроек видео
        /// для Type 2 игры.
        /// </summary>
        private void PrepareWriteType2VideoSettings()
        {
            // Создаём резервную копию если включена опция безопасной очистки...
            if (Properties.Settings.Default.SafeCleanup)
            {
                FileManager.CreateConfigBackUp(SelGame.VideoCfgFiles, SelGame.FullBackUpDirPath, Properties.Resources.BU_PrefixVidAuto);
            }
            
            // Запускаем процесс...
            try
            {
                // Записываем настройки в файл...
                WriteType2VideoSettings();

                // Выводим сообщение об успехе...
                MessageBox.Show(AppStrings.GT_SaveSuccess, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception Ex)
            {
                // Записываем в журнал и выводим сообщение об ошибке...
                CoreLib.HandleExceptionEx(AppStrings.GT_NCFFailure, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Сохраняет настройки видео для выбранной игры.
        /// </summary>
        private void WriteGraphicSettings()
        {
            // Определим тип игры...
            switch (SelGame.SourceType)
            {
                case "1":
                    if (App.Platform.OS == CurrentPlatform.OSType.Windows) { PrepareWriteType1VideoSettings(); } else { PrepareWriteType2VideoSettings(); }
                    break;
                case "2":
                    PrepareWriteType2VideoSettings();
                    break;
            }
        }

        /// <summary>
        /// Переключает вид страницы графического твикера с в соответствие с выбранным
        /// движком.
        /// </summary>
        /// <param name="SType">Тип движка Source</param>
        private void SelectGraphicWidget(string SType)
        {
            // Переключаем виджеты...
            switch (SType)
            {
                case "1":
                    GT_GType1.Visible = true;
                    GT_GType2.Visible = false;
                    break;
                case "2":
                    GT_GType1.Visible = false;
                    GT_GType2.Visible = true;
                    break;
            }
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
            // Задаём начальное значение...
            string Result = String.Empty;
            
            // Указываем текст в диалоге поиска каталога...
            FldrBrwse.Description = AppStrings.SteamPathEnterText;

            // Отображаем стандартный диалог поиска каталога...
            if (FldrBrwse.ShowDialog() == DialogResult.OK) { if (!(File.Exists(Path.Combine(FldrBrwse.SelectedPath, App.Platform.SteamBinaryName)))) { throw new FileNotFoundException("Invalid Steam directory entered by user", Path.Combine(FldrBrwse.SelectedPath, App.Platform.SteamBinaryName)); } else { Result = FldrBrwse.SelectedPath; } } else { throw new OperationCanceledException("User closed opendir window"); }

            // Возвращаем результат...
            return Result;
        }

        /// <summary>
        /// Проверяет значение OldPath на наличие верного пути к клиенту Steam.
        /// </summary>
        /// <param name="OldPath">Проверяемый путь</param>
        private string CheckLastSteamPath(string OldPath)
        {
            return (!(String.IsNullOrWhiteSpace(OldPath)) && File.Exists(Path.Combine(OldPath, App.Platform.SteamBinaryName))) ? OldPath : GetPathByMEnter();
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
            // Проверяем платформу выполнения приложения...
            if (App.Platform.OS == CurrentPlatform.OSType.Windows)
            {
                // Платформа Windows, применяем стандартные ограничения...
                MNUWinMnuDisabler.Enabled = State;
            }
            else
            {
                // Платформа GNU/Linux или MacOS X, отключим ряд контролов...
                MNUReportBuilder.Enabled = false;
                MNUWinMnuDisabler.Enabled = false;
            }
        }

        /// <summary>
        /// Выполняет определение и вывод названия файловой системы на диске установки клиента игры.
        /// </summary>
        private void DetectFS()
        {
            try
            {
                PS_OSDrive.Text = String.Format(PS_OSDrive.Text, FileManager.DetectDriveFileSystem(Path.GetPathRoot(SelGame.FullGamePath)));
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
                        UpdateStatusBar();
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
        private void CheckSymbolsSteam()
        {
            if (!(FileManager.CheckNonASCII(App.FullSteamPath)))
            {
                PS_PathSteam.ForeColor = Color.Red;
                PS_PathSteam.Image = Properties.Resources.upd_err;
                CoreLib.WriteStringToLog(String.Format(AppStrings.AppRestrSymbLog, App.FullSteamPath));
            }
        }

        /// <summary>
        /// Запускает проверку на наличие запрещённых символов в пути установки игры.
        /// </summary>
        private void CheckSymbolsGame()
        {
            if (!(FileManager.CheckNonASCII(SelGame.FullGamePath)))
            {
                PS_PathGame.ForeColor = Color.Red;
                PS_PathGame.Image = Properties.Resources.upd_err;
                CoreLib.WriteStringToLog(String.Format(AppStrings.AppRestrSymbLog, SelGame.FullGamePath));
            }
            else
            {
                PS_PathGame.ForeColor = Color.Green;
                PS_PathGame.Image = Properties.Resources.upd_nx;
            }
        }

        /// <summary>
        /// Управляет выводом значка активного FPS-конфига и кнопки их удаления.
        /// </summary>
        private void HandleConfigs()
        {
            SelGame.FPSConfigs = FileManager.ExpandFileList(ConfigManager.ListFPSConfigs(SelGame.FullGamePath, SelGame.IsUsingUserDir), true);
            GT_Warning.Visible = SelGame.FPSConfigs.Count > 0;
            FP_Uninstall.Enabled = SelGame.FPSConfigs.Count > 0;
        }

        /// <summary>
        /// Управляет выводом текущего SteamID.
        /// </summary>
        /// <param name="SID">Сохранённый SteamID</param>
        private void HandleSteamIDs(string SID)
        {
            try
            {
                string Result = SelGame.GetCurrentSteamID(SID);
                SB_SteamID.Text = Result;
                Properties.Settings.Default.LastSteamID = Result;
            }
            catch (Exception Ex)
            {
                CoreLib.WriteStringToLog(Ex.Message);
                SB_SteamID.Text = String.Empty;
            }
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
        /// Обновляет содержимое строки состояния в зависимости от контекста.
        /// </summary>
        private void UpdateStatusBar()
        {
            switch (MainTabControl.SelectedIndex)
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
                        bool HUDDbStatus = HUDManager.CheckHUDDatabase(Properties.Settings.Default.LastHUDTime);
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
        /// Проверяет правильность заполнения графических настроек для Type 1 игр.
        /// </summary>
        private bool CheckType1Settings()
        {
            return (GT_ScreenType.SelectedIndex != -1) && (GT_ModelQuality.SelectedIndex != -1)
                && (GT_TextureQuality.SelectedIndex != -1) && (GT_ShaderQuality.SelectedIndex != -1)
                && (GT_WaterQuality.SelectedIndex != -1) && (GT_ShadowQuality.SelectedIndex != -1)
                && (GT_ColorCorrectionT.SelectedIndex != -1) && (GT_AntiAliasing.SelectedIndex != -1)
                && (GT_Filtering.SelectedIndex != -1) && (GT_VSync.SelectedIndex != -1)
                && (GT_MotionBlur.SelectedIndex != -1) && (GT_DxMode.SelectedIndex != -1)
                && (GT_HDR.SelectedIndex != -1);
        }

        /// <summary>
        /// Проверяет правильность заполнения графических настроек для Type 2 игр.
        /// </summary>
        private bool CheckType2Settings()
        {
            return (GT_NCF_Quality.SelectedIndex != -1) && (GT_NCF_MemPool.SelectedIndex != -1)
                && (GT_NCF_EffectD.SelectedIndex != -1) && (GT_NCF_ShaderE.SelectedIndex != -1)
                && (GT_NCF_Multicore.SelectedIndex != -1) && (GT_NCF_VSync.SelectedIndex != -1)
                && (GT_NCF_Filtering.SelectedIndex != -1) && (GT_NCF_AntiAlias.SelectedIndex != -1)
                && (GT_NCF_DispMode.SelectedIndex != -1) && (GT_NCF_Ratio.SelectedIndex != -1)
                && (GT_NCF_Brightness.SelectedIndex != -1) && (GT_NCF_Shadows.SelectedIndex != -1)
                && (GT_NCF_MBlur.SelectedIndex != -1);
        }

        /// <summary>
        /// Проверяет верность заполнения графических настроек
        /// </summary>
        private bool ValidateGameSettings()
        {
            bool Result = false;
            switch (SelGame.SourceType)
            {
                case "1":
                    Result = App.Platform.OS == CurrentPlatform.OSType.Windows ? CheckType1Settings() : CheckType2Settings();
                    break;
                case "2":
                    Result = CheckType2Settings();
                    break;
            }
            return Result;
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
        private void UpdateBackUpList()
        {
            try
            {
                // Считываем и выводим в таблицу файлы резервных копий...
                ReadBackUpList2Table();
            }
            catch (Exception Ex)
            {
                // Произошло исключение. Запишем в журнал...
                CoreLib.WriteStringToLog(Ex.Message);

                // Создадим каталог для хранения резервных копий если его ещё нет...
                if (!Directory.Exists(SelGame.FullBackUpDirPath)) { Directory.CreateDirectory(SelGame.FullBackUpDirPath); }
            }
        }

        /// <summary>
        /// Ищет установленные игры и выполняет ряд необходимых проверок.
        /// </summary>
        private void FindGames()
        {
            // Начинаем определять установленные игры...
            try { DetectInstalledGames(); } catch (Exception Ex) { CoreLib.HandleExceptionEx(AppStrings.AppXMLParseError, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Error); Environment.Exit(16); }

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


        /// <summary>
        /// Возвращает список строк для передачи в особые методы.
        /// </summary>
        /// <param name="Str">Строка для создания списка</param>
        /// <returns>Возвращает список строк</returns>
        private List<String> SingleToArray(string Str)
        {
            List<String> Result = new List<String> { Str };
            return Result;
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
                    if (AutoUpdateCheck())
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
                SelGame.CFGMan = new ConfigManager(Path.Combine(App.FullAppPath, Properties.Resources.CfgDbFile), AppStrings.AppLangPrefix);

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
            UpdateBackUpList();
        }

        private void BW_HUDList_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Получаем список доступных HUD...
                SelGame.HUDMan = new HUDManager(Path.Combine(App.FullAppPath, Properties.Resources.HUDDbFile), SelGame.AppHUDDir);

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
                try { Directory.Move(Path.Combine(InstallTmp, HUDManager.FormatIntDir(SelGame.HUDMan.SelectedHUD.ArchiveDir)), Path.Combine(SelGame.CustomInstallDir, SelGame.HUDMan.SelectedHUD.InstallDir)); }
                finally { if (Directory.Exists(InstallTmp)) { Directory.Delete(InstallTmp, true); } }

                // Удаляем архив с загруженным HUD...
                if (File.Exists(SelGame.HUDMan.SelectedHUD.LocalFile)) { File.Delete(SelGame.HUDMan.SelectedHUD.LocalFile); }
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

        private void FrmMainW_Load(object sender, EventArgs e)
        {
            // Событие инициализации формы...
            App = new CurrentApp();
            SourceGames = new List<SourceGame>();

            // Узнаем путь к установленному клиенту Steam...
            try { App.FullSteamPath = App.Platform.OS == CurrentPlatform.OSType.Windows ? SteamManager.GetSteamPath() : SteamManager.TrySteamPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Steam")); } catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); ValidateAndHandle(); }

            // Начинаем платформо-зависимые процедуры...
            ChangePrvControlState(ProcessManager.IsCurrentUserAdmin());

            // Сохраним последний путь к Steam в файл конфигурации...
            Properties.Settings.Default.LastSteamPath = App.FullSteamPath;

            // Вставляем информацию о версии в заголовок формы...
            Text = String.Format(Text, Properties.Resources.AppName, App.Platform.OSFriendlyName, CurrentApp.AppVersion);

            // Укажем статус Безопасной очистки...
            CheckSafeClnStatus();

            // Укажем путь к Steam на странице "Устранение проблем"...
            PS_StPath.Text = String.Format(PS_StPath.Text, App.FullSteamPath);
            
            // Проверим на наличие запрещённых символов в пути к установленному клиенту Steam...
            CheckSymbolsSteam();

            // Запустим поиск установленных игр и проверим нашлось ли что-то...
            FindGames();

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
                    if (ProcessManager.ProcessTerminate("Steam") != 0)
                    {
                        MessageBox.Show(AppStrings.PS_ProcessDetected, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    // Проверяем нужно ли чистить блобы...
                    if (PS_CleanBlobs.Checked)
                    {
                        try
                        {
                            // Чистим блобы...
                            SteamManager.CleanBlobsNow(App.FullSteamPath);
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
                                SteamManager.CleanRegistryNow(PS_SteamLang.SelectedIndex);
                            }
                            else
                            {
                                // Пользователь не выбрал язык, поэтому будем использовать английский...
                                MessageBox.Show(AppStrings.PS_NoLangSelected, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                SteamManager.CleanRegistryNow(0);
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
                    if (File.Exists(Path.Combine(App.FullSteamPath, App.Platform.SteamBinaryName))) { Process.Start(Path.Combine(App.FullSteamPath, App.Platform.SteamBinaryName)); }
                }
            }
        }

        private void FrmMainW_FormClosing(object sender, FormClosingEventArgs e)
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
                CheckSymbolsGame();

                // Распознаем файловую систему на диске с игрой...
                DetectFS();

                // Считаем настройки графики...
                try { LoadGraphicSettings(); } catch (NotSupportedException) { MessageBox.Show(AppStrings.AppIncorrectSrcVersion, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error); }

                // Получим параметры запуска...
                GT_LaunchOptions.Text = SelGame.LaunchOptions;

                // Получаем текущий SteamID...
                HandleSteamIDs(Properties.Settings.Default.LastSteamID);

                // Проверим, установлен ли FPS-конфиг...
                HandleConfigs();

                // Закроем открытые конфиги в редакторе...
                if (!(String.IsNullOrEmpty(CFGFileName))) { CloseEditorConfigs(); }

                // Считаем имеющиеся FPS-конфиги...
                if (!BW_FPRecv.IsBusy) { BW_FPRecv.RunWorkerAsync(); }

                // Обновляем статус...
                UpdateStatusBar();

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
            FindGames();
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Максимальное качество".
        /// Устанавливает графические настройки на рекомендуемый максимум.
        /// </summary>
        private void GT_Maximum_Graphics_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(AppStrings.GT_MaxPerfMsg, Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                switch (SelGame.SourceType)
                {
                    case "1":
                        GT_ScreenType.SelectedIndex = 0;
                        GT_ModelQuality.SelectedIndex = 2;
                        GT_TextureQuality.SelectedIndex = 2;
                        GT_ShaderQuality.SelectedIndex = 1;
                        GT_WaterQuality.SelectedIndex = 1;
                        GT_ShadowQuality.SelectedIndex = 1;
                        GT_ColorCorrectionT.SelectedIndex = 1;
                        GT_AntiAliasing.SelectedIndex = 5;
                        GT_Filtering.SelectedIndex = 5;
                        GT_VSync.SelectedIndex = 0;
                        GT_MotionBlur.SelectedIndex = 0;
                        GT_DxMode.SelectedIndex = 3;
                        GT_HDR.SelectedIndex = 2;
                        break;
                    case "2":
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
                        break;
                }
                MessageBox.Show(AppStrings.GT_PerfSet, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Максимальная производительность".
        /// Устанавливает графические настройки на рекомендуемый минимум.
        /// </summary>
        private void GT_Maximum_Performance_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(AppStrings.GT_MinPerfMsg, Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                switch (SelGame.SourceType)
                {
                    case "1":
                        GT_ScreenType.SelectedIndex = 0;
                        GT_ModelQuality.SelectedIndex = 0;
                        GT_TextureQuality.SelectedIndex = 0;
                        GT_ShaderQuality.SelectedIndex = 0;
                        GT_WaterQuality.SelectedIndex = 0;
                        GT_ShadowQuality.SelectedIndex = 0;
                        GT_ColorCorrectionT.SelectedIndex = 0;
                        GT_AntiAliasing.SelectedIndex = 0;
                        GT_Filtering.SelectedIndex = 1;
                        GT_VSync.SelectedIndex = 0;
                        GT_MotionBlur.SelectedIndex = 0;
                        GT_DxMode.SelectedIndex = MessageBox.Show(AppStrings.GT_DxLevelMsg, Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes ? 0 : 3;
                        GT_HDR.SelectedIndex = 0;
                        break;
                    case "2":
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
                        break;
                }
                MessageBox.Show(AppStrings.GT_PerfSet, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки сохранения графических настроек.
        /// </summary>
        private void GT_SaveApply_Click(object sender, EventArgs e)
        {
            // Сохраняем изменения в графических настройках...
            if (ValidateGameSettings())
            {
                // Запрашиваем подтверждение у пользователя...
                if (MessageBox.Show(AppStrings.GT_SaveMsg, Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    // Сохраняем настройки графики...
                    WriteGraphicSettings();
                }
            }
            else
            {
                // Пользователь заполнил не все поля. Сообщаем ему об этом...
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

                // Проверим совместимость конфига с игрой...
                FP_Comp.Visible = !SelGame.CFGMan.FPSConfig.CheckCompactibility(SelGame.GameInternalID);

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
                            FileManager.CompressFiles(SelGame.FPSConfigs, FileManager.GenerateBackUpFileName(SelGame.FullBackUpDirPath, Properties.Resources.BU_PrefixCfg));
                        }
                    }

                    try
                    {
                        // Устанавливаем...
                        ConfigManager.InstallConfigNow(SelGame.CFGMan.FPSConfig.FileName, App.FullAppPath, SelGame.FullGamePath, SelGame.IsUsingUserDir);
                        
                        // Выводим сообщение об успешной установке...
                        MessageBox.Show(AppStrings.FP_InstallSuccessful, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        // Перечитаем конфиги...
                        HandleConfigs();
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
                    FormManager.FormShowCleanup(SelGame.FPSConfigs, ((Button)sender).Text.ToLower(), AppStrings.FP_RemoveSuccessful, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile, false, false, false, Properties.Settings.Default.SafeCleanup);

                    // Перечитаем список конфигов...
                    HandleConfigs();
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
            try
            {
                // Предложим пользователю выбрать FPS-конфиг...
                string ConfigFile = FormManager.FormShowCfgSelect(SelGame.FPSConfigs);

                // Проверим выбрал ли что-то пользователь в специальной форме...
                if (!(String.IsNullOrWhiteSpace(ConfigFile)))
                {
                    // Загрузим выбранный конфиг в Редактор конфигов...
                    ReadConfigFromFile(ConfigFile);

                    // Переключимся на него...
                    MainTabControl.SelectedIndex = 1;
                }
            }
            catch (Exception Ex)
            {
                CoreLib.HandleExceptionEx(AppStrings.CS_FailedToOpenCfg, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
            }
        }

        private void CE_New_Click(object sender, EventArgs e)
        {
            // Закрываем все открытые конфиги в Редакторе конфигов и создаём новый пустой файл...
            CloseEditorConfigs();

            // Обновляем содержимое строки статуса...
            UpdateStatusBar();
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
                    if (File.Exists(CFGFileName)) { FileManager.CreateConfigBackUp(CFGFileName, SelGame.FullBackUpDirPath, Properties.Resources.BU_PrefixCfg); }
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
                    UpdateStatusBar();
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
            List<String> CleanDirs = new List<string>
            {
                Path.Combine(SelGame.FullGamePath, "custom", "*.bsp"),
                Path.Combine(SelGame.FullGamePath, "download", "*.bsp"),
                Path.Combine(SelGame.AppWorkshopDir, "*.bsp")
            };
            if (Properties.Settings.Default.AllowUnSafeCleanup) { CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "maps", "*.bsp")); }
            FormManager.FormShowCleanup(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void PS_RemDnlCache_Click(object sender, EventArgs e)
        {
            // Удаляем кэш загрузок...
            List<String> CleanDirs = new List<string>
            {
                Path.Combine(SelGame.FullGamePath, "download", "*.*"),
                Path.Combine(SelGame.FullGamePath, "downloads", "*.*"),
                Path.Combine(SelGame.FullGamePath, "streams", "*.*")
            };
            FormManager.FormShowCleanup(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void PS_RemSoundCache_Click(object sender, EventArgs e)
        {
            // Удаляем звуковой кэш...
            List<String> CleanDirs = new List<string>
            {
                Path.Combine(SelGame.FullGamePath, "maps", "graphs", "*.*"),
                Path.Combine(SelGame.FullGamePath, "maps", "soundcache", "*.*"),
                Path.Combine(SelGame.FullGamePath, "download", "sound", "*.*"),
                Path.Combine(SelGame.FullGamePath, "*.cache")
            };
            FormManager.FormShowCleanup(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void PS_RemScreenShots_Click(object sender, EventArgs e)
        {
            // Удаляем все скриншоты...
            List<String> CleanDirs = new List<string>
            {
                Path.Combine(SelGame.FullGamePath, "screenshots", "*.*")
            };
            FormManager.FormShowCleanup(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile, false, false, false);
        }

        private void PS_RemDemos_Click(object sender, EventArgs e)
        {
            // Удаляем все записанные демки...
            List<String> CleanDirs = new List<string>
            {
                Path.Combine(SelGame.FullGamePath, "demos", "*.*"),
                Path.Combine(SelGame.FullGamePath, "*.dem"),
                Path.Combine(SelGame.FullGamePath, "*.mp4"),
                Path.Combine(SelGame.FullGamePath, "*.tga"),
                Path.Combine(SelGame.FullGamePath, "*.wav")
            };
            FormManager.FormShowCleanup(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile, false, false, false, false);
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
                        // Получаем полный путь к ветке реестра игры...
                        string GameRegKey = Type1Video.GetGameRegKey(SelGame.SmallAppName);

                        // Создаём резервную копию куста реестра...
                        if (Properties.Settings.Default.SafeCleanup) { try { Type1Video.BackUpVideoSettings(GameRegKey, "Game_AutoBackUp", SelGame.FullBackUpDirPath); } catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); } }

                        // Удаляем ключ HKEY_CURRENT_USER\Software\Valve\Source\tf\Settings из реестра...
                        Type1Video.RemoveRegKey(GameRegKey);
                    }
                    else
                    {
                        // Создадим бэкап файла с графическими настройками...
                        if (Properties.Settings.Default.SafeCleanup) { FileManager.CreateConfigBackUp(SelGame.VideoCfgFiles, SelGame.FullBackUpDirPath, Properties.Resources.BU_PrefixVidAuto); }

                        // Помечаем его на удаление...
                        CleanDirs.AddRange(SelGame.VideoCfgFiles);
                    }

                    // Создаём резервную копию...
                    if (Properties.Settings.Default.SafeCleanup) { FileManager.CreateConfigBackUp(SelGame.CloudConfigs, SelGame.FullBackUpDirPath, Properties.Resources.BU_PrefixCfg); }

                    // Помечаем конфиги игры на удаление...
                    CleanDirs.Add(Path.Combine(SelGame.FullCfgPath, "config.cfg"));
                    CleanDirs.AddRange(SelGame.CloudConfigs);

                    // Удаляем всю очередь...
                    FormManager.FormShowRemoveFiles(CleanDirs);

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
            List<String> CleanDirs = new List<string>
            {
                Path.Combine(SelGame.GamePath, Path.GetDirectoryName(SelGame.SmallAppName), "bin", "*.*"),
                Path.Combine(SelGame.FullGamePath, "bin", "*.*"),
                Path.Combine(SelGame.GamePath, "*.exe")
            };
            if (Properties.Settings.Default.AllowUnSafeCleanup) { CleanDirs.Add(Path.Combine(SelGame.GamePath, "platform", "*.*")); }
            FormManager.FormShowCleanup(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CacheChkReq, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
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
                FormManager.FormShowRepBuilder(App.AppUserDir, App.FullSteamPath, SelGame);
            }
            else
            {
                MessageBox.Show(AppStrings.AppNoGamesSelected, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void MNUInstaller_Click(object sender, EventArgs e)
        {
            // Запускаем форму установщика спреев, демок и конфигов...
            FormManager.FormShowInstaller(SelGame.FullGamePath, SelGame.IsUsingUserDir, SelGame.CustomInstallDir);
        }

        private void MNUExit_Click(object sender, EventArgs e)
        {
            // Завершаем работу программы...
            Environment.Exit(0);
        }

        private void MNUAbout_Click(object sender, EventArgs e)
        {
            // Отобразим форму "О программе"...
            FormManager.FormShowAboutApp();
        }

        private void MNUReportBug_Click(object sender, EventArgs e)
        {
            // Перейдём в баг-трекер...
            ProcessManager.OpenWebPage(Properties.Resources.AppBtURL);
        }

        private void BUT_Refresh_Click(object sender, EventArgs e)
        {
            // Обновим список резервных копий...
            UpdateBackUpList();
        }

        private void BUT_RestoreB_Click(object sender, EventArgs e)
        {
            // Восстановим выделенный бэкап...
            if (BU_LVTable.Items.Count > 0)
            {
                if (BU_LVTable.SelectedItems.Count > 0)
                {
                    // Запрашиваем подтверждение...
                    if (MessageBox.Show(AppStrings.BU_QMsg, Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                    {
                        // Обходим выбранные бэкапы в цикле...
                        foreach (ListViewItem BU_Item in BU_LVTable.SelectedItems)
                        {
                            // Проверяем что восстанавливать: конфиг или реестр...
                            switch (Path.GetExtension(BU_Item.SubItems[4].Text))
                            {
                                case ".reg":
                                    // Восстанавливаем файл реестра...
                                    try
                                    {
                                        // Восстанавливаем...
                                        Process.Start("regedit.exe", String.Format("/s \"{0}\"", Path.Combine(SelGame.FullBackUpDirPath, BU_Item.SubItems[4].Text)));
                                    }
                                    catch (Exception Ex)
                                    {
                                        // Произошло исключение...
                                        CoreLib.HandleExceptionEx(AppStrings.BU_RestFailed, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
                                    }
                                    break;
                                case ".bud":
                                    // Распаковываем архив с выводом прогресса...
                                    FormManager.FormShowArchiveExtract(Path.Combine(SelGame.FullBackUpDirPath, BU_Item.SubItems[4].Text), Path.GetPathRoot(App.FullSteamPath));

                                    // Обновляем список FPS-конфигов...
                                    HandleConfigs();
                                    break;
                                default:
                                    // Выводим сообщение о неизвестном формате резервной копии...
                                    MessageBox.Show(AppStrings.BU_UnknownType, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    break;
                            }

                            // Выводим сообщение об успехе...
                            MessageBox.Show(AppStrings.BU_RestSuccessful, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    // Запросим подтверждение...
                    if (MessageBox.Show(AppStrings.BU_DelMsg, Properties.Resources.AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                    {
                        // Обходим выбранные бэкапы в цикле...
                        foreach (ListViewItem BU_Item in BU_LVTable.SelectedItems)
                        {
                            try
                            {
                                // Удаляем файл...
                                File.Delete(Path.Combine(SelGame.FullBackUpDirPath, BU_Item.SubItems[4].Text));

                                // Удаляем строку...
                                BU_LVTable.Items.Remove(BU_Item);
                            }
                            catch (Exception Ex)
                            {
                                // Произошло исключение при попытке удаления файла резервной копии...
                                CoreLib.HandleExceptionEx(AppStrings.BU_DelFailed, Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Warning);
                            }
                        }

                        // Показываем сообщение об успешном удалении...
                        MessageBox.Show(AppStrings.BU_DelSuccessful, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                        Type1Video.BackUpVideoSettings(Type1Video.GetGameRegKey(SelGame.SmallAppName), "Game_Options", SelGame.FullBackUpDirPath);
                    }
                    else
                    {
                        // Проверяем существование файла с графическими настройками игры...
                        FileManager.CreateConfigBackUp(SelGame.VideoCfgFiles, SelGame.FullBackUpDirPath, Properties.Resources.BU_PrefixVideo);
                    }
                    
                    // Выводим сообщение об успехе...
                    MessageBox.Show(AppStrings.BU_RegDone, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Обновляем список резервных копий...
                    UpdateBackUpList();
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
                    Type1Video.CreateRegBackUpNow(Path.Combine("HKEY_CURRENT_USER", "Software", "Valve"), "Steam_BackUp", SelGame.FullBackUpDirPath);
                    
                    // Выводим сообщение...
                    MessageBox.Show(AppStrings.BU_RegDone, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Обновим список бэкапов...
                    UpdateBackUpList();
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
                    Type1Video.CreateRegBackUpNow(Path.Combine("HKEY_CURRENT_USER", "Software", "Valve", "Source"), "Source_Options", SelGame.FullBackUpDirPath);
                    MessageBox.Show(AppStrings.BU_RegDone, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    UpdateBackUpList();
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
            UpdateStatusBar();
        }

        private void CE_ShowHint_Click(object sender, EventArgs e)
        {
            try
            {
                string Buf = CE_Editor.Rows[CE_Editor.CurrentRow.Index].Cells[0].Value.ToString();
                if (!(String.IsNullOrEmpty(Buf))) { Buf = CurrentApp.GetConVarDescription(Buf); if (!(String.IsNullOrEmpty(Buf))) { MessageBox.Show(Buf, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information); } else { MessageBox.Show(AppStrings.CE_ClNoDescr, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning); } } else { MessageBox.Show(AppStrings.CE_ClSelErr, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            }
            catch
            {
                MessageBox.Show(AppStrings.CE_ClSelErr, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void MNUHelp_Click(object sender, EventArgs e)
        {
            // Отобразим справочную систему в зависимости от контекста...
            ProcessManager.OpenWebPage(GetHelpWebPage(MainTabControl.SelectedIndex));
        }

        private void MNUOpinion_Click(object sender, EventArgs e)
        {
            ProcessManager.OpenWebPage(Properties.Resources.AppURLReply);
        }

        private void MNUSteamGroup_Click(object sender, EventArgs e)
        {
            try { Process.Start(Properties.Resources.AppURLSteamGrID); } catch { ProcessManager.OpenWebPage(Properties.Resources.AppURLSteamGroup); }
        }

        private void MNULnkEasyCoding_Click(object sender, EventArgs e)
        {
            ProcessManager.OpenWebPage(Properties.Resources.AppURLOffSite);
        }

        private void MNULnkTFRU_Click(object sender, EventArgs e)
        {
            ProcessManager.OpenWebPage(Properties.Resources.AppURLSpnTFSU);
        }

        private void MNUHEd_Click(object sender, EventArgs e)
        {
            // Отобразим форму редактора файла hosts...
            FormManager.FormShowHostsEditor();
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
            catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); }
        }

        private void CE_Copy_Click(object sender, EventArgs e)
        {
            try
            {
                StringBuilder SB = new StringBuilder();
                foreach (DataGridViewCell DV in CE_Editor.SelectedCells) { if (DV.Value != null) { SB.AppendFormat("{0} ", DV.Value); } }
                Clipboard.SetText(SB.ToString().Trim());
            }
            catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); }
        }

        private void CE_Cut_Click(object sender, EventArgs e)
        {
            try
            {
                StringBuilder SB = new StringBuilder();
                foreach (DataGridViewCell DV in CE_Editor.SelectedCells) { if (DV.Value != null) { SB.AppendFormat("{0} ", DV.Value); DV.Value = null; } }
                Clipboard.SetText(SB.ToString().Trim());
            }
            catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); }
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
            catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); }
        }

        private void FP_OpenNotepad_Click(object sender, EventArgs e)
        {
            // Сгенерируем путь к файлу...
            string ConfigFile = Path.Combine(App.FullAppPath, "cfgs", SelGame.CFGMan.FPSConfig.FileName);
            
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
                ProcessManager.OpenTextEditor(ConfigFile, App.Platform.OS);
            }
        }

        private void MNUUpdateCheck_Click(object sender, EventArgs e)
        {
            // Откроем форму модуля проверки обновлений...
            FormManager.FormShowUpdater(App.UserAgent, App.FullAppPath, App.AppUserDir, App.Platform);
            
            // Перечитаем базу игр...
            FindGames();
        }

        private void BUT_OpenNpad_Click(object sender, EventArgs e)
        {
            // Откроем выбранный бэкап в Блокноте Windows...
            if (BU_LVTable.Items.Count > 0)
            {
                if (BU_LVTable.SelectedItems.Count > 0)
                {
                    if (Regex.IsMatch(Path.GetExtension(BU_LVTable.SelectedItems[0].SubItems[4].Text), @"\.(txt|cfg|[0-9]|reg)")) { ProcessManager.OpenTextEditor(Path.Combine(SelGame.FullBackUpDirPath, BU_LVTable.SelectedItems[0].SubItems[4].Text), App.Platform.OS); } else { MessageBox.Show(AppStrings.BU_BinaryFile, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
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
            FormManager.FormShowOptions();
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
                    ProcessManager.OpenExplorer(Path.Combine(SelGame.FullBackUpDirPath, BU_LVTable.SelectedItems[0].SubItems[4].Text), App.Platform.OS);
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

        private void FrmMainW_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Форма была закрыта, сохраняем настройки приложения...
            Properties.Settings.Default.Save();
        }

        private void MNUWinMnuDisabler_Click(object sender, EventArgs e)
        {
            // Показываем модуля отключения клавиш...
            FormManager.FormShowKBHelper();
        }

        private void CE_OpenInNotepad_Click(object sender, EventArgs e)
        {
            if (!(String.IsNullOrEmpty(CFGFileName)))
            {
                ProcessManager.OpenTextEditor(CFGFileName, App.Platform.OS);
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
            List<String> CleanDirs = new List<string>
            {
                Path.Combine(SelGame.FullGamePath, "replay", "*.*")
            };
            FormManager.FormShowCleanup(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void PS_RemTextures_Click(object sender, EventArgs e)
        {
            // Удаляем все кастомные текстуры...
            List<String> CleanDirs = new List<string>
            {
                // Чистим загруженные с серверов модели и текстуры...
                Path.Combine(SelGame.FullGamePath, "download", "*.vt*"),
                Path.Combine(SelGame.FullGamePath, "download", "*.vmt"),
                Path.Combine(SelGame.FullGamePath, "download", "*.mdl"),
                Path.Combine(SelGame.FullGamePath, "download", "*.phy"),
                
                // Чистим установленные пользователем модели и текстуры...
                Path.Combine(SelGame.FullGamePath, "custom", "*.vt*"),
                Path.Combine(SelGame.FullGamePath, "custom", "*.vmt"),
                Path.Combine(SelGame.FullGamePath, "custom", "*.mdl"),
                Path.Combine(SelGame.FullGamePath, "custom", "*.phy")
            };

            // Чистим базы игр со старой системой. Удалить после полного перехода на новую...
            if (Properties.Settings.Default.AllowUnSafeCleanup)
            {
                CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "materials", "*.*"));
                CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "models", "*.*"));
            }
            FormManager.FormShowCleanup(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void PS_RemSecndCache_Click(object sender, EventArgs e)
        {
            // Удаляем содержимое вторичного кэша загрузок...
            List<String> CleanDirs = new List<string>
            {
                Path.Combine(SelGame.FullGamePath, "cache", "*.*"), // Кэш...
                Path.Combine(SelGame.FullGamePath, "custom", "user_custom", "*.*"), // Кэш спреев игр с н.с.к...
                Path.Combine(SelGame.GamePath, "config", "html", "*.*") // Кэш MOTD...
            };
            FormManager.FormShowCleanup(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
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
            ProcessManager.OpenWebPage(AppStrings.AppCVListURL);
        }

        private void CE_ManualBackUpCfg_Click(object sender, EventArgs e)
        {
            if (!(String.IsNullOrEmpty(CFGFileName)))
            {
                if (File.Exists(CFGFileName))
                {
                    FileManager.CreateConfigBackUp(CFGFileName, SelGame.FullBackUpDirPath, Properties.Resources.BU_PrefixCfg);
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
            List<String> CleanDirs = new List<string>
            {
                Path.Combine(SelGame.FullGamePath, "download", "*.mp3"),
                Path.Combine(SelGame.FullGamePath, "download", "*.wav")
            };
            if (Properties.Settings.Default.AllowUnSafeCleanup) { CleanDirs.Add(Path.Combine(SelGame.FullGamePath, "sound", "*.*")); }
            FormManager.FormShowCleanup(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void PS_RemCustDir_Click(object sender, EventArgs e)
        {
            // Удаляем пользовательного каталога...
            List<String> CleanDirs = new List<string>
            {
                Path.Combine(SelGame.FullGamePath, "custom", "*.*"),
                Path.Combine(SelGame.AppWorkshopDir, "*.*")
            };
            FormManager.FormShowCleanup(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void PS_DeepCleanup_Click(object sender, EventArgs e)
        {
            // Проведём глубокую очистку...
            List<String> CleanDirs = new List<string>
            {
                // Удалим старые бинарники и лаунчеры...
                Path.Combine(SelGame.GamePath, Path.GetDirectoryName(SelGame.SmallAppName), "bin", "*.*"),
                Path.Combine(SelGame.FullGamePath, "bin", "*.*"),
                Path.Combine(SelGame.GamePath, "*.exe"),

                // Удалим кэш загрузок...
                Path.Combine(SelGame.FullGamePath, "download", "*.*"),

                // Удалим кастомные файлы...
                Path.Combine(SelGame.FullGamePath, "custom", "*.*"),
                Path.Combine(SelGame.AppWorkshopDir, "*.*"),

                // Удалим другие кэши...
                Path.Combine(SelGame.FullGamePath, "cache", "*.*"),

                // Удалим пользовательские конфиги...
                Path.Combine(SelGame.FullGamePath, "cfg", "*.cfg")
            };

            // Конфиги их хранилища Steam Cloud...
            CleanDirs.AddRange(SelGame.CloudConfigs);

            // Данные платформы...
            if (Properties.Settings.Default.AllowUnSafeCleanup) { CleanDirs.Add(Path.Combine(SelGame.GamePath, "platform", "*.*")); }
            
            // Удаляем графические настройки NCF-игры...
            if (SelGame.IsUsingVideoFile) { CleanDirs.AddRange(SelGame.VideoCfgFiles); }

            // Запускаем процесс очистки...
            FormManager.FormShowCleanup(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CacheChkReq, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void PS_RemConfigs_Click(object sender, EventArgs e)
        {
            // Удаляем пользовательного каталога...
            List<String> CleanDirs = new List<string>
            {
                Path.Combine(SelGame.FullGamePath, "cfg", "*.*"),
                Path.Combine(SelGame.FullGamePath, "custom", "*.cfg")
            };
            CleanDirs.AddRange(SelGame.CloudConfigs);
            FormManager.FormShowCleanup(CleanDirs, ((Button)sender).Text.ToLower(), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
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
            if (Success) { HD_LastUpdate.Text = String.Format(AppStrings.HD_LastUpdateInfo, FileManager.Unix2DateTime(SelGame.HUDMan.SelectedHUD.LastUpdate).ToLocalTime()); }

            // Проверяем установлен ли выбранный HUD...
            SetHUDButtons(HUDManager.CheckInstalledHUD(SelGame.CustomInstallDir, SelGame.HUDMan.SelectedHUD.InstallDir));

            // Загрузим скриншот выбранного HUD...
            if (Success && !BW_HUDScreen.IsBusy) { BW_HUDScreen.RunWorkerAsync(); }
        }

        private void HD_Install_Click(object sender, EventArgs e)
        {
            if (!HUDManager.CheckHUDDatabase(Properties.Settings.Default.LastHUDTime))
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
                            FormManager.FormShowRemoveFiles(SingleToArray(Path.Combine(SelGame.CustomInstallDir, SelGame.HUDMan.SelectedHUD.InstallDir)));
                        }

                        // Проверяем существует ли такой файл. Если да, то удаляем...
                        if (File.Exists(SelGame.HUDMan.SelectedHUD.LocalFile)) { File.Delete(SelGame.HUDMan.SelectedHUD.LocalFile); }

                        // Начинаем загрузку архива с HUD...
                        FormManager.FormShowDownloader(Properties.Settings.Default.HUDUseUpstream ? SelGame.HUDMan.SelectedHUD.UpURI : SelGame.HUDMan.SelectedHUD.URI, SelGame.HUDMan.SelectedHUD.LocalFile);

                        // Распаковываем загруженный архив с файлами HUD...
                        FormManager.FormShowArchiveExtract(SelGame.HUDMan.SelectedHUD.LocalFile, Path.Combine(SelGame.CustomInstallDir, "hudtemp"));

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
                FormManager.FormShowRemoveFiles(SingleToArray(HUDPath));

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
            if (!String.IsNullOrEmpty(SelGame.HUDMan.SelectedHUD.Site)) { ProcessManager.OpenWebPage(SelGame.HUDMan.SelectedHUD.Site); }
        }

        private void MNUExtClnAppCache_Click(object sender, EventArgs e)
        {
            // Очистим загруженные приложением файлы...
            List<String> CleanDirs = new List<string>
            {
                Path.Combine(App.AppUserDir, Properties.Resources.HUDLocalDir, "*.*")
            };
            FormManager.FormShowCleanup(CleanDirs, ((ToolStripMenuItem)sender).Text.ToLower().Replace("&", String.Empty), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void MNUExtClnTmpDir_Click(object sender, EventArgs e)
        {
            // Очистим каталоги с временными файлами системы...
            List<String> CleanDirs = new List<string>
            {
                Path.Combine(Path.GetTempPath(), "*.*")
            };
            FormManager.FormShowCleanup(CleanDirs, ((ToolStripMenuItem)sender).Text.ToLower().Replace("&", String.Empty), AppStrings.PS_CleanupSuccess, SelGame.FullBackUpDirPath, SelGame.GameBinaryFile);
        }

        private void MNUShowLog_Click(object sender, EventArgs e)
        {
            // Выведем на экран содержимое отладочного журнала...
            if (Properties.Settings.Default.EnableDebugLog)
            {
                string DFile = Path.Combine(App.AppUserDir, Properties.Resources.DebugLogFileName);
                if (File.Exists(DFile)) { FormManager.FormShowLogViewer(DFile); } else { MessageBox.Show(AppStrings.AppNoDebugFile, Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
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
            ProcessManager.OpenExplorer(Path.Combine(SelGame.CustomInstallDir, SelGame.HUDMan.SelectedHUD.InstallDir), App.Platform.OS);
        }

        private void MNUExtClnSteam_Click(object sender, EventArgs e)
        {
            // Запустим модуль очистки кэшей Steam...
            FormManager.FormShowStmCleaner(App.FullSteamPath, SelGame.FullBackUpDirPath, App.Platform.SteamAppsFolderName, App.Platform.SteamProcName);
        }

        private void MNUMuteMan_Click(object sender, EventArgs e)
        {
            // Запустим менеджер управления отключёнными игроками...
            FormManager.FormShowMuteManager(SelGame.GetActualBanlistFile(), SelGame.FullBackUpDirPath);
        }

        private void MNUSupportChat_Click(object sender, EventArgs e)
        {
            // Откроем канал поддержки в клиенте Telegram для десктопа, а если он не установлен - в браузере...
            try { Process.Start(Properties.Resources.AppTgChannel); } catch { ProcessManager.OpenWebPage(Properties.Resources.AppTgChannelURL); }
        }

        private void SB_SteamID_Click(object sender, EventArgs e)
        {
            // Открываем диалог выбора SteamID и прописываем пользовательский выбор...
            try { string Result = FormManager.FormShowIDSelect(SelGame.SteamIDs); if (!(String.IsNullOrWhiteSpace(Result))) { SB_SteamID.Text = Result; Properties.Settings.Default.LastSteamID = Result; FindGames(); } } catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); }
        }

        private void BU_LVTable_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Блокируем некоторые кнопки на панели инструментов модуля управления резервными копиями если выбрано более одной...
            bool IsSingle = BU_LVTable.SelectedItems.Count <= 1;
            BUT_OpenNpad.Enabled = IsSingle;
            BUT_ExploreBUp.Enabled = IsSingle;
        }

        private void MNUDonate_Click(object sender, EventArgs e)
        {
            // Откроем веб-страницу с реквизитами...
            ProcessManager.OpenWebPage(Properties.Resources.AppURLDonate);
        }
    }
}
