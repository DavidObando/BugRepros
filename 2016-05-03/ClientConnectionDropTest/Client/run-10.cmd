for %%i in (1 2 3 4 5 6 7 8 9 10) do (
start cmd "/c dotnet .\bin\Debug\netcoreapp1.0\Client.dll -u http://localhost:9999/ -t 3000 -i 500 -c 20"
)
