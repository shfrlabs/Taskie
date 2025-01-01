<p align=center><img src="header.png" alt="Taskie logo" width=300></p>
<p align=center>Simple to-do app for Windows 10/11. Made with <b>UWP</b>.</p>

---

# HOW DO I INSTALL THIS????
soon, you'll be able to just open the Microsoft Store, search for Taskie and click "Install". For now, follow these instructions.
First of all, download the `Taskie_X.X.X.X_x86_x64_arm64.msixbundle` file and the ZIP file that's the same as your architecture. (if you don't know that, it's probably `x64`)
#### Dependencies
- Open the ZIP file you downloaded. In there, you should see another folder, open it.
- Open every file with a box icon that you can find.
- In the opened windows, click "Install". You can ignore errors here. (if any)
#### Signature
- Right-click the `.msixbundle` file and choose "Properties".
- Go to the "Digital signatures" tab.
- In the first list, choose the only option you see and click the "Details" button.
- Click the "View certificate" button.
- In the opened window, click the "Install certificate" button.
- In the opened window, choose "Local machine" and click "Next".
- Choose the "Place all certificates in the following store" option and click "Browse..."
- In there, find "Trusted Root Cerification Authorities" and click "OK".
- Click "Next" and "Finish".
#### Installation
Now, you should be able to open the `Taskie_X.X.X.X_x86_x64_arm64.msixbundle` file and click "Install". If you get an error, try following the steps in [Dependencies](#dependencies) for the Win32 zip file.

### File structure
Taskie stores your lists as JSON files in the app's local folder. You can export them into a `.taskie` file (that's just a renamed ZIP containing the JSON files)
