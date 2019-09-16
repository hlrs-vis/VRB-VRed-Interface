using System;
using System.Collections.Generic;

namespace BuisnessLayer.Base
{
    class SharedStateSerializer
    {
        public enum SharedStateDataType
        {
            UNDEFINED = 0,
            BOOL,   //1
            INT,    //2
            FLOAT,  //3
            STRING, //4
            DOUBLE, //5
            VECTOR, //6
            SET,    //7
            MAP,    //8
            PAIR,    //9
            TRANSFERFUCTION    //10
        }

       public interface ISerializer<T>
        {
            void serialize(ref MessageBuffer mb, T value);
            void deserialize(ref MessageBuffer mb, ref T value);
        }
        public class Serializer<T>
            : ISerializer<T>
            , ISerializer<List<T>>
        {
            public static readonly ISerializer<T> P = Serializer.P as ISerializer<T> ?? new Serializer<T>();
            void ISerializer<T>.deserialize(ref MessageBuffer mb, ref T value)
            {
                throw new NotSupportedException();
            }

            void ISerializer<List<T>>.deserialize(ref MessageBuffer mb, ref List<T> value)
            {
                value.Clear();
                int l = mb.readInt();
                for (int i = 0; i < l; i++)
                {
                    T v = default(T);
                    deserialize(ref mb, ref v);
                    value.Add(v);
                }
            }

            void ISerializer<T>.serialize(ref MessageBuffer mb, T value)
            {
                throw new NotSupportedException();
            }

            void ISerializer<List<T>>.serialize(ref MessageBuffer mb, List<T> value)
            {
                mb.add(value.Count);
                foreach (var item in value)
                {
                    serialize(ref mb, item);
                }
            }
        }

        public class Serializer
            : ISerializer<bool>
            , ISerializer<int>
            , ISerializer<float>
            , ISerializer<double>
            , ISerializer<SessionID>
            , ISerializer<string>
            ,ISerializer<List<float>>
        {
            public static Serializer P = new Serializer();
            void ISerializer<bool>.deserialize(ref MessageBuffer mb, ref bool value)
            {
                value = mb.readBool();
            }

            void ISerializer<int>.deserialize(ref MessageBuffer mb, ref int value)
            {
                value = mb.readInt();
            }

            void ISerializer<float>.deserialize(ref MessageBuffer mb, ref float value)
            {
                value = mb.readFloat();
            }

            void ISerializer<double>.deserialize(ref MessageBuffer mb, ref double value)
            {
                value = mb.readDouble();
            }

            void ISerializer<SessionID>.deserialize(ref MessageBuffer mb, ref SessionID value)
            {
                value.m_owner = mb.readInt();
                value.m_name = mb.readString();
                value.m_isPrivate = mb.readBool();
            }

            void ISerializer<string>.deserialize(ref MessageBuffer mb, ref string value)
            {
                value = mb.readString();
            }

            void ISerializer<List<float>>.deserialize(ref MessageBuffer mb, ref List<float> value)
            {
                value.Clear();
                mb.readInt(); //typeID of List
                int l = mb.readInt();
                for (int i = 0; i < l; i++)
                {
                    float v = mb.readFloat();
                    value.Add(v);
                }
            }

            void ISerializer<bool>.serialize(ref MessageBuffer mb, bool value)
            {
                mb.add(value);
            }

            void ISerializer<int>.serialize(ref MessageBuffer mb, int value)
            {
                mb.add(value);
            }

            void ISerializer<float>.serialize(ref MessageBuffer mb, float value)
            {
                mb.add(value);
            }

            void ISerializer<double>.serialize(ref MessageBuffer mb, double value)
            {
                mb.add(value);
            }

            void ISerializer<SessionID>.serialize(ref MessageBuffer mb, SessionID value)
            {
                mb.add(value.m_owner);
                mb.add(value.m_name);
                mb.add(value.m_isPrivate);
            }

            void ISerializer<string>.serialize(ref MessageBuffer mb, string value)
            {
                mb.add(value);
            }

            void ISerializer<List<float>>.serialize(ref MessageBuffer mb, List<float> value)
            {
                mb.add((int)SharedStateDataType.FLOAT);
                mb.add(value.Count);
                foreach (var item in value)
                {
                    serialize(ref mb, item);
                }
            }
        }
        public static void serialize<T>(ref MessageBuffer mb, T value)
        {
            Serializer<T>.P.serialize(ref mb, value);
        }
        public static void deserialize<T>(ref MessageBuffer mb, ref T value)
        {
            Serializer<T>.P.deserialize(ref mb, ref value);
        }
        public interface ISharedStateType<T>
        {
            SharedStateDataType getSharedStateType(T type);
        }
        public class SharedStateType<T>
            : ISharedStateType<T>
            , ISharedStateType<List<T>>
        {
            public static readonly ISharedStateType<T> p = SharedStateType.p as ISharedStateType<T> ?? new SharedStateType<T>();
            SharedStateDataType ISharedStateType<T>.getSharedStateType(T type)
            {
                return SharedStateDataType.UNDEFINED;
            }

            SharedStateDataType ISharedStateType<List<T>>.getSharedStateType(List<T> type)
            {
                return SharedStateDataType.VECTOR;
            }
        }
        class SharedStateType
            : ISharedStateType<bool>
            , ISharedStateType<string>
            , ISharedStateType<float>
            , ISharedStateType<double>
            , ISharedStateType<char>
            , ISharedStateType<List<float>>
        {
            public static SharedStateType p = new SharedStateType();
            SharedStateDataType ISharedStateType<bool>.getSharedStateType(bool type)
            {
                return SharedStateDataType.BOOL;
            }
            SharedStateDataType ISharedStateType<string>.getSharedStateType(string type)
            {
                return SharedStateDataType.STRING;
            }
            SharedStateDataType ISharedStateType<float>.getSharedStateType(float type)
            {
                return SharedStateDataType.FLOAT;
            }
            SharedStateDataType ISharedStateType<double>.getSharedStateType(double type)
            {
                return SharedStateDataType.DOUBLE;
            }
            SharedStateDataType ISharedStateType<char>.getSharedStateType(char type)
            {
                return SharedStateDataType.STRING;
            }

            SharedStateDataType ISharedStateType<List<float>>.getSharedStateType(List<float> type)
            {
                return SharedStateDataType.VECTOR;
            }
        }
        static SharedStateDataType getSharedStateType<T>(T type)
        {
            return SharedStateType<T>.p.getSharedStateType(type);
        }
        public static void serializeWithType<T>(ref MessageBuffer mb, T value)
        {
            int typeId = (int)getSharedStateType(value);
            mb.add(typeId);
            serialize(ref mb, value);
        }
        public static void deserializeWithType<T>(ref MessageBuffer mb, ref T value)
        {
            int type = mb.readInt(); //typeInfo
            deserialize(ref mb, ref value);
        }
        



    }
}
