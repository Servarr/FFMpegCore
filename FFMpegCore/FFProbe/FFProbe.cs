﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FFMpegCore.Arguments;
using FFMpegCore.Exceptions;
using FFMpegCore.Helpers;
using FFMpegCore.Pipes;
using Instances;

namespace FFMpegCore
{
    public static class FFProbe
    {
        private static readonly JsonSerializerOptions StreamSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions FramesSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public static string GetStreamJson(string filePath, int outputCapacity = int.MaxValue,
            FFOptions? ffOptions = null)
        {
            return GetStreamJson(FileUri(filePath), outputCapacity, ffOptions);
        }
        public static string GetStreamJson(Uri uri, int outputCapacity = int.MaxValue,
            FFOptions? ffOptions = null)
        {
            if (IsLocalFile(uri) && !File.Exists(uri.LocalPath))
                throw new FFMpegException(FFMpegExceptionType.File, $"No file found at '{uri.LocalPath}'");

            using var instance = PrepareStreamAnalysisInstance(InputArgument(uri), outputCapacity, ffOptions ?? GlobalFFOptions.Current);
            var exitCode = instance.BlockUntilFinished();
            if (exitCode != 0)
                throw new FFMpegException(FFMpegExceptionType.Process, $"ffprobe exited with non-zero exit-code ({exitCode} - {string.Join("\n", instance.ErrorData)})", null, string.Join("\n", instance.ErrorData));

            return string.Join(string.Empty, instance.OutputData);
        }
        public static IMediaAnalysis AnalyseStreamJson(string json)
        {
            var ffprobeAnalysis = JsonSerializer.Deserialize<FFProbeAnalysis>(json, StreamSerializerOptions);
            if (ffprobeAnalysis?.Format == null)
                throw new FormatNullException();

            return new MediaAnalysis(ffprobeAnalysis);
        }
        public static IMediaAnalysis Analyse(string filePath, int outputCapacity = int.MaxValue, FFOptions? ffOptions = null)
        {
            return Analyse(FileUri(filePath), outputCapacity, ffOptions);
        }
        public static IMediaAnalysis Analyse(Uri uri, int outputCapacity = int.MaxValue, FFOptions? ffOptions = null)
        {
            if (IsLocalFile(uri) && !File.Exists(uri.LocalPath))
                throw new FFMpegException(FFMpegExceptionType.File, $"No file found at '{uri.LocalPath}'");
            
            using var instance = PrepareStreamAnalysisInstance(InputArgument(uri), outputCapacity, ffOptions ?? GlobalFFOptions.Current);
            var exitCode = instance.BlockUntilFinished();
            if (exitCode != 0)
                throw new FFMpegException(FFMpegExceptionType.Process, $"ffprobe exited with non-zero exit-code ({exitCode} - {string.Join("\n", instance.ErrorData)})", null, string.Join("\n", instance.ErrorData));
            
            return ParseOutput(instance);
        }
        public static string GetFrameJson(string filePath, int outputCapacity = int.MaxValue,
            FFOptions? ffOptions = null)
        {
            return GetFrameJson(FileUri(filePath), outputCapacity, ffOptions);
        }
        public static string GetFrameJson(Uri uri, int outputCapacity = int.MaxValue,
            FFOptions? ffOptions = null)
        {
            if (IsLocalFile(uri) && !File.Exists(uri.LocalPath))
                throw new FFMpegException(FFMpegExceptionType.File, $"No file found at '{uri.LocalPath}'");

            using var instance = PrepareFrameAnalysisInstance(InputArgument(uri), outputCapacity, ffOptions ?? GlobalFFOptions.Current);
            var exitCode = instance.BlockUntilFinished();
            if (exitCode != 0)
                throw new FFMpegException(FFMpegExceptionType.Process, $"ffprobe exited with non-zero exit-code ({exitCode} - {string.Join("\n", instance.ErrorData)})", null, string.Join("\n", instance.ErrorData));

            return string.Join(string.Empty, instance.OutputData);
        }
        public static FFProbeFrames AnalyseFrameJson(string json)
        {
            return JsonSerializer.Deserialize<FFProbeFrames>(json, FramesSerializerOptions);
        }
        public static FFProbeFrames GetFrames(string filePath, int outputCapacity = int.MaxValue, FFOptions? ffOptions = null)
        {
            return GetFrames(FileUri(filePath), outputCapacity, ffOptions);
        }
        public static FFProbeFrames GetFrames(Uri uri, int outputCapacity = int.MaxValue, FFOptions? ffOptions = null)
        {
            if (IsLocalFile(uri) && !File.Exists(uri.LocalPath))
                throw new FFMpegException(FFMpegExceptionType.File, $"No file found at '{uri.LocalPath}'");

            using var instance = PrepareFrameAnalysisInstance(InputArgument(uri), outputCapacity, ffOptions ?? GlobalFFOptions.Current);
            var exitCode = instance.BlockUntilFinished();
            if (exitCode != 0)
                throw new FFMpegException(FFMpegExceptionType.Process, $"ffprobe exited with non-zero exit-code ({exitCode} - {string.Join("\n", instance.ErrorData)})", null, string.Join("\n", instance.ErrorData));

            return ParseFramesOutput(instance);
        }

        public static FFProbePackets GetPackets(string filePath, int outputCapacity = int.MaxValue, FFOptions? ffOptions = null)
        {
            return GetPackets(FileUri(filePath), outputCapacity, ffOptions);
        }
        public static FFProbePackets GetPackets(Uri uri, int outputCapacity = int.MaxValue, FFOptions? ffOptions = null)
        {
            if (IsLocalFile(uri) && !File.Exists(uri.LocalPath))
                throw new FFMpegException(FFMpegExceptionType.File, $"No file found at '{uri.LocalPath}'");

            using var instance = PreparePacketAnalysisInstance(InputArgument(uri), outputCapacity, ffOptions ?? GlobalFFOptions.Current);
            var exitCode = instance.BlockUntilFinished();
            if (exitCode != 0)
                throw new FFMpegException(FFMpegExceptionType.Process, $"ffprobe exited with non-zero exit-code ({exitCode} - {string.Join("\n", instance.ErrorData)})", null, string.Join("\n", instance.ErrorData));

            return ParsePacketsOutput(instance);
        }

        public static IMediaAnalysis Analyse(Stream stream, int outputCapacity = int.MaxValue, FFOptions? ffOptions = null)
        {
            var streamPipeSource = new StreamPipeSource(stream);
            var pipeArgument = new InputPipeArgument(streamPipeSource);
            using var instance = PrepareStreamAnalysisInstance(pipeArgument.PipePath, outputCapacity, ffOptions ?? GlobalFFOptions.Current);
            pipeArgument.Pre();

            var task = instance.FinishedRunning();
            try
            {
                pipeArgument.During().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (IOException) { }
            finally
            {
                pipeArgument.Post();
            }
            var exitCode = task.ConfigureAwait(false).GetAwaiter().GetResult();
            if (exitCode != 0)
                throw new FFMpegException(FFMpegExceptionType.Process, $"ffprobe exited with non-zero exit-code ({exitCode} - {string.Join("\n", instance.ErrorData)})", null, string.Join("\n", instance.ErrorData));
            
            return ParseOutput(instance);
        }
        public static async Task<IMediaAnalysis> AnalyseAsync(string filePath, int outputCapacity = int.MaxValue, FFOptions? ffOptions = null)
        {
            return await AnalyseAsync(FileUri(filePath), outputCapacity, ffOptions);
        }
        public static async Task<IMediaAnalysis> AnalyseAsync(Uri uri, int outputCapacity = int.MaxValue, FFOptions? ffOptions = null)
        {
            if (IsLocalFile(uri) && !File.Exists(uri.LocalPath))
                throw new FFMpegException(FFMpegExceptionType.File, $"No file found at '{uri.LocalPath}'");
            
            using var instance = PrepareStreamAnalysisInstance(InputArgument(uri), outputCapacity, ffOptions ?? GlobalFFOptions.Current);
            var exitCode = await instance.FinishedRunning().ConfigureAwait(false);
            if (exitCode != 0)
                throw new FFMpegException(FFMpegExceptionType.Process, $"ffprobe exited with non-zero exit-code ({exitCode} - {string.Join("\n", instance.ErrorData)})", null, string.Join("\n", instance.ErrorData));

            return ParseOutput(instance);
        }

        public static async Task<FFProbeFrames> GetFramesAsync(string filePath, int outputCapacity = int.MaxValue, FFOptions? ffOptions = null)
        {
            return await GetFramesAsync(FileUri(filePath), outputCapacity, ffOptions);
        }
        public static async Task<FFProbeFrames> GetFramesAsync(Uri uri, int outputCapacity = int.MaxValue, FFOptions? ffOptions = null)
        {
            if (IsLocalFile(uri) && !File.Exists(uri.LocalPath))
                throw new FFMpegException(FFMpegExceptionType.File, $"No file found at '{uri.LocalPath}'");

            using var instance = PrepareFrameAnalysisInstance(InputArgument(uri), outputCapacity, ffOptions ?? GlobalFFOptions.Current);
            await instance.FinishedRunning().ConfigureAwait(false);
            return ParseFramesOutput(instance);
        }

        public static async Task<FFProbePackets> GetPacketsAsync(string filePath, int outputCapacity = int.MaxValue, FFOptions? ffOptions = null)
        {
            return await GetPacketsAsync(FileUri(filePath), outputCapacity, ffOptions);
        }
        public static async Task<FFProbePackets> GetPacketsAsync(Uri uri, int outputCapacity = int.MaxValue, FFOptions? ffOptions = null)
        {
            if (IsLocalFile(uri) && !File.Exists(uri.LocalPath))
                throw new FFMpegException(FFMpegExceptionType.File, $"No file found at '{uri.LocalPath}'");

            using var instance = PreparePacketAnalysisInstance(InputArgument(uri), outputCapacity, ffOptions ?? GlobalFFOptions.Current);
            await instance.FinishedRunning().ConfigureAwait(false);
            return ParsePacketsOutput(instance);
        }

        public static async Task<IMediaAnalysis> AnalyseAsync(Stream stream, int outputCapacity = int.MaxValue, FFOptions? ffOptions = null)
        {
            var streamPipeSource = new StreamPipeSource(stream);
            var pipeArgument = new InputPipeArgument(streamPipeSource);
            using var instance = PrepareStreamAnalysisInstance(pipeArgument.PipePath, outputCapacity, ffOptions ?? GlobalFFOptions.Current);
            pipeArgument.Pre();

            var task = instance.FinishedRunning();
            try
            {
                await pipeArgument.During().ConfigureAwait(false);
            }
            catch(IOException)
            {
            }
            finally
            {
                pipeArgument.Post();
            }
            var exitCode = await task.ConfigureAwait(false);
            if (exitCode != 0)
                throw new FFProbeProcessException($"ffprobe exited with non-zero exit-code ({exitCode} - {string.Join("\n", instance.ErrorData)})", instance.ErrorData);
            
            pipeArgument.Post();
            return ParseOutput(instance);
        }

        public static List<FFProbePixelFormat> GetPixelFormats(FFOptions? ffOptions = null)
        {
            FFProbeHelper.RootExceptionCheck();

            var options = ffOptions ?? GlobalFFOptions.Current;
            FFProbeHelper.VerifyFFProbeExists(options);

            using var instance = new Instances.Instance(GlobalFFOptions.GetFFProbeBinaryPath(), "-loglevel error -print_format json -show_pixel_formats")
            {
                DataBufferCapacity = int.MaxValue
            };

            var exitCode = instance.BlockUntilFinished();
            if (exitCode != 0)
                throw new FFProbeProcessException($"ffprobe exited with non-zero exit-code ({exitCode} - {string.Join("\n", instance.ErrorData)})", instance.ErrorData);

            var output = string.Join(string.Empty, instance.OutputData);

            return JsonSerializer.Deserialize<FFProbePixelFormats>(output, StreamSerializerOptions)?.PixelFormats ?? new List<FFProbePixelFormat>();
        }

        private static IMediaAnalysis ParseOutput(Instance instance)
        {
            return AnalyseStreamJson(string.Join(string.Empty, instance.OutputData));
        }
        private static FFProbeFrames ParseFramesOutput(Instance instance)
        {
            return AnalyseFrameJson(string.Join(string.Empty, instance.OutputData));
        }

        private static FFProbePackets ParsePacketsOutput(Instance instance)
        {
            var json = string.Join(string.Empty, instance.OutputData);
            var ffprobeAnalysis = JsonSerializer.Deserialize<FFProbePackets>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString | System.Text.Json.Serialization.JsonNumberHandling.WriteAsString
            }) ;

            return ffprobeAnalysis;
        }

        private static bool IsLocalFile(Uri uri)
        {
            return uri.IsFile || uri.Scheme == "bluray";
        }
        private static Uri FileUri(string filePath)
        {
            return new Uri(Path.GetFullPath(filePath));
        }
        private static string InputArgument(Uri uri)
        {
            return uri.IsFile ? uri.LocalPath : uri.GetComponents(UriComponents.AbsoluteUri, UriFormat.Unescaped);
        }

        private static Instance PrepareStreamAnalysisInstance(string input, int outputCapacity, FFOptions ffOptions)
            => PrepareInstance($"-loglevel error -print_format json -show_format -sexagesimal -show_streams {ffOptions.ExtraArguments} \"{input}\"", outputCapacity, ffOptions);
        private static Instance PrepareFrameAnalysisInstance(string input, int outputCapacity, FFOptions ffOptions)
            => PrepareInstance($"-loglevel error -print_format json -show_frames -v quiet -sexagesimal {ffOptions.ExtraArguments} \"{input}\"", outputCapacity, ffOptions);
        private static Instance PreparePacketAnalysisInstance(string input, int outputCapacity, FFOptions ffOptions)
            => PrepareInstance($"-loglevel error -print_format json -show_packets -v quiet -sexagesimal \"{input}\"", outputCapacity, ffOptions);
        
        private static Instance PrepareInstance(string arguments, int outputCapacity, FFOptions ffOptions)
        {
            FFProbeHelper.RootExceptionCheck();
            FFProbeHelper.VerifyFFProbeExists(ffOptions);

            var startInfo = new ProcessStartInfo(GlobalFFOptions.GetFFProbeBinaryPath(), arguments)
            {
                StandardOutputEncoding = ffOptions.Encoding,
                StandardErrorEncoding = ffOptions.Encoding,
                WorkingDirectory = ffOptions.WorkingDirectory
            };
            var instance = new Instance(startInfo) { DataBufferCapacity = outputCapacity };
            return instance;
        }
    }
}
