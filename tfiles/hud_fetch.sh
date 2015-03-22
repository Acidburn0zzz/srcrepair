#!/bin/bash

function fetch_hud
{
    # Проверяем существование файла со скриншотом и если он не существует, загружаем...
    if [ ! -f "$3.png" ]; then
        # Загружаем скриншот с удалённого сервера...
        wget $1 -O $3.png > /dev/null 2> /dev/null  
    fi
    
    # Проверяем существование архива и если он не существует, загружаем...
    if [ ! -f "$3.zip" ]; then
        # Загружаем новую версию архива из апстрима...
        wget $2 -O $3.zip > /dev/null 2> /dev/null
    fi
}

fetch_hud http://huds.tf/img/main/7hud.png https://github.com/Sevin7/7HUD/archive/master.zip 7HUD
fetch_hud http://huds.tf/img/main/herganhud.png https://github.com/Hergan5/herganhud/archive/master.zip herganhud
fetch_hud http://huds.tf/img/main/pikleshud.png https://github.com/piklestf2/pikles-hud/archive/master.zip pikles-hud
fetch_hud http://huds.tf/img/main/broeselhudblue.png https://github.com/fblue/broeselhud_blue/archive/master.zip broeselhud_blue
fetch_hud http://huds.tf/img/main/zhud.png https://github.com/z4-/zhud/archive/master.zip zhud
fetch_hud http://huds.tf/img/main/basthud.png https://github.com/basbanaan/bastHUD/archive/master.zip basthud
fetch_hud http://huds.tf/img/main/takyahud.png https://github.com/takram/takyahud-classic/archive/master.zip takyahud
fetch_hud http://huds.tf/img/main/sirhud.png https://github.com/sirgrey/SirHUD/archive/master.zip sirhud
fetch_hud http://huds.tf/img/main/tf2hudplus.png https://github.com/SnowshoeIceboot/TF2HudPlus/archive/master.zip tf2hudplus
fetch_hud http://huds.tf/img/main/rpvhud.png https://github.com/harvardbb/rpvhud/archive/master.zip rpvhud
