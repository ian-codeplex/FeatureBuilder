using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace FeatureBuilder.Web.Services;

public class FileParsingService : IFileParsingService
{
    private readonly ILogger<FileParsingService> _logger;

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".doc", ".txt", ".md", ".csv", ".json", ".xml", ".yaml", ".yml"
    };

    private static readonly HashSet<string> SupportedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/msword",
        "text/plain",
        "text/markdown",
        "text/csv",
        "application/json",
        "application/xml",
        "text/xml",
        "application/x-yaml",
        "text/yaml"
    };

    public FileParsingService(ILogger<FileParsingService> logger)
    {
        _logger = logger;
    }

    public bool IsSupportedFile(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        return SupportedExtensions.Contains(extension) || SupportedMimeTypes.Contains(file.ContentType);
    }

    public async Task<string> ExtractTextFromFileAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        try
        {
            return extension switch
            {
                ".pdf" => await ExtractFromPdfAsync(file),
                ".docx" => await ExtractFromDocxAsync(file),
                ".doc" => $"[Legacy .doc format detected - content from {file.FileName}. Please convert to .docx for full text extraction.]",
                _ => await ExtractFromTextAsync(file)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from file {FileName}", file.FileName);
            return $"[Error extracting content from {file.FileName}: {ex.Message}]";
        }
    }

    private static async Task<string> ExtractFromPdfAsync(IFormFile file)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        using var pdf = PdfDocument.Open(stream.ToArray());
        var text = new System.Text.StringBuilder();
        foreach (Page page in pdf.GetPages())
        {
            text.AppendLine(page.Text);
        }
        return text.ToString().Trim();
    }

    private static async Task<string> ExtractFromDocxAsync(IFormFile file)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return string.Empty;

        var text = new System.Text.StringBuilder();
        foreach (var paragraph in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
        {
            text.AppendLine(paragraph.InnerText);
        }
        return text.ToString().Trim();
    }

    private static async Task<string> ExtractFromTextAsync(IFormFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        return await reader.ReadToEndAsync();
    }
}
