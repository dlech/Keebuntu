#!/usr/bin/make -f

# this prevents intermediate files from being removed
.SECONDARY:

all: libunity-sharp.dll

libunity-sharp.dll: libdbusmenu-glib-sharp.dll

CFLAGS-libunity = `pkg-config --cflags gio-sharp-2.0` -I:libdbusmenu-glib-api.xml
PKG-libunity = -pkg:gio-sharp-2.0 -r:./libdbusmenu-glib-sharp.dll

%-sharp.dll: %-api.xml
	gapi2-codegen --outdir=$*-generated $(CFLAGS-$*) --generate $<
	mcs -pkg:glib-sharp-2.0 $(PKG-$*) -target:library -unsafe -out:$@ $*-generated/*.cs

%-api.xml: %-api.raw %-symbols.xml %-metadata.xml
	cp $< $@
	gapi2-fixup --api=$@ --symbols=$*-symbols.xml --metadata=$*-metadata.xml

%-api.raw: %-source.xml
	gapi2-parser $<

clean: clean-libdbusmenu-glib clean-libunity

clean-%:
	rm -f $*-api.raw $*-api.xml $*-sharp.dll
	rm -rf $*-generated
