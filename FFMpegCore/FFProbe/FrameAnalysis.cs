using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FFMpegCore
{
    public class FFProbeFrameAnalysis
    {
        [JsonPropertyName("media_type")]
        public string MediaType { get; set; }
        
        [JsonPropertyName("stream_index")]
        public int StreamIndex { get; set; }
        
        [JsonPropertyName("key_frame")]
        public int KeyFrame { get; set; }
        
        [JsonPropertyName("pkt_pts")]
        public long PacketPts { get; set; }
        
        [JsonPropertyName("pkt_pts_time")]
        public string PacketPtsTime { get; set; }
        
        [JsonPropertyName("pkt_dts")]
        public long PacketDts { get; set; }
        
        [JsonPropertyName("pkt_dts_time")]
        public string PacketDtsTime { get; set; }
        
        [JsonPropertyName("best_effort_timestamp")]
        public long BestEffortTimestamp { get; set; }
        
        [JsonPropertyName("best_effort_timestamp_time")]
        public string BestEffortTimestampTime { get; set; }
        
        [JsonPropertyName("pkt_duration")]
        public int PacketDuration { get; set; }
        
        [JsonPropertyName("pkt_duration_time")]
        public string PacketDurationTime { get; set; }
        
        [JsonPropertyName("pkt_pos")]
        public long PacketPos { get; set; }
        
        [JsonPropertyName("pkt_size")]
        public int PacketSize { get; set; }
        
        [JsonPropertyName("width")]
        public long Width { get; set; }
        
        [JsonPropertyName("height")]
        public long Height { get; set; }
        
        [JsonPropertyName("pix_fmt")]
        public string PixelFormat { get; set; }
        
        [JsonPropertyName("pict_type")]
        public string PictureType { get; set; }
        
        [JsonPropertyName("coded_picture_number")]
        public long CodedPictureNumber { get; set; }
        
        [JsonPropertyName("display_picture_number")]
        public long DisplayPictureNumber { get; set; }
        
        [JsonPropertyName("interlaced_frame")]
        public int InterlacedFrame { get; set; }
        
        [JsonPropertyName("top_field_first")]
        public int TopFieldFirst { get; set; }
        
        [JsonPropertyName("repeat_pict")]
        public int RepeatPicture { get; set; }
        
        [JsonPropertyName("chroma_location")]
        public string ChromaLocation { get; set; }

        [JsonPropertyName("side_data_list")]
        public List<SideData> SideDataList { get; set; } = null!;
    }

    public class FFProbeFrames
    {
        [JsonPropertyName("frames")]
        public List<FFProbeFrameAnalysis> Frames { get; set; }
    }

    public class MasteringDisplayMetadata : SideData
    {
        [JsonPropertyName("red_x")]
        public string RedX { get; set; }

        [JsonPropertyName("red_y")]
        public string RedY { get; set; }

        [JsonPropertyName("green_x")]
        public string GreenX { get; set; }

        [JsonPropertyName("green_y")]
        public string GreenY { get; set; }

        [JsonPropertyName("blue_x")]
        public string BlueX { get; set; }

        [JsonPropertyName("blue_y")]
        public string BlueY { get; set; }

        [JsonPropertyName("white_point_x")]
        public string WhitePointX { get; set; }

        [JsonPropertyName("white_point_y")]
        public string WhitePointY { get; set; }

        [JsonPropertyName("min_luminance")]
        public string MinLuminance { get; set; }

        [JsonPropertyName("max_luminance")]
        public string MaxLuminance { get; set; }
    }

    public class ContentLightLevelMetadata : SideData
    {
        [JsonPropertyName("max_content")]
        public int MaxContent { get; set; }

        [JsonPropertyName("max_average")]
        public string MaxAverage { get; set; }
    }

    public class HdrDynamicMetadataSpmte2094 : SideData
    {
        [JsonPropertyName("application version")]
        public int ApplicationVersion { get; set; }

        [JsonPropertyName("num_windows")]
        public int NumWindows { get; set; }

        [JsonPropertyName("targeted_system_display_maximum_luminance")]
        public string TargetedSystemDisplayMaximumLuminance { get; set; }

        [JsonPropertyName("maxscl")]
        public string Maxscl { get; set; }

        [JsonPropertyName("average_maxrgb")]
        public string AverageMaxrgb { get; set; }

        [JsonPropertyName("num_distribution_maxrgb_percentiles")]
        public int NumDistributionMaxrgbPercentiles { get; set; }

        [JsonPropertyName("distribution_maxrgb_percentage")]
        public int DistributionMaxrgbPercentage { get; set; }

        [JsonPropertyName("distribution_maxrgb_percentile")]
        public string DistributionMaxrgbPercentile { get; set; }

        [JsonPropertyName("fraction_bright_pixels")]
        public string FractionBrightPixels { get; set; }

        [JsonPropertyName("knee_point_x")]
        public string KneePointX { get; set; }

        [JsonPropertyName("knee_point_y")]
        public string KneePointY { get; set; }

        [JsonPropertyName("num_bezier_curve_anchors")]
        public int NumBezierCurveAnchors { get; set; }

        [JsonPropertyName("bezier_curve_anchors")]
        public string BezierCurveAnchors { get; set; }
    }
}
