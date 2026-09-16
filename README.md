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

**KJV is bundled** — `Community.PowerToys.Run.Plugin.VerseLink\Bibles\KJV.xml` ships with
the plugin and works out of the box. (The KJV is public domain in the United States. In
the UK it is under perpetual Crown copyright administered by Cambridge University Press.)

**ESV and NASB are not bundled.** Both are copyrighted and cannot be redistributed here.
To use them, add `ESV.xml` / `NASB.xml` to that same `Bibles\` folder under whatever
licence you hold. Anything in the folder is copied to the build output and packaged by
`Build.ps1`. Selecting a translation whose file is absent leaves the plugin unable to
resolve any reference.

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
