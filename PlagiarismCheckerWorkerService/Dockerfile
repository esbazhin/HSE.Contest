#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["PlagiarismCheckerWorkerService/PlagiarismCheckerWorkerService.csproj", "PlagiarismCheckerWorkerService/"]
COPY ["HSE.Contest.ClassLibrary/HSE.Contest.ClassLibrary.csproj", "HSE.Contest.ClassLibrary/"]
RUN dotnet restore "PlagiarismCheckerWorkerService/PlagiarismCheckerWorkerService.csproj"
COPY . .
WORKDIR "/src/PlagiarismCheckerWorkerService"
RUN dotnet build "PlagiarismCheckerWorkerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PlagiarismCheckerWorkerService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PlagiarismCheckerWorkerService.dll"]