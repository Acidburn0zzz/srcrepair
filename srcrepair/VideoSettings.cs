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
namespace srcrepair
{
    /// <summary>
    /// Общий класс VideoSettings. Напрямую не используется.
    /// </summary>
    public abstract class VideoSettings
    {
        /// <summary>
        /// Хранит разрешение по горизонтали.
        /// </summary>
        protected int _ScreenWidth = 800;

        /// <summary>
        /// Хранит разрешение по вертикали.
        /// </summary>
        protected int _ScreenHeight = 600;

        /// <summary>
        /// Возвращает / задаёт разрешение по горизонтали.
        /// </summary>
        public int ScreenWidth { get { return _ScreenWidth; } set { _ScreenWidth = value; } }

        /// <summary>
        /// Возвращает / задаёт разрешение по вертикали.
        /// </summary>
        public int ScreenHeight { get { return _ScreenHeight; } set { _ScreenHeight = value; } }
    }
}
