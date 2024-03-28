//https://github.com/xamarin/mobile-samples/blob/master/EmbeddedResources/SharedLib/ResourceLoader.cs

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SharpGraphics.Utils
{
    /// <summary>
    /// Utility class that can be used to find and load embedded resources into memory.
    /// </summary>
    public static class ResourceLoader
    {

        /// <summary>
        /// Attempts to find and return the given resource from within the calling assembly.
        /// </summary>
        /// <returns>The embedded resource as a stream.</returns>
        /// <param name="resourceFileName">Resource file name.</param>
        public static Stream GetEmbeddedResourceStream(string resourceFileName)
        	=> GetEmbeddedResourceStream (Assembly.GetCallingAssembly(), resourceFileName);
        
        /// <summary>
        /// Attempts to find and return the given resource from within the calling assembly.
        /// </summary>
        /// <returns>The embedded resource as a byte array.</returns>
        /// <param name="resourceFileName">Resource file name.</param>
        public static byte[] GetEmbeddedResourceBytes (string resourceFileName)
        	=> GetEmbeddedResourceBytes (Assembly.GetCallingAssembly(), resourceFileName);
        
        /// <summary>
        /// Attempts to find and return the given resource from within the calling assembly.
        /// </summary>
        /// <returns>The embedded resource as a string.</returns>
        /// <param name="resourceFileName">Resource file name.</param>
        public static string GetEmbeddedResourceString (string resourceFileName)
            => GetEmbeddedResourceString(Assembly.GetCallingAssembly(), resourceFileName);
        public static bool IsEmbeddedResourceExists(string resourceFileName)
            => IsEmbeddedResourceExists(Assembly.GetCallingAssembly(), resourceFileName);

        /// <summary>
        /// Attempts to find and return the given resource from within the specified assembly.
        /// </summary>
        /// <returns>The embedded resource stream.</returns>
        /// <param name="assembly">Assembly.</param>
        /// <param name="resourceFileName">Resource file name.</param>
        public static Stream GetEmbeddedResourceStream(this Assembly assembly, string resourceFileName)
        {
            string? resourcePath = assembly.GetManifestResourceNames()
                .Where(x => x.EndsWith(resourceFileName, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(resourcePath))
                throw new Exception($"Resource {resourceFileName} not found.");

            Stream? result = assembly.GetManifestResourceStream(resourcePath);
            if (result == null)
                throw new Exception($"Resource {resourceFileName} could't be loaded.");
            else return result;
        }

        public static bool TryGetEmbeddedResourceStream(this Assembly assembly, string resourceFileName, [NotNullWhen(returnValue: true)] out Stream? stream)
        {
            string? resourcePath = assembly.GetManifestResourceNames()
                .Where(x => x.EndsWith(resourceFileName, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                stream = null;
                return false;
            }

            stream = assembly.GetManifestResourceStream(resourcePath);
            return stream != null;
        }



        public static bool IsEmbeddedResourceExists(this Assembly assembly, string resourceFileName)
            => assembly.GetManifestResourceNames().Any(x => x.EndsWith(resourceFileName, StringComparison.CurrentCultureIgnoreCase));

        /// <summary>
        /// Attempts to find and return the given resource from within the specified assembly.
        /// </summary>
        /// <returns>The embedded resource as a byte array.</returns>
        /// <param name="assembly">Assembly.</param>
        /// <param name="resourceFileName">Resource file name.</param>
        public static byte[] GetEmbeddedResourceBytes(this Assembly assembly, string resourceFileName)
        {
            using (Stream stream = GetEmbeddedResourceStream(assembly, resourceFileName))
            {
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                return data;
            }
        }
        public static bool TryGetEmbeddedResourceBytes(this Assembly assembly, string resourceFileName, [NotNullWhen(returnValue: true)] out byte[]? result)
        {
            if (TryGetEmbeddedResourceStream(assembly, resourceFileName, out Stream? stream))
                using (stream)
                {
                    result = new byte[stream.Length];
                    stream.Read(result, 0, result.Length);
                    return true;
                }

            result = null;
            return false;
        }


        /// <summary>
        /// Attempts to find and return the given resource from within the specified assembly.
        /// </summary>
        /// <returns>The embedded resource as a string.</returns>
        /// <param name="assembly">Assembly.</param>
        /// <param name="resourceFileName">Resource file name.</param>
        public static string GetEmbeddedResourceString(this Assembly assembly, string resourceFileName)
        {
            using (Stream stream = GetEmbeddedResourceStream(assembly, resourceFileName))
            using (StreamReader streamReader = new StreamReader(stream))
                return streamReader.ReadToEnd();
        }
        public static bool TryGetEmbeddedResourceString(this Assembly assembly, string resourceFileName, [NotNullWhen(returnValue: true)] out string? result)
        {
            if (TryGetEmbeddedResourceStream(assembly, resourceFileName, out Stream? stream))
                using (stream)
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    result = streamReader.ReadToEnd();
                    return true;
                }

            result = null;
            return false;
        }
    }
}
