# OTEX 
A OT framework for collaborative plain-text editors in C#/.NET.  
Originally developed by [Mark Gillard](https://github.com/marzer/) as the major project submission for Flinders University topic `COMP7722`, Semester 2, 2016.  

## Structure
- `\comp`: Property sheets, batch scripts, command-line tools.
- `\doc`: HTML documentation.
- `\licenses`: License notices for libraries, resources etc. used by OTEX.
- `\OTEX`: Root folder for the `OTEX` C# project.
- `\OTEXDedicatedServer`: Root folder for the `OTEXDedicatedServer` C# project.
- `\OTEXEditor`: Root folder for the `OTEXEditor` C# project.
- `\sandcastle`: Root folder for the sandcastle HTML documentation project.

## Downloads
- OTEX Framework, Editor and Dedicated Server: [OTEX.zip](https://drive.google.com/open?id=0B6cTOEVTlAMJSng0djZaREZFRmM)
- Readme (also included in zip): [OTEX Readme.pdf](https://github.com/marzer/OTEX/raw/master/OTEX%20Readme.pdf)

## Compiling  
If you just want the applications, download the [zip file](https://drive.google.com/open?id=0B6cTOEVTlAMJSng0djZaREZFRmM) above.
Otherwise, you'll need:
- Visual studio 2013 or higher ([link](https://www.visualstudio.com/downloads/))
- .NET Framework 4.5.2 or higher (included with Windows 8.1, [link](https://www.microsoft.com/en-au/download/details.aspx?id=42642))
- [Sandcastle](https://github.com/EWSoftware/SHFB) (if you wish to rebuild the HTML documentation)

### Dependencies
#### DiffPlex
OTEX Editor uses a diff generation package called `DiffPlex` to create the client-side OT operations when the text is edited by the user. DiffPlex can found on [GitHub](https://github.com/mmanela/diffplex) and [NuGet](https://www.nuget.org/packages/DiffPlex/). DiffPlex is used under the [Apache License, Version 2.0](https://github.com/mmanela/diffplex/blob/master/License.txt).  
- **Note:** If you are using Visual Studio you will not need to manually install DiffPlex; it is configured as a NuGet package in the `OTEXEditor` C# project.

#### FastColoredTextBox
OTEX Editor uses a very powerful RichTextBox alternative called `FastColoredTextBox`, which helps provides a lot of the more advanced features of the editor interface. FastColoredTextBox can found on [GitHub](https://github.com/PavelTorgashov/FastColoredTextBox), [NuGet](https://www.nuget.org/packages/FCTB/) and [CodeProject](http://www.codeproject.com/Articles/161871/Fast-Colored-TextBox-for-syntax-highlighting). FastColoredTextBox is used under [LGPLv3](https://github.com/PavelTorgashov/FastColoredTextBox/blob/master/license.txt).
- **Note:** If you are using Visual Studio you will not need to manually install DiffPlex; it is configured as a NuGet package in the `OTEXEditor` C# project.

#### Marzersoft.dll
All three of the projects contained in the solution share a dependency on `Marzersoft.dll`, which is a personal library of useful classes, extensions and wrappers I’ve built over my years working with C#. It’s not open-source, but OTEX can be compiled by linking against the Marzersoft.dll files included with the [OTEX distribution](https://drive.google.com/open?id=0B6cTOEVTlAMJSng0djZaREZFRmM). 


## Samples and Documentation
The [Editor](https://github.com/marzer/OTEX/blob/master/OTEXEditor/EditorForm.cs) and [Dedicated Server](https://github.com/marzer/OTEX/blob/master/OTEXDedicatedServer/DedicatedServer.cs) are short, self-contained uses of the OTEX functionality. They are all the sample code necessary to tease apart a working implementation. Additionally, the `\doc` folder contains a full set of HTML documentation for all the public OTEX interfaces and classes.
