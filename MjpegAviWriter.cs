using System.Drawing.Imaging;

namespace HS175D
{
    /// <summary>
    /// Minimal Motion-JPEG AVI writer - each frame is a standalone JPEG inside a plain RIFF/AVI
    /// container, with an idx1 index appended at close.
    ///
    /// Written by hand rather than pulled from a library because every practical alternative brings a
    /// dependency this project would rather not carry: ffmpeg means shipping/locating an executable,
    /// and Media Foundation or VFW means COM interop and a codec negotiation that can fail on exactly
    /// the machine you need it to work on. MJPEG is the one format that can be emitted correctly with
    /// nothing but a FileStream and System.Drawing's JPEG encoder, and it plays in VLC, MPC and any
    /// NLE without a codec install. It also happens to be what the drone's own camera streams, so it
    /// isn't an odd choice for this footage.
    ///
    /// Every frame is a keyframe, so the file stays usable even if the process dies mid-recording -
    /// the header and index will be missing, but the JPEG data is all there and recoverable.
    ///
    /// Thread-safe: AddFrame is called from the capture loop while Dispose comes from the UI thread.
    /// </summary>
    public sealed class MjpegAviWriter : IDisposable
    {
        readonly object _gate = new();
        readonly FileStream _fs;
        readonly BinaryWriter _w;
        readonly int _width, _height;
        readonly List<(uint Offset, uint Length)> _index = new();
        readonly ImageCodecInfo _jpegCodec;
        readonly EncoderParameters _jpegParams;
        readonly DateTime _startedUtc = DateTime.UtcNow;

        long _riffSizePos, _avihMicroPos, _avihFramesPos, _strhRatePos, _strhLengthPos, _moviSizePos, _moviFourCcPos;
        int _frames;
        bool _closed;

        public string FilePath { get; }
        public int FrameCount { get { lock (_gate) return _frames; } }
        public TimeSpan Elapsed => DateTime.UtcNow - _startedUtc;

        public MjpegAviWriter(string path, int width, int height, int jpegQuality = 85)
        {
            FilePath = path;
            _width = width;
            _height = height;
            _jpegCodec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            _jpegParams = new EncoderParameters(1);
            _jpegParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)jpegQuality);
            _fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 1 << 16);
            _w = new BinaryWriter(_fs);
            WriteHeaders();
        }

        void Fcc(string s) { foreach (char c in s) _w.Write((byte)c); }

        /// <summary>Backfills a RIFF chunk's length field once its contents are known.</summary>
        void PatchSize(long sizePos)
        {
            long end = _fs.Position;
            _fs.Position = sizePos;
            _w.Write((int)(end - sizePos - 4));
            _fs.Position = end;
        }

        void WriteHeaders()
        {
            Fcc("RIFF"); _riffSizePos = _fs.Position; _w.Write(0); Fcc("AVI ");

            Fcc("LIST"); long hdrlSizePos = _fs.Position; _w.Write(0); Fcc("hdrl");

            Fcc("avih"); _w.Write(56);
            _avihMicroPos = _fs.Position; _w.Write(33333);       // dwMicroSecPerFrame, patched at close
            _w.Write(0);                                         // dwMaxBytesPerSec
            _w.Write(0);                                         // dwPaddingGranularity
            _w.Write(0x10);                                      // dwFlags = AVIF_HASINDEX
            _avihFramesPos = _fs.Position; _w.Write(0);          // dwTotalFrames, patched
            _w.Write(0);                                         // dwInitialFrames
            _w.Write(1);                                         // dwStreams
            _w.Write(0);                                         // dwSuggestedBufferSize
            _w.Write(_width);
            _w.Write(_height);
            _w.Write(0); _w.Write(0); _w.Write(0); _w.Write(0);  // dwReserved[4]

            Fcc("LIST"); long strlSizePos = _fs.Position; _w.Write(0); Fcc("strl");

            Fcc("strh"); _w.Write(56);
            Fcc("vids"); Fcc("MJPG");
            _w.Write(0);                                         // dwFlags
            _w.Write((short)0);                                  // wPriority
            _w.Write((short)0);                                  // wLanguage
            _w.Write(0);                                         // dwInitialFrames
            _w.Write(1);                                         // dwScale
            _strhRatePos = _fs.Position; _w.Write(30);           // dwRate, patched
            _w.Write(0);                                         // dwStart
            _strhLengthPos = _fs.Position; _w.Write(0);          // dwLength, patched
            _w.Write(0);                                         // dwSuggestedBufferSize
            _w.Write(-1);                                        // dwQuality = default
            _w.Write(0);                                         // dwSampleSize
            _w.Write((short)0); _w.Write((short)0); _w.Write((short)_width); _w.Write((short)_height);

            Fcc("strf"); _w.Write(40);                           // BITMAPINFOHEADER
            _w.Write(40);
            _w.Write(_width);
            _w.Write(_height);
            _w.Write((short)1);                                  // biPlanes
            _w.Write((short)24);                                 // biBitCount
            Fcc("MJPG");                                         // biCompression
            _w.Write(_width * _height * 3);                      // biSizeImage
            _w.Write(0); _w.Write(0); _w.Write(0); _w.Write(0);

            PatchSize(strlSizePos);
            PatchSize(hdrlSizePos);

            Fcc("LIST"); _moviSizePos = _fs.Position; _w.Write(0);
            _moviFourCcPos = _fs.Position; Fcc("movi");
        }

        public void AddFrame(Bitmap bmp)
        {
            lock (_gate)
            {
                if (_closed) return;
                using var ms = new MemoryStream();
                // AVI has one fixed frame size for the whole file, but the window can be resized
                // mid-recording and window capture will start handing back a different size. Rescaling
                // keeps the recording valid; the alternative (dropping those frames) would silently
                // stop recording the moment the operator touched the window.
                if (bmp.Width != _width || bmp.Height != _height)
                {
                    using var scaled = new Bitmap(_width, _height);
                    using (var g = Graphics.FromImage(scaled))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                        g.DrawImage(bmp, 0, 0, _width, _height);
                    }
                    scaled.Save(ms, _jpegCodec, _jpegParams);
                }
                else bmp.Save(ms, _jpegCodec, _jpegParams);

                byte[] data = ms.GetBuffer();
                int len = (int)ms.Length;

                long chunkPos = _fs.Position;
                Fcc("00dc"); _w.Write(len);
                _fs.Write(data, 0, len);
                if ((len & 1) != 0) _w.Write((byte)0);   // RIFF chunks are word-aligned

                // idx1 offsets are relative to the 'movi' FourCC itself, which puts the first frame at 4.
                _index.Add(((uint)(chunkPos - _moviFourCcPos), (uint)len));
                _frames++;
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_closed) return;
                _closed = true;
                try
                {
                    PatchSize(_moviSizePos);   // must happen before idx1 starts

                    Fcc("idx1"); _w.Write(_index.Count * 16);
                    foreach (var (offset, length) in _index)
                    {
                        Fcc("00dc");
                        _w.Write(0x10);        // AVIIF_KEYFRAME - true of every MJPEG frame
                        _w.Write(offset);
                        _w.Write(length);
                    }

                    // Frame rate is MEASURED, not assumed. The capture loop's real rate depends on the
                    // poll delay, the capture method and how loaded the machine is, so writing a
                    // hardcoded 30 would play the footage back at the wrong speed - which matters when
                    // the point of the recording is judging how the aircraft actually behaved.
                    double seconds = Math.Max(0.001, (DateTime.UtcNow - _startedUtc).TotalSeconds);
                    int fps = Math.Clamp((int)Math.Round(_frames / seconds), 1, 240);
                    _fs.Position = _avihMicroPos; _w.Write((int)(1_000_000.0 / fps));
                    _fs.Position = _avihFramesPos; _w.Write(_frames);
                    _fs.Position = _strhRatePos; _w.Write(fps);
                    _fs.Position = _strhLengthPos; _w.Write(_frames);

                    _fs.Seek(0, SeekOrigin.End);
                    PatchSize(_riffSizePos);
                }
                catch { /* a half-written index still leaves recoverable JPEG data on disk */ }
                finally
                {
                    _w.Dispose();
                    _fs.Dispose();
                    _jpegParams.Dispose();
                }
            }
        }
    }
}
