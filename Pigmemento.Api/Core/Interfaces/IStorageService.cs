namespace Pigmemento.Api.Core.Interfaces;

public interface IStorageService {
    Task<string> UploadAsync(string key, Stream content, string contentType);
    string GetReadableUrl(string key);
    Task DeleteAsync(string key);
}