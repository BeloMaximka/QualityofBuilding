using System.IO;
using Vintagestory.API.Common;

namespace ImmersiveBuilding.Source.Extensions;

public static class ByteSerializationExtensions
{
    public static T Read<T>(this BinaryReader reader, IWorldAccessor resolver)
        where T : IByteSerializable, new()
    {
        T result = new();
        result.FromBytes(reader, resolver);
        return result;
    }

    public static T[] ReadArray<T>(this BinaryReader reader, IWorldAccessor resolver)
        where T : IByteSerializable, new()
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
        where T : IByteSerializable
    {
        value.ToBytes(writer);
    }

    public static void WriteArray<T>(this BinaryWriter writer, T[] array)
        where T : IByteSerializable
    {
        writer.Write(array.Length);
        foreach (var item in array)
        {
            item.ToBytes(writer);
        }
    }
}
