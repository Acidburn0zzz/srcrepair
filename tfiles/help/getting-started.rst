.. _getting_started:

***********************
Приступая к работе
***********************

.. index:: установка программы
.. _gs-install:

Установка программы
==========================================

Для установки программы в систему скачайте её, затем запустите загруженный файл и следуйте всем инструкциям программы-мастера. Далее мы рассмотрим все шаги максимально подробно.

.. index:: шаг установки 1
.. _gs-step1:

Шаг 1: выбор языка
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Сначала необходимо выбрать язык, который будет использовать программа установки SRC Repair (это не язык самого приложения, а только мастера установки).

.. index:: шаг установки 2
.. _gs-step2:

Шаг 2: приветствие мастера
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Здесь отображается приветствие программы установки. Закройте все программы, которые могут помешать установке SRC Repair, и нажмите **Далее** для перехода к следующему шагу.

.. index:: шаг установки 3
.. _gs-step3:

Шаг 3: лицензионное соглашение
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

На этом шаге вы должны внимательно прочитать и принять условия лицензионного соглашения программы SRC Repair. Если вы согласны с ним, поставьте точку в пункт **Я принимаю условия соглашения** и нажмите **Далее**. Если не согласны, то программа не будет установлена.

.. index:: шаг установки 4
.. _gs-step4:

Шаг 4: информация о программе
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Теперь вы увидите подробную информацию о программе, её авторах, сможете посетить официальный сайт, вступить в официальную группу программы в Steam. Внимательно прочитайте предложенный текст и нажмите **Далее**.

.. index:: шаг установки 5
.. _gs-step5:

Шаг 5: путь установки
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Программа может быть установлена как глобально для всех пользователей (установщик в таком случае должен быть запущен с правами администратора), так и только для текущего (дополнительные права не требуются).

На этом шаге мастер просит указать в какую папку следует установить SRC Repair. Значение по умолчанию — **%LOCALAPPDATA%\\SRC Repair** если установщик запущен без прав администратора, либо **%PROGRAMFILES%\\SRC Repair** при их наличии.

Если на системном диске недостаточно свободного места, можете установить в любое другое место. Для выбора папки, нажмите кнопку **Обзор**, выберите в дереве каталогов нужный, нажмите **OK**, а затем **Далее**.

.. index:: шаг установки 6
.. _gs-step6:

Шаг 6: меню Пуск
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Сейчас вам предложено задать название для группы SRC Repair в меню кнопки Пуск, либо отключить создание ярлыков вообще (не рекомендуется отключать создание ярлыков). Сделайте свой выбор, затем нажмите **Далее**.

.. index:: шаг установки 7
.. _gs-step7:

Шаг 7: дополнительные задачи
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

На этом шаге, можно включить/отключить создание ярлыков программы на рабочем столе, а также на панели быстрого запуска. Выбрав нужные настройки, нажмите **Далее**.

.. index:: шаг установки 8
.. _gs-step8:

Шаг 8: дополнительные задачи
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Итак, всё почти готово к установке. Сейчас будет показано резюме установки для того, чтобы вы смогли всё ещё раз проверить. Если что-то указано неверно, нажмите **Назад** и сделайте нужные правки. Если всё правильно, нажмите **Установить** для запуска процесса установки.

.. index:: шаг установки 9
.. _gs-step9:

Шаг 9: установка
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Запущена установка: копирование файлов, создание каталогов, ярлыков и нужных записей. Что-либо делать на этом шаге не нужно — просто следите за процессом. Обычно этот шаг занимает несколько секунд.

.. index:: шаг установки 10
.. _gs-step10:

Шаг 10: установка завершена
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Программа успешно установлена. Нажмите **Завершить** для выхода из мастера. Если хотите, установите флажок в пункт **Запустить SRC Repair**.

.. index:: запуск программы
.. _gs-launch:

Запуск программы
==========================================

Запустить программу можно сразу после установки, либо из меню **Пуск** — **(Все) Программы** — **SRC Repair** — **SRC Repair**.

SRC Repair, запущенная стандартным способом, автоматически выбирает язык в зависимости от региональных настроек операционной системы Windows.

Если вы запустите программу без прав локального администратора, ряд функций, включая :ref:`Редактор Hosts <modules-mhed>` и :ref:`Модуль отключения системных клавиш <modules-kbd>`, будут недоступны. Чтобы активировать отключённые функции просто запустите программу от учётной записи с правами администратора, а в Windows 7 и выше достаточно нажать правой кнопкой по ярлыку SRC Repair и выбрать в контекстном меню пункт **Запустить от имени администратора**.

.. index:: действия при запуске
.. _gs-startup:

Действия, выполняемые при запуске
==========================================

При запуске SRC Repair получит из реестра Windows пути к Steam, проверит наличие каталога Steam и игровых данных на диске, а также считает список установленных поддерживаемых игр. Если при проверке не удастся получить путь к каталогу Steam из реестра, будет выдано окно с предложением указать его вручную. Путь, заданный вручную, сохранится в файле конфигурации SRC Repair и при следующем запуске запрошен вновь не будет.

Внимание! При использовании 64-битной версии SRC Repair на 64-битной операционной системе Windows при использовании 32-битного клиента Steam программа потребует указать путь вручную (только один раз). Это не баг, а особенности 64-битной платформы (32-битный Steam хранит свои параметры в 32-битном реестре). Если же установлен 64-битный Steam, никаких сообщений не возникнет.

.. index:: начало работы
.. _gs-useapp:

Начало работы с программой
==========================================

Для начала работы из списка одну из поддерживаемых игр. Если на вашем компьютере обнаружена только одна установленная игра, она будет выбрана автоматически. Ваш выбор сохраняется в настройках программы.

Если программа не находит ни одной поддерживаемой игры, то просто хотя бы раз запустите эту игру из Steam. После выхода из неё и повторного запуска SRC Repair данная игра появится в списке.

.. index:: хранение резервных копий, хранение настроек программы
.. _gs-backups:

Хранение параметров и резервных копий
==========================================

Настройки SRC Repair хранятся в каталоге **%LOCALAPPDATA%\\EasyCoding_Team**, для каждой версии отдельно.

Резервные копии, создаваемые программой, хранятся в каталоге **%APPDATA%\\SRC Repair**.

.. index:: запуск модуля обновлений
.. _gs-update:

Обновление программы
==========================================

Вы можете запустить обновление программы через меню **Справка** — **Проверить наличие обновлений**.

.. index:: удаление программы, деинсталляция
.. _gs-uninstall:

Удаление программы
==========================================

Чтобы удалить программу, запустите встроенную программу удаления через **Панель управления** — **Установка и удаление программ** — **SRC Repair** — **Удалить**.

Программа автоматического удаления удалит все файлы программы, записи реестра, ярлыки, но не будет удалять её настройки, хранящиеся в XML-файлах, и резервные копии (на случай если вы решите когда-нибудь переустановить SRC Repair). Настройки и резервные копии вы можете удалить вручную из каталогов, которые указаны выше.

.. index:: ЧаВо, FAQ, часто задаваемые вопросы
.. _gs-faq:

Часто задаваемые вопросы
==========================================

**При попытке запуска программы появляется сообщение об ошибке Microsoft .NET Framework 4.**

Для работы программы необходим пакет Microsoft .NET Framework версии 4.6.1 или выше. Установите его с `официального сайта Microsoft <https://www.microsoft.com/ru-RU/download/details.aspx?id=49981>`_.

**Как я могу запустить программу под Windows XP/Vista?**

Поддержка данных устаревших операционных систем полностью прекращена.

**Программа медленно запускается.**

Для ускорения запуска программы :ref:`установите <gs-install>` её с правами администратора для всех пользователей. При такой установке будут автоматически созданы и установлены машинные сборки, что значительно ускорит холодный запуск приложения.

**Будет ли программа поддерживать пиратские версии игр?**

Нет, не будет. Мы за лицензионный софт.

**Будет ли поддержка игр на движке Source 2 и когда?**

Да, планируется, но точная дата неизвестна.

**Почему не поддерживается Dota 2?**

Данная игра использует движок Source 2, который пока не поддерживается.

**Зачем утилите модуль Редактор файла Hosts?**

Многие вредоносные программы прописывают туда свои серверы для кражи паролей, поэтому с помощью данного редактора пользователи могут быстро проверить содержимое и удалить строки, добавленные вредоносными программами.

**Может ли программа украсть пароль?**

Нет, не может. Весь функционал программы документирован, исходный код открыт для всех желающих.

**У меня есть предложения/замечания. Где я могу их высказать?**

Справка — Высказать мнение о программе авторам.

**На чём написана программа?**

C#.

**Мой файрвол (брандмауэр) заявляет, что SRC Repair хочет получить доступ к сети Интернет. Зачем?**

Работает модуль автоматического обновления программы. Раз в неделю он проверяет наличие новых версий на нашем сервере. Если вы не хотите, чтобы программа проверяла обновления, отключите эту функцию в настройках.

**Я хочу добавить поддержку модификации. Как это сделать?**

Выберите пункт Справка — Сообщить об ошибке и создайте заявку на добавление новой игры или модификации. Помните, что поддерживаются только игры на движке Source.

**Сколько занимает пакет Microsoft .NET Framework 4.6.1?**

Около 40-45 МБ.

**Как собрать программу из исходников?**

1. Установите пакет Microsoft Visual Studio 2017 Community.
2. Откройте файл проекта **srcrepair.sln** в Microsoft Visual Studio.
3. На главной панели инструментов переключите тип сборки с **Debug** на **Release**.
4. В меню **Tools** - **NuGet Package Manager** выберите пункт **Manage NuGet packages for Solution**, затем **Restore**.
5. В меню **Build** выберите пункт **Build Solution**.

**Я нашёл ошибку. Как мне сообщить о ней?**

Справка — Сообщить об ошибке.

**Умеет ли данная программа очищать мусор, остающийся на диске в результате использования игр на движке Source?**

Да, умеет. Инструменты вы найдёте на странице :ref:`Устранение проблем и очистка <cleanup-wizard>`.

**Умеет ли данная программа очищать мусор, остающийся на диске в результате использования клиента Steam?**

Да. Всё необходимое находится в меню Инструменты — :ref:`Модуль очистки кэшей Steam <modules-stmcln>`.

**При запуске программы или выборе игры появляется ошибка при открытии ключа реестра. Что делать?**

Для начала запустите данную игру из Steam хотя бы один раз, затем перезапустите SRC Repair. Если это не помогает, то скорее всего у вас включён :ref:`параметр запуска <gt-params>` -autoconfig или -dxlevel. Откройте :ref:`окно изменения <gt-setparams>` параметров запуска и сотрите всё из данной строки. Зайдите в настройки видео игры и задайте настройки графики. Выйдите из игры.

**Хочу установить программу в особый каталог, но при запуске установки появляется ошибка.**

Чтобы установить программу в привилегированный каталог, установщик должен быть запущен с правами администратора. Нажмите правой кнопкой по установщику и выберите пункт **Запуск от имени администратора**.
