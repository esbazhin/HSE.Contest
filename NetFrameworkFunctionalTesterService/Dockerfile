#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/framework/runtime:4.8 AS base
RUN powershell -Command  \
        $ErrorActionPreference = 'Stop';  \
        $ProgressPreference = 'SilentlyContinue';  \
        Invoke-WebRequest \
            -UseBasicParsing  \
            -Uri https://dot.net/v1/dotnet-install.ps1  \
            -OutFile dotnet-install.ps1;  \
        ./dotnet-install.ps1  \
            -InstallDir '/Program Files/dotnet'  \
            -Channel 5.0  \
            -Runtime aspnetcore;  \
        Remove-Item -Force dotnet-install.ps1  \
    && setx /M PATH "%PATH%;C:\Program Files\dotnet"
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["NetFrameworkFunctionalTesterService/NetFrameworkFunctionalTesterService.csproj", "NetFrameworkFunctionalTesterService/"]
COPY ["HSE.Contest.ClassLibrary/HSE.Contest.ClassLibrary.csproj", "HSE.Contest.ClassLibrary/"]
RUN dotnet restore "NetFrameworkFunctionalTesterService/NetFrameworkFunctionalTesterService.csproj"
COPY . .
WORKDIR "/src/NetFrameworkFunctionalTesterService"
RUN dotnet build "NetFrameworkFunctionalTesterService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NetFrameworkFunctionalTesterService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=publish ["/Windows/System32/drivers/etc", "/Windows/System32/drivers/etc"]
ENTRYPOINT ["dotnet", "NetFrameworkFunctionalTesterService.dll"]