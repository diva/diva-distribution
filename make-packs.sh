MAUTIL=../opensim-core/bin/mautil.exe
DIVADISTRO=../diva-distribution
MIREPO=../mono-addin-repos/metaverseink
wd=`pwd`

cd $DIVADISTRO
$MAUTIL pack bin/Diva.AddinExample.dll
$MAUTIL pack bin/Diva.Interfaces.dll
$MAUTIL pack bin/Diva.Wifi.dll

mv Diva.AddinExample_* $MIREPO
mv Diva.Interfaces_* $MIREPO
mv Diva.Wifi_* $MIREPO

$MAUTIL rep-build $MIREPO

cd $wd
