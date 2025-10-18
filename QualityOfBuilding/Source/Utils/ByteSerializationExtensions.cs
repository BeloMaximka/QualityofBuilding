using Newtonsoft.Json.Linq;
using System.IO;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;

namespace QualityOfBuilding.Source.Utils;

public static class ByteSerializationExtensions
{
    public static T Read<T>(this BinaryReader reader, IWorldAccessor resolver)
        where T : notnull, IByteSerializable
    {
        T result = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        result.FromBytes(reader, resolver);
        return result;
    }

    public static JToken? ReadNullableJToken(this BinaryReader reader)
    {
        if (reader.ReadBoolean())
        {
            return JToken.Parse(reader.ReadString());
        }

        return default;
    }

    public static string? ReadNullableString(this BinaryReader reader)
    {
        if (reader.ReadBoolean())
        {
            return reader.ReadString();
        }

        return default;
    }

    public static T? ReadNullable<T>(this BinaryReader reader, IWorldAccessor resolver)
        where T : class?, IByteSerializable
    {
        if (reader.ReadBoolean())
        {
            return reader.Read<T>(resolver);
        }

        return default;
    }

    public static T[] ReadArray<T>(this BinaryReader reader, IWorldAccessor resolver)
        where T : notnull, IByteSerializable
    {
        int count = reader.ReadInt32();
        T[] result = new T[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = reader.Read<T>(resolver);
        }
        return result;
    }

    public static void Write<T>(this BinaryWriter writer, T value)
        where T : notnull, IByteSerializable
    {
        value.ToBytes(writer);
    }

    public static void WriteNullable(this BinaryWriter writer, JToken? value)
    {
        if (value is not null)
        {
            writer.Write(true);
            writer.Write(value.ToString());
        }
        else
        {
            writer.Write(false);
        }
    }

    public static void WriteNullable(this BinaryWriter writer, string? value)
    {
        if (value is not null)
        {
            writer.Write(true);
            writer.Write(value);
        }
        else
        {
            writer.Write(false);
        }
    }

    public static void WriteNullable<T>(this BinaryWriter writer, T? value)
        where T : IByteSerializable
    {
        if (value is not null)
        {
            writer.Write(true);
            writer.Write(value);
        }
        else
        {
            writer.Write(false);
        }
    }

    public static void WriteArray<T>(this BinaryWriter writer, T[] array)
        where T : notnull, IByteSerializable
    {
        writer.Write(array.Length);
        foreach (var item in array)
        {
            item.ToBytes(writer);
        }
    }
}
