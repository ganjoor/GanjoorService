using OggVorbisEncoder;
using System;
using System.IO;

namespace RMuseum.Services.Implementation
{
    internal enum PCMSample : int
    {
        EightBit = 1,
        SixteenBit = 2
    }
    /// <summary>
    /// ogg vorbis convertor
    /// </summary>
    internal static class OggVorbisConvertor
    {
        public static byte[] ConvertRawPCMFile(int OutputSampleRate, int OutputChannels, byte[] PCMSamples, PCMSample PCMSampleSize, int PCMSampleRate, int PCMChannels)
        {
            int NumPCMSamples = (PCMSamples.Length / (int)PCMSampleSize / PCMChannels);
            float PCMDuraton = NumPCMSamples / (float)PCMSampleRate;

            int NumOutputSamples = (int)(PCMDuraton * OutputSampleRate);
            //Ensure that samble buffer is aligned to write chunk size
            NumOutputSamples = (NumOutputSamples / WriteBufferSize) * WriteBufferSize;

            float[][] OutSamples = new float[OutputChannels][];

            for (int ch = 0; ch < OutputChannels; ch++)
                OutSamples[ch] = new float[NumOutputSamples];

            for (int sampleNumber = 0; sampleNumber < NumOutputSamples; sampleNumber++)
            {
                float rawSample = 0.0f;

                for (int ch = 0; ch < OutputChannels; ch++)
                {
                    int sampleIndex = (sampleNumber * PCMChannels) * (int)PCMSampleSize;

                    if (ch < PCMChannels) sampleIndex += (ch * (int)PCMSampleSize);

                    switch (PCMSampleSize)
                    {
                        case PCMSample.EightBit:
                            rawSample = ByteToSample(PCMSamples[sampleIndex]);
                            break;
                        case PCMSample.SixteenBit:
                            rawSample = ShortToSample((short)(PCMSamples[sampleIndex + 1] << 8 | PCMSamples[sampleIndex]));
                            break;
                    }

                    OutSamples[ch][sampleNumber] = rawSample;
                }
            }

            return GenerateFile(OutSamples, OutputSampleRate, OutputChannels);
        }

        private static float ByteToSample(short pcmValue)
        {
            return pcmValue / 128f;
        }

        private static float ShortToSample(short pcmValue)
        {
            return pcmValue / 32768f;
        }

        private static byte[] GenerateFile(float[][] FloatSamples, int SampleRate, int Channels)
        {
            using (MemoryStream outputData = new MemoryStream())
            {
                // Stores all the static vorbis bitstream settings
                var info = VorbisInfo.InitVariableBitRate(Channels, SampleRate, 0.5f);

                // set up our packet->stream encoder
                var serial = new Random().Next();
                var oggStream = new OggStream(serial);

                // =========================================================
                // HEADER
                // =========================================================
                // Vorbis streams begin with three headers; the initial header (with
                // most of the codec setup parameters) which is mandated by the Ogg
                // bitstream spec.  The second header holds any comment fields.  The
                // third header holds the bitstream codebook.
                var headerBuilder = new HeaderPacketBuilder();

                var comments = new Comments();
                comments.AddTag("ARTIST", "TEST");

                var infoPacket = headerBuilder.BuildInfoPacket(info);
                var commentsPacket = headerBuilder.BuildCommentsPacket(comments);
                var booksPacket = headerBuilder.BuildBooksPacket(info);

                oggStream.PacketIn(infoPacket);
                oggStream.PacketIn(commentsPacket);
                oggStream.PacketIn(booksPacket);

                // Flush to force audio data onto its own page per the spec
                FlushPages(oggStream, outputData, true);

                // =========================================================
                // BODY (Audio Data)
                // =========================================================
                var processingState = ProcessingState.Create(info);

                processingState.WriteData(FloatSamples, FloatSamples[0].Length);
                for (int readIndex = 0; readIndex <= FloatSamples[0].Length; readIndex += WriteBufferSize)
                {

                    OggPacket packet;
                    while (!oggStream.Finished
                            && processingState.PacketOut(out packet))
                    {
                        oggStream.PacketIn(packet);

                        FlushPages(oggStream, outputData, false);
                    }
                }

                FlushPages(oggStream, outputData, true);

                return outputData.ToArray();
            }
        }

        private static void FlushPages(OggStream oggStream, Stream Output, bool Force)
        {
            OggPage page;
            while (oggStream.PageOut(out page, Force))
            {
                Output.Write(page.Header, 0, page.Header.Length);
                Output.Write(page.Body, 0, page.Body.Length);
            }
        }

        private static int WriteBufferSize = 512;
        private static readonly int[] SampleRates = { 8000, 11025, 16000, 22050, 32000, 44100 };

    }
}
