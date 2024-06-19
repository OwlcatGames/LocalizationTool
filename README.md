# LocTool (or LocalizationTracker) User Guide

## Introduction
### Terms and Definitions
1. String - a unit of localization corresponding to an individual .json file in the repository, containing the source text, all translated texts, and metadata.
2. Locale - the language in which the string is written. One string can contain text in multiple locales.
3. Source (and language selection option) -  the language from which the translation is made.
4. Target (and language selection option) - the language to which the translation is made.
5. Trait - a custom property assigned to a string or locale, used for string filtering.
6. Tag - a special label that defines text formatting or allows displaying specific text instead.

### Getting Started

You can get latest LocTool build from [Github Releases page](https://github.com/OwlcatGames/LocalizationTool/releases). Download `LocTool_<buildNumber>.zip`  file and unpack it to any folder on you computer.

To start working with the LocTool, make sure that `config.json`is located in the folder where you unpacked the LocTool build. The defualt version of the file can be found near the LocalizationTracker.exe file.

The default config contains one important empty parameter:

`"StringsFolder": "",`

You should fill in this parameter according to the way you want to use the LocTool:

* To use the LocTool for your own project, create and empty folder named "Strings" in any folder on your computer and specify the path to this folder as the `StringsFolder` parameter value. The path should be specified relatively to the `LocalizationTracker.exe` file. 
* To use the LocTool as part of the modding toolset for Owlcat Games products (for example, for Warhammer 40,000: Rogue Trader), specify the path to the Strings folder inside `WhRtModificationTemplate` (which is shipped together with the game build).

## Functionality
### Main Window
When the application starts, it scans the Strings folder at the path specified in the config file. This process takes some time, typically about 5 to 10 seconds.

![Main window](/../readme/docs_images/1.jpg)
Main window includes a lot of different functionality, so let's consider each part separately:
1. Directory tree
2. Filters and search
3. Trait filters
4. Buttons with additional functionality
5. Strings table

#### Directory Tree
![Directory tree](/../readme/docs_images/2.png)

The Directory Tree has several features:

* The Show Count checkbox at the top is enabled by default, showing the number of strings and wordcount in each folder.
* Directories can be collapsed and expanded. Your personal state of these directories (expanded/collapsed) is saved when the LocTool is closed.
* To view the strings in the Strings table, select the corresponding folder in the Directory Tree.
#### Filters and Search
![Filters](/../readme/docs_images/3.jpg)
Filters and Search section is used to filter out strings according to the data entered in the fields.

In the Mode field, you can select the mode in which you want to view the strings. For more information, see the Modes section below.

The checkboxes next to the Mode field are as follows:
* Ignore Case: By default, the checkbox is always enabled to ignore the case of character you enter in the text fields. Disable the checkbox to make your search queries case-sensitive.
* Hide Tags: By default, the checkbox is disabled to display tags in the Strings table. Enable it if you want to hide tags.
##### Name 
This field is used for searching in the file and directory names (corresponding to the Path column in the Excel file, see the Export section below). The Name field allows you to search for one entry at a time. To search for several file or folder names, click a button with three dots next to the Name field to open the MultilineSearch window. After you add the search data in the opened window, you can close it. The Name field will now show "Multiple Lines", the values you added are saved, and the corresponding strings will be displayed in the Strings table. 
To clear this filter, delete all the data you typed or delete the "Multiple Lines" text.

![Name column](/../readme/docs_images/4.png)
![Name column](/../readme/docs_images/5.png)

_Designation of multiple searches in the Name column_

##### Text
This field allows you to search in the locales selected in the Source and Target language selector. All the strings containing the entered sequence of characters will be filtered. The exact matches to the entered sequence will be highlighted.
![Text column](/../readme/docs_images/6.png)

Next to the Text field, there is also a button with three dots. Right-click this button to open a context menu which allows you to customize the hightlighting feature:

![Context menu](/../readme/docs_images/7.png)

* Select Color allows you to set a custom text highlight color. Colors change in real-time, and the setting selected last is saved for future use when the application is closed.
 
![Context menu](/../readme/docs_images/8.png)

* Set Default Color allows you to reset to the default color, which is currently light blue as shown above.

##### Modified

![Modified](/../readme/docs_images/9.png)

In the Modified field, you can select a locale to filter all the strings, in which there were changes in the selected locale within a specified period. Alternatively, you can select a combination of a locale and a trait to filter strings that acquired that trait with the selected date range. You can also specify only the upper limit date, which filters the strings modified starting from the chosen date, or the lower limit date, which filters the strings modified up to a certain date.

The Speaker and Comment fields allow you to filter strings that have a matching character sequence in the corresponding part of a string.

When you apply any filter, the strings are filtered both in the String table and in the directory tree showing only the relevant folders.

![Filtered Strings](/../readme/docs_images/10.png)

_These are the same strings from Pathfinder, but filtered._

##### Trait Filters
Trait filters are configured using a set of buttons and checkboxes:

![Trait Filters](/../readme/docs_images/11.jpg)

_Filter control buttons_

* Add: Adds a new filter, where you can select a locale (or leave the first box empty) and a trait. To display all strings except those matching the filter, enable the Not checkbox. To remove the filter, click the Remove button.
* Clear: Allows you to remove all filters.
* Save/Load: Saves a set of filters to a .json file / loads a set of filters from a .json file.
* Checkbox OR: By default, the search is always conducted through logical "AND". If you eable the checkbox, the search will be performed through logical "OR".

##### String Table
![String Table](/../readme/docs_images/12.png)
The table has the following structure:
* Path: Path from the selected directory in the directory tree to the string file.
* Status: Shows the status of the text in the Target locale.
* Speaker: Speaker of the string.
* Source: Text in the locale selected as the source language of the translation.
* Target: Text in the locale selected as the target language of the translation.
* Comment (source)/Comment (target): Comments for the corresponding locales.
 
Words with incorrect spelling are displayed in red. Terms are underlined in blue, which means that the term entry can be viewed in the context menu for the string (see below).


### Modes
Strings can be viewed in several different modes:

1. Normal: Regular mode, in which the Strings table displays all the strings according to the selected text and trait filters.
2. Spelling_Errors: Shows only strings that contain spelling errors in either of the two selected locales in the Strings table. This feature is currently work in progress.
3. Key_Duplicates: Shows only strings with non-unique keys. Duplicate keys is a bug caused by internal processes, it shouldn't be applicable to external work with localization.
4. Updated source - show only strings where the text has changed since translation
   ![Updated source](/../readme/docs_images/13.png)
   To view strings as a diff (with all the changes highlighted) in this mode:

   a. Select the target locale in the right column.

   b. Select Translation Source locale as the source locale in the left column

5. Updated_Trait: Shows only strings where the text has changed in the selected locale since a certain trait was assigned to it.
   In this mode, only one column with text is displayed in the string list. To view strings as a diff (with all the changes highlighted) in this mode, select the desired locale and trait.
   ![Updated trait](/../readme/docs_images/14.png)

6. Tags_Mismatch: Shows strings with mismatching tags in the source and target locales. The matching tags are shown in green. The tags that are present in one locale but not the other (mismatching tags) are shown in red. Yellow indicates incomplete paired tags.
   ![Tags mismatch](/../readme/docs_images/15.png)

7. Unreal_Unused: Used only internally for projects based on Unreal Engine.
8. Glossary_Mismatch: Shows only strings where the terms in the source locale and target locale do not match.

### Context Menu
![Updated trait](/../readme/docs_images/16.png)![Updated trait](/../readme/docs_images/17.png)
_Context menu for the directory tree_

The context menu slightly differs for the directory tree and the Strings table. You can access the context menu by right-clicking a single folder/string or multiple selected strings/folders (using either Ctrl or Shift keys).

1. Rescan Strings: Rescans all  .json files in the local repository copy to update the text in strings shown in the LocTool. This command should be used after you update the repository or import new translated .xlsx files.
2. Export: Exports seletect strings to an external file, typically .xlsx. Export can be performed in several modes.
3. Import: Import an external file with strings. The LocTool automatically updates the text in the corresponding .json files in the repository copy, The LocTool won't recognize .xlsm files (Excel files with macros), so you need to change the format of such files to .xlsx.
4. Export wordcount: Export the wordcounts for the selected folder and subfolders to the structure.csv file and saves it in the Localization folder of the repository copy. This command is only available for the directory tree.
5. String Details: Opens a window with detailed information about the entire string, including the trait change history. This command can can be executed only on one selected string.
![String Details](/../readme/docs_images/19.png)
6. Force Translation Source: Sets the locale selected in the Source locale selector as the source translation language for the text selected in the target locale.
7. Change Traits: Opens a window that allows you to add traits to a string or a set of selected strings or remove the added traits.
8. Delete: Deletes the string.

### Export
![Export](/../readme/docs_images/20.png)
1. The Export command from the context menu opens a special window that allows you to configure string export parameters:
2. Format: Allows you to select the file format and export mode, for details see below.
3. Source locale: Allows you to select the source language for the translation.
4. Target locale: Allows you to select one or several target languages for the translation.
5. Add traits: Allows you to select traits that will be added to the target locale when the translation is imported.
6. Remove tags: Allows you to choose which tags to keep or remove from the file, for details see below.
7. Context: Adds context by exportint a preeceding string for each string, if available. This is only used for creating voice-over scripts.
8. Hierarchy: Exports strings in several files and folders, according to the directory tree (hierarchy of files in the repository).
9. Comments: Exports comments to strings. This checkbox is enabled by default.
 Export to separate files: If several target locales were selected, this option allows you to export each source-target pair into a separate file. This checkbox is disabled by default.

#### Format
1. LocalizationToExcel - This is a regular mode of exporting the selected strings into a .xlsx file, which contains the following columns:

   * Key: A unique string identifier.
   * Name: Path to the string in a local repository copy.
   * Source [locale code]: Text in the source locale.
   * Current [locale code]: Existing translation in the target locale, if any.
   * Result [locale code]: Column dedicated for target translations.
   * Comment: If the Comments checkbox was enabled (which is the default option), this column shows comments to the string, if available.
2. LocalizationToOpenOffice - The same mode of exporting strings with the same table structure in the .odt (Open Office) format.
3. StringDiffToExcel - This mode allows you to export strings as diffs with changes highlighted.
   ![StringDiffToExcel](/../readme/docs_images/21.png)
4. SpeakersStrings - This mode exports the wordcounts and the number of strings in dialogues broken down by cues and answers and by speakers.
   ![SpeakersStrings](/../readme/docs_images/22.png)

### Remove Tags
The Remove Tags list is used in all formats except SpeakersStrings. It allows you to choose among the following options:

![RemoveTags](/../readme/docs_images/23.png)
1. RetainAll: Keep all tags in the strings in the exported file.
2. RetainMfN: Keep only {mf} (gender) tags in the exported file.
3. RetainNone: Excludes all tags from the exported file.
4. DeleteUpdatedTag: Removes only the tags that were added after the string was translated.

### Import

After the translation in the exported file is finished, you can import the strings back. The Localization Tracker will automatically distributes the text from the strings to the corresponding .json files and update them. During the import, the LocTool will notify you of any unexpected changes or errors encountered. All changes made in the translation as compared to the previous target text and all changes in the source text will be shown in the Import Results window as a diff (with all changes highlighted).

![Import Results](/../readme/docs_images/24.png)

