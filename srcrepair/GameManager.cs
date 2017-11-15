﻿/*
 * This file is a part of SRC Repair project. For more information
 * visit official site: https://www.easycoding.org/projects/srcrepair
 * 
 * Copyright (c) 2011 - 2017 EasyCoding Team (ECTeam).
 * Copyright (c) 2005 - 2017 EasyCoding Team.
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
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace srcrepair
{
    public sealed class GameManager
    {
        private List<SourceGame> SourceGames;
        public List<String> InstalledGames { get; private set; }
        public SourceGame SelectedGame { get; private set; }

        public void Select(string GameName)
        {
            SelectedGame = SourceGames.Find(Item => String.Equals(Item.FullAppName, GameName, StringComparison.CurrentCultureIgnoreCase));
        }

        public GameManager(CurrentApp App)
        {
            // Создаём объекты для хранения базы игр...
            SourceGames = new List<SourceGame>();
            InstalledGames = new List<String>();

            // При использовании нового метода поиска установленных игр, считаем их из конфига Steam...
            List<String> GameDirs = SteamManager.FormatInstallDirs(App.FullSteamPath, App.Platform.SteamAppsFolderName);

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
                        if (XMLD.GetElementsByTagName("Enabled")[i].InnerText == "1" || !Properties.Settings.Default.HideUnsupportedGames)
                        {
                            SourceGame SG = new SourceGame(XMLNode[i].Attributes["Name"].Value, XMLD.GetElementsByTagName("DirName")[i].InnerText, XMLD.GetElementsByTagName("SmallName")[i].InnerText, XMLD.GetElementsByTagName("Executable")[i].InnerText, XMLD.GetElementsByTagName("SID")[i].InnerText, XMLD.GetElementsByTagName("SVer")[i].InnerText, XMLD.GetElementsByTagName("VFDir")[i].InnerText, App.Platform.OS == CurrentPlatform.OSType.Windows ? XMLD.GetElementsByTagName("HasVF")[i].InnerText == "1" : true, XMLD.GetElementsByTagName("UserDir")[i].InnerText == "1", XMLD.GetElementsByTagName("HUDsAvail")[i].InnerText == "1", App.FullAppPath, App.AppUserDir, App.FullSteamPath, App.Platform.SteamAppsFolderName, GameDirs);
                            if (SG.IsInstalled)
                            {
                                SourceGames.Add(SG);
                                InstalledGames.Add(SG.FullAppName);
                            }
                        }
                    }
                    catch (Exception Ex) { CoreLib.WriteStringToLog(Ex.Message); }
                }
            }
        }
    }
}
