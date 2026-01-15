sudo dotnet publish -c Release -r linux-x64 --self-contained true
mkdir bin/Release/net8.0/linux-x64/logs
touch bin/Release/net8.0/linux-x64/logs/err.log
touch bin/Release/net8.0/linux-x64/logs/out.log
touch bin/Release/net8.0/linux-x64/logs/combined.log
sudo chmod -R 755 bin