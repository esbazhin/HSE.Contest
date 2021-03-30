#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/powershell:nanoserver-2004 AS installer
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
RUN  pwsh -Command  \
        $ErrorActionPreference = 'Stop';  \
        $ProgressPreference = 'SilentlyContinue';  \
        Invoke-WebRequest \
            -UseBasicParsing  \
            -Uri https://dot.net/v1/dotnet-install.ps1  \
            -OutFile dotnet-install.ps1;  \
        ./dotnet-install.ps1  \
            -InstallDir '/Program Files/dotnet'  \
            -Channel 3.1  \
            -Runtime dotnet;  \
		./dotnet-install.ps1  \
            -InstallDir '/Program Files/dotnet'  \
            -Channel 2.1  \
            -Runtime dotnet;  \
        ./dotnet-install.ps1  \
            -InstallDir '/Program Files/dotnet'  \
            -Channel 5.0  \
            -Runtime aspnetcore;  \
        Remove-Item -Force dotnet-install.ps1

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["NetCoreFunctionalTesterService/NetCoreFunctionalTesterService.csproj", "NetCoreFunctionalTesterService/"]
COPY ["HSE.Contest.ClassLibrary/HSE.Contest.ClassLibrary.csproj", "HSE.Contest.ClassLibrary/"]
RUN dotnet restore "NetCoreFunctionalTesterService/NetCoreFunctionalTesterService.csproj"
COPY . .
WORKDIR "/src/NetCoreFunctionalTesterService"
RUN dotnet build "NetCoreFunctionalTesterService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NetCoreFunctionalTesterService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=installer ["/Program Files/dotnet", "/Program Files/dotnet"]
ENTRYPOINT ["dotnet", "NetCoreFunctionalTesterService.dll"]