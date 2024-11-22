namespace Five9SpeechClient.Helpers
{
    public struct WavHeaderReader
    {
        private readonly ReadOnlyMemory<byte> _header;

        public WavHeaderReader(ReadOnlyMemory<byte> header)
        {
            if(header.Length != 44) 
                throw new ArgumentException("WAV header must be 44 bytes long.");
            _header = header;
        }

        public int GetSampleRate()
        {
            return BitConverter.ToInt32(_header.Span.Slice(24, 4));
        }

        public int GetBitsPerSample()
        {
            return BitConverter.ToInt16(_header.Span.Slice(34, 2));
        }

        public int GetChannels()
        {
            return BitConverter.ToInt16(_header.Span.Slice(22, 2));
        }

        public int GetAudioFormat()
        {
            return BitConverter.ToInt16(_header.Span.Slice(20, 2));
        }

        public int GetByteRate()
        {
            return BitConverter.ToInt32(_header.Span.Slice(28, 4));
        }

        public int GetBlockAlign()
        {
            return BitConverter.ToInt16(_header.Span.Slice(32, 2));
        }
    }
}
