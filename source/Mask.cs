#define SIMPLE_WEB_INFO_LOG

namespace Mirror.SimpleWeb
{
    internal struct Mask
    {
        byte a;
        byte b;
        byte c;
        byte d;

        public Mask(byte[] buffer, int offset)
        {
            a = buffer[offset];
            b = buffer[offset + 1];
            c = buffer[offset + 2];
            d = buffer[offset + 3];
        }

        public byte getMaskByte(int index)
        {
            switch (index % 4)
            {
                default:
                case 0: return a;
                case 1: return b;
                case 2: return c;
                case 3: return d;
            }

        }
    }
}
