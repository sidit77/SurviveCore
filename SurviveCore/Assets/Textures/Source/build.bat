.\bin\texassemble.exe array -srgbi -srgbo -y -o ..\Blocks.dds -flist ..\Blocks.txt
.\bin\texconv.exe -srgbi -srgbo -f BC1_UNORM -y -o ..\ ..\Blocks.dds
pause