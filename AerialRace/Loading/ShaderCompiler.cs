﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AerialRace.RenderData;
using OpenTK.Graphics.OpenGL;
using AerialRace.Debugging;
using System.IO;
using System.Runtime.CompilerServices;

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

            var pipeline = new ShaderPipeline(name, handle, null, null, null, null);

            if (vertexProgram != null)
                GL.UseProgramStages(pipeline.Handle, UseProgramStageMask.VertexShaderBit, vertexProgram.Handle);
            pipeline.VertexProgram = vertexProgram;

            if (geometryProgram != null)
                GL.UseProgramStages(pipeline.Handle, UseProgramStageMask.FragmentShaderBit, geometryProgram.Handle);
            pipeline.GeometryProgram = geometryProgram;

            if (fragmentProgram != null)
                GL.UseProgramStages(pipeline.Handle, UseProgramStageMask.FragmentShaderBit, fragmentProgram.Handle);
            pipeline.FragmentProgram = fragmentProgram;

            // FIXME: Move this function out of the class
            pipeline.UpdateUniforms();

            GL.ValidateProgramPipeline(pipeline.Handle);

            int valid = GL.GetProgramPipelinei(pipeline.Handle, (PipelineParameterName)All.ValidateStatus);
            if (valid == 0)
            {
                int logLength = GL.GetProgramPipelinei(pipeline.Handle, PipelineParameterName.InfoLogLength);
                string info = GL.GetProgramPipelineInfoLog(pipeline.Handle, logLength, null!);

                Debug.WriteLine($"Error in program pipeline '{pipeline.Name}':\n{info}");

                // FIXME: Consider using this for debug!!
                // GL.DebugMessageInsert()
                throw new Exception();
            }

            LiveShaderLoader.TrackPipeline(pipeline);

            return pipeline;
        }

        public static ShaderPipeline CompilePipeline(string name, string computePath)
        {
            var computeProgram = CompileProgram($"{name}: Compute", ShaderStage.Compute, computePath);

            GLUtil.CreateProgramPipeline(name, out var handle);

            var pipeline = new ShaderPipeline(name, handle, null, null, null, computeProgram);

            GL.UseProgramStages(pipeline.Handle, UseProgramStageMask.ComputeShaderBit, computeProgram.Handle);
            pipeline.ComputeProgram = computeProgram;

            // FIXME: Move this function out of the class
            pipeline.UpdateUniforms();

            GL.ValidateProgramPipeline(pipeline.Handle);
            int valid = GL.GetProgramPipelinei(pipeline.Handle, (PipelineParameterName)All.ValidateStatus);
            if (valid == 0)
            {
                int logLength = GL.GetProgramPipelinei(pipeline.Handle, PipelineParameterName.InfoLogLength);
                string info = GL.GetProgramPipelineInfoLog(pipeline.Handle, logLength, null!);

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
            GL.ProgramParameteri(handle, ProgramParameterPName.ProgramSeparable, 1);

            //program = new ShaderProgram(name, handle, stage, new Dictionary<string, int>(), new Dictionary<string, int>(), null, null);

            GLUtil.CreateShader(name, RenderDataUtil.ToGLShaderType(stage), out var shader);
            GL.ShaderSource(shader, source);

            GL.CompileShader(shader);

            int success = GL.GetShaderi(shader, ShaderParameterName.CompileStatus);
            if (success == 0)
            {
                GL.GetShaderInfoLog(shader, out string info);
                // FIXME: Process the compile error using sourceDesc
                Debug.WriteLine($"Error in {stage} shader '{name}':\n{info}");

                // Do some gl cleanup
                GL.DeleteProgram(handle);
                GL.DeleteShader(shader);

                return GetErrorShaderForStage(name, stage, sourceDesc);
            }

            GL.AttachShader(handle, shader);
            GL.LinkProgram(handle);

            GL.DetachShader(handle, shader);
            GL.DeleteShader(shader);

            int isLinked = GL.GetProgrami(handle, ProgramPropertyARB.LinkStatus);
            if (isLinked == 0)
            {
                GL.GetProgramInfoLog(handle, out string info);
                Debug.WriteLine($"Error in {stage} program '{name}':\n{info}");

                GL.DeleteProgram(handle);
                return GetErrorShaderForStage(name, stage, sourceDesc);
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
            GL.ProgramParameteri(handle, ProgramParameterPName.ProgramSeparable, 1);

            GLUtil.CreateShader(program.Name, RenderDataUtil.ToGLShaderType(program.Stage), out var shader);

            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            int success = GL.GetShaderi(shader, ShaderParameterName.CompileStatus);
            if (success == 0)
            {
                GL.GetShaderInfoLog(shader, out string info);
                // FIXME: Process the compile error using sourceDesc
                Debug.WriteLine($"Error in {program.Stage} shader '{program.Name}':\n{info}");

                // Do some gl cleanup
                GL.DeleteProgram(handle);
                GL.DeleteShader(shader);

                program.Handle = GetErrorProgramHandleForStage(program.Stage);

                UpdateUniformInformation(program);
                return;
            }

            GL.AttachShader(handle, shader);
            GL.LinkProgram(handle);

            GL.DetachShader(handle, shader);
            GL.DeleteShader(shader);

            int isLinked = GL.GetProgrami(handle, ProgramPropertyARB.LinkStatus);
            if (isLinked == 0)
            {
                GL.GetProgramInfoLog(handle, out string info);
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
                GL.UseProgramStages(handle, UseProgramStageMask.VertexShaderBit, pipeline.VertexProgram.Handle);

            if (pipeline.GeometryProgram != null)
                GL.UseProgramStages(handle, UseProgramStageMask.GeometryShaderBit, pipeline.GeometryProgram.Handle);

            if (pipeline.FragmentProgram != null)
                GL.UseProgramStages(handle, UseProgramStageMask.FragmentShaderBit, pipeline.FragmentProgram.Handle);

            if (pipeline.ComputeProgram != null)
                GL.UseProgramStages(handle, UseProgramStageMask.ComputeShaderBit, pipeline.ComputeProgram.Handle);

            GL.ValidateProgramPipeline(handle);
            int valid = GL.GetProgramPipelinei(handle, (PipelineParameterName)All.ValidateStatus);
            if (valid == 0)
            {
                int logLength = GL.GetProgramPipelinei(handle, PipelineParameterName.InfoLogLength);
                string info = GL.GetProgramPipelineInfoLog(handle, logLength, ref Unsafe.NullRef<int>());

                Debug.WriteLine($"Error in program pipeline '{pipeline.Name}':\n{info}");

                // FIXME: Consider using this for debug!!
                // GL.DebugMessageInsert()
                throw new Exception();
            }

            var oldHandle = pipeline.Handle;
            pipeline.Handle = handle;
            GL.DeleteProgramPipeline(oldHandle);

            // FIXME: Remove this?
            pipeline.Uniforms.Clear();
            pipeline.UpdateUniforms();
        }

        public static void UpdateUniformInformation(ShaderProgram program)
        {
            Dictionary<string, int> uniformLocations = new Dictionary<string, int>();

            UniformFieldInfo[] uniformInfo;
            if (false) {
                int uniformCount = GL.GetProgrami(program.Handle, ProgramPropertyARB.ActiveUniforms);

                uniformInfo = new UniformFieldInfo[uniformCount];

                for (int i = 0; i < uniformCount; i++)
                {
                    int size = default;
                    UniformType type = default;
                    string uniformName = GL.GetActiveUniform(program.Handle, (uint)i, 1024, ref Unsafe.NullRef<int>(), ref size, ref type);
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

            {
                int uniformCount = GL.GetProgramInterfacei(program.Handle, ProgramInterface.Uniform, ProgramInterfacePName.ActiveResources);
                RefList<UniformFieldInfo> uniformInfo2 = new RefList<UniformFieldInfo>(uniformCount);
                Span<ProgramResourceProperty> props = stackalloc ProgramResourceProperty[] {
                    ProgramResourceProperty.BlockIndex,
                    ProgramResourceProperty.NameLength,
                    ProgramResourceProperty.Type,
                    ProgramResourceProperty.Location,
                    ProgramResourceProperty.ArraySize,
                };
                Span<int> data = stackalloc int[props.Length];

                for (int i = 0; i < uniformCount; i++)
                {
                    GL.GetProgramResourcei(program.Handle, ProgramInterface.Uniform, (uint)i, props.Length, in props[0], data.Length, ref Unsafe.NullRef<int>(), ref data[0]);

                    if (data[0] != -1) continue;

                    ref var uniform = ref uniformInfo2.RefAdd();

                    GL.GetProgramResourceName(program.Handle, ProgramInterface.Uniform, (uint)i, data[1], ref Unsafe.NullRef<int>(), out uniform.Name);

                    uniform.Type = (UniformType)data[2];
                    uniform.Location = data[3];
                    uniform.Size = data[4];

                    uniformLocations.Add(uniform.Name, uniform.Location);
                }

                uniformInfo = uniformInfo2.ToArray();
            }

            Dictionary<string, int> uniformBlockBindings = new Dictionary<string, int>();

            UniformBlockInfo[] blockInfo;
            {
                int uniformBlockCount = GL.GetProgrami(program.Handle, ProgramPropertyARB.ActiveUniformBlocks);

                blockInfo = new UniformBlockInfo[uniformBlockCount];

                for (int i = 0; i < uniformBlockCount; i++)
                {

                    int uniformsInBlockCount = GL.GetActiveUniformBlocki(program.Handle, (uint)i, UniformBlockPName.UniformBlockActiveUniforms);

                    Span<int> uniformIndices = stackalloc int[uniformsInBlockCount];
                    GL.GetActiveUniformBlocki(program.Handle, (uint)i, UniformBlockPName.UniformBlockActiveUniformIndices, uniformIndices);

                    int nameLength = GL.GetActiveUniformBlocki(program.Handle, (uint)i, UniformBlockPName.UniformBlockNameLength);
                    GL.GetActiveUniformBlockName(program.Handle, (uint)i, nameLength, ref Unsafe.NullRef<int>(), out string uniformBlockName);

                    int blockIndex = (int)GL.GetUniformBlockIndex(program.Handle, uniformBlockName);

                    // FIXME: Do something better here!
                    GL.UniformBlockBinding(program.Handle, (uint)blockIndex, (uint)blockIndex);

                    blockInfo[i].Name = uniformBlockName;
                    blockInfo[i].Index = blockIndex;
                    blockInfo[i].Members = new UniformFieldInfo[uniformIndices.Length];

                    uniformBlockBindings.Add(uniformBlockName, blockIndex);

                    //Debug.WriteLine($"Block {i} '{uniformBlockName}':");
                    //Debug.Indent();
                    var blockMember = blockInfo[i].Members;
                    for (int j = 0; j < uniformIndices.Length; j++)
                    {
                        int uniformSize = default;
                        UniformType uniformType = default;
                        string uniformName = GL.GetActiveUniform(program.Handle, (uint)uniformIndices[j], 1024, ref Unsafe.NullRef<int>(), ref uniformSize, ref uniformType);

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

        public static ShaderProgram GetErrorShaderForStage(string name, ShaderStage stage, ShaderSourceDescription? sourceDesc)
        {
            int handle = GetErrorProgramHandleForStage(stage);
            return new ShaderProgram(name, sourceDesc, handle, stage, null!, null!, null, null);
        }

        public static int GetErrorProgramHandleForStage(ShaderStage stage)
        {
            return stage switch
            {
                ShaderStage.Vertex => BuiltIn.ErrorVertexProgram.Handle,
                ShaderStage.Fragment => BuiltIn.ErrorFragmentProgram.Handle,
                _ => throw new Exception(), // FIXME: return null?
            };
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
            TrackShaderIfNotNullAndHasFile(pipeline.ComputeProgram, pipeline);

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
                // Here we go through and try to find a file with the same path as this file
                // this means we want to use the instance of FileInfo to do the rest of the
                // operations in this function.
                bool found = false;
                foreach (var tracked in TrackedFiles)
                {
                    if (tracked.File.FullName == file.FullName)
                    {
                        file = tracked.File;
                        break;
                    }
                }

                if (found == false)
                    TrackedFiles.Add(new TrackedFile(file));

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
