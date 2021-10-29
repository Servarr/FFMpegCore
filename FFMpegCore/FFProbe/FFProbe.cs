using System;
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
            if (!File.Exists(filePath))
                throw new FFMpegException(FFMpegExceptionType.File, $"No file found at '{filePath}'");

            using var instance = PrepareStreamAnalysisInstance(filePath, outputCapacity, ffOptions ?? GlobalFFOptions.Current);
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
            if (!File.Exists(filePath)) 
                throw new FFMpegException(FFMpegExceptionType.File, $"No file found at '{filePath}'");
            
            using var instance = PrepareStreamAnalysisInstance(filePath, outputCapacity, ffOptions ?? GlobalFFOptions.Current);
            var exitCode = instance.BlockUntilFinished();
            if (exitCode != 0)
                throw new FFMpegException(FFMpegExceptionType.Process, $"ffprobe exited with non-zero exit-code ({exitCode} - {string.Join("\n", instance.ErrorData)})", null, string.Join("\n", instance.ErrorData));
            
            return ParseOutput(instance);
        }
        public static string GetFrameJson(string filePath, int outputCapacity = int.MaxValue,
            FFOptions? ffOptions = null)
        {
            if (!File.Exists(filePath))
                throw new FFMpegException(FFMpegExceptionType.File, $"No file found at '{filePath}'");

            using var instance = PrepareFrameAnalysisInstance(filePath, outputCapacity, ffOptions ?? GlobalFFOptions.Current);
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
            if (!File.Exists(filePath))
                throw new FFMpegException(FFMpegExceptionType.File, $"No file found at '{filePath}'");

            using var instance = PrepareFrameAnalysisInstance(filePath, outputCapacity, ffOptions ?? GlobalFFOptions.Current);
            var exitCode = instance.BlockUntilFinished();
            if (exitCode != 0)
                throw new FFMpegException(FFMpegExceptionType.Process, $"ffprobe exited with non-zero exit-code ({exitCode} - {string.Join("\n", instance.ErrorData)})", null, string.Join("\n", instance.ErrorData));

            return ParseFramesOutput(instance);
        }
        public static IMediaAnalysis Analyse(Uri uri, int outputCapacity = int.MaxValue, FFOptions? ffOptions = null)
        {
            using var instance = PrepareStreamAnalysisInstance(uri.AbsoluteUri, outputCapacity, ffOptions ?? GlobalFFOptions.Current);
            var exitCode = instance.BlockUntilFinished();
            if (exitCode != 0)
                throw new FFMpegException(FFMpegExceptionType.Process, $"ffprobe exited with non-zero exit-code ({exitCode} - {string.Join("\n", instance.ErrorData)})", null, string.Join("\n", instance.ErrorData));

            return ParseOutput(instance);
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
            if (!File.Exists(filePath)) 
                throw new FFMpegException(FFMpegExceptionType.File, $"No file found at '{filePath}'");
            
            using var instance = PrepareStreamAnalysisInstance(filePath, outputCapacity, ffOptions ?? GlobalFFOptions.Current);
            var exitCode = await instance.FinishedRunning().ConfigureAwait(false);
            if (exitCode != 0)
                throw new FFMpegException(FFMpegExceptionType.Process, $"ffprobe exited with non-zero exit-code ({exitCode} - {string.Join("\n", instance.ErrorData)})", null, string.Join("\n", instance.ErrorData));

            return ParseOutput(instance);
        }

        public static async Task<FFProbeFrames> GetFramesAsync(string filePath, int outputCapacity = int.MaxValue, FFOptions? ffOptions = null)
        {
            if (!File.Exists(filePath))
                throw new FFMpegException(FFMpegExceptionType.File, $"No file found at '{filePath}'");

            using var instance = PrepareFrameAnalysisInstance(filePath, outputCapacity, ffOptions ?? GlobalFFOptions.Current);
            await instance.FinishedRunning().ConfigureAwait(false);
            return ParseFramesOutput(instance);
        }
        public static async Task<IMediaAnalysis> AnalyseAsync(Uri uri, int outputCapacity = int.MaxValue, FFOptions? ffOptions = null)
        {
            using var instance = PrepareStreamAnalysisInstance(uri.AbsoluteUri, outputCapacity, ffOptions ?? GlobalFFOptions.Current);
            var exitCode = await instance.FinishedRunning().ConfigureAwait(false);
            if (exitCode != 0)
                throw new FFMpegException(FFMpegExceptionType.Process, $"ffprobe exited with non-zero exit-code ({exitCode} - {string.Join("\n", instance.ErrorData)})", null, string.Join("\n", instance.ErrorData));

            return ParseOutput(instance);
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

        private static Instance PrepareStreamAnalysisInstance(string filePath, int outputCapacity, FFOptions ffOptions)
            => PrepareInstance($"-loglevel error -print_format json -show_format -sexagesimal -show_streams {ffOptions.ExtraArguments} \"{filePath}\"", outputCapacity, ffOptions);
        private static Instance PrepareFrameAnalysisInstance(string filePath, int outputCapacity, FFOptions ffOptions)
            => PrepareInstance($"-loglevel error -print_format json -show_frames -v quiet -sexagesimal {ffOptions.ExtraArguments} \"{filePath}\"", outputCapacity, ffOptions);
        
        private static Instance PrepareInstance(string arguments, int outputCapacity, FFOptions ffOptions)
        {
            FFProbeHelper.RootExceptionCheck();
            FFProbeHelper.VerifyFFProbeExists(ffOptions);

            var startInfo = new ProcessStartInfo(GlobalFFOptions.GetFFProbeBinaryPath(), arguments)
            {
                StandardOutputEncoding = ffOptions.Encoding,
                StandardErrorEncoding = ffOptions.Encoding
            };
            var instance = new Instance(startInfo) { DataBufferCapacity = outputCapacity };
            return instance;
        }
    }
}
