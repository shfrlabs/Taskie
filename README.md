<img src="https://i.imgur.com/FGfQV6A.png" align="left" width=210>

<div id="user-content-toc">
  <ul style="list-style: none;">
    <summary>
	<br><br>
      <h1>Taskie</h1>
	  <p>Get it done, keep it yours. A simple to-do app for Windows 10/11.</p>
    </summary>
  </ul>
</div>

[<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="220">](https://www.microsoft.com/store/productId/9N201WBCFJ91?mode=direct)

<br>

# 


### ✨ Now with new features:
- Reminders - set a custom notification to appear at your chosen time.
- List customization - change a list's emoji, wallpaper, and title font
- Attachments - add files and Fairmark notes to tasks to organize your life further.
- Interconnectivity - reference Taskie lists in Fairmark notes and attach notes to tasks.

## How does it work?

### File structure
Taskie stores your lists as JSON files in the app's local folder (`%localappdata%\Packages\BRStudios.Taskie_xxxxxxxxxxxx\LocalState`.
You can export them into a `.taskie` file (that's just a renamed ZIP containing the JSON files) and import them in Settings, by choosing either a loose JSON file or a full backup.

## Translations
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
