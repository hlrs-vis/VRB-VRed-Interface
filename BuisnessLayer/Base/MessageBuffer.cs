using BuisnessLayer.Manager;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace BuisnessLayer.Base
{
    [Serializable]
    public class MessageBuffer
    {
        private static readonly log4net.ILog log = LogHelper.GetLogger(); //log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public enum Types
        {
            TbBool = 7,
            TbInt64,
            TbInt32,
            TbFloat,
            TbDouble,
            TbString,
            TbChar,
            TbTB,
            TbBinary,
        };

        public byte[] buf;
        public int currentPos;
        private int len = 0;
        bool typeInfo = false;
        System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();

        public MessageBuffer()
        {
            currentPos = 1;
            buf = new byte[254];
            if (typeInfo)
                buf[0] = 1; // no type info in TokenBuffer
            else
                buf[0] = 0; // no type info in TokenBuffer
        }

        public MessageBuffer(byte[] b)
        {
            currentPos = 1;
            buf = new byte[b.Length];
            if (b[0] == 0)
                typeInfo = false;
            else
                typeInfo = true;
            b.CopyTo(buf, 0);
        }
        private void readTypeInfo(Types expectedType)
        {
            if (typeInfo)
            {
                if (buf[currentPos] != (byte)expectedType)
                {
                    // Todo print error message
                    System.Threading.Thread.Sleep(5000);
                }
                currentPos++;
            }
        }
        private void addTypeInfo(Types followingType)
        {
            if (typeInfo)
            {
                incsize(1);
                buf[currentPos] = (byte)followingType;
                currentPos++;
            }
        }
        private void incsize(int s)
        {
            if (buf.Length < currentPos + s)
            {
                byte[] newbuf = new byte[buf.Length + s + 1024];
                buf.CopyTo(newbuf, 0);
                buf = newbuf;
            }
        }

        public byte[] readByteArray(int length)
        {
            byte[] result = new byte[] { };

            readTypeInfo(Types.TbBinary);
            for (int i = currentPos; i < length; i++)
            {
                result = addByteToArray(result, buf[i]);
                currentPos += 1;
            }

            Array.Reverse(result, 0, result.Length);
            return result;
        }

        public byte[] addByteToArray(byte[] bArray, byte newByte)
        {
            byte[] newArray = new byte[bArray.Length + 1];
            bArray.CopyTo(newArray, 1);
            newArray[0] = newByte;
            return newArray;
        }

        public bool readBool()
        {
            readTypeInfo(Types.TbBool);
            readTypeInfo(Types.TbChar);
            bool b = (buf[currentPos] == 1);
            currentPos += 1;
            return b;
        }
        public byte readByte()
        {
            readTypeInfo(Types.TbChar);
            byte b = buf[currentPos];
            currentPos += 1;
            return b;
        }
        public int readInt()
        {
            readTypeInfo(Types.TbInt32);
            int i = BitConverter.ToInt32(buf, currentPos);
            currentPos += 4;
            return i;
        }
        public float readFloat()
        {
            readTypeInfo(Types.TbFloat);
            float i = BitConverter.ToSingle(buf, currentPos);
            currentPos += 4;
            return i;
        }
        public double readDouble()
        {
            readTypeInfo(Types.TbDouble);
            double i = BitConverter.ToDouble(buf, currentPos);
            currentPos += 8;
            return i;
        }

        public MessageBuffer readMessageBuffer()
        {
            readTypeInfo(Types.TbTB);
            int l = readInt();
            MessageBuffer newMb = new MessageBuffer();
            readTypeInfo(Types.TbBinary);
            newMb.buf = new byte[l];
            if (l > buf.Length - currentPos)
            {
                throw new IndexOutOfRangeException();
            }
            for (int i = 0; i < l; i++)
            {
                newMb.buf[i] = buf[currentPos + i];
            }
            if (newMb.buf[0] == 1)
            {
                newMb.typeInfo = true;
            }
            return newMb;
        }

        public string readString()
        {
            readTypeInfo(Types.TbString);
            string str;
            int len = 0;
            while (buf[currentPos + len] != '\0')
            {
                len++;
            }
            str = enc.GetString(buf, currentPos, len);
            currentPos += len + 1;
            return str;
        }

        //public MessageBuffer readMessageBuffer(byte[] objectInBytes)
        //{
        //    readTypeInfo(Types.TbTB);
  
        //    MessageBuffer mb = new MessageBuffer();
        //    //mb.buf = readByteArray(objectInBytes.Length);

        //    using (MemoryStream ms = new MemoryStream(objectInBytes))
        //    {
        //        IFormatter br = new BinaryFormatter();
        //        mb = (br.Deserialize(ms) as MessageBuffer);
        //    }

        //    return mb;
        //}

        public void add(MessageBuffer mb2)
        {
            addTypeInfo(Types.TbTB);
            add(buf.Length);
            add(mb2.buf);
        }

        // Convert an object to a byte array
        public byte[] ObjectToByteArray(MessageBuffer obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public Object ByteArrayToObject(byte[] arrBytes)
        {
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(arrBytes, 0, arrBytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = binForm.Deserialize(memStream);
                return obj;
            }
        }
        public void add(bool num)
		{
            addTypeInfo(Types.TbBool);
            addTypeInfo(Types.TbChar);
			incsize(1);
			if (num)
				buf[currentPos] = 1;
			else
				buf[currentPos] = 0;
			currentPos += 1;
		}
		public void add(byte b)
		{
			addTypeInfo(Types.TbChar);
			incsize(1);
			buf[currentPos] = b;
			currentPos += 1;
		}
		public void add(int num)
		{
			addTypeInfo(Types.TbInt32);
			incsize(4);

			BitConverter.GetBytes(num).CopyTo(buf, currentPos);
			currentPos += 4;
		}

		public void add(float num)
		{
			addTypeInfo(Types.TbFloat);
			incsize(4);
			BitConverter.GetBytes(num).CopyTo(buf, currentPos);
			currentPos += 4;
		}
		public void add(double num)
		{
			addTypeInfo(Types.TbDouble);
			incsize(8);
			BitConverter.GetBytes(num).CopyTo(buf, currentPos);
			currentPos += 8;
		}
		public void add(string str)
		{
			addTypeInfo(Types.TbDouble);
			if (str != null)
			{
				byte[] ba = enc.GetBytes(str);
				incsize(ba.Length + 1);
				for (int i = 0; i < ba.Length; i++)
				{
					buf.SetValue(ba[i], currentPos);
					currentPos++;
				}
				buf[currentPos] = 0;
				currentPos++;
			}
			else
			{
				incsize(1);
				buf[currentPos] = 0;
				currentPos++;
			}
		}

		public void add(byte[] ba)
		{
			addTypeInfo(Types.TbBinary);
			incsize(ba.Length);
			for (int i = 0; i < ba.Length; i++)
			{
				buf.SetValue(ba[i], currentPos);
				currentPos++;
			}
		}
	}
}
