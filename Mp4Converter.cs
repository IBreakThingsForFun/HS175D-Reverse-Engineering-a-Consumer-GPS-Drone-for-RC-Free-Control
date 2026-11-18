using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;

namespace HS175D
{
    /// <summary>
    /// Converts a finished MJPEG .avi recording to H.264 .mp4 using Windows' own Media Foundation
    /// encoder, reached through the WinRT MediaTranscoder projection.
    ///
    /// Why convert rather than record MP4 directly: MJPEG is what can be written live with certainty
    /// (see MjpegAviWriter), but it has no inter-frame compression at all - every frame is a complete
    /// JPEG - so a short clip runs to tens of megabytes. H.264 encodes mostly the differences between
    /// frames, which on drone footage is typically 20-40x smaller. Doing it as a post-pass keeps the
    /// live capture path exactly as reliable as it was while making the output shareable.
    ///
    /// Nothing here is load-bearing for recording: every failure path leaves the .avi untouched, and
    /// the caller keeps it. Whether Windows can demux this hand-written AVI is reported up front by
    /// PrepareFileTranscodeAsync's CanTranscode rather than discovered halfway through.
    /// </summary>
    public static class Mp4Converter
    {
        /// <summary>Transcodes aviPath to a sibling .mp4 and returns its path and duration. Throws on
        /// any failure - including an output that turns out not to be playable - so the caller can keep
        /// the source file. Never deletes anything itself.</summary>
        public static async Task<(string Path, TimeSpan Duration)> ConvertAsync(string aviPath, int width, int height)
        {
            string mp4Path = Path.ChangeExtension(aviPath, ".mp4");
            string dir = Path.GetDirectoryName(mp4Path) ?? throw new InvalidOperationException("no output directory");

            var source = await StorageFile.GetFileFromPathAsync(aviPath);
            var folder = await StorageFolder.GetFolderFromPathAsync(dir);
            var destination = await folder.CreateFileAsync(Path.GetFileName(mp4Path), CreationCollisionOption.ReplaceExisting);

            // Start from a 1080p preset, then override the frame size to whatever was actually captured
            // so the recording isn't silently rescaled to a preset's dimensions. Bitrate is scaled from
            // the pixel count - roughly 3.7 Mbps at 720p - with a floor so a small capture doesn't end
            // up starved of bits.
            var profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD1080p);
            profile.Video.Width = (uint)Math.Max(2, width);
            profile.Video.Height = (uint)Math.Max(2, height);
            profile.Video.Bitrate = (uint)Math.Max(2_000_000, (long)width * height * 4);

            // The CreateMp4 presets all describe an AAC audio track as well as video, but this AVI is
            // video-only. Leaving the audio track in the profile makes the transcoder build a file
            // around a stream that never receives a single sample, and what lands on disk is a
            // container no player will open - which is exactly the symptom: a plausible-sized .mp4
            // that neither Media Player nor VLC would touch. Dropping it makes the profile match the
            // source.
            profile.Audio = null;

            var transcoder = new MediaTranscoder();
            var prepared = await transcoder.PrepareFileTranscodeAsync(source, destination, profile);
            if (!prepared.CanTranscode)
                throw new InvalidOperationException(prepared.FailureReason.ToString());

            await prepared.TranscodeAsync();

            // Ask the OS to parse back what was just written. A non-zero duration means Windows' own
            // demuxer can read the container and finds real media in it - a far stronger guarantee
            // than "the file has bytes in it", which is what the caller used to decide it was safe to
            // delete the source. If this can't be established, the conversion is treated as failed.
            TimeSpan duration;
            try
            {
                var props = await destination.Properties.GetVideoPropertiesAsync();
                duration = props.Duration;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"output not readable ({ex.Message})");
            }
            if (duration <= TimeSpan.Zero)
                throw new InvalidOperationException("output has no playable duration");

            return (mp4Path, duration);
        }
    }
}
