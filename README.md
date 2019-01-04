﻿# D support for Visual Studio

A Visual Studio extension for [Dlang](https://dlang.org).
Provides syntax highlighting and editing features using the [Language Server protocol](https://microsoft.github.io/language-server-protocol).

## Features

[DLS](https://github.com/d-language-server/dls) is used as a Language Server, which in turn uses libraries such as [DCD](http://dcd.dub.pm), [DFMT](http://dfmt.dub.pm), [D-Scanner](http://dscanner.dub.pm) as well as [other libraries](https://github.com/d-language-server/dls/blob/master/README.md) to provide language editing features.

Look [here](https://github.com/d-language-server/dls) for an up-to-date list of features currently supported.
Far from every possible feature is implemented, but the server will update itself as new features come.

## Requirements

Dub and either DMD or LDC should be installed for the extension to work properly.
