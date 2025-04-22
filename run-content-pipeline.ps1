$contentProj = "src\Hanamura.AssetPipeline\Hanamura.AssetPipeline.csproj"
$args = @(".\assets", ".\src\Hanamura\bin\Debug\net9.0\assets")

dotnet run --project $contentProj -- $args