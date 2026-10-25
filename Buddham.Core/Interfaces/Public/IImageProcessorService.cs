namespace Buddham.Core.Interfaces.Public;

public interface IImageProcessorService
{
    Task ProcessAsync(Stream input, string outputDir, string fileName);
}