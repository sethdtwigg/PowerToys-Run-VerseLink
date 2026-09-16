# PowerToys Run: Verse Link plugin

Simple [PowerToys Run](https://learn.microsoft.com/windows/powertoys/run) plugin to insert the text of a Bible verse(s) when given a valid Bible Reference.

## Requirements

- PowerToys minimum version 0.76.0

## Installation

- Download the latest release by selecting the architecture that matches your machine.
- Close PowerToys
- Extract the archive to `%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins`
- Open PowerToys

## Bible texts

The plugin reads its verse text from `Bibles\<VERSION>.xml`, next to the plugin DLL.
Those files are **not** included in this repository, because most modern translations
are copyrighted and cannot be redistributed.

To build a working plugin, create a `Community.PowerToys.Run.Plugin.VerseLink\Bibles\`
folder and add one XML file per translation you are licensed to use, named to match the
Bible Version setting (`KJV.xml`, `ESV.xml`, `NASB.xml`). Anything in that folder is
copied to the build output and packaged by `Build.ps1`.

The KJV is in the public domain. ESV and NASB are not — obtain them under their
respective publishers' licensing terms.

Expected shape:

```xml
<bible>
  <b n="John">
    <c n="3">
      <v n="16">For God so loved the world...</v>
    </c>
  </b>
</bible>
```

## Usage
- Select/Place cursor where text should be placed 
- Open PowerToys Run
- Input: "^^ \<reference\>" or "^^" for clipboard
- Select the result (ENTER)
- \<verse\> is placed into the selected location

## Credit
- This is a fork of Corey Hayward's InsertText Project
- https://github.com/CoreyHayward/PowerToys-Run-InputTyper
