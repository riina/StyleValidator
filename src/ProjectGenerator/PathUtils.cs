using System;
using System.IO;

namespace ProjectGenerator;

internal static class PathUtils
{
    private static bool IsContainedPath(ReadOnlySpan<char> basePath, ReadOnlySpan<char> subPath)
    {
        while (Path.GetDirectoryName(subPath) is { Length: > 0 } dir)
        {
            if (basePath.SequenceEqual(dir))
            {
                return true;
            }
        }
        return false;
    }
}
