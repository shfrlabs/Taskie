<p align=center><img src="header.png" alt="Taskie logo" width=300></p>
<p align=center><i>Get it done, keep it yours.</i></p>
<p align=center><a href="https://www.microsoft.com/store/productId/9N201WBCFJ91?mode=direct">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a></p>

<h2 align=center>A simple to-do app for Windows 10/11.</h2>

### ✨ Now with new features:
- Reminders - set a custom notification to appear at your chosen time.
- List customization - change a list's emoji, *wallpaper*, and *font* (*Pro only*)
- Attachments - add files and Fairmark notes to tasks to organize your life further. (*Pro only*)

## How does it work?

### File structure
Taskie stores your lists as JSON files in the app's local folder (`%localappdata%\Packages\BRStudios.Taskie_xxxxxxxxxxxx\LocalState`.
You can export them into a `.taskie` file (that's just a renamed ZIP containing the JSON files) and import them in Settings, by choosing either a loose JSON file or a full backup.

## How to translate?
- Download [this](https://github.com/shef3r/Taskie/blob/main/Taskie/Strings/en-US/Resources.resw) file. (or clone the repo if you know what you're doing)
- Open it in a text editor.
- After scrolling down, you should find data entries like this:
```xml
<data name="AboutCategory" xml:space="preserve">
    <value>About</value>
</data>
```
- Translate the text in the `<value>` tag.
- Find your locale online (ex. `en-US`)
- Create an issue with the file, adding "Translation: " and the locale in the title. (or a pull request if you know what you're doing)
