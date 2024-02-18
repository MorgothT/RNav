$Projectfile = ".\Mapper_v1.csproj"
$xml = [Xml] (Get-Content $Projectfile)
"$version = [Version] $xml.Project.PropertyGroup.Version"

dotnet publish $Projectfile -c Release --no-self-contained -r win-x64 -o Releases\publish
vpk pack -u RNav -v 1.2.5 -p .\Releases\publish -e RNav.exe
"vpk pack -u RNav -v $version.ToString(3) -p .\Releases\publish -e RNav.exe"