#!/bin/sh

Usage()
{
    cat <<EOT

Syntax: `basename $0` [-o <pathToOpenSimDir>] [-p] [[<langCode>] ...]

Generate satellite assemblies for the specified languages from resource files
named Diva.Wifi.<langCode>.resx.
If option -p is present, then generate from PO files Diva.Wifi.<langCode>.po instead.

EOT
}

ConvertPO()
{
    lang=$1
    resgen Diva.Wifi.$lang.po Diva.Wifi.$lang.resx
}

CreateSatelliteAssembly()
{
    lang=$1
    dest=$2
    if resgen Diva.Wifi.$lang.resx Diva.Wifi.$lang.resources; then
        mkdir $dest/$lang 2> /dev/null
        al /target:library /culture:$lang \
            /embed:Diva.Wifi.$lang.resources \
            /out:$dest/$lang/Diva.Wifi.resources.dll
        rm -f Diva.Wifi.$lang.resources
    fi
}

# Default parameters
convertOnly=false
convertFrom=resx
osbin=`dirname $0`/../../../bin

# Read command arguments
while getopts :co:p arg; do
    case $arg in
    c) convertOnly=true;;
    p) convertFrom=po;;
    o) osbin=$OPTARG;;
    \?) Usage; exit 1;;
    esac
done
shift `expr $OPTIND - 1`
languages=$*

# Check path to bin directory
if [ ! -f $osbin/Diva.Wifi.dll ]; then
    echo "Please specify the path to OpenSimulator's bin/ directory with parameter -o"
    exit 2
fi

# Find language resources
if [ -z "$languages" ]; then
    for resource in Diva.Wifi.*.$convertFrom; do
        l=${resource#Diva.Wifi.}
        l=${l%.$convertFrom}
        languages="$languages $l"
    done
fi

# Create satellite assemblies
for language in $languages; do
    $convertOnly || echo Creating satellite assembly for language: $language
    test "$convertFrom" = "po" && ConvertPO $language
    $convertOnly || CreateSatelliteAssembly $language $osbin
done

