using System.Net.Http.Headers;

namespace Koras.ApiClient;

/// <summary>Composes <c>multipart/form-data</c> content from string fields and file streams.</summary>
public sealed class MultipartBuilder
{
    private readonly MultipartFormDataContent _content = [];

    internal MultipartBuilder()
    {
    }

    /// <summary>Adds a string form field.</summary>
    /// <param name="name">The field name.</param>
    /// <param name="value">The field value.</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public MultipartBuilder AddField(string name, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(value);
        _content.Add(new StringContent(value), name);
        return this;
    }

    /// <summary>Adds a file part streamed from <paramref name="stream"/> (the stream is not buffered into memory).</summary>
    /// <param name="name">The form field name.</param>
    /// <param name="stream">The readable stream supplying the file bytes. Disposed with the request content.</param>
    /// <param name="fileName">The file name reported to the server.</param>
    /// <param name="contentType">The optional media type of the file (for example <c>application/pdf</c>).</param>
    /// <returns>This builder.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> or <paramref name="fileName"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    public MultipartBuilder AddFile(string name, Stream stream, string fileName, string? contentType = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        var fileContent = new StreamContent(stream);
        if (contentType is not null)
        {
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }

        _content.Add(fileContent, name, fileName);
        return this;
    }

    internal MultipartFormDataContent Build() => _content;
}
