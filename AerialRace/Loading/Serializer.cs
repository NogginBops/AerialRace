using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Metadata;
using OpenTK.Mathematics;

namespace AerialRace.Loading
{
    class VersionAttribute : Attribute
    {
        public int Version;

        public VersionAttribute(int version)
        {
            Version = version;
        }
    }

    class Serializer
    {
        public static void Serialize<T>(IndentedTextWriter writer, T instance)
        {
            var type = typeof(T);
            if (type.Attributes.HasFlag(TypeAttributes.Serializable) == false)
                throw new Exception();
            
            var fields = type.GetFields();

            foreach (var field in fields)
            {
                if (field.Attributes.HasFlag(FieldAttributes.NotSerialized))
                    continue;

                //writer.WriteLine($"{field.Name}:");
                //using var scope = writer.Indentation();
                SerializeField(writer, field, instance);
            }

            static void SerializeField(IndentedTextWriter writer, FieldInfo field, object? containingObject)
            {
                writer.Flush();
                if (field.FieldType.Attributes.HasFlag(TypeAttributes.Serializable) == false)
                    throw new Exception();

                if (containingObject == null) writer.WriteLine("null");
                else
                {
                    writer.WriteStartOfLine($"{field.Name}: ");
                    var type = field.FieldType;
                    if (type == typeof(string))
                    {
                        // FIXME: Deal with nullable fields etc
                        string? str = (string?)field.GetValue(containingObject);
                        writer.WriteEndOfLine($"\"{str}\"");
                    }
                    else if (type == typeof(Vector3))
                    {
                        Vector3 value = (Vector3)field.GetValue(containingObject)!;
                        writer.WriteEndOfLine($"v3({value.X}, {value.X}, {value.X})");
                    }
                    else if (type.IsPrimitive)
                    {
                        // FIXME: Maybe do something better?
                        var primitive = field.GetValue(containingObject);
                        writer.WriteEndOfLine($"{primitive}");
                    }
                    else
                    {
                        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                        var fields = type.GetFields(flags);
                        var fObj = field.GetValue(containingObject);
                        if (fObj == null)
                        {
                            writer.WriteEndOfLine("null");
                        }
                        else
                        {
                            writer.WriteEndOfLine("");
                            foreach (var nesetedField in fields)
                            {
                                using var scope = writer.Indentation();
                                SerializeField(writer, nesetedField, fObj);
                            }
                        }
                    }
                }
            }
        }

        public T Deserialize<T>(TextReader reader)
        {
            return default;
        }
    }
}
