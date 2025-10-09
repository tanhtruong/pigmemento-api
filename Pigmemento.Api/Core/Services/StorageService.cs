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
        var cfg = config.GetSection("Storage");
        _bucket = cfg["Bucket"]!;
        _publicBaseUrl = cfg["PublicBaseUrl"];
        _useSignedUrls = bool.TryParse(cfg["UseSignedUrls"], out var b) && b;
        _signedUrlMinutes = int.TryParse(cfg["SignedUrlMinutes"], out var m) ? m : 15;
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType)
    {
        var put = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType
            // For public buckets, ACLs are typically ignored by R2; use bucket policy/custom domain instead.
            // CannedACL = S3CannedACL.PublicRead
        };
        
        put.Headers.CacheControl = "public, max-age=31536000, immutable";

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