@rem This requires resgen.exe and al.exe to be available via the system's %PATH%.
@rem Starting an Microsoft SDK Command Prompt will take care of this.
@echo off
if "%1" == "/?" goto usage
if "%1" == "-?" goto usage
if "%1" == "/h" goto usage
if "%1" == "-h" goto usage
goto nohelp
:usage
echo.
echo Syntax: make_languages [-o ^<pathToOpenSimDir^>] [[^<langCode^>] ...]
echo.
echo Generate satellite assemblies for the specified languages from resource files named Diva.Wifi.^<langCode^>.resx.
exit /b
:nohelp
rem Enable delayed variable expansion
if not "%1" == "DelayedExpansion" (
    %comspec% /v:on /c %0 DelayedExpansion %*
    exit /b
)
shift
rem Read command arguments
set osbin=%~p0\..\..\..\bin
if "%1" == "-o" (
    set osbin=%2
    shift
    shift
)
if not exist %osbin%\Diva.Wifi.dll (
    echo Please specify the path to OpenSimulator's bin\ directory with parameter -o
    exit /b
)
shift
set languages=%0 %1 %2 %3 %4 %5 %6 %7 %8 %9
rem Find language resources
if "%0" == "" (
    for %%f in (Diva.Wifi.*.resx) do (
        for /f "usebackq delims=. tokens=3" %%l in ('%%f') do (
            set languages=!languages! %%l
        )
    )
)
rem Create satellite assemblies
for %%l in (%languages%) do (
    echo Creating satellite assembly for language: %%l
    resgen Diva.Wifi.%%l.resx Diva.Wifi.%%l.resources
    if exist Diva.Wifi.%%l.resources (
        mkdir %osbin%\%%l 2> NUL
        al /target:library /culture:%%l /embed:Diva.Wifi.%%l.resources /out:%osbin%\%%l\Diva.Wifi.resources.dll
        del Diva.Wifi.%%l.resources
    )
)
