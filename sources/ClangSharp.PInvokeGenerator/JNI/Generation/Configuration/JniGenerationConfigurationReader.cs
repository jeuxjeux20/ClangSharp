// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.IO;
using System.Xml.Serialization;

namespace ClangSharp.JNI.Generation.Configuration;

public static class JniGenerationConfigurationReader
{
    public static JniGenerationConfiguration Read(string path)
    {
        using var fileStream = new FileStream(path, FileMode.Open);
        var serializer = new XmlSerializer(typeof(JniGenerationConfiguration));
        return (JniGenerationConfiguration) serializer.Deserialize(fileStream);
    }
}
