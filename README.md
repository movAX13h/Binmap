# ![Binmap Logo](https://cloud.githubusercontent.com/assets/1974959/25785795/2718aaf2-3389-11e7-9078-fbf5b20801bf.png) Binmap (experimental)

A tool that helps to analyse and document binary data files/formats. It can also be used to edit values and write back changes, just like a regular hex-editor but does not allow to insert or delete bytes.
All UI controls of this application are custom-made for fast rendering and style.

## Screenshots

start screen:

![Binmap start screen](https://cloud.githubusercontent.com/assets/1974959/26288049/e2834f54-3e88-11e7-9d82-d5850e95cee0.png)

data loaded, unformatted; all bytes are formatted in hex by default:

![Binmap unformatted data](https://cloud.githubusercontent.com/assets/1974959/25785754/6fac491e-3388-11e7-9d53-cf7eb44e84da.png)

data formatted and commented:

![Binmap formatted and commented data](https://cloud.githubusercontent.com/assets/1974959/26132012/a6eb02fa-3a9d-11e7-8ca5-6d78efa377de.png)


## How it works and what it does

Drop any file into Binmap to load its data. Binmap shows all bytes in hex format by default. The format of each byte can be changed by selecting single bytes or ranges and clicking the respective format button. Line-breaks can be entered by hitting the ENTER key and can be removed again with the BACKSPACE key. Each section can be commented.

Binmap saves all modifications to a .binmap file which also contains the data file name. If a .binmap file exists, Binmap loads and applies it right after the new data file is loaded. In addition, .binmap files can be double-clicked or dropped into Binmap to load the data file and immediately apply the formatting.

[![Binmap 1.0 usage demo](http://img.youtube.com/vi/-Wx9N8A53AM/0.jpg)](http://www.youtube.com/watch?v=-Wx9N8A53AM "Binmap 1.0 usage demo")

Binmap 1.0 usage demo on YouTube: http://www.youtube.com/watch?v=-Wx9N8A53AM
## Source Code

Binmap is a .NET application using XNA/MonoGame/SharpDX (Visual Studio Community 2015 solution) for fast rendering on the GPU. The dll overkill feels a little weird but I wanted to see how it performs for UIs.
The font in use is called [Pixel Operator](http://www.dafont.com/de/pixel-operator.font) and should be installed for compiling.

## Download
If you are interested in a release download, here is the latest public release (might be some subversions behind): [Binmap Release](https://github.com/movAX13h/Binmap/releases/latest)

### Todo
 - TextInput text range selection (mouse and keyboard), handle longer texts
 - Scrollbar track click
 - view for multiple bytes combined as int, float, double, ...

### Changelog
 - v1.5 - 2017-05-22
   - F3 to repeat search (scrolls to next result till end of file is reached)
   - welcome screen shader

 - v1.4 - 2017-05-16
   - added goto address panel (scrolls list to address) with format switch (hex/dec)
   - added search panel with format switch (hex/dec)
   - added format switch to value editing panel (hex/dec)

 - v1.3 - 2017-05-13
   - marks on scrollbar indicating items with linebreak in the color of the item format
   - changed clipboard data from comma-separated to space-separated
   - cursor info panel position limit at the bottom
   - added DEL key support for TextInput
   
 - v1.2 - 2017-05-10
   - values editing and writing changes back to data file
   - CTRL+C to copy selected bytes in the selected format and comma-separated to clipboard
   - comment color from first byte in row (item that causes the line break)

 - v1.1 - 2017-05-09 
   - added byte and range info on mouse over
   - fixed window focus problem allowing click through other windows
   - fixed range selection in combination with scrolling
   - storing format as byte now; was int before
   
 - v1.0 - 2017-05-08 
   - github release
 
