# Bulk Properties for Navisworks Manage

Bulk Properties (aka BulkProps) is a Navisworks Manage plugin that allows bulk creation and modification of user defined tabs and properties.  It works with Navisworks Manage 2022 only at this stage.

![Screenshot the Editor v2022.1.0.0](https://github.com/shaun-wilson/BulkProps-Navisworks-Plugin/blob/main/Screenshot-v2022.1.0.0.png)

# About / Contact

This plugin was created by me, Shaun Wilson, from Perth, Western Australia. I am available for consultation regarding your Digital Engineering requirements, and specialise in bespoke solutions. I am also proficient in technologies such as BIM, VR (UnrealEngine4), CAD (eg Tekla Structures), and data management (eg SQL databases). I can be contacted here or via email at [swilson.digitalengineering@gmail.com](mailto:swilson.digitalengineering@gmail.com)

# License

Refer to the [LICENSE.md](https://github.com/shaun-wilson/BulkProps-Navisworks-Plugin/blob/main/license.md) file that is included on this website, as part of the source-code files, and as part of the downloadable releases.

# Download

Download the compiled plugin from the [Releases](https://github.com/shaun-wilson/BulkProps-Navisworks-Plugin/releases/) page.

# Install

Copy the contents of the zip file to the plugin folder of your Navisworks Manage install, eg `C:\Program Files\Autodesk\Navisworks Manage 2022\Plugins\BulkProps\BulkProps.dll`

The plugin has not been tested with Navisworks Simulate.

Plugins do not work with Navisworks Freedom - this is a restriction of Navisworks Freedom, not this plugin.

# Instructions

In Navisworks Manage, there will be tab called "Tool add-ins", and it will have 2 buttons: [**Bulk Properties Editor**](#bulk-properties-editor) and [**Process Bulk Property Configs**](#process-bulk-property-configs).

## Bulk Properties Editor

This is the main interface for managing properties. Here you can configure a property tab, apply the changes, save the config to a file, load a config from file, or load and apply bulk configs.

The following screenshot shows a configured tab using many of the features, and the generated property tab can be seen at the bottom of the window.

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
  * The plugin will look for instructions in `{ }` brackets, of the fromat "Tab.Property".
  * To select from an ancestor, prefix with as many "..\" as required.
  * If you want to use curly-brackets in your value, escape them with another bracket, ie `{{Regular Text In Brackets}}`, as per C#.NET string.Format() rules.
  * An example value is: `This item is {Item.Layer}, and its parent is called {..\Item.Name}'`
  * When you run the plugin, if a tab, property, or ancestor cannot be found, this will cause the current process to raise an error, stop, and undo its changes.

### Buttons

When you have configured all the above, you can press the **Process Bulk Property Changes** to apply the modifications to the model.

You can **Save** the current config to file, so that you can re-apply it at a later time. The file is an XML format text file, and nominally has the BPC (BulkPropsConfig) file extension, but you can use TXT or XML file formats too. Refer to the XML File Format section below for a description of the file format. ***Note*** that the plugin does not save your "Current Selection" - you will need to save this yourself to a Selection Set.

You can also **Load** configs from file. This will configure the Editor so that you can manually **Process Bulk Property Changes**.

Alternatively, you can **Process Configs**, aka **Process Bulk Property Configs** - see below.

### Tab and Property Processes

There are currently 4 process available to both Tabs and Properties. They mimic SQL functions, as described below;
 * **Upsert** will **Insert** the tab or property if it does not exist, or **Update** ones that do. Thus, it will always occur.
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

Whilst using the plugin in accordance with the [License](#license), you are able to change the way the plugin works by replacing certain classes. You do this by instantiating the class `Processor` found within the `BulkProps.Process` namespace, and using the constructor `Processor(IGetSelectionSetForConfig, IProcessTabName, IProcessPropertyName, IProcessPropertyValue)` along with your own implementations of the necessary interface(s).

The functionality of the 4 interfaces are as follows;

### IGetModelItemsForConfig

This receives the `ItemsSource` value from the Editor / XML content (when it isn't null or empty), and uses the value to get a selection set. For example, this could be replaced with a class that treats the string as an index path as used by the method `Autodesk.Navisworks.Api.DocumentParts.DocumentSelectionSets.ResolveIndexPath`.

### IProcessTabName

This receives the `TabConfig.Name` value from the Editor / XML content (when it isn't null or empty), and uses the value to return formatted strings for the tabs display and internal names. For example, this could be replaced with a class that generates a name based upon another tab or property.

### IProcessPropertyName

This receives the `PropertyConfig.Name` value from the Editor / XML content (when it isn't null or empty), and uses the value to return formatted strings for the proprties display and internal names. For example, this could be replaced with a class that generates a name based upon another tab or property.

### IProcessPropertyValue

This receives the `PropertyConfig.Value` value from the Editor / XML content (including when it is null or empty), and uses the value to return a formatted value. For example, this could be replaced with a class that can use rules to count the amount of child items.
