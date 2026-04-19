namespace MdPdf.Library.Runtime;

public interface IAssetDownloader
{
    Task<string> DownloadAsync(Uri assetUri);
}
