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
