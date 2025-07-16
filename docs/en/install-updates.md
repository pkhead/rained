# Installation and Updates
There are two methods of installing and updating Rained: from the GitHub releases page, or using rainedvm. You can use both methods interchangeably.

## From GitHub
You can install Rained from the [GitHub releases page](https://github.com/pkhead/rained/releases). Download the archive (.zip or .tar.gz) for your platform and extract it to disk. Execute the file named "Rained.exe" or "Rained" to launch Rained. If you are on Windows, there will also be an executable named "Rained.Console.exe", which exists to be ran from the terminal or from a command-line tool. On Linux, there is no need for such a separation, so the file is not included.

Rained should notify you of any new updates upon startup or in the About window. You may disable the update checker in the preferences window.

If you want to update Rained, you should remove and replace all the files and folders from the installation folder **except**:

- config/
- Your Data folder, if present.

Then, download and extract the new version of Rained from the releases page.

You should ensure that any potential changes you made in any files aren't accidentally overwritten. Additionally, if you open Rained after updating and the windows are messed up, deleting the config/imgui.ini file should reset the UI layout.

## rainedvm
rainedvm is a program that eases the process of installing Rained versions. Downloads are [here](https://github.com/pkhead/rainedvm/releases).

Inside the .zip or .tar.gz download is the executable **rainedvm**. Simply extract it and drop it in the folder where you want to install or update Rained. Then, launch the executable, which should open a window that looks like this:

<figure markdown="span">
    ![rainedvm](img//rainedvm.png)
</figure>

Select the version you want to install and then press the "Install" button. Once, done you can run Rained from your file manager.

Rained should notify you of any new updates upon startup or in the About window. You may disable the update checker in the preferences window.

If you want to update Rained, run rainedvm again, select the version you want to upgrade to, and press "Install". If you launch Rained after updating and the windows are messed up, delete the config/imgui.ini file, select the version you are on in the version manager, and press "Sync", which replaces the "Install" button. This will reset the window configuration to the default for that version.

### File conflicts
rainedvm will detect if you have modified any files (other than config/preferences and config/imgui.ini) and if that file had been changed in the new version, will ask you if you want to either overwrite the changes with the new version, or keep your file changes. On each prompt, if you want the file to be updated, select "Overwrite Changes". Otherwise, select "Keep Changes". You may also cancel the entire operation at that point by pressing the "Cancel" button.

## Asset installation
Your first launch of Rained should look like this:

<figure markdown="span">
    ![The asset installation screen.](img/configure-data.png)
</figure>

!!! important

    If you do not already have an installation of a different Rain World level editor on your computer, select "Download Data". Otherwise, continue reading.

Each Rain World level editor should come with a set of tiles, props, and vanilla and DLC levels. However, trying using the same set of tiles, props, and created levels across different level editors can be cumbersome, as they have their own folder/directory to store this data. Additionally, the set of bundled level files takes around 4 GB of storage.

To accomodate for this, Rained allows you to choose whether or not you want it to use a pre-existing directory on your computer for tiles, props, and levels. If you do not already have these installed on your computer (or if you just want to), you need to select "Download Data". It will download the required files from the Internet. Otherwise, you may select "Choose Data Folder". This will open up a file browser where you will choose the correct data directory. The directory you choose should contain the following subdirectories:

- Graphics
- Props
- Levels
- LevelEditorProjects *(optional)*
- Materials *(optional)*

You can also change the data directory after this initial set-up. This is done by modifying the Data Path option, located in the preferences window (**File > Preferences**) and the asset manager (**File > Asset Manager**).

!!! note

    When you choose "Download Data", the progress bar will stay at 0% for most of the duration. This is a fault of the download provider that is being used.

## Dependencies
!!! important

    This section is only relevant if you are running Rained on Windows.

The only dependency Rained has is the Microsoft Visual Studio C++ runtime. Rained *is* programmed in C#, but some of the libraries it uses for windowing and graphics were programmed in C++, which is why it is required.

To check if you have the required dependency installed or not, simply try running Rained. If it can't open a window and fails to launch, it's likely that you will need to install it. Fortunately, it is very simple. The installer for the C++ runtime can be downloaded here: 

<figure markdown="span">
[https://aka.ms/vs/17/release/vc_redist.x64.exe](https://aka.ms/vs/17/release/vc_redist.x64.exe)
</figure>

Run the executable and once the installation process is finished, you now can run Rained (hopefully).