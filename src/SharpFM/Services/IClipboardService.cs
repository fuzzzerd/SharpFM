using System.Threading.Tasks;

namespace SharpFM.Services;

public interface IClipboardService
{
    Task SetTextAsync(string text);
    Task SetDataAsync(string format, byte[] data);
    Task<string[]> GetFormatsAsync();
    Task<object?> GetDataAsync(string format);
}
