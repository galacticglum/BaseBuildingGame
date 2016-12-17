[Unity Version](#unity-version)    
[Style Guidelines](#style-guidelines)  
[Best Practices for Contributing](#best-practices-for-contributing)
[File Formats](#file-formats)  

These document outlines the different guidelines for contributing to this repository.

## Unity version
Aquatic Warfare is using Unity version 5.4.2f2. All pull requests must build in Unity version 5.4.2f2 to be considered valid.

## Style Guidelines

TODO

## Best Practices for Contributing
[Best Practices for Contributing]: #best-practices-for-contributing
* Pull Requests represent final code. Please ensure they are:

    * Well tested by the author. It is the author's job to ensure their code works as expected.
    * Be free of unnecessary log calls. Logging is great for debugging, but when a PR is made, log calls should only be present when there is an actual error or to warn of an unimplemented feature.


* If your code is untested, log heavy, or incomplete, prefix your pull request with "[WIP]", so others know it is still being tested and shouldn't be considered for merging yet.

* Small changes are preferable over large ones. The larger a change is the more likely it is to conflict with the project and thus be denied. If your addition is large, be sure to extensively discuss it in an "issue" before you submit a pull request, or even start coding.

* Document your changes in your pull request. If you add a feature that you expect others to use, explain exactly how future code should interact with your additions.

* Avoid making changes to more files than necessary for your feature (i.e. refrain from combining your "real" pull request with incidental bug fixes). This will simplify the merging process and make your changes clearer.

* Avoid making changes to the Unity-specific files, like the scene and the project settings unless absolutely necessary. Changes here are very likely to cause difficult merge conflicts. Work in code as much as possible. (We will be trying to change the UI to be more code-driven in the future.) Making changes to prefabs should generally be safe -- but create a copy of the main scene and work there instead (then delete your copy of the scene before committing).

* Include screenshots demonstrating your change if applicable. All UI changes should include screenshots.

That's it! Following these guidelines will ensure that your additions are approved quickly and integrated into the project.

## File Formats

Aquatic Warfare **only accepts** files in the following formats:

### Images

Image files should be in a compressed, openly accessible file format such as: PNG or JPEG. The original source (Photoshop/GIMP/etc...) files are not to be commited to the repository but instead hosted on the Aquatic Warfare Google Drive asset directory.

### Sound Files and Music

Audio files should be in the OGG audio format, as opposed to MP3 or uncompressed WAV files. Small sound effects that are played frequently should be set to: "Decompress on Load" in Unity's inspector, whereas longer sounds (such as songs) should not be. Uncompressed (such as WAV) files should be hosted on the Aquatic Warfare Google Drive asset directory.
