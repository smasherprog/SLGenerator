call del *.nupkg
nuget pack nuget\SLGeneratorLib.nuspec
for /r . %%g in (*.nupkg) do nuget push %%g -Timeout 2147483