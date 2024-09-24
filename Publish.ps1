$Projectfile = ".\Mapper_v1.csproj"
$xml = [Xml] (Get-Content $Projectfile)
$version = [Version] $xml.Project.PropertyGroup.Version[0]

dotnet publish --no-self-contained $Projectfile -c Release -r win-x64 -o Releases\publish
vpk pack -u RNav -v $version.ToString(3) -p .\Releases\publish -e RNav.exe -f net8-x64-desktop
dotnet publish --no-self-contained $Projectfile -c Release -r win-x86 -o Releases\publish-x86
vpk pack -u RNav-x86 -v $version.ToString(3) -p .\Releases\publish-x86 -e RNav.exe -f net8-x86-desktop --channel win-x86