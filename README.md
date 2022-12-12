# YGG Scrapper

### Requirments

[.NET 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) 

### Build

```bash
dotnet build --output target
```

### Run

Search for cookie `ygg` in your request after sign in.

```bash
dotnet target\YggScrapper.dll <ygg_cookie> <output_folder> <uploader_filter>
```
