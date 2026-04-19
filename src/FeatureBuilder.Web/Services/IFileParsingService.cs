namespace FeatureBuilder.Web.Services;

public interface IFileParsingService
{
    Task<string> ExtractTextFromFileAsync(IFormFile file);
    bool IsSupportedFile(IFormFile file);
}
