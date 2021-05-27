using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AerialRace.RenderData;
using OpenTK.Graphics.OpenGL4;
using AerialRace.Debugging;
using System.IO;

namespace AerialRace.Loading
{
    static class ShaderCompiler
    {
        public static ShaderPipeline CompilePipeline(string name, string vertexPath, string fragmentPath)
        {
            var vertex = CompileProgram($"{name}: Vertex", ShaderStage.Vertex, vertexPath);
            var fragment = CompileProgram($"{name}: Fragment", ShaderStage.Fragment, fragmentPath);
            return CompilePipeline(name, vertex, fragment);
        }

        public static ShaderPipeline CompilePipeline(string name, ShaderProgram vertexProgram, ShaderProgram fragmentProgram)
        {
            return CompilePipeline(name, vertexProgram, null, fragmentProgram);
        }

        public static ShaderPipeline CompilePipeline(string name, ShaderProgram? vertexProgram, ShaderProgram? geometryProgram, ShaderProgram? fragmentProgram)
        {
            GLUtil.CreateProgramPipeline(name, out var handle);

            var pipeline = new ShaderPipeline(name, handle, null, null, null);

            if (vertexProgram != null)
                GL.UseProgramStages(pipeline.Handle, ProgramStageMask.VertexShaderBit, vertexProgram.Handle);
            pipeline.VertexProgram = vertexProgram;

            if (geometryProgram != null)
                GL.UseProgramStages(pipeline.Handle, ProgramStageMask.FragmentShaderBit, geometryProgram.Handle);
            pipeline.GeometryProgram = geometryProgram;

            if (fragmentProgram != null)
                GL.UseProgramStages(pipeline.Handle, ProgramStageMask.FragmentShaderBit, fragmentProgram.Handle);
            pipeline.FragmentProgram = fragmentProgram;

            // FIXME: Move this function out of the class
            pipeline.UpdateUniforms();

            GL.ValidateProgramPipeline(pipeline.Handle);
            GL.GetProgramPipeline(pipeline.Handle, ProgramPipelineParameter.ValidateStatus, out int valid);
            if (valid == 0)
            {
                GL.GetProgramPipeline(pipeline.Handle, ProgramPipelineParameter.InfoLogLength, out int logLength);
                GL.GetProgramPipelineInfoLog(pipeline.Handle, logLength, out _, out string info);

                Debug.WriteLine($"Error in program pipeline '{pipeline.Name}':\n{info}");

                // FIXME: Consider using this for debug!!
                // GL.DebugMessageInsert()
                throw new Exception();
            }

            LiveShaderLoader.TrackPipeline(pipeline);

            return pipeline;
        }

        public static ShaderProgram CompileProgram(string name, ShaderStage stage, string path)
        {
            var source = ShaderPreprocessor.PreprocessSource(path, out var sourceDesc);
            return CompileProgram(name, stage, source, sourceDesc);
        }

        public static ShaderProgram CompileProgramFromSource(string name, ShaderStage stage, string source)
        {
            return CompileProgram(name, stage, source, null);
        }

        private static ShaderProgram CompileProgram(string name, ShaderStage stage, string source, ShaderSourceDescription? sourceDesc)
        {
            GLUtil.CreateProgram(name, out int handle);
            GL.ProgramParameter(handle, ProgramParameterName.ProgramSeparable, 1);

            //program = new ShaderProgram(name, handle, stage, new Dictionary<string, int>(), new Dictionary<string, int>(), null, null);

            GLUtil.CreateShader(name, RenderDataUtil.ToGLShaderType(stage), out var shader);
            GL.ShaderSource(shader, source);

            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string info = GL.GetShaderInfoLog(shader);
                // FIXME: Process the compile error using sourceDesc
                Debug.WriteLine($"Error in {stage} shader '{name}':\n{info}");

                // Do some gl cleanup
                GL.DeleteProgram(handle);
                GL.DeleteShader(shader);

                // FIXME: return null?
                throw new Exception();
            }

            GL.AttachShader(handle, shader);
            GL.LinkProgram(handle);

            GL.DetachShader(handle, shader);
            GL.DeleteShader(shader);

            GL.GetProgram(handle, GetProgramParameterName.LinkStatus, out int isLinked);
            if (isLinked == 0)
            {
                string info = GL.GetProgramInfoLog(handle);
                Debug.WriteLine($"Error in {stage} program '{name}':\n{info}");

                GL.DeleteProgram(handle);
                throw new Exception();
            }

            var result = new ShaderProgram(name, sourceDesc, handle, stage, null!, null!, null, null);
            UpdateUniformInformation(result);

            return result;
        }

        // FIXME: Where should we handle swtiching out for an error shader?
        // Or should that be done on a pipeline level?
        public static void RecompileShader(ShaderProgram program)
        {
            var source = ShaderPreprocessor.PreprocessSource(program.Source!.MainFile.FullName, out program.Source);

            GLUtil.CreateProgram(program.Name, out var handle);
            GL.ProgramParameter(handle, ProgramParameterName.ProgramSeparable, 1);

            GLUtil.CreateShader(program.Name, RenderDataUtil.ToGLShaderType(program.Stage), out var shader);

            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string info = GL.GetShaderInfoLog(shader);
                // FIXME: Process the compile error using sourceDesc
                Debug.WriteLine($"Error in {program.Stage} shader '{program.Name}':\n{info}");

                // Do some gl cleanup
                GL.DeleteProgram(handle);
                GL.DeleteShader(shader);

                // FIXME: return null?
                throw new Exception();
            }

            GL.AttachShader(handle, shader);
            GL.LinkProgram(handle);

            GL.DetachShader(handle, shader);
            GL.DeleteShader(shader);

            GL.GetProgram(handle, GetProgramParameterName.LinkStatus, out int isLinked);
            if (isLinked == 0)
            {
                string info = GL.GetProgramInfoLog(handle);
                Debug.WriteLine($"Error in {program.Stage} program '{program.Name}':\n{info}");

                GL.DeleteProgram(handle);
                throw new Exception();
            }

            var oldHandle = program.Handle;
            program.Handle = handle;
            GL.DeleteProgram(oldHandle);

            UpdateUniformInformation(program);
        }

        public static void RecompilePipeline(ShaderPipeline pipeline)
        {
            GLUtil.CreateProgramPipeline(pipeline.Name, out var handle);

            if (pipeline.VertexProgram != null)
                GL.UseProgramStages(handle, ProgramStageMask.VertexShaderBit, pipeline.VertexProgram.Handle);

            if (pipeline.GeometryProgram != null)
                GL.UseProgramStages(handle, ProgramStageMask.GeometryShaderBit, pipeline.GeometryProgram.Handle);

            if (pipeline.FragmentProgram != null)
                GL.UseProgramStages(handle, ProgramStageMask.FragmentShaderBit, pipeline.FragmentProgram.Handle);

            GL.ValidateProgramPipeline(handle);
            GL.GetProgramPipeline(handle, ProgramPipelineParameter.ValidateStatus, out int valid);
            if (valid == 0)
            {
                GL.GetProgramPipeline(handle, ProgramPipelineParameter.InfoLogLength, out int logLength);
                GL.GetProgramPipelineInfoLog(handle, logLength, out _, out string info);

                Debug.WriteLine($"Error in program pipeline '{pipeline.Name}':\n{info}");

                // FIXME: Consider using this for debug!!
                // GL.DebugMessageInsert()
                throw new Exception();
            }

            var oldHandle = pipeline.Handle;
            pipeline.Handle = handle;
            GL.DeleteProgramPipeline(oldHandle);
        }

        public static void UpdateUniformInformation(ShaderProgram program)
        {
            Dictionary<string, int> uniformLocations = new Dictionary<string, int>();

            UniformFieldInfo[] uniformInfo;
            {
                GL.GetProgram(program.Handle, GetProgramParameterName.ActiveUniforms, out int uniformCount);

                uniformInfo = new UniformFieldInfo[uniformCount];

                for (int i = 0; i < uniformCount; i++)
                {
                    string uniformName = GL.GetActiveUniform(program.Handle, i, out int size, out ActiveUniformType type);
                    var location = GL.GetUniformLocation(program.Handle, uniformName);

                    UniformFieldInfo fieldInfo;
                    fieldInfo.Location = location;
                    fieldInfo.Name = uniformName;
                    fieldInfo.Size = size;
                    fieldInfo.Type = type;

                    uniformInfo[i] = fieldInfo;
                    uniformLocations.Add(uniformName, location);

                    //Debug.WriteLine($"{name} uniform {location} '{uniformName}' {type} ({size})");
                }
            }

            Dictionary<string, int> uniformBlockBindings = new Dictionary<string, int>();

            UniformBlockInfo[] blockInfo;
            {
                GL.GetProgram(program.Handle, GetProgramParameterName.ActiveUniformBlocks, out int uniformBlockCount);

                blockInfo = new UniformBlockInfo[uniformBlockCount];

                for (int i = 0; i < uniformBlockCount; i++)
                {
                    GL.GetActiveUniformBlock(program.Handle, i, ActiveUniformBlockParameter.UniformBlockActiveUniforms, out int uniformsInBlockCount);

                    Span<int> uniformIndices = stackalloc int[uniformsInBlockCount];
                    GL.GetActiveUniformBlock(program.Handle, i, ActiveUniformBlockParameter.UniformBlockActiveUniformIndices, out uniformIndices[0]);

                    GL.GetActiveUniformBlock(program.Handle, i, ActiveUniformBlockParameter.UniformBlockNameLength, out int nameLength);
                    GL.GetActiveUniformBlockName(program.Handle, i, nameLength, out _, out string uniformBlockName);

                    int blockIndex = GL.GetUniformBlockIndex(program.Handle, uniformBlockName);

                    // FIXME: Do something better here!
                    GL.UniformBlockBinding(program.Handle, blockIndex, blockIndex);

                    blockInfo[i].Name = uniformBlockName;
                    blockInfo[i].Index = blockIndex;
                    blockInfo[i].Members = new UniformFieldInfo[uniformIndices.Length];

                    uniformBlockBindings.Add(uniformBlockName, blockIndex);

                    //Debug.WriteLine($"Block {i} '{uniformBlockName}':");
                    //Debug.Indent();
                    var blockMember = blockInfo[i].Members;
                    for (int j = 0; j < uniformIndices.Length; j++)
                    {
                        string uniformName = GL.GetActiveUniform(program.Handle, uniformIndices[j], out int uniformSize, out var uniformType);

                        blockMember[j].Location = uniformIndices[j];
                        blockMember[j].Name = uniformName;
                        blockMember[j].Size = uniformSize;
                        blockMember[j].Type = uniformType;

                        //Debug.WriteLine($"{name} uniform {uniformIndices[j]} '{uniformName}' {uniformType} ({uniformSize})");
                    }
                    //Debug.Unindent();
                }
                //Debug.Unindent();
            }

            program.UniformLocations = uniformLocations;
            program.UniformInfo = uniformInfo;
            program.UniformBlockIndices = uniformBlockBindings;
            program.UniformBlockInfo = blockInfo;
        }

    }

    static class LiveShaderLoader
    {
        public class TrackedFile
        {
            public FileInfo File;
            public DateTime LastUpdate;

            public TrackedFile(FileInfo file)
            {
                File = file;
                LastUpdate = file.LastWriteTimeUtc;
            }
        }

        public static List<TrackedFile> TrackedFiles = new List<TrackedFile>();

        public static Dictionary<FileInfo, List<ShaderProgram>> FileDependencies = new Dictionary<FileInfo, List<ShaderProgram>>();
        public static DirtyCollection<ShaderProgram> TrackedShaders = new DirtyCollection<ShaderProgram>();

        public static Dictionary<ShaderProgram, List<ShaderPipeline>> ShaderDependencies = new Dictionary<ShaderProgram, List<ShaderPipeline>>();
        public static DirtyCollection<ShaderPipeline> TrackedPipelines = new DirtyCollection<ShaderPipeline>();

        public static void TrackPipeline(ShaderPipeline pipeline)
        {
            TrackedPipelines.Add(pipeline);

            TrackShaderIfNotNullAndHasFile(pipeline.VertexProgram, pipeline);
            TrackShaderIfNotNullAndHasFile(pipeline.GeometryProgram, pipeline);
            TrackShaderIfNotNullAndHasFile(pipeline.FragmentProgram, pipeline);

            static void TrackShaderIfNotNullAndHasFile(ShaderProgram? shader, ShaderPipeline pipeline)
            {
                if (shader != null && shader.Source != null)
                {
                    TrackedShaders.Add(shader);

                    if (ShaderDependencies.TryGetValue(shader, out var list) == false)
                    {
                        list = new List<ShaderPipeline>();
                        ShaderDependencies.Add(shader, list);
                    }

                    if (list.Contains(pipeline) == false)
                        list.Add(pipeline);

                    TrackFile(shader.Source!.MainFile, shader);
                    foreach (var file in shader.Source.Dependencies)
                    {
                        TrackFile(file, shader);
                    }
                }
            }

            static void TrackFile(FileInfo file, ShaderProgram program)
            {
                // FIXME: More efficient test!!
                if (TrackedFiles.Any(t => t.File.FullName == file.FullName) == false)
                    TrackedFiles.Add(new TrackedFile(file));

                if (FileDependencies.TryGetValue(file, out var list) == false)
                {
                    list = new List<ShaderProgram>();
                    FileDependencies.Add(file, list);
                }

                if (list.Contains(program) == false)
                    list.Add(program);
            }
        }

        public static void RecompileShadersIfNeeded()
        {
            foreach (var file in TrackedFiles)
            {
                file.File.Refresh();
                if (file.LastUpdate.Before(file.File.LastWriteTimeUtc))
                {
                    Debug.WriteLine($"File changed '{file.File.FullName}' ({file.LastUpdate} -> {file.File.LastWriteTimeUtc})");

                    foreach (var shader in FileDependencies[file.File])
                    {
                        TrackedShaders.MarkDirty(shader);
                    }

                    file.LastUpdate = file.File.LastWriteTimeUtc;
                }
            }

            // Go through all dirty shaders and try to recompile them
            for (int i = 0; i < TrackedShaders.Count; i++)
            {
                if (TrackedShaders.IsDirty(i))
                {
                    var dirtyShader = TrackedShaders[i];
                    ShaderCompiler.RecompileShader(dirtyShader);

                    Debug.WriteLine($"Recompiled program '{dirtyShader.Name}'");

                    // Now we want to mark all depending pipleines as dirty
                    foreach (var pipeline in ShaderDependencies[dirtyShader])
                    {
                        TrackedPipelines.MarkDirty(pipeline);
                    }

                    TrackedShaders.MarkClean(i);
                }
            }

            // Go through all of the dirty pipelines and try to remake them
            for (int i = 0; i < TrackedPipelines.Count; i++)
            {
                if (TrackedPipelines.IsDirty(i))
                {
                    var dirtyPipeline = TrackedPipelines[i];
                    ShaderCompiler.RecompilePipeline(dirtyPipeline);

                    Debug.WriteLine($"Recompiled pipeline '{dirtyPipeline.Name}'");

                    TrackedPipelines.MarkClean(i);
                }
            }
        }
    }
}
