using Amazon.S3;
using Amazon.S3.Model;
using Pigmemento.Api.Core.Interfaces;

namespace Pigmemento.Api.Core.Services;

public class StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly string? _publicBaseUrl;
    private readonly bool _useSignedUrls;
    private readonly int _signedUrlMinutes;

    public StorageService(IAmazonS3 s3, IConfiguration config)
    {
        _s3 = s3;

        // Try both "Storage" section and flat env vars
        var section = config.GetSection("Storage");

        _bucket =
            section["Bucket"]
            ?? config["STORAGE_BUCKET"]
            ?? throw new InvalidOperationException("Missing STORAGE_BUCKET or Storage:Bucket");

        _publicBaseUrl =
            section["PublicBaseUrl"]
            ?? config["STORAGE_PUBLIC_BASE_URL"];

        _useSignedUrls =
            (bool.TryParse(section["UseSignedUrls"], out var s1) && s1)
            || (bool.TryParse(config["STORAGE_USE_SIGNED_URLS"], out var s2) && s2);

        _signedUrlMinutes =
            int.TryParse(section["SignedUrlMinutes"], out var m1)
                ? m1
                : int.TryParse(config["STORAGE_SIGNED_URL_MINUTES"], out var m2)
                    ? m2
                    : 15;
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType, long? contentLength = null)
    {
        var put = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType
        };

        if (contentLength.HasValue)
            put.Headers.ContentLength = contentLength.Value; // avoids chunked mode

        // (Optional caching)
        // put.Headers.CacheControl = "public, max-age=31536000, immutable";

        await _s3.PutObjectAsync(put);
        return GetReadableUrl(key);
    }

    public string GetReadableUrl(string key)
    {
        // If you’ve set a public custom domain for R2, use that.
        if (!_useSignedUrls && !string.IsNullOrWhiteSpace(_publicBaseUrl))
            return $"{_publicBaseUrl.TrimEnd('/')}/{Uri.EscapeDataString(key)}";

        // Otherwise issue a signed URL (works on private buckets).
        var req = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Expires = DateTime.UtcNow.AddMinutes(_signedUrlMinutes)
        };
        return _s3.GetPreSignedURL(req);
    }

    public async Task DeleteAsync(string key)
    {
        await _s3.DeleteObjectAsync(_bucket, key);
    }
}