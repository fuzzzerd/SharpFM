using System.Collections.Generic;
using System.Threading.Tasks;
using SharpFM.Model;

namespace SharpFM.Services;

public interface IClipboardService
{
    Task<IReadOnlyList<Clip>> ReadFileMakerAsync();
    Task WriteFileMakerAsync(Clip clip);
    Task SetTextAsync(string text);
    Task SetDataAsync(string format, byte[] data);
    Task<string[]> GetFormatsAsync();
    Task<object?> GetDataAsync(string format);
}
