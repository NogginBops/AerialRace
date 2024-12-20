﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace.Loading
{
    public class ShaderSourceDescription
    {
        public FileInfo MainFile;
        public FileInfo[] Dependencies;

        public ShaderSourceDescription(FileInfo mainFile, FileInfo[] dependencies)
        {
            MainFile = mainFile;
            Dependencies = dependencies;
        }
    }

    static class ShaderPreprocessor
    {
        public static string PreprocessSource(string path, out ShaderSourceDescription sourceDesc)
        {
            var file = new FileInfo(path);
            string directory = file.Directory!.FullName;
            string source = File.ReadAllText(path);

            List<FileInfo> dependencies = new List<FileInfo>();

            StringBuilder sb = new StringBuilder();

            int fileNumber = 1;
            int currentLine = 1;

            int index = 0;
            int prevIndex = 0;
            // FIXME: Check that the include is at the start of the line!!
            while ((index = IndexOfWithLinesTraversed(source, index, "#include", out var lines)) != -1)
            {
                sb.Append(source, prevIndex, index - prevIndex);
                currentLine += lines;
                
                int start = source.IndexOf('<', index);
                int end = source.IndexOf('>', index);

                string fileName = source[(start + 1)..end];

                var includeFile = new FileInfo(Path.Combine(directory, fileName));
                dependencies.Add(includeFile);
                string includeContent = File.ReadAllText(includeFile.FullName);

                sb.AppendLine($"#line {0} {fileNumber}");
                fileNumber++;

                sb.Append(includeContent);

                sb.AppendLine($"#line {currentLine} {0}");

                prevIndex = end + 1;
                index = prevIndex;
            }

            sb.Append(source, prevIndex, source.Length - prevIndex);

            string result = sb.ToString();

            var relativePath = path;
            if (Path.IsPathFullyQualified(path))
            {
                relativePath = Path.GetRelativePath(".", path);
            }

            string debugPath = Path.Combine(".", "ShaderDebug", relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(debugPath)!);
            File.WriteAllText(debugPath, result);

            sourceDesc = new ShaderSourceDescription(file, dependencies.ToArray());
            return result;
        }

        public static int IndexOfWithLinesTraversed(string source, int startIndex, string search, out int linesTraversed)
        {
            int matchCount = 0;
            linesTraversed = 0;
            int lastChar = source.Length - search.Length;
            for (int i = startIndex; i < lastChar; i++)
            {
                if (source[i] == '\n') 
                {
                    linesTraversed++;
                    continue;
                }
                else
                {
                    if (source[i] == search[matchCount])
                    {
                        matchCount++;

                        if (matchCount == search.Length)
                        {
                            return i - matchCount;
                        }
                    }
                    else
                    {
                        matchCount = 0;
                    }
                }
            }

            return -1;
        }
    }
}
