﻿/*
 * Класс системы управления FPS-конфигами.
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
using System.Linq;
using System.Text;

namespace srcrepair
{
    /// <summary>
    /// Класс для работы с коллекцией FPS-конфигов.
    /// </summary>
    class ConfigManager
    {
        /// <summary>
        /// Хранит информацию обо всех доступных конфигах.
        /// </summary>
        private List<CFGTlx> Configs;

        /// <summary>
        /// Управляет выбранным конфигом.
        /// </summary>
        public CFGTlx FPSConfig { get; set; }
    }
}