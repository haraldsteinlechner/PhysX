very raw WIP  on physx for aardvark

![image](https://user-images.githubusercontent.com/513281/216617404-6a616147-055e-4e9f-908c-18a4cae4e10e.png)

just run the project:
- dotnet tool restore
- dotnet paket restore
- open visual studio 2022, run
- Unhandled exception. System.Runtime.InteropServices.SEHException (0x80004005): External component has thrown an exception.
- copy contents of libs\Native\PhysX\windows\AMD64 to the output (e.g. bin\Debug\net6.0)
- the demo should now work


space: pause/run simulation
q: shoot things
enter: create more boxes



- if you need to change the c++ wrapper code (e.g. wrapping new stuff, fixing bugs), you need to create a visual studio project using cmake..

download sdk prebuilts (see discord) - thoe are debug builds - stay with debug everywhere currently

copy the binaries found on discord to: libs\Native\PhysX\windows\AMD64

![image](https://user-images.githubusercontent.com/513281/216617060-01c2be79-f4d7-41fa-9f89-d6ea61dce3af.png)
if you have problems you might need to use v141 toolset  (just tested with everything default and it worked)
![image](https://user-images.githubusercontent.com/513281/216617157-7366b570-efab-4d97-a9fd-c6417ec8c7b5.png)

the install target should take care of copying the files to libs\Native\PhysX\windows\AMD64 which should then be used when running the demo. to be sure, copy to the output folder also
![image](https://user-images.githubusercontent.com/513281/216618962-27e8cbaa-3be5-49e7-9321-cf3cafe929aa.png)


also copy those to the output folder (you should have done that already when running just the demo)

![image](https://user-images.githubusercontent.com/513281/216616891-59efaca9-b30f-4600-9e8d-49a604545f0d.png)
