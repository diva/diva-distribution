About localizing Wifi
=====================

Contents:
  Information for users
  Information for translators
  Information for administrators and grid owners
  Information for developers

Information for users
---------------------
Wifi is able to present its web pages to you in your preferred language. It will use the language that you have set in your web browser. If you do not see Wifi in your preferred language, then it might help to ask the grid owner or adminsitrator whether the localization option is enabled and whether appropriate translations are available.

Information for translators
---------------------------
To create a new translation for Wifi, there are two areas to address: the web pages and the texts within the Wifi module. Translating the web pages is done by copying the HTML files from directory WifiPages/de to a new directory, for example WifiPages/fr-BE. The name of the subdirectory must be a language code; this is used by Wifi to find the appropriate translations. Wifi is able to use translations for neutral language codes as a fallback, if it can't find one for a specific language code. For example, it uses the translation for "de", if it can not find one for "de-AT".

In the new subdirectory, edit all files by replacing the parts using German text with your translations. You can also use the English defaults found in directory WifiPages as an aid for your translations. However, be aware that not all HTML files in WifiPages need to be translated. The subset found in WifiPages/de comprises all files that require translation.

To translate the Wifi module texts, use the provided PO template file, Diva.Wifi.pot. Make a copy of this file and rename it such that its name contains the language code and its extension becomes ".po". Then add the translations for each application string in this PO file. For example, a German translation goes into file Diva.Wifi.de.po. The benefit of using a PO file is that there are many tools available for various operating systems (e.g. Poedit, http://www.poedit.net). As an alternative, Windows XML resource files with extension .resx can be used as well. Make a copy of Diva.Wifi.en.resx and rename it to contain the corresponding language code. Then edit this copy by replacing the strings enclosed by <value> and </value> with the translations.

New localization sets consisting of a set of translated HTML files and translated PO or .resx files can be submitted to a grid owner for deployment or to the Wifi distributor for inclusion into the distribution.

Information for administrators and grid owners
----------------------------------------------
Localization is optional and disabled by default. If you want to offer a localized Wifi, then you need to set configuration option "LocalizationCachingPeriod" in section [WifiService] to a value other than 0 (24 hours is a recommended value). Furthermore, for each language you want to support, the appropriate translation data must be provided. This translation data comprises a so-called satellite assemby and a set of translated HTML files for each language.

The satellite assembly is a DLL file named Diva.Wifi.resources.dll that must reside in a subdirectory of OpenSimulator's bin directory. For example, the German translations are found in directory bin/de. The translated HTML files must reside in a dedicated subdirectory of directory WifiPages. To use the German example again, these localized web pages are found in directory WifiPages/de. Usually, Wifi is distributed with a predefined set of localizations which are already deployed to the right places. But if you want to add translations provided by other sources, then be sure to install them correctly. 

To deploy localization sets obtained as translated HTML files and PO or .resx files to an existing installation of Wifi/OpenSimulator, the HTML files must be manually copied/moved into the right subdirectory of WifiPages. To create and deploy the satellite assembly, use the helper scripts make_languages.sh (for PO or .resx files) or make_languages.bat (for .resx files). These scripts require an installation of Mono (make_languages.sh) or of the Microsoft SDK (make_languages.bat); the tools Resource Generator, resgen, and Assembly Linker, al, must be accessible via the system's path. Invoke one of these script in the directory where you saved the PO or .resx file(s); the satellite assemblies for each language will be generated and deployed to OpenSimulator's bin directory. After a restart of OpenSimulator, the new language(s) will be available.

Information for developers
--------------------------
Each string with English text that is presented to the user (or administrator) must be used with the auxiliary method _(string, IEnvironment) in order to take advantage of the localization. (The environment data holds the language information that is used to determine the proper translation.) 

For special cases like the use of properties or fields with extension methods, such elements can be marked with the custom attribute [Translate]. See class Avatar in WebApp.cs for an example.

Every text string that is added to the source code requires a corresponding entry in the translation data files for all the supported languages. The easiest way to achieve this is by adding the new strings to file Diva.Wifi.pot. There are tools available that can scan the source code for calls to method _() and update the PO template file with the new strings (e.g. Poedit). Special cases like the use of custom attribute [Translate] require manual addition of the affected strings.

Translators are then able to use the PO template file to update the PO files for the various languages. Developers should also update at least Diva.Wifi.en.po and Diva.Wifi.en.resx with the default translations which Wifi uses as a fallback when localization is enabled. (Strictly speaking, this is not necessary because the hard-coded strings will be used as defaults when those two files are not updated. However, with localization enabled, there will be warning messages logged each time when the translations for a text is not found.)

The .resx files can be generated from PO files with the Mono Resource Generator, resgen. The script make_languages.sh does this automatically when invoked with parameter "-p". In addition, this tool deploys all available translation resource data to OpenSimulator's bin directory. Without parameter "-p", make_languages.sh expects .resx files, just like the Windows variant, make_languages.bat, which can not read PO files because of a limitation in resgen.exe of Microsoft's SDK. For more information about the usage of these scripts, please see the last paragraph in the section for administrators and grid owners above.
