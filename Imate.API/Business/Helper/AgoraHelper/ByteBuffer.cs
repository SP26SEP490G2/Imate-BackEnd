using System;

namespace AgoraIO.Media
{
    /// <summary>
        /// ????????Byte????Push???Pop??
        /// ????????1024,???????
        /// ??????????MAX_LENGTH??
        /// 
        /// ?:??????,??????????,????
        /// ???Pop????????????,???0??.
        /// 
        /// @Author: Red_angelX
        /// </summary>
    public class ByteBuffer
    {
        //???????
        private const int MAX_LENGTH = 1024;

        //?????????
        private byte[] TEMP_BYTE_ARRAY = new byte[MAX_LENGTH];

        //??????
        private int CURRENT_LENGTH = 0;

        //??Pop????
        private int CURRENT_POSITION = 0;

        //??????
        private byte[] RETURN_ARRAY;

        /// <summary>
        /// ??????
        /// </summary>
        public ByteBuffer()
        {
            this.Initialize();
        }

        /// <summary>
        /// ???????,???Byte?????
        /// </summary>
        /// <param name="bytes">????ByteBuffer???</param>
        public ByteBuffer(byte[] bytes)
        {
            this.Initialize();
            this.PushByteArray(bytes);
        }


        /// <summary>
                /// ????ByteBuffer???
                /// </summary>
        public int Length
        {
            get
            {
                return CURRENT_LENGTH;
            }
        }

        /// <summary>
        /// ??/??????????
        /// </summary>
        public int Position
        {
            get
            {
                return CURRENT_POSITION;
            }
            set
            {
                CURRENT_POSITION = value;
            }
        }

        /// <summary>
        /// ??ByteBuffer??????
        /// ?????? [MAXSIZE]
        /// </summary>
        /// <returns>Byte[]</returns>
        public byte[] ToByteArray()
        {
            //????
            RETURN_ARRAY = new byte[CURRENT_LENGTH];
            //????
            Array.Copy(TEMP_BYTE_ARRAY, 0, RETURN_ARRAY, 0, CURRENT_LENGTH);
            return RETURN_ARRAY;
        }

        /// <summary>
        /// ???ByteBuffer??????,???????????
        /// </summary>
        public void Initialize()
        {
            TEMP_BYTE_ARRAY.Initialize();
            CURRENT_LENGTH = 0;
            CURRENT_POSITION = 0;
        }

        /// <summary>
        /// ?ByteBuffer??????
        /// </summary>
        /// <param name="by">????</param>
        public void PushByte(byte by)
        {
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = by;
        }

        /// <summary>
        /// ?ByteBuffer????
        /// </summary>
        /// <param name="ByteArray">??</param>
        public void PushByteArray(byte[] ByteArray)
        {
            //???CopyTo????
            ByteArray.CopyTo(TEMP_BYTE_ARRAY, CURRENT_LENGTH);
            //????
            CURRENT_LENGTH += ByteArray.Length;
        }

        /// <summary>
        /// ?ByteBuffer??????Short
        /// </summary>
        /// <param name="Num">2??Short</param>
        public void PushUInt16(UInt16 Num)
        {
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = (byte)((Num & 0x00ff) & 0xff);
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = (byte)(((Num & 0xff00) >> 8) & 0xff);
        }

        /// <summary>
        /// ?ByteBuffer??????Int?
        /// </summary>
        /// <param name="Num">4??UInt32</param>
        public void PushInt(UInt32 Num)
        {
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = (byte)((Num & 0x000000ff) & 0xff);
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = (byte)(((Num & 0x0000ff00) >> 8) & 0xff);
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = (byte)(((Num & 0x00ff0000) >> 16) & 0xff);
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = (byte)(((Num & 0xff000000) >> 24) & 0xff);
        }

        /// <summary>
        /// ?ByteBuffer????Long?
        /// </summary>
        /// <param name="Num">4??Long</param>
        public void PushLong(long Num)
        {
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = (byte)((Num & 0x000000ff) & 0xff);
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = (byte)(((Num & 0x0000ff00) >> 8) & 0xff);
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = (byte)(((Num & 0x00ff0000) >> 16) & 0xff);
            TEMP_BYTE_ARRAY[CURRENT_LENGTH++] = (byte)(((Num & 0xff000000) >> 24) & 0xff);
        }

        /// <summary>
        /// ?ByteBuffer?????????Byte,?????
        /// </summary>
        /// <returns>1??Byte</returns>
        public byte PopByte()
        {
            byte ret = TEMP_BYTE_ARRAY[CURRENT_POSITION++];
            return ret;
        }

        /// <summary>
        /// ?ByteBuffer?????????Short,?????
        /// </summary>
        /// <returns>2??Short</returns>
        public UInt16 PopUInt16()
        {
            //??
            if (CURRENT_POSITION + 1 >= CURRENT_LENGTH)
            {
                return 0;
            }
            //UInt16 ret = (UInt16)(TEMP_BYTE_ARRAY[CURRENT_POSITION] << 8 | TEMP_BYTE_ARRAY[CURRENT_POSITION + 1]);
            UInt16 ret = (UInt16)(TEMP_BYTE_ARRAY[CURRENT_POSITION] | TEMP_BYTE_ARRAY[CURRENT_POSITION + 1] << 8);
            CURRENT_POSITION += 2;
            return ret;
        }

        /// <summary>
        /// ?ByteBuffer?????????uint,???4?
        /// </summary>
        /// <returns>4??UInt</returns>
        public uint PopUInt()
        {
            if (CURRENT_POSITION + 3 >= CURRENT_LENGTH)
                return 0;
            uint ret = (uint)(TEMP_BYTE_ARRAY[CURRENT_POSITION] | TEMP_BYTE_ARRAY[CURRENT_POSITION + 1] << 8 | TEMP_BYTE_ARRAY[CURRENT_POSITION + 2] << 16 | TEMP_BYTE_ARRAY[CURRENT_POSITION + 3] << 24);
            CURRENT_POSITION += 4;
            return ret;
        }

        /// <summary>
        /// ?ByteBuffer?????????long,???4?
        /// </summary>
        /// <returns>4??Long</returns>
        public long PopLong()
        {
            if (CURRENT_POSITION + 3 >= CURRENT_LENGTH)
                return 0;
            long ret = (long)(TEMP_BYTE_ARRAY[CURRENT_POSITION] << 24 | TEMP_BYTE_ARRAY[CURRENT_POSITION + 1] << 16 | TEMP_BYTE_ARRAY[CURRENT_POSITION + 2] << 8 | TEMP_BYTE_ARRAY[CURRENT_POSITION + 3]);
            CURRENT_POSITION += 4;
            return ret;
        }

        /// <summary>
        /// ?ByteBuffer??????????Length?Byte??,??Length?
        /// </summary>
        /// <param name="Length">????</param>
        /// <returns>Length???byte??</returns>
        public byte[] PopByteArray(int Length)
        {
            //??
            if (CURRENT_POSITION + Length > CURRENT_LENGTH)
            {
                return new byte[0];
            }
            byte[] ret = new byte[Length];
            Array.Copy(TEMP_BYTE_ARRAY, CURRENT_POSITION, ret, 0, Length);
            //????
            CURRENT_POSITION += Length;
            return ret;
        }

        public byte[] PopByteArray2(int Length)
        {
            //??
            if (CURRENT_POSITION <= Length)
            {
                return new byte[0];
            }
            byte[] ret = new byte[Length];
            Array.Copy(TEMP_BYTE_ARRAY, CURRENT_POSITION - Length, ret, 0, Length);
            //????
            CURRENT_POSITION -= Length;
            return ret;
        }

    }
}
