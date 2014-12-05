#!/bin/bash
cat $@ | grep '<ReleaseVersion>' | sed 's/[ \t]*<\/\?ReleaseVersion>//g'
