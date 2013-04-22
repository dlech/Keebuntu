#!/bin/bash

# first, we need to pause the KeePass process otherwise it will start compiling before this script finishes
# hopefully it has not read the .csroj file yet
KEEPASS_PID=$(ps ax | grep [K]eePass.exe | grep -o '^\s*[0-9]*')
# there is probably only one process, but we use a for loop just in case
for pid in $KEEPASS_PID
do
    kill -s STOP $p    
done
notify-send "start"
# argument should be "{PLGX_TEMP_DIR}"
cd $1

# take care of pkg-config package references manually because KeePass does not know how to handle them
HINT_PATH=$(pkg-config --libs gtk-sharp-2.0 | egrep -o '/[^[:space:]]*/gtk-sharp.dll')
sed -i '0,/<Package>gtk-sharp-2.0<\/Package>/{s|<Package>gtk-sharp-2.0</Package>|<HintPath>'"$HINT_PATH"'</HintPath>|}' Keebuntu.csproj
HINT_PATH=$(pkg-config --libs appindicator-sharp-0.1 | egrep -o '/[^[:space:]]*/appindicator-sharp.dll')
sed -i '0,/<Package>appindicator-sharp-0.1<\/Package>/{s|<Package>appindicator-sharp-0.1</Package>|<HintPath>'"$HINT_PATH"'</HintPath>|}' Keebuntu.csproj
HINT_PATH=$(pkg-config --libs dbus-sharp-1.0 | egrep -o '/[^[:space:]]*/dbus-sharp.dll')
sed -i '0,/<Package>dbus-sharp-1.0<\/Package>/{s|<Package>dbus-sharp-1.0</Package>|<HintPath>'"$HINT_PATH"'</HintPath>|}' Keebuntu.csproj
HINT_PATH=$(pkg-config --libs dbus-sharp-glib-1.0 | egrep -o '/[^[:space:]]*/dbus-sharp-glib.dll')
sed -i '0,/<Package>dbus-sharp-glib-1.0<\/Package>/{s|<Package>dbus-sharp-glib-1.0</Package>|<HintPath>'"$HINT_PATH"'</HintPath>|}' Keebuntu.csproj
HINT_PATH=$(pkg-config --libs gtk-sharp-2.0 | egrep -o '/[^[:space:]]*/gtk-sharp.dll')
sed -i '0,/<Package>gtk-sharp-2.0<\/Package>/{s|<Package>gtk-sharp-2.0</Package>|<HintPath>'"$HINT_PATH"'</HintPath>|}' Keebuntu.csproj

# restart the KeePass process
for pid in $KEEPASS_PID
do
    kill -s CONT $p    
done
