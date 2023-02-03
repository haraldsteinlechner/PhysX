very raw WIP  on physx for aardvark

![image](https://user-images.githubusercontent.com/513281/216617404-6a616147-055e-4e9f-908c-18a4cae4e10e.png)

just run the project:
- dotnet tool restore
- dotnet paket restore
- open the solution in visual studio 2022, run
- Unhandled exception. System.Runtime.InteropServices.SEHException (0x80004005): External component has thrown an exception.
- copy contents of libs\Native\PhysX\windows\AMD64 to the output (e.g. bin\Debug\net6.0)
- the demo should now work


space: pause/run simulation
q: shoot things
enter: create more boxes



- if you need to change the c++ wrapper code (e.g. wrapping new stuff, fixing bugs), you need to create a visual studio project using cmake..

download sdk prebuilts (see discord) - thoe are debug builds - stay with debug everywhere currently.
the folder should look like: ![image](https://user-images.githubusercontent.com/513281/216637245-ef8ae54c-edf9-4781-a328-7cf3c9640467.png)

copy the binaries found on discord to: libs\Native\PhysX\windows\AMD64 - this way the will get found by cmake:

![image](https://user-images.githubusercontent.com/513281/216617060-01c2be79-f4d7-41fa-9f89-d6ea61dce3af.png)
if you have problems you might need to use v141 toolset  (just tested with everything default and it worked)
![image](https://user-images.githubusercontent.com/513281/216617157-7366b570-efab-4d97-a9fd-c6417ec8c7b5.png)

the install target (in the generated solution) updates the libs/Native/Physx/windows/AMD64 folder but currently fails to copy the new dll over into the output folder. so this needs to be done manually:
![image](https://user-images.githubusercontent.com/513281/216637063-b3fddbe1-a5e7-49c9-b1fc-abd31a73f28f.png)



also copy those to the output folder (you should have done that already when running just the demo)

![image](https://user-images.githubusercontent.com/513281/216616891-59efaca9-b30f-4600-9e8d-49a604545f0d.png)
