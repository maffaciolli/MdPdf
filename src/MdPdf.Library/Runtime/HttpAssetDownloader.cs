namespace MdPdf.Library.Runtime;

public sealed class HttpAssetDownloader : IAssetDownloader
{
    public async Task<string> DownloadAsync(Uri assetUri)
    {
        using var httpClient = new HttpClient();
        return await httpClient.GetStringAsync(assetUri);
    }
}
