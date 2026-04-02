using System.Threading.Tasks;

namespace SharpFM.Services;

public interface IFolderService
{
    Task<string> GetFolderAsync();
}
