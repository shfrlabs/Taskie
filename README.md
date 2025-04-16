<p align=center><img src="header.png" alt="Taskie logo" width=300></p>
<p align=center><i>Get it done, keep it yours.</i></p>
<p align=center><a href="https://www.microsoft.com/store/productId/9N201WBCFJ91?mode=direct">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a></p>

<h2 align=center>The simple, yet powerful to-do companion for Windows 10/11.</h2>
<h4 align=center>Made with <b>UWP</b>.</h4>

### ✨ Now with new features:
- Reminders - set a custom notification to appear at your chosen time.
- Subtasks - split any task into managable chunks.
- List customization - change a list's emoji, *wallpaper*, and *font* (*Pro only*)


## 🦾 Why should I get Pro?
*(I won't nag you in the app, don't worry)*

You can get Pro for its current and future features:
- [x] priority support
- [x] advanced list customization
- [ ] task attachments
- [ ] kanban/Trello-like view for specific list groups

..all for a small price of $1.99 (lifetime) or 0.99 zł in Poland.

## 🛠️ How does it work?

### 📂 File structure
Taskie stores your lists as JSON files in the app's local folder (`%localappdata%\Packages\BRStudios.Taskie_xxxxxxxxxxxx\LocalState`.
You can export them into a `.taskie` file (that's just a renamed ZIP containing the JSON files) and import them in Settings, by choosing either a loose JSON file or a full backup.

### 🧠 How was it made?
Taskie uses the **Universal Windows Platform** for its sandboxing and clean installs. *(yes, I really do like UWP)*

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
