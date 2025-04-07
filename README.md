# Networked Producer and Consumer
A producer-consumer exercise simulating a distributed media upload service.

* **Producer:** Reads video files from specified directories (configurable number of threads) and uploads them via network sockets.
* **Consumer:** Accepts concurrent uploads (configurable number of threads), manages incoming videos with a queue, saves files to a single folder, and provides a GUI.
  * **GUI:** Displays uploaded videos, offers a 10-second preview on hover, and full playback on click.
* **Core Concepts:** Practices concurrent programming, network sockets, file I/O, queue management, and basic GUI development in a distributed setup (designed for separate machines).
 
## How to Run
### Run both apps in local machine/ Generate .exe files to transfer to another machine
1. Double click on the provided `.sln` file and open the project in Visual Studio.
2. Build the project by clicking **Build → Build Solution**.
3. Configure the project to have multiple startup projects. See [link](https://learn.microsoft.com/en-us/visualstudio/ide/how-to-set-multiple-startup-projects?view=vs-2022) for guide.
4. Set Action to **Start** and Debug Target to the respective project.
5. Run the project by clicking **Release → Start**.
6. Before you start using the program, download the `ffmpeg.exe` file in the `prerequisites` folder found in the project root folder. Afterwards, place this `ffmpeg.exe` file in the `\P3 - Networked Producer and Consumer\bin\Release\net8.0-windows` directory.
7. Rerun the porject and start using the program.

### Running app in different machine
1. After generating the needed files, go to `\P3 - Networked Producer and Consumer\bin\Release`  (For Producer) or `\P3 - Networked Consumer\bin\Release` (For Consumer) and copy the `net8.0-windows` folder.
2. Copy this folder to a desired machine and simply run the `.exe` file to start the program.
