#! /bin/sh

if [ $# -eq 0 ]
  then
    echo "Please specify a directory to copy from"
    exit
fi

if [ ! -d "bin" ]; then
    mkdir bin
fi

cp $1/README.* .
cp $1/CONTRIBUTORS.txt .
cp $1/LICENSE.txt .
cp -r $1/ThirdPartyLicenses .
cp -r $1/bin/* bin

