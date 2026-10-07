using System;

namespace WhiteStarConversor
{
    public interface IConversionStrategy
    {
        string Key { get; }
        string Description { get; }
        string DefaultExtension { get; }
        void Execute(UmlParser parser, string inputFilePath, string? targetPath);
    }
}
