<p align=center><img src="header.png" alt="Taskie logo" width=300></p>
<p align=center>Simple to-do app for Windows 10/11. Made with <b>UWP</b>.</p>

---

### File structure
Taskie stores your lists as JSON files in the app's local folder. You can export them into a `.taskie` file (that's just a renamed ZIP containing the JSON files) The JSON files are made with this simple syntax:

```json
{
   "listmetadata":{
      "CreationDate":"2024-11-30T20:22:06.4124193Z",
      "Name":"Homework",
      "Emoji":"😷",
      "GroupID":0
   },
   "tasks":[
      {
         "CreationDate":"2024-12-04T13:37:12.5330407+01:00",
         "Name":"Math",
         "IsDone":true
      },
      {
         "CreationDate":"2024-12-04T13:37:25.7817104+01:00",
         "Name":"Science",
         "IsDone":false
      }
   ]
}
```
