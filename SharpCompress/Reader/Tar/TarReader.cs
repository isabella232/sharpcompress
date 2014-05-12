using System;
using System.Collections.Generic;
using System.IO;
#if GZIP
using SharpCompress.Archive.GZip;
#endif
using SharpCompress.Archive.Tar;
using SharpCompress.Common;
using SharpCompress.Common.Tar;
#if BZIP2
using SharpCompress.Compressor.BZip2;
#endif
#if BZIP2
using SharpCompress.Compressor.Deflate;
#endif
using SharpCompress.IO;

namespace SharpCompress.Reader.Tar
{
    public class TarReader : AbstractReader<TarEntry, TarVolume>
    {
        private readonly TarVolume volume;
        private readonly CompressionType compressionType;

        internal TarReader(Stream stream, CompressionType compressionType,
                           Options options)
            : base(options, ArchiveType.Tar)
        {
            this.compressionType = compressionType;
            volume = new TarVolume(stream, options);
        }

        public override TarVolume Volume
        {
            get { return volume; }
        }

        internal override Stream RequestInitialStream()
        {
            var stream = base.RequestInitialStream();
            switch (compressionType)
            {
#if BZIP2
                case CompressionType.BZip2:
                    {
                        return new BZip2Stream(stream, CompressionMode.Decompress, false);
                    }
#endif
#if GZIP
                case CompressionType.GZip:
                    {
                        return new GZipStream(stream, CompressionMode.Decompress);
                    }
#endif
                case CompressionType.None:
                    {
                        return stream;
                    }
                default:
                    {
                        throw new NotSupportedException("Invalid compression type: " + compressionType);
                    }
            }
        }

        #region Open

        /// <summary>
        /// Opens a TarReader for Non-seeking usage with a single volume
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static TarReader Open(Stream stream, Options options = Options.KeepStreamsOpen)
        {
            stream.CheckNotNull("stream");

            RewindableStream rewindableStream = new RewindableStream(stream);
            rewindableStream.StartRecording();
#if GZIP
            if (GZipArchive.IsGZipFile(rewindableStream))
            {
                rewindableStream.Rewind(false);
                GZipStream testStream = new GZipStream(rewindableStream, CompressionMode.Decompress);
                if (TarArchive.IsTarFile(testStream))
                {
                    rewindableStream.Rewind(true);
                    return new TarReader(rewindableStream, CompressionType.GZip, options);
                }
                throw new InvalidFormatException("Not a tar file.");
            }
#endif

#if BZIP2
            rewindableStream.Rewind(false);
            if (BZip2Stream.IsBZip2(rewindableStream))
            {
                rewindableStream.Rewind(false);
                BZip2Stream testStream = new BZip2Stream(rewindableStream, CompressionMode.Decompress, false);
                if (TarArchive.IsTarFile(testStream))
                {
                    rewindableStream.Rewind(true);
                    return new TarReader(rewindableStream, CompressionType.BZip2, options);
                }
                throw new InvalidFormatException("Not a tar file.");
            }
            rewindableStream.Rewind(true);
            return new TarReader(rewindableStream, CompressionType.None, options);
        }
#endif

        #endregion

        internal override IEnumerable<TarEntry> GetEntries(Stream stream)
        {
            return TarEntry.GetEntries(StreamingMode.Streaming, stream, compressionType);
        }
    }
}