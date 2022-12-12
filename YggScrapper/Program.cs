using HtmlAgilityPack;
using System.Net;
using System.Text.RegularExpressions;

var authCookie = args[0];
var output = args[1];
var uploader = args[2];
var baseUrl = "https://www5.yggtorrent.fi";
var paginationItemCount = 50;
var query = "engine/search?name=&description=&file=&uploader={0}&category=all&sub_category=&do=search&page={1}";
var dowloadQuery = "engine/download_torrent?id={0}";
var idRegex = new Regex("yggtorrent.*/(\\d+)-");
var currentPage = -paginationItemCount;
var downloadCount = 0;
var cookieContainer = new CookieContainer();
using var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
using var client = new HttpClient(handler);
cookieContainer.Add(new Uri(baseUrl), new Cookie("ygg_", authCookie));

async Task<HtmlDocument> loadNext()
{
    currentPage += paginationItemCount;
    var url = $"{baseUrl}/{string.Format(query, uploader, currentPage)}";
    var html = await DownloadHtml(client, url);

    await Console.Out.WriteLineAsync($"page [{currentPage}] requested.");
    var htmlDoc = new HtmlDocument();
    htmlDoc.LoadHtml(html);

    return htmlDoc;
}

var htmlDoc = await loadNext();
while (!isEmptyPage(htmlDoc))
{
    var torrents = getTorrentPageLinks(htmlDoc);
    await Task.WhenAll(torrents.Select(torrent => DownloadFile(client, torrent, output)));
    htmlDoc = await loadNext();
}
await Console.Out.WriteLineAsync($"[{downloadCount}] torrent file downloaded.");


static bool isEmptyPage(HtmlDocument htmlDocument)
{
    var resultTable = htmlDocument.DocumentNode.SelectSingleNode(".//div[contains(@class, 'results')]");
    return resultTable == null;
}

Torrent[] getTorrentPageLinks(HtmlDocument htmlDocument)
{
    var resultTable = htmlDocument.DocumentNode.SelectSingleNode(".//div[contains(@class, 'results')]");
    if (resultTable == null) return new Torrent[0];

    return resultTable.Descendants("tr").SelectMany(e => e.Descendants("a"))
        .Where(a => a.GetAttributeValue("id", "") == "torrent_name")
        .Select(a => GetTorrentFromANode(a))
        .ToArray();
}

static string SanitizeFileName(string name)
{
    string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
    string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

    return Regex.Replace(name, invalidRegStr, " ");
}

Torrent GetTorrentFromANode(HtmlNode node)
{
    var torrentPage = node.GetAttributeValue("href", "");
    var torrentId = idRegex.Match(torrentPage).Groups[1].Value;
    var name = SanitizeFileName(node.InnerText.Trim());
    var link = $"{baseUrl}/{string.Format(dowloadQuery, torrentId)}";
    return new Torrent
    {
        Id = torrentId,
        Name = name,
        Link = link,
    };
}

static async Task<string> DownloadHtml(HttpClient client, string url)
{
    var response = client.GetStringAsync(url);
    return await response;
}

async Task DownloadFile(HttpClient client, Torrent torrent, string outputPath)
{
    var response = client.GetStreamAsync(torrent.Link);
    var stream = await response;
    var path = Path.Combine(outputPath, $"{torrent.Name}.torrent");
    using (var fs = new FileStream(path, FileMode.CreateNew))
    {
        await stream.CopyToAsync(fs);
    }
    await Console.Out.WriteLineAsync($"[{path}] downloaded.");
    downloadCount++;
}


record Torrent
{
    public string Name { get; init; }
    public string Id { get; init; }
    public string Link { get; init; }
}