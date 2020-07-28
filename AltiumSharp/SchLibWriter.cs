﻿using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AltiumSharp.BasicTypes;
using AltiumSharp.Records;
using OpenMcdf;

namespace AltiumSharp
{
    /// <summary>
    /// Schematic library writer.
    /// </summary>
    public sealed class SchLibWriter : SchWriter<SchLib>
    {
        public SchLibWriter() : base()
        {

        }

        protected override void DoWrite()
        {
            WriteFileHeader();
            WriteSectionKeys();

            foreach (var component in Data.Items)
            {
                WriteComponent(component);
            }

            var embeddedImages = GetEmbeddedImages(Data.Items);
            WriteStorageEmbeddedImages(embeddedImages);
        }

        private void WriteSectionKeys()
        {
            // only write section keys for components that need them
            var components = Data.Items.Where(c => GetSectionKeyFromRefName(c.LibReference) != c.LibReference).ToList();
            if (components.Count == 0) return;

            var parameters = new ParameterCollection
            {
                { "KEYCOUNT", components.Count }
            };

            for (int i = 0; i < components.Count; ++i)
            {
                var component = components[i];
                var componentRefName = component.LibReference;
                var sectionKey = GetSectionKeyFromRefName(componentRefName);

                parameters.Add($"LIBREF{i}", componentRefName);
                parameters.Add($"SECTIONKEY{i}", sectionKey);
            }

            Cf.RootStorage.GetOrAddStream("SectionKeys").Write(writer =>
            {
                WriteBlock(writer, w => WriteParameters(w, parameters));
            });
        }

        /// <summary>
        /// Writes the "FileHeader" section which contains the list of components that
        /// exist in the current library file.
        /// </summary>
        /// <returns></returns>
        private void WriteFileHeader()
        {
            Cf.RootStorage.GetOrAddStream("FileHeader").Write(writer =>
            {
                // write header
                var parameters = Data.Header.ExportToParameters();
                WriteBlock(writer, w => WriteParameters(w, parameters));

                // write the binary list of component ref names
                writer.Write(Data.Items.Count);
                foreach (var component in Data.Items)
                {
                    WriteStringBlock(writer, component.LibReference);
                }
            });
        }

        /// <summary>
        /// Writes a component to the adequate section key in the current file.
        /// </summary>
        /// <param name="component">Component to be serialized.</param>
        private void WriteComponent(SchComponent component)
        {
            var resourceName = GetSectionKeyFromRefName(component.LibReference);
            var componentStorage = Cf.RootStorage.GetOrAddStorage(resourceName);

            var pinsWideText = new Dictionary<int, ParameterCollection>();
            var pinsTextData = new Dictionary<int, byte[]>();
            var pinsSymbolLineWidth = new Dictionary<int, ParameterCollection>();

            component.AllPinCount = component.GetPrimitivesOfType<SchPin>().Count();

            componentStorage.GetOrAddStream("Data").Write(writer =>
            {
                WriteComponentPrimitives(writer, component, pinsWideText, pinsTextData, pinsSymbolLineWidth);
            });

            WritePinTextData(componentStorage, pinsTextData);
            WriteComponentExtendedParameters(componentStorage, "PinWideText", pinsWideText);
            WriteComponentExtendedParameters(componentStorage, "PinSymbolLineWidth", pinsSymbolLineWidth);
        }

        private static void WriteComponentPrimitives(BinaryWriter writer, SchComponent component,
            Dictionary<int, ParameterCollection> pinsWideText, Dictionary<int, byte[]> pinsTextData,
            Dictionary<int, ParameterCollection> pinsSymbolLineWidth)
        {
            var index = 0;
            var pinIndex = 0;
            WritePrimitive(writer, component, true, 0, ref index, ref pinIndex, pinsWideText, pinsTextData, pinsSymbolLineWidth);
        }

        /// <summary>
        /// Writes a pin text data for the component at <paramref name="componentStorage"/>.
        /// </summary>
        private static void WritePinTextData(CFStorage componentStorage, Dictionary<int, byte[]> data)
        {
            if (data.Count == 0) return;

            componentStorage.GetOrAddStream("PinTextData").Write(writer =>
            {
                var parameters = new ParameterCollection
                {
                    { "HEADER", "PinTextData" },
                    { "WEIGHT", data.Count }
                };
                WriteBlock(writer, w => WriteParameters(w, parameters));

                foreach (var kv in data)
                {
                    WriteCompressedStorage(writer, kv.Key.ToString(CultureInfo.InvariantCulture), kv.Value);
                }
            });
        }

        /// <summary>
        /// Writes a component extended list of parameter at <paramref name="componentStorage"/>.
        /// </summary>
        /// <param name="componentStorage">Component storage.</param>
        /// <param name="streamName">Name of the stream inside the component storage.</param>
        /// <param name="data">Key, value set of parameters to store.</param>
        private static void WriteComponentExtendedParameters(CFStorage componentStorage, string streamName,
            Dictionary<int, ParameterCollection> data)
        {
            if (data.Count == 0) return;

            componentStorage.GetOrAddStream(streamName).Write(writer =>
            {
                var parameters = new ParameterCollection
                {
                    { "HEADER", streamName },
                    { "WEIGHT", data.Count }
                };
                WriteBlock(writer, w => WriteParameters(w, parameters));

                foreach (var kv in data)
                {
                    WriteCompressedStorage(writer, kv.Key.ToString(CultureInfo.InvariantCulture), ws =>
                        WriteBlock(ws, wb => WriteParameters(wb, kv.Value, true, Encoding.Unicode, false)));
                }
            });
        }
    }
}
