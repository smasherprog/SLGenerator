# SLGenerator
Sl Generator - A Visual studio plugin to dynamically load .net code and execute it from within a visual studio plugin. While the power of this might be difficult to explain, and example is better!

<h2>Generate Typescript from c#</h2>
<h3>First, open Visual studio and install the extension. This has to be done only once!</h3>
<p>Download and install the extension by going to Tools -> Extensions and Updates</p>
<img style="width:50%;float:left;" src="https://raw.githubusercontent.com/smasherprog/SLGenerator/master/pic0.png" target="_blank"/>
<img style="width:50%;float:left;" src="https://raw.githubusercontent.com/smasherprog/SLGenerator/master/pic00.PNG" target="_blank"/>

<h3>Getting Started</h3>
<p>First, open Visual studio create a new project.  File -> New Project</p>
<p>Select ASP.NET Web Application -> MVC Template</p>
<img style="width:50%;float:left;" src="https://raw.githubusercontent.com/smasherprog/SLGenerator/master/pic1.png" target="_blank"/>
<img style="width:50%;float:left;" src="https://raw.githubusercontent.com/smasherprog/SLGenerator/master/pic2.png" target="_blank"/>
<p>Once loaded, right click the solution and goto Add -> New Project</p>
<p>Select C# Class Library</p>
<img style="width:50%;float:left;" src="https://raw.githubusercontent.com/smasherprog/SLGenerator/master/pic3.png" target="_blank"/>
<img style="width:50%;float:left;" src="https://raw.githubusercontent.com/smasherprog/SLGenerator/master/pic4.png" target="_blank"/>
<p>Right click the newly created project and goto Manage Nuget Packages. Goto Browse and search for SLGeneratorLib. Install the package for the class <b>library only! This nuget package has very specific version dependencies -- do not attempt to update the referenced packages!</b> A seperate project is created to make this easier.</p>
<p>We are almost there . . . </p>
<p>Add the tsgenerationexample.cs file to the class library project. Right click the newly added file and goto properties. Add SLCodeGeneratorin the Custom Tool textbox and save.</p>
<img style="width:50%;float:left;" src="https://raw.githubusercontent.com/smasherprog/SLGenerator/master/pic5.PNG" target="_blank"/>
<p>After you save all, Visual studio should notify you that it wants to setup your project for typescript. Press yes. You should notice two files in your project</p>
<img style="width:50%;float:left;" src="https://raw.githubusercontent.com/smasherprog/SLGenerator/master/pic6.PNG" target="_blank"/>
<p>You are done. You can see the example in action by opening the Web Project's Models Folder in the solution. Open the file AccountViewModels.cs and change the name of a variable. You will see it instantly updated in the typescript file!</p>
<p>All of the functionality for this is in the tsgenerationexample.cs file you added in the class library project.</p>

