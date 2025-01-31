# Bulk Properties for Navisworks Manage

Bulk Properties (aka BulkProps) is a Navisworks Manage plugin that allows bulk creation and modification of user defined tabs and properties.

![Screenshot the Editor v2022.1.0.0](https://github.com/shaun-wilson/BulkProps-Navisworks-Plugin/blob/main/Screenshot-v2022.1.0.0.png)

# About / Contact

This plugin was created by me - Shaun Wilson - of Perth, Western Australia. <br>
You can contact me via [email](mailto:47620271+shaun-wilson@users.noreply.github.com), or by opening a Github issue.

# License

Refer to the [LICENSE.md file](https://github.com/shaun-wilson/BulkProps-Navisworks-Plugin/blob/main/LICENSE.md) that is included on this website, as part of the source-code files, and as part of the downloadable releases.

# Download

Download compiled plugins from the [Releases](https://github.com/shaun-wilson/BulkProps-Navisworks-Plugin/releases/) page.

## Versions

This plugin was created alongside **Navisworks Manage** 2022. It has not been tested with ***Simulate***. Updates to suit newer versions of Navisworks may be released from time to time, or you can attempt to compile the plugin yourself. The code on the master Git branch is the latest version. Older versions are "archived" in separate branches when they are superseded.

<sup><sub>Note - Plugins do not work with Navisworks **Freedom**. This is a restriction of Navisworks Freedom, not this plugin.</sub></sup>

# Install

Copy the contents of the zip file to the plugin folder of your Navisworks Manage install, ie `C:\Program Files\Autodesk\Navisworks Manage {Version}\Plugins\BulkProps\BulkProps.dll`

Alternatively, you can locate the plugin anywhere and then start Navisworks with the 'AddPluginAssembly' [command line switch](https://help.autodesk.com/view/NAV/2025/ENU/?guid=GUID-62870620-2B92-4FD8-A788-E0687F7030AD), ie `"C:\Program Files\Autodesk\Navisworks Manage {Version}\roamer.exe" -AddPluginAssembly "D:\BulkProps\BulkProps.dll"`

# Instructions

In Navisworks Manage, there will be tab called "Tool add-ins", and it will have 2 buttons: [**Bulk Properties Editor**](#bulk-properties-editor) and [**Process Bulk Property Configs**](#process-bulk-property-configs).

## Bulk Properties Editor

This is the main interface for managing properties. Here you can configure a property tab, apply the changes, save the config to a file, load a config from file, or load and apply bulk configs.

The screenshot at the top shows a configured tab using many of the features, and the generated property tab can be seen at the bottom of the window.

### Item Source

This selects the source of model items that the plugin will update the properties for. You can select **Current Selection** to modify the parts currently selected in the model, or choose from a pre-defined Selection Set (which can be Search Sets or Saved Selections). Currently, folders of Selection Sets cannot be used.

Upon selecting a Selection Set, it is translated to a UNC style path. This path can also be entered manually, and is saved/loaded from config files.

### Apply to Items of Type

This can be used to filter out specific items from the selected Item Source. Bulk Properties will only be applied to items that are enabled here.

### Alter Property Tab

Here you can configure the **Tab Name** of the user-defined tab you wish to alter. This accepts a simple string. This plugin cannot alter tabs that were created when importing files.

You must then choose a **Process** to apply to the tabs. See the [Processes](#tab-and-property-processes) section for explanations of the various options.

### Property Configs

The table area is where you setup all the property changes that will be applied by the plugin. You can **Add** a new property config by pressing the button at the bottom of the table. You can **Delete** a config by pressing the red X at the end of each row.

Each Property Config has a field for;
* A **Name**, which is a simple text string.
* A **Process** to apply to the property - see the [Processes](#tab-and-property-processes) section for explanations of the various options.
* A **Value**, an advanced text string that is able to copy property values from other tabs, which may also be on ancestor items. The following rules apply;
  * The plugin will look for instructions in `{ }` brackets, of the fromat "TabName.PropertyName".
  * To select from an ancestor, prefix with as many "..\\" as required.
  * If you want to use curly-brackets in your value, escape them with another bracket, ie `{{Regular Text In Brackets}}`, as per C#.NET string.Format() rules.
  * An example value is: `This item is {Item.Layer}, and its parent is called {..\Item.Name}`
  * When you run the plugin, if a tab, property, or ancestor cannot be found, this will cause the current process to raise an error, stop, and undo its changes.

### Buttons

When you have configured all the above, you can press the **Process Bulk Property Changes** to apply the modifications to the model.

You can **Save** the current config to file, so that you can re-apply it at a later time. The file is an XML format text file, and nominally has the BPC (BulkPropsConfig) file extension, but you can use TXT or XML file formats too. Refer to the XML File Format section below for a description of the file format. ***Note*** that the plugin does not save your "Current Selection" - you will need to save this yourself to a Selection Set.

You can also **Load** configs from file. This will configure the Editor so that you can manually **Process Bulk Property Changes**.

Alternatively, you can **Process Configs**, aka **Process Bulk Property Configs** - see below.

### Tab and Property Processes

There are currently 4 process available to both Tabs and Properties. They mimic SQL functions, as described below;
 * **Upsert** will **Insert** the tab or property if it does not exist, or **Update** an existing one. Thus, it will always occur.
 * **Insert** will only occur if a tab or property with the same name does not already exist.
 * **Update** will only occur if a tab or property with the same name already exists.
 * **Delete** will remove the tab or property, but can only affect user-defined tabs and their properties (not imported ones).

If a tab or property process cannot be applied because the rules are not met, the plugin will continue without error or warning.

## Process Bulk Property Configs

This button allows you to select multiple **Bulk Property Config** files, and it will then apply them all to the model.

You should save your model before running this command, in case something goes wrong. If the plugin encounters a problem whilst processing one config, it will raise an error telling you which config file failed, then undo all the changes made so far, and stop.  If this happens, it is recommended to close the model without saving, and open it again.

### Bulk Properties Config (BPC) XML File Format

Below is the BPC file content that matches the above screenshot.

Note that the XML format requires the characters `& < >` to be escaped and replaced with `&amp;` `&lt;` and `&gt;`. This is automatically done when saving a config with the Editor, but will need to be manually done if you are generating config files with other methods (ie, automatically from Excel or similar).

    <BulkPropertiesConfig>
        <ItemsSource>\\Concrete</ItemsSource>
        <ItemFilters>
            <Files>False</Files>
            <Layers>False</Layers>
            <Groups>True</Groups>
            <Inserts>True</Inserts>
            <Geometry>True</Geometry>
        </ItemFilters>
        <TabConfig>
            <Name>My Bulk Data</Name>
            <Process>Upsert</Process>
        </TabConfig>
        <PropertyConfigs>
            <PropertyConfig>
                <Name>A simple property</Name>
                <Process>Upsert</Process>
                <Value>Demo Data</Value>
            </PropertyConfig>
            <PropertyConfig>
                <Name>A copied property</Name>
                <Process>Upsert</Process>
                <Value>The parent is {..\Item.Name}</Value>
            </PropertyConfig>
        </PropertyConfigs>
    </BulkPropertiesConfig>
    
## Plugin .NET Interface (Advanced)

Whilst using the plugin in accordance with the [License](#license), you are able to change the way the plugin works by replacing certain classes. You do this by instantiating the class `Processor` found within the `BulkProps.Process` namespace, and using the constructor
`Processor(IGetModelItemsForConfig, IProcessTabName, IProcessPropertyName, IProcessPropertyValue)`
along with your own implementations of the necessary interface(s).

The functionality of the 4 interfaces are as follows;

### IGetModelItemsForConfig

This receives the `ItemsSource` value from the Editor / XML content (when it isn't null or empty), and uses the value to get a selection set. For example, this could be replaced with a class that treats the string as an index path as used by the method `Autodesk.Navisworks.Api.DocumentParts.DocumentSelectionSets.ResolveIndexPath`.

### IProcessTabName

This receives the `TabConfig.Name` value from the Editor / XML content (when it isn't null or empty), and uses the value to return formatted strings for the tabs display and internal names. For example, this could be replaced with a class that generates a name based upon another tab or property.

### IProcessPropertyName

This receives the `PropertyConfig.Name` value from the Editor / XML content (when it isn't null or empty), and uses the value to return formatted strings for the proprties display and internal names. For example, this could be replaced with a class that generates a name based upon another tab or property.

### IProcessPropertyValue

This receives the `PropertyConfig.Value` value from the Editor / XML content (including when it is null or empty), and uses the value to return a formatted value. For example, this could be replaced with a class that can use rules to count the amount of child items.

## Build Guide (Advanced)

The plugin has been updated to use the latest .NET Core SDK. This means the "Solution" can still be edited with software such as Visual Studio (paid software), and alternatively it can now be edited using simpler editors like VSCode, and then compiled using the dotnet CLI tool (both free). These alternatives can also be run as "portable" applications, which helps if you don't have admin rights on your PC.

#### If you have admin rights;
* You can follow the instructions described here - https://learn.microsoft.com/en-us/dotnet/core/install/windows#install-the-sdk
  * The instructions are basically to open powershell and run `winget install Microsoft.DotNet.SDK.8`
  * If you get an error about winget not being installed, then follow the troubleshooting here - https://learn.microsoft.com/en-us/windows/package-manager/winget/#install-winget
#### If you don't have admin rights;
* Get 'dotnet.exe' via the script here - https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script
#### Make sure everything is properly configured;
* Make sure nuget has a source configured. If it doesn't, add one with `dotnet nuget add source "https://api.nuget.org/v3/index.json" --name "nuget.org"`
* You may need to install a "Developer Pack" to compile the '.NET Framework 4.8' code, which can be done via `winget install -e --id Microsoft.DotNet.Framework.DeveloperPack_4`

*Note*: To enable development of the plugin on machines that do not have Navisworks Manage installed, the Project references a NuGet package that has a copy of Navisworks Manage API. The actual package used is not a fixed requirement - it just needs to provide the necessary `Autodesk.*.dll` files.
