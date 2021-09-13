using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
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

        public static string GetStreamJson(string filePath, FFOptions? ffOptions = null)
        {
            if (!File.Exists(filePath))
                throw new FFMpegException(FFMpegExceptionType.File, $"No file found at '{filePath}'");

            var instance = PrepareStreamAnalysisInstance(filePath, ffOptions ?? GlobalFFOptions.Current);
            var result = instance.StartAndWaitForExit();
            ThrowIfExitCodeNotZero(result);

            return string.Join(string.Empty, result.OutputData);
        }
        
        public static IMediaAnalysis AnalyseStreamJson(string json)
        {
            var ffprobeAnalysis = JsonSerializer.Deserialize<FFProbeAnalysis>(json, StreamSerializerOptions);
            if (ffprobeAnalysis?.Format == null)
                throw new FormatNullException();

            return new MediaAnalysis(ffprobeAnalysis);
        }

        public static IMediaAnalysis Analyse(string filePath, FFOptions? ffOptions = null)
        {
            ThrowIfInputFileDoesNotExist(filePath);
            
            var processArguments = PrepareStreamAnalysisInstance(filePath, ffOptions ?? GlobalFFOptions.Current);
            var result = processArguments.StartAndWaitForExit();
            ThrowIfExitCodeNotZero(result);
            
            return ParseOutput(result);
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
        public static FFProbeFrames GetFrames(string filePath, FFOptions? ffOptions = null)
        {
            ThrowIfInputFileDoesNotExist(filePath);

            var instance = PrepareFrameAnalysisInstance(filePath, ffOptions ?? GlobalFFOptions.Current);
            var result = instance.StartAndWaitForExit();
            ThrowIfExitCodeNotZero(result);

            return ParseFramesOutput(result);
        }

        public static FFProbePackets GetPackets(string filePath, FFOptions? ffOptions = null)
        {
            ThrowIfInputFileDoesNotExist(filePath);

            var instance = PreparePacketAnalysisInstance(filePath, ffOptions ?? GlobalFFOptions.Current);
            var result = instance.StartAndWaitForExit();
            ThrowIfExitCodeNotZero(result);

            return ParsePacketsOutput(result);
        }

        public static IMediaAnalysis Analyse(Uri uri, FFOptions? ffOptions = null)
        {
            var instance = PrepareStreamAnalysisInstance(uri.AbsoluteUri, ffOptions ?? GlobalFFOptions.Current);
            var result = instance.StartAndWaitForExit();
            ThrowIfExitCodeNotZero(result);

            return ParseOutput(result);
        }
        public static IMediaAnalysis Analyse(Stream stream, FFOptions? ffOptions = null)
        {
            var streamPipeSource = new StreamPipeSource(stream);
            var pipeArgument = new InputPipeArgument(streamPipeSource);
            var instance = PrepareStreamAnalysisInstance(pipeArgument.PipePath, ffOptions ?? GlobalFFOptions.Current);
            pipeArgument.Pre();

            var task = instance.StartAndWaitForExitAsync();
            try
            {
                pipeArgument.During().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (IOException) { }
            finally
            {
                pipeArgument.Post();
            }
            var result = task.ConfigureAwait(false).GetAwaiter().GetResult();
            ThrowIfExitCodeNotZero(result);
            
            return ParseOutput(result);
        }

        public static async Task<IMediaAnalysis> AnalyseAsync(string filePath, FFOptions? ffOptions = null, CancellationToken cancellationToken = default)
        {
            ThrowIfInputFileDoesNotExist(filePath);
            
            var instance = PrepareStreamAnalysisInstance(filePath, ffOptions ?? GlobalFFOptions.Current);
            var result = await instance.StartAndWaitForExitAsync(cancellationToken).ConfigureAwait(false);
            ThrowIfExitCodeNotZero(result);

            return ParseOutput(result);
        }

        public static async Task<FFProbeFrames> GetFramesAsync(string filePath, FFOptions? ffOptions = null, CancellationToken cancellationToken = default)
        {
            ThrowIfInputFileDoesNotExist(filePath);

            var instance = PrepareFrameAnalysisInstance(filePath, ffOptions ?? GlobalFFOptions.Current);
            var result = await instance.StartAndWaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return ParseFramesOutput(result);
        }

        public static async Task<FFProbePackets> GetPacketsAsync(string filePath, FFOptions? ffOptions = null, CancellationToken cancellationToken = default)
        {
            ThrowIfInputFileDoesNotExist(filePath);

            var instance = PreparePacketAnalysisInstance(filePath, ffOptions ?? GlobalFFOptions.Current);
            var result = await instance.StartAndWaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return ParsePacketsOutput(result);
        }

        public static async Task<IMediaAnalysis> AnalyseAsync(Uri uri, FFOptions? ffOptions = null, CancellationToken cancellationToken = default)
        {
            var instance = PrepareStreamAnalysisInstance(uri.AbsoluteUri, ffOptions ?? GlobalFFOptions.Current);
            var result = await instance.StartAndWaitForExitAsync(cancellationToken).ConfigureAwait(false);
            ThrowIfExitCodeNotZero(result);

            return ParseOutput(result);
        }
        public static async Task<IMediaAnalysis> AnalyseAsync(Stream stream, FFOptions? ffOptions = null, CancellationToken cancellationToken = default)
        {
            var streamPipeSource = new StreamPipeSource(stream);
            var pipeArgument = new InputPipeArgument(streamPipeSource);
            var instance = PrepareStreamAnalysisInstance(pipeArgument.PipePath, ffOptions ?? GlobalFFOptions.Current);
            pipeArgument.Pre();

            var task = instance.StartAndWaitForExitAsync(cancellationToken);
            try
            {
                await pipeArgument.During(cancellationToken).ConfigureAwait(false);
            }
            catch(IOException)
            {
            }
            finally
            {
                pipeArgument.Post();
            }
            var result = await task.ConfigureAwait(false);
            ThrowIfExitCodeNotZero(result);
            
            pipeArgument.Post();
            return ParseOutput(result);
        }

        public static List<FFProbePixelFormat> GetPixelFormats(FFOptions? ffOptions = null)
        {
            FFProbeHelper.RootExceptionCheck();

            var options = ffOptions ?? GlobalFFOptions.Current;
            FFProbeHelper.VerifyFFProbeExists(options);

            var instance = new ProcessArguments(GlobalFFOptions.GetFFProbeBinaryPath(), "-loglevel error -print_format json -show_pixel_formats")
            {
                DataBufferCapacity = int.MaxValue
            };

            var result = instance.StartAndWaitForExit();
            ThrowIfExitCodeNotZero(result);

            var output = string.Join(string.Empty, result.OutputData);

            return JsonSerializer.Deserialize<FFProbePixelFormats>(output, StreamSerializerOptions)?.PixelFormats ?? new List<FFProbePixelFormat>();
        }

        private static IMediaAnalysis ParseOutput(IProcessResult instance)
        {
            return AnalyseStreamJson(string.Join(string.Empty, instance.OutputData));
        }

        private static FFProbeFrames ParseFramesOutput(IProcessResult instance)
        {
            return AnalyseFrameJson(string.Join(string.Empty, instance.OutputData));
        }

        private static FFProbePackets ParsePacketsOutput(IProcessResult instance)
        {
            var json = string.Join(string.Empty, instance.OutputData);
            var ffprobeAnalysis = JsonSerializer.Deserialize<FFProbePackets>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString | System.Text.Json.Serialization.JsonNumberHandling.WriteAsString
            }) ;

            return ffprobeAnalysis!;
        }

        private static void ThrowIfInputFileDoesNotExist(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FFMpegException(FFMpegExceptionType.File, $"No file found at '{filePath}'");
            }
        }

        private static void ThrowIfExitCodeNotZero(IProcessResult result)
        {
            if (result.ExitCode != 0)
            {
                var message = $"ffprobe exited with non-zero exit-code ({result.ExitCode} - {string.Join("\n", result.ErrorData)})";
                throw new FFMpegException(FFMpegExceptionType.Process, message, null, string.Join("\n", result.ErrorData));
            }
        }

        private static ProcessArguments PrepareStreamAnalysisInstance(string filePath, FFOptions ffOptions)
            => PrepareInstance($"-loglevel error -print_format json -show_format -sexagesimal -show_streams {ffOptions.ExtraArguments} \"{filePath}\"", ffOptions);
        private static ProcessArguments PrepareFrameAnalysisInstance(string filePath, FFOptions ffOptions)
            => PrepareInstance($"-loglevel error -print_format json -show_frames -v quiet -sexagesimal {ffOptions.ExtraArguments} \"{filePath}\"", ffOptions);
        private static ProcessArguments PreparePacketAnalysisInstance(string filePath, FFOptions ffOptions)
            => PrepareInstance($"-loglevel error -print_format json -show_packets -v quiet -sexagesimal \"{filePath}\"", ffOptions);
        
        private static ProcessArguments PrepareInstance(string arguments, FFOptions ffOptions)
        {
            FFProbeHelper.RootExceptionCheck();
            FFProbeHelper.VerifyFFProbeExists(ffOptions);
            var startInfo = new ProcessStartInfo(GlobalFFOptions.GetFFProbeBinaryPath(ffOptions), arguments)
            {
                StandardOutputEncoding = ffOptions.Encoding,
                StandardErrorEncoding = ffOptions.Encoding,
                WorkingDirectory = ffOptions.WorkingDirectory
            };
            return new ProcessArguments(startInfo);
        }
    }
}
