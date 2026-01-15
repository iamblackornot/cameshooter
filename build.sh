sudo dotnet publish -c Release -r linux-x64 --self-contained true
mkdir bin/Release/net8.0/linux-x64/logs
sudo chmod -R 755 bin