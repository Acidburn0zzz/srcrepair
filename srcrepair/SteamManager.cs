﻿using System;
using System.Collections.Generic;
using System.IO;
using Gameloop.Vdf;

namespace srcrepair
{
    public static class SteamManager
    {
        /// <summary>
        /// Возвращает путь к главному VDF конфигу Steam.
        /// </summary>
        /// <param name="SteamPath">Каталог установки Steam</param>
        /// <returns>Путь к VDF конфигу</returns>
        public static string GetSteamConfig(string SteamPath)
        {
            return Path.Combine(SteamPath, "config", "config.vdf");
        }

        /// <summary>
        /// Возвращает список используемых на данном компьютере SteamID. Устаревший способ.
        /// </summary>
        /// <param name="SteamPath">Каталог установки Steam</param>
        /// <returns>Список Steam user ID</returns>
        public static List<String> GetUserIDs(string SteamPath)
        {
            // Создаём новый список...
            List<String> Result = new List<String>();

            // Получаем список каталогов...
            string DDir = Path.Combine(SteamPath, "userdata");
            if (Directory.Exists(DDir))
            {
                DirectoryInfo DInfo = new DirectoryInfo(DDir);
                foreach (DirectoryInfo SubDir in DInfo.GetDirectories())
                {
                    Result.Add(SubDir.FullName);
                }
            }

            // Возвращаем результат...
            return Result;
        }

        /// <summary>
        /// Возвращает список используемых на данном компьютере SteamID64.
        /// </summary>
        /// <param name="SteamPath">Каталог установки Steam</param>
        /// <returns>Список SteamID64</returns>
        public static List<String> GetSteamIDs(string SteamPath)
        {
            // Создаём список...
            List<String> Result = new List<String>();

            // Обходим базу данных...
            using (StreamReader SR = new StreamReader(GetSteamConfig(SteamPath)))
            {
                dynamic AX = VdfConvert.Deserialize(SR, VdfSerializerSettings.Default);
                foreach (dynamic AZ in AX.Value["Software"]["Valve"]["Steam"]["Accounts"].Children())
                {
                    Result.Add(AZ.Value["SteamID"].ToString());
                }
            }

            // Возвращаем результат...
            return Result;
        }
    }
}
