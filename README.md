<p align=center><img src="header.png" alt="Taskie logo" width=300></p>
<p align=center>Simple to-do app for Windows 10/11. Made with <b>UWP</b>.</p>

---

### File structure
Taskie stores your lists as JSON files in the app's local folder. You can export them into a `.taskie` file (that's just a renamed ZIP containing the JSON files) The JSON files are made with this simple syntax:

```json
[
  {
    "CreationDate": "2023-08-04T18:41:17.7177428+02:00",
    "Name": "Make dinner",
    "IsDone": true
  },
  {
    "CreationDate": "2023-08-04T18:41:25.3742071+02:00",
    "Name": "Prepare for tomorrow's test",
    "IsDone": false
  }
]
```

### Credits
Thanks to Aurumaker72 for MVVM refactor efforts.
