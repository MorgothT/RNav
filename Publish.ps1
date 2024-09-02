$Projectfile = ".\Mapper_v1.csproj"
$xml = [Xml] (Get-Content $Projectfile)
$version = [Version] $xml.Project.PropertyGroup.Version[0]

dotnet publish --no-self-contained $Projectfile -c Release --os win -o Releases\publish
vpk pack -u RNav -v $version.ToString(3) -p .\Releases\publish -e RNav.exe -f net8-x64-desktop