# Changelog

## 0.1.0

Released on May 16, 2020

- Initial release

## 0.2.0

Released on March 23, 2021

- Added some more notes to the document and fixed some mistakes and typos in it
- Fixed the name of the author in the NuGet package
- Added all new languages that are now supported by DeepL:
  - British English (EN-GB) and American English (EN-US) were added (previously only invariant English was supported, the invariant version is still available for backwards compatibility)
  - Portuguese is no longer invariant (was changed from PT to PT-PT)
  - Chinese
  - Bulgarian
  - Czech
  - Danish
  - Greek
  - Estonian
  - Finnish
  - Hungarian
  - Lithuanian
  - Latvian
  - Romanian
  - Slovak
  - Slovenian
  - Swedish

This release was made possible by the following contributors:

- [Markus Mattes](https://github.com/mmattes)
- [Patrick Beer](https://github.com/vandebeer)

## 0.2.1

Released on April 13, 2021

- Fixed the version number that is used in the `User-Agent` string
  - Previously, the version number of the application using the DeepL.NET library was used (`Assembly.GetEntryAssembly()`)
  - Now, the version number of the DeepL.NET library itself is used (`Assembly.GetExecutingAssembly()`)
  - In certain circumstances, using `Assembly.GetEntryAssembly()` returns `null`, which resulted in a `NullReferenceException` in the constructor of `DeepLClient`

This release was made possible by the following contributors:

- [jcmag](https://github.com/jcmag)

## 0.3.0

Released on May 11, 2021

- It is now possible to choose between the free and the pro version of the DeepL API
- Cleaned up the documentation and integrated the new constructor argument of the `DeepLClient`
- Renamed the sample project to CLI, because it is more than just a sample
- Integrated the option for the free API into the CLI as well
- Integrated the new languages that were introduced in 0.2.0 into the CLI

This release was made possible by the following contributors:

- [Michal Cadecky](https://github.com/MichalCadecky)
- [Raoul Jacobs](https://github.com/RaoulJacobs)

## 0.4.0

Released on January 28, 2022

- Added the formality parameter, which determines how formal the translation should be
- Upgraded Newtonsoft.Json to its latest version (12.0.3 -> 13.0.1)
- Updated the DeepL.NET documentation

This release was made possible by the following contributors:

- [Paul-SB](https://github.com/Paul-SB)

## 0.4.1

Released on August 18, 2022

- Added all new languages that are now supported by DeepL:
  - Indonesian
  - Turkish
- Fixed some spelling mistakes in the XML documentation of the source code
- Updated the DeepL.NET documentation


## 0.4.2

Released on October 6, 2022

- Added Ukrainian as a language which is now supported by DeepL

## 0.4.3

Unreleased

- Added a deprecation notice to the read me, as the project will soon be discontinued
- Added a migration guide, to help existing users to easily migrate to the official DeepL .NET binding
- Made the `DeepLClient` class obsolete, so that users will receive a compiler warning, concerning the deprecation of DeepL.NET
