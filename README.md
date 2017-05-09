# ![Binmap Logo](https://cloud.githubusercontent.com/assets/1974959/25785795/2718aaf2-3389-11e7-9078-fbf5b20801bf.png) Binmap

A tool that helps to analyse and document binary data file formats. It also allows to edit values and write back changes.

## Screenshots

start screen:

![Binmap start screen](https://cloud.githubusercontent.com/assets/1974959/25785729/f80b0ab2-3387-11e7-8a06-2a2b3fe750a1.png)

data loaded, unformatted:

![Binmap unformatted data](https://cloud.githubusercontent.com/assets/1974959/25785754/6fac491e-3388-11e7-9d53-cf7eb44e84da.png)

data formatted and commented:

![Binmap formatted and commented data](https://cloud.githubusercontent.com/assets/1974959/25872302/75fe61ac-350a-11e7-97af-12cbf29d1cf4.png)


## How it works and what it does

Drop any file into Binmap to load its data. Binmap shows all bytes in hex format by default. The format of each byte can be changed by selecting single bytes or ranges and clicking the respective format button. Line-breaks can be entered by hitting the ENTER key and can be removed again with the BACKSPACE key. Each section can be commented.

Binmap saves all modifications to a .binmap file which also contains the data file name. If a .binmap file exists, Binmap loads and applies it right after the new data file is loaded. In addition, .binmap files can be double-clicked or dropped into Binmap to load the data file and immediately apply the formatting.

[![Binmap 1.0 usage demo](http://img.youtube.com/vi/-Wx9N8A53AM/0.jpg)](http://www.youtube.com/watch?v=-Wx9N8A53AM "Binmap 1.0 usage demo")

Binmap 1.0 usage demo on YouTube: http://www.youtube.com/watch?v=-Wx9N8A53AM
## Source Code

Binmap is a .NET application using XNA/MonoGame/SharpDX (Visual Studio Community 2015 solution) for fast rendering on the GPU. The dll overkill feels a little weird but I wanted to see how it performs for UIs.
The font in use is called [Pixel Operator](http://www.dafont.com/de/pixel-operator.font) and should be installed for compiling.

## Status
Binmap is currently in development. Consider this repo a personal backup of my work on this project.
Feel free to clone and compile the tool if you want to try it. A download link will be added here when I feel that the program has reached a certain state.

### Soon
 - TextInput text range selection (mouse and keyboard)
 - Scrollbar track click
 - view for multiple bytes combined as int, float, double, ...

### Changelog 
 - v1.2 - 2017-05-10
   - Editing values and writing changes to data file
   - CTRL+C to copy selected bytes in the selected format and comma-separated to clipboard
   - comment color from first byte in row (item that causes the line break)

 - v1.1 - 2017-05-09 
   - added byte and range info on mouse over
   - fixed window focus problem allowing click through other windows
   - fixed range selection in combination with scrolling
   - changed: storing format as byte now (was int previously)
   
 - v1.0 - 2017-05-08 
   - github release
 