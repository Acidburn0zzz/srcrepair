﻿/*
 * Модуль обновления программы SRC Repair.
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
using System.ComponentModel;
using System.Windows.Forms;
using System.Net;
using System.Diagnostics;
using System.IO;

namespace srcrepair
{
    public partial class frmUpdate : Form
    {
        public frmUpdate(string UA, string A, string V, string U)
        {
            InitializeComponent();
            UserAgent = UA;
            FullAppPath = A;
            AppVersionInfo = V;
            AppUserDir = U;
        }

        private string NewVersion;
        private string UpdateURI;
        private string UpdateFileName;
        private string DBHash;
        private string DBHashNew;
        private string HUDHash;
        private string HUDHashNew;
        private string FullAppPath;
        private string UserAgent;
        private string AppVersionInfo;
        private string AppUserDir;
        private bool AppAvailable;
        private bool DbAvailable;
        private bool HudAvailable;

        private void frmUpdate_Load(object sender, EventArgs e)
        {
            // Заполняем...
            Text = String.Format(Text, Properties.Resources.AppName);

            // Запускаем функции проверки обновлений программы...
            if (!WrkChkApp.IsBusy) { WrkChkApp.RunWorkerAsync(); }

            // Запускаем функции проверки обновлений базы данных игр...
            if (!WrkChkDb.IsBusy) { WrkChkDb.RunWorkerAsync(); }
            
            // Запускаем функции проверки обновлений базы данных HUD...
            if (!WrkChkHUD.IsBusy) { WrkChkHUD.RunWorkerAsync(); }
        }

        private string GenerateUpdateFileName(string Url)
        {
            return Path.HasExtension(Url) ? Url : Path.ChangeExtension(Url, "exe");
        }

        private void UpdateTimeSetApp()
        {
            // Установим дату последней проверки обновлений...
            Properties.Settings.Default.LastUpdateTime = DateTime.Now;
        }

        private void UpdateTimeSetHUD()
        {
            // Установим дату последней проверки обновлений базы HUD...
            Properties.Settings.Default.LastHUDTime = DateTime.Now;
        }

        private void WrkChkApp_DoWork(object sender, DoWorkEventArgs e)
        {
            // Установим значок проверки обновлений...
            Invoke((MethodInvoker)delegate() { UpdAppImg.Image = Properties.Resources.upd_chk; });

            // Опишем буферные переменные...
            string DnlStr;

            // Получаем файл с номером версии и ссылкой на новую...
            using (WebClient Downloader = new WebClient())
            {
                Downloader.Headers.Add("User-Agent", UserAgent);
                DnlStr = Downloader.DownloadString(Properties.Resources.UpdateChURI);
            }

            // Мы получили URL и версию...
            NewVersion = DnlStr.Substring(0, DnlStr.IndexOf("!")); // Получаем версию...
            UpdateURI = DnlStr.Remove(0, DnlStr.IndexOf("!") + 1); // Получаем URL...
        }

        private void WrkChkApp_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                if (e.Error == null)
                {
                    // Проверим, является ли версия на сервере новее, чем текущая...
                    if (CoreLib.CompareVersions(AppVersionInfo, NewVersion))
                    {
                        // Доступна новая версия...
                        Invoke((MethodInvoker)delegate()
                        {
                            UpdAppImg.Image = Properties.Resources.upd_av;
                            UpdAppStatus.Text = String.Format(CoreLib.GetLocalizedString("UPD_AppUpdateAvail"), NewVersion);
                        });
                        AppAvailable = true;
                    }
                    else
                    {
                        // Новых версий не обнаружено...
                        Invoke((MethodInvoker)delegate()
                        {
                            UpdAppImg.Image = Properties.Resources.upd_nx;
                            UpdAppStatus.Text = CoreLib.GetLocalizedString("UPD_AppNoUpdates");
                        });
                        AppAvailable = false; UpdateTimeSetApp();
                    }
                }
                else
                {
                    // Произошла ошибка...
                    Invoke((MethodInvoker)delegate()
                    {
                        UpdAppImg.Image = Properties.Resources.upd_err;
                        UpdAppStatus.Text = CoreLib.GetLocalizedString("UPD_AppCheckFailure");
                    });

                    // Запишем в журнал...
                    CoreLib.WriteStringToLog(e.Error.Message);

                    // Переключим свойство доступности обновления...
                    AppAvailable = false;
                }
            }
            catch (Exception Ex)
            {
                CoreLib.WriteStringToLog(Ex.Message);
            }
        }

        private void WrkChkDb_DoWork(object sender, DoWorkEventArgs e)
        {
            // Установим значок проверки обновлений...
            Invoke((MethodInvoker)delegate() { UpdDBImg.Image = Properties.Resources.upd_chk; });

            // Получаем файл с номером версии и ссылкой на новую...
            using (WebClient Downloader = new WebClient())
            {
                // Получим хеш...
                Downloader.Headers.Add("User-Agent", UserAgent);
                DBHashNew = Downloader.DownloadString(Properties.Resources.UpdateGameDBHash);
            }

            // Рассчитаем хеш текущего файла...
            DBHash = CoreLib.CalculateFileMD5(Path.Combine(FullAppPath, Properties.Settings.Default.GameListFile));
        }

        private void WrkChkDb_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                // Проверим на наличие ошибок...
                if (e.Error == null)
                {
                    // Проверим хеши...
                    if (DBHash != DBHashNew)
                    {
                        // Хеши не совпадают, будем обновлять...
                        Invoke((MethodInvoker)delegate()
                        {
                            UpdDBImg.Image = Properties.Resources.upd_av;
                            UpdDBStatus.Text = String.Format(CoreLib.GetLocalizedString("UPD_DbUpdateAvail"), DBHashNew.Substring(0, 7));
                        });
                        DbAvailable = true;
                    }
                    else
                    {
                        // Хеши совпали, обновление не требуется...
                        Invoke((MethodInvoker)delegate()
                        {
                            UpdDBImg.Image = Properties.Resources.upd_nx;
                            UpdDBStatus.Text = CoreLib.GetLocalizedString("UPD_DbNoUpdates");
                        });
                        DbAvailable = false;
                    }
                }
                else
                {
                    // Произошла ошибка...
                    Invoke((MethodInvoker)delegate()
                    {
                        UpdDBImg.Image = Properties.Resources.upd_err;
                        UpdDBStatus.Text = CoreLib.GetLocalizedString("UPD_DbCheckFailure");
                    });

                    // Запишем в журнал...
                    CoreLib.WriteStringToLog(e.Error.Message);

                    // Переключим свойство доступности обновления...
                    DbAvailable = false;
                }
            }
            catch (Exception Ex)
            {
                CoreLib.WriteStringToLog(Ex.Message);
            }
        }

        private void WrkChkHUD_DoWork(object sender, DoWorkEventArgs e)
        {
            // Установим значок проверки обновлений...
            Invoke((MethodInvoker)delegate() { UpdHUDDbImg.Image = Properties.Resources.upd_chk; });

            // Получаем файл с номером версии и ссылкой на новую...
            using (WebClient Downloader = new WebClient())
            {
                // Получим хеш...
                Downloader.Headers.Add("User-Agent", UserAgent);
                HUDHashNew = Downloader.DownloadString(Properties.Resources.UpdateHUDDBFileHash);
            }

            // Рассчитаем хеш текущего файла...
            HUDHash = CoreLib.CalculateFileMD5(Path.Combine(FullAppPath, Properties.Settings.Default.HUDDbFile));
        }

        private void WrkChkHUD_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                // Проверим на наличие ошибок...
                if (e.Error == null)
                {
                    // Проверим хеши...
                    if (HUDHash != HUDHashNew)
                    {
                        // Хеши не совпадают, будем обновлять...
                        Invoke((MethodInvoker)delegate()
                        {
                            UpdHUDDbImg.Image = Properties.Resources.upd_av;
                            UpdHUDStatus.Text = String.Format(CoreLib.GetLocalizedString("UPD_HUDUpdateAvail"), HUDHashNew.Substring(0, 7));
                        });
                        HudAvailable = true;
                    }
                    else
                    {
                        // Хеши совпали, обновление не требуется...
                        Invoke((MethodInvoker)delegate()
                        {
                            UpdHUDDbImg.Image = Properties.Resources.upd_nx;
                            UpdHUDStatus.Text = CoreLib.GetLocalizedString("UPD_HUDNoUpdates");
                        });
                        HudAvailable = false; UpdateTimeSetHUD();
                    }
                }
                else
                {
                    // Произошла ошибка...
                    Invoke((MethodInvoker)delegate()
                    {
                        UpdHUDDbImg.Image = Properties.Resources.upd_err;
                        UpdHUDStatus.Text = CoreLib.GetLocalizedString("UPD_HUDCheckFailure");
                    });

                    // Запишем в журнал...
                    CoreLib.WriteStringToLog(e.Error.Message);

                    // Переключим свойство доступности обновления...
                    HudAvailable = false;
                }
            }
            catch (Exception Ex)
            {
                CoreLib.WriteStringToLog(Ex.Message);
            }
        }

        private void frmUpdate_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = (e.CloseReason == CloseReason.UserClosing) && (WrkChkApp.IsBusy || WrkChkDb.IsBusy || WrkChkHUD.IsBusy);
        }

        private void UpdDBStatus_Click(object sender, EventArgs e)
        {
            if (!WrkChkDb.IsBusy)
            {
                if (DbAvailable)
                {
                    if (CoreLib.IsDirectoryWritable(FullAppPath))
                    {
                        UpdateFileName = GenerateUpdateFileName(Path.Combine(FullAppPath, Properties.Settings.Default.GameListFile));
                        CoreLib.DownloadFileEx(Properties.Resources.UpdateGameDBFile, UpdateFileName);
                        if (File.Exists(UpdateFileName)) { MessageBox.Show(CoreLib.GetLocalizedString("UPD_GamL_Updated"), Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information); } else { MessageBox.Show(CoreLib.GetLocalizedString("UPD_UpdateFailure"), Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                        Close();
                    }
                    else
                    {
                        MessageBox.Show(CoreLib.GetLocalizedString("UPD_NoWritePermissions"), Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show(CoreLib.GetLocalizedString("UPD_GamL_Latest"), Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void UpdAppStatus_Click(object sender, EventArgs e)
        {
            if (!WrkChkApp.IsBusy)
            {
                if (AppAvailable)
                {
                    UpdateFileName = GenerateUpdateFileName(Path.Combine(AppUserDir, Path.GetFileName(UpdateURI)));
                    CoreLib.DownloadFileEx(UpdateURI, UpdateFileName);
                    if (File.Exists(UpdateFileName))
                    {
                        UpdateTimeSetApp();
                        MessageBox.Show(CoreLib.GetLocalizedString("UPD_UpdateSuccessful"), Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        try { Process.Start(UpdateFileName); } catch (Exception Ex) { CoreLib.HandleExceptionEx(CoreLib.GetLocalizedString("UPD_UpdateFailure"), Properties.Resources.AppName, Ex.Message, Ex.Source, MessageBoxIcon.Error); }
                        Environment.Exit(9);
                    }
                    else
                    {
                        MessageBox.Show(CoreLib.GetLocalizedString("UPD_UpdateFailure"), Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Close();
                    }
                }
                else
                {
                    MessageBox.Show(CoreLib.GetLocalizedString("UPD_LatestInstalled"), Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void UpdHUDStatus_Click(object sender, EventArgs e)
        {
            if (!WrkChkHUD.IsBusy)
            {
                if (HudAvailable)
                {
                    if (CoreLib.IsDirectoryWritable(FullAppPath))
                    {
                        UpdateFileName = GenerateUpdateFileName(Path.Combine(FullAppPath, Properties.Settings.Default.HUDDbFile));
                        CoreLib.DownloadFileEx(Properties.Resources.UpdateHUDDBFile, UpdateFileName);
                        if (File.Exists(UpdateFileName)) { MessageBox.Show(CoreLib.GetLocalizedString("UPD_HUDDb_Updated"), Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information); UpdateTimeSetHUD(); } else { MessageBox.Show(CoreLib.GetLocalizedString("UPD_UpdateFailure"), Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                        Close();
                    }
                    else
                    {
                        MessageBox.Show(CoreLib.GetLocalizedString("UPD_NoWritePermissions"), Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show(CoreLib.GetLocalizedString("UPD_HUDDb_Latest"), Properties.Resources.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
